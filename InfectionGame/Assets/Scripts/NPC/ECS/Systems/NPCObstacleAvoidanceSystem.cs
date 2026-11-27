
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCFleeSystem))]
[UpdateBefore(typeof(NPCMovementSystem))]
public partial struct NPCObstacleAvoidanceSystem : ISystem
{
    private const float CheckDistance = 2.5f;
    private const float CheckRadius = 0.4f;
    private const float CheckHeight = 0.8f;

    private const int DirectionCount = 7;
    private const int BatchSize = 64;

    public void OnUpdate(ref SystemState state)
    {
        int wallMask = LayerMask.GetMask("Walls");

        if (wallMask == 0)
            return;

        WallProximityGrid grid = WallProximityGrid.Instance;

        grid.Initialize(
            wallMask,
            CheckDistance,
            CheckRadius
        );

        var queryParameters = new QueryParameters(
            wallMask,
            false,
            QueryTriggerInteraction.Ignore,
            false
        );

        using var entities =
            new NativeList<Entity>(Allocator.TempJob);

        using var origins =
            new NativeList<Vector3>(Allocator.TempJob);

        using var forwards =
            new NativeList<Vector3>(Allocator.TempJob);

        using var commands =
            new NativeList<SpherecastCommand>(Allocator.TempJob);

        foreach (var (movement, transform, entity) in
            SystemAPI.Query<
                RefRO<NPCMovementComponent>,
                RefRO<LocalTransform>>()
                .WithAll<NPCComponent>()
                .WithEntityAccess())
        {
            float3 direction = math.normalizesafe(
                movement.ValueRO.Direction
            );

            if (math.lengthsq(direction) < 0.001f)
                continue;

            float3 position = transform.ValueRO.Position;

            if (!grid.RequiresPhysicsCheck(position))
                continue;

            Vector3 origin = new Vector3(
                position.x,
                position.y + CheckHeight,
                position.z
            );

            Vector3 forward = new Vector3(
                direction.x,
                0f,
                direction.z
            );

            entities.Add(entity);
            origins.Add(origin);
            forwards.Add(forward);

            commands.Add(new SpherecastCommand(
                origin,
                CheckRadius,
                forward,
                queryParameters,
                CheckDistance
            ));
        }

        if (commands.Length == 0)
            return;

        using var results = new NativeArray<RaycastHit>(
            commands.Length,
            Allocator.TempJob
        );

        JobHandle handle = SpherecastCommand.ScheduleBatch(
            commands.AsArray(),
            results,
            BatchSize,
            1,
            state.Dependency
        );

        handle.Complete();

        using var blockedIndices =
            new NativeList<int>(Allocator.TempJob);

        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].collider != null)
                blockedIndices.Add(i);
        }

        if (blockedIndices.Length == 0)
            return;

        int blockedCount = blockedIndices.Length;

        using var alternativeCommands =
            new NativeList<SpherecastCommand>(
                blockedCount * DirectionCount,
                Allocator.TempJob
            );

        for (int i = 0; i < blockedCount; i++)
        {
            int npcIndex = blockedIndices[i];

            Vector3 origin = origins[npcIndex];
            Vector3 forward = forwards[npcIndex];

            for (int d = 0; d < DirectionCount; d++)
            {
                float angle = GetAngle(d);

                Vector3 candidate =
                    Quaternion.Euler(0f, angle, 0f) * forward;

                alternativeCommands.Add(
                    new SpherecastCommand(
                        origin,
                        CheckRadius,
                        candidate,
                        queryParameters,
                        CheckDistance
                    )
                );
            }
        }

        using var alternativeResults =
            new NativeArray<RaycastHit>(
                alternativeCommands.Length,
                Allocator.TempJob
            );

        handle = SpherecastCommand.ScheduleBatch(
            alternativeCommands.AsArray(),
            alternativeResults,
            BatchSize,
            1
        );

        handle.Complete();

        var movementLookup =
            SystemAPI.GetComponentLookup<NPCMovementComponent>();

        for (int i = 0; i < blockedCount; i++)
        {
            int npcIndex = blockedIndices[i];
            Entity entity = entities[npcIndex];

            Vector3 forward = forwards[npcIndex];
            Vector3 bestDirection = -forward;

            float bestClearance = -1f;

            for (int d = 0; d < DirectionCount; d++)
            {
                int resultIndex =
                    i * DirectionCount + d;

                RaycastHit hit =
                    alternativeResults[resultIndex];

                float angle = GetAngle(d);

                Vector3 candidate =
                    Quaternion.Euler(0f, angle, 0f) * forward;

                if (hit.collider == null)
                {
                    bestDirection = candidate;
                    break;
                }

                if (hit.distance > bestClearance)
                {
                    bestClearance = hit.distance;
                    bestDirection = candidate;
                }
            }

            NPCMovementComponent movement =
                movementLookup[entity];

            movement.Direction = new float3(
                bestDirection.x,
                0f,
                bestDirection.z
            );

            movementLookup[entity] = movement;
        }
    }

    private static float GetAngle(int index)
    {
        switch (index)
        {
            case 0: return 45f;
            case 1: return -45f;
            case 2: return 90f;
            case 3: return -90f;
            case 4: return 135f;
            case 5: return -135f;
            default: return 180f;
        }
    }
}
