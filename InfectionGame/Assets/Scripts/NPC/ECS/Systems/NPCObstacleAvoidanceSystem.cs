
using Unity.Entities;
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

    public void OnUpdate(ref SystemState state)
    {
        int wallMask = LayerMask.GetMask("Walls");

        if (wallMask == 0)
            return;

        foreach (var (movement, transform) in
            SystemAPI.Query<
                RefRW<NPCMovementComponent>,
                RefRO<LocalTransform>>()
                .WithAll<NPCComponent>())
        {
            float3 direction =
                math.normalizesafe(movement.ValueRO.Direction);

            if (math.lengthsq(direction) < 0.001f)
                continue;

            float3 position = transform.ValueRO.Position;

            Vector3 origin = new Vector3(
                position.x,
                position.y + CheckHeight,
                position.z
            );

            Vector3 forward = new Vector3(
                direction.x, 0f, direction.z
            );

            if (!IsBlocked(origin, forward, wallMask))
                continue;

            // Пробуем обойти препятствие
            float[] angles =
            {
                45f, -45f,
                90f, -90f,
                135f, -135f,
                180f
            };

            bool found = false;

            foreach (float angle in angles)
            {
                Vector3 candidate =
                    Quaternion.Euler(0f, angle, 0f) * forward;

                if (IsBlocked(origin, candidate, wallMask))
                    continue;

                movement.ValueRW.Direction =
                    new float3(
                        candidate.x, 0f, candidate.z
                    );

                found = true;
                break;
            }

            if (!found)
            {
                movement.ValueRW.Direction = float3.zero;
            }
        }
    }

    private static bool IsBlocked(
        Vector3 origin,
        Vector3 direction,
        int wallMask)
    {
        return Physics.SphereCast(
            origin,
            CheckRadius,
            direction,
            out _,
            CheckDistance,
            wallMask,
            QueryTriggerInteraction.Ignore
        );
    }
}
