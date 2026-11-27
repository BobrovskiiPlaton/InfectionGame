
using InfectionGame.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCWanderSystem))]
[UpdateBefore(typeof(NPCObstacleAvoidanceSystem))]
public partial struct NPCFleeSystem : ISystem
{
    private const int DirectionCount = 16;
    private const float PredictionTime = 1.5f;
    private const float RecalculationDistanceSq = 4f;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (movement, transform, fleeState, targets, snapshots) in
            SystemAPI.Query<
                RefRW<NPCMovementComponent>,
                RefRO<LocalTransform>,
                RefRW<NPCFleeState>,
                DynamicBuffer<DetectedTarget>,
                DynamicBuffer<NPCFleeThreatSnapshot>>()
                .WithAll<NPCComponent>())
        {
            float3 position = transform.ValueRO.Position;

            int visibleCount = 0;
            bool recalculate = false;

            for (int i = 0; i < targets.Length; i++)
            {
                DetectedTarget enemy = targets[i];

                if (!IsVisibleEnemy(enemy))
                    continue;

                visibleCount++;

                float3 relativePosition =
                    enemy.LastKnownPosition - position;

                relativePosition.y = 0f;

                bool found = false;

                for (int j = 0; j < snapshots.Length; j++)
                {
                    if (snapshots[j].Target != enemy.Target)
                        continue;

                    found = true;

                    if (math.lengthsq(
                        relativePosition -
                        snapshots[j].RelativePosition
                    ) >= RecalculationDistanceSq)
                    {
                        recalculate = true;
                    }

                    break;
                }

                if (!found)
                    recalculate = true;
            }

            if (visibleCount != snapshots.Length)
                recalculate = true;

            if (visibleCount == 0)
            {
                if (snapshots.Length > 0)
                    snapshots.Clear();

                movement.ValueRW.IsFleeing = 0;
                continue;
            }

            if (!recalculate)
            {
                movement.ValueRW.Direction =
                    fleeState.ValueRO.Direction;

                movement.ValueRW.IsFleeing = 1;
                continue;
            }

            float3 previousDirection = math.normalizesafe(
                movement.ValueRO.Direction
            );

            float bestScore = float.NegativeInfinity;
            float3 bestDirection = previousDirection;

            for (int d = 0; d < DirectionCount; d++)
            {
                float angle =
                    d * (2f * math.PI / DirectionCount);

                float3 direction = new float3(
                    math.cos(angle),
                    0f,
                    math.sin(angle)
                );

                float3 futurePosition =
                    position +
                    direction *
                    movement.ValueRO.FleeSpeed *
                    PredictionTime;

                float score = 0f;
                float nearestDistance = float.MaxValue;

                for (int i = 0; i < targets.Length; i++)
                {
                    DetectedTarget enemy = targets[i];

                    if (!IsVisibleEnemy(enemy))
                        continue;

                    float3 enemyPosition =
                        enemy.LastKnownPosition;

                    enemyPosition.y = position.y;

                    float currentDistance =
                        math.distance(position, enemyPosition);

                    float futureDistance =
                        math.distance(futurePosition, enemyPosition);

                    nearestDistance = math.min(
                        nearestDistance,
                        futureDistance
                    );

                    float danger =
                        1f / math.max(futureDistance, 0.5f);

                    score -= danger * 10f;

                    if (futureDistance < currentDistance)
                    {
                        score -=
                            (currentDistance - futureDistance) * 2f;
                    }
                }

                score += nearestDistance * 0.5f;

                score += math.dot(
                    direction,
                    previousDirection
                ) * 0.5f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }

            movement.ValueRW.Direction = bestDirection;
            movement.ValueRW.IsFleeing = 1;

            fleeState.ValueRW.Direction = bestDirection;

            snapshots.Clear();

            for (int i = 0; i < targets.Length; i++)
            {
                DetectedTarget enemy = targets[i];

                if (!IsVisibleEnemy(enemy))
                    continue;

                float3 relativePosition =
                    enemy.LastKnownPosition - position;

                relativePosition.y = 0f;

                snapshots.Add(new NPCFleeThreatSnapshot
                {
                    Target = enemy.Target,
                    RelativePosition = relativePosition
                });
            }
        }
    }

    private static bool IsVisibleEnemy(DetectedTarget target)
    {
        return
            (target.Faction & TargetFaction.Enemy) != 0 &&
            (target.SensedBy & SenseKind.Vision) != 0;
    }
}
