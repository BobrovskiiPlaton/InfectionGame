
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct NPCSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NPCSpawnerComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity spawnerEntity =
            SystemAPI.GetSingletonEntity<NPCSpawnerComponent>();

        NPCSpawnerComponent spawner =
            SystemAPI.GetSingleton<NPCSpawnerComponent>();

        var random = new Unity.Mathematics.Random(12345);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        for (int i = 0; i < spawner.Count; i++)
        {
            Entity npc = ecb.Instantiate(spawner.Prefab);

            float angle = random.NextFloat(
                0f, math.PI * 2f
            );

            float radius = math.sqrt(
                random.NextFloat()
            ) * spawner.Radius;

            float3 position = spawner.Center + new float3(
                math.cos(angle) * radius,
                spawner.SpawnHeight,
                math.sin(angle) * radius
            );

            ecb.SetComponent(npc, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Индивидуальный генератор направлений
            ecb.SetComponent(npc, new NPCWanderComponent
            {
                ChangeDirectionTimer = 0f,
                RandomState = (uint)(i + 1)
            });
        }

        // Запрещаем повторный спавн на следующем кадре
        ecb.RemoveComponent<NPCSpawnerComponent>(
            spawnerEntity
        );

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
