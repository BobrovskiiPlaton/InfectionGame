
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct EnemySpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawnerComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var random = new Unity.Mathematics.Random(12345);

        foreach (var (spawner, entity) in
            SystemAPI.Query<RefRW<EnemySpawnerComponent>>()
                .WithEntityAccess())
        {
            if (spawner.ValueRO.SpawnedCount >= spawner.ValueRO.Count)
            {
                ecb.RemoveComponent<EnemySpawnerComponent>(entity);
                continue;
            }

            spawner.ValueRW.SpawnTimer -= deltaTime;

            if (spawner.ValueRO.SpawnTimer > 0f)
                continue;

            Entity enemy = ecb.Instantiate(spawner.ValueRO.Prefab);

            float angle = random.NextFloat(
                0f, math.PI * 2f
            );

            float radius = math.sqrt(
                random.NextFloat()
            ) * spawner.ValueRO.Radius;

            float3 position = spawner.ValueRO.Center + new float3(
                math.cos(angle) * radius,
                spawner.ValueRO.SpawnHeight,
                math.sin(angle) * radius
            );

            ecb.SetComponent(enemy, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            spawner.ValueRW.SpawnedCount++;

            spawner.ValueRW.CurrentInterval = math.max(
                spawner.ValueRO.MinimumSpawnInterval,
                spawner.ValueRO.CurrentInterval -
                spawner.ValueRO.IntervalDecrease
            );

            spawner.ValueRW.SpawnTimer =
                math.max(0.01f, spawner.ValueRO.CurrentInterval);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
