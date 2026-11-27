using InfectionGame.Enemies.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Потребляет NPCConversionRequest: заражённый NPC превращается во врага и исчезает.
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCInfectionSystem))]
public partial struct NPCConversionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<EnemyConversionPrefab>(
                out EnemyConversionPrefab enemyPrefab))
            return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, entity) in
            SystemAPI.Query<RefRO<LocalTransform>>()
                .WithAll<NPCComponent, NPCConversionRequest>()
                .WithEntityAccess())
        {
            Entity enemy = ecb.Instantiate(enemyPrefab.Prefab);

            ecb.SetComponent(enemy, new LocalTransform
            {
                Position = transform.ValueRO.Position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
