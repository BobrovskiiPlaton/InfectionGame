using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Enemies.ECS
{
    /// <summary>
    /// Спавнит companion GameObject для каждой ECS-сущности врага. Unity Behavior работает
    /// только с GameObject, поэтому для каждого врага рождается объект с
    /// <c>BehaviorGraphAgent</c> и <see cref="EnemyCompanionAgent"/> (мост к ECS).
    /// Сущность помечается <see cref="EnemyCompanionSpawned"/>, чтобы не спавнить дубликаты.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAgentSystem))]
    public partial class EnemyCompanionSpawnSystem : SystemBase
    {
        private EntityQuery _pendingQuery;

        protected override void OnCreate()
        {
            _pendingQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<EnemySpecies>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = _pendingQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];

                if (EntityManager.HasComponent<EnemyCompanionSpawned>(entity))
                    continue;

                SpawnCompanion(entity);
                EntityManager.AddComponent<EnemyCompanionSpawned>(entity);
            }
        }

        private void SpawnCompanion(Entity entity)
        {
            EnemySpecies species = EntityManager.GetComponentData<EnemySpecies>(entity);
            LocalTransform localTransform = EntityManager.GetComponentData<LocalTransform>(entity);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Enemy Companion ({entity})";
            go.transform.SetPositionAndRotation(localTransform.Position, localTransform.Rotation);

            // Компаньон не участвует в физике — двигает его ECS-система, а колайдер
            // только мешал бы проходу других персонажей.
            if (go.TryGetComponent<Collider>(out var collider))
                Object.Destroy(collider);

            var behaviorAgent = go.AddComponent<Unity.Behavior.BehaviorGraphAgent>();
            behaviorAgent.Graph = species.BehaviorGraph.Value;

            go.AddComponent<EnemyCompanionAgent>();
        }
    }
}
