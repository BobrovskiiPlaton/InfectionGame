
using InfectionGame.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCWanderSystem))]
[UpdateBefore(typeof(NPCMovementSystem))]
public partial struct NPCFleeSystem : ISystem
{
    private const int DirectionCount = 16;
    private const float PredictionTime = 1.5f;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (movement, transform, targets) in
            SystemAPI.Query<
                RefRW<NPCMovementComponent>,
                RefRO<LocalTransform>,
                DynamicBuffer<DetectedTarget>>()
                .WithAll<NPCComponent>())
        {
            float3 position = transform.ValueRO.Position;

            bool hasEnemies = false;

            for (int i = 0; i < targets.Length; i++)
            {
                if (IsVisibleEnemy(targets[i]))
                {
                    hasEnemies = true;
                    break;
                }
            }

            if (!hasEnemies)
            {
                movement.ValueRW.IsFleeing = 0;
                continue;
            }

            float3 previousDirection =
                math.normalizesafe(
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
                        math.distance(
                            futurePosition,
                            enemyPosition
                        );

                    nearestDistance = math.min(
                        nearestDistance,
                        futureDistance
                    );

                    // Штраф за приближение к врагу
                    float danger =
                        1f / math.max(futureDistance, 0.5f);

                    score -= danger * 10f;

                    if (futureDistance < currentDistance)
                    {
                        score -=
                            (currentDistance - futureDistance) * 2f;
                    }
                }

                // Предпочитаем направления,
                // увеличивающие дистанцию до ближайшего врага
                score += nearestDistance * 0.5f;

                // Стараемся не менять направление резко
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
        }
    }

    private static bool IsVisibleEnemy(DetectedTarget target)
    {
        return
            (target.Faction & TargetFaction.Enemy) != 0 &&
            (target.SensedBy & SenseKind.Vision) != 0;
    }
}
