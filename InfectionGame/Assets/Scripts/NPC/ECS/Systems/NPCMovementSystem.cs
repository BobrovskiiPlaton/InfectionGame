
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCFleeSystem))]
public partial struct NPCMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (movement, transform) in
            SystemAPI.Query<
                RefRO<NPCMovementComponent>,
                RefRW<LocalTransform>>()
                .WithAll<NPCComponent>())
        {
            float3 direction = movement.ValueRO.Direction;

            if (math.lengthsq(direction) < 0.0001f)
                continue;

            float speed = movement.ValueRO.IsFleeing != 0
                ? movement.ValueRO.FleeSpeed
                : movement.ValueRO.MoveSpeed;

            transform.ValueRW.Position +=
                math.normalizesafe(direction) *
                speed * deltaTime;
        }
    }
}
