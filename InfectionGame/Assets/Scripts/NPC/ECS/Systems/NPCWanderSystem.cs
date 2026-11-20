
using InfectionGame.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(VisionDetectionSystem))]
[UpdateBefore(typeof(NPCFleeSystem))]
public partial struct NPCWanderSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (movement, wander, entity) in
            SystemAPI.Query<
                RefRW<NPCMovementComponent>,
                RefRW<NPCWanderComponent>>()
                .WithAll<NPCComponent>()
                .WithEntityAccess())
        {
            wander.ValueRW.ChangeDirectionTimer -= deltaTime;

            if (wander.ValueRO.ChangeDirectionTimer > 0f)
                continue;

            uint seed = wander.ValueRO.RandomState;

            if (seed == 0)
            {
                seed = math.hash(new uint2(
                    (uint)entity.Index,
                    (uint)entity.Version
                ));

                if (seed == 0)
                    seed = 1;
            }

            var random = new Unity.Mathematics.Random(seed);

            float angle = random.NextFloat(
                0f, math.PI * 2f);

            movement.ValueRW.Direction = new float3(
                math.cos(angle),
                0f,
                math.sin(angle)
            );

            wander.ValueRW.ChangeDirectionTimer =
                random.NextFloat(2f, 5f);

            wander.ValueRW.RandomState = random.state;
        }
    }
}
