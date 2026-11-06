using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct NPCMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (movement, transform) in SystemAPI.Query<
            RefRO<NPCMovementComponent>,
            RefRW<LocalTransform>>().WithAll<NPCComponent>())
        {
            float3 direction = movement.ValueRO.Direction;

            if (math.lengthsq(direction) < 0.0001f)
                continue;

            transform.ValueRW.Position +=
                math.normalizesafe(direction) *
                movement.ValueRO.MoveSpeed *
                deltaTime;
        }

    }
}
