using InfectionGame.Actors;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Enemies.ECS
{
    /// <summary>
    /// Спавнит companion GameObject для каждой ECS-сущности врага. Общая механика спавна —
    /// в <see cref="CompanionSpawnSystemBase"/>; здесь только враговое: запрос, маркер,
    /// источник behavior graph и мост.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAgentSystem))]
    public partial class EnemyCompanionSpawnSystem : CompanionSpawnSystemBase
    {
        protected override EntityQuery CreatePendingQuery()
        {
            return GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<EnemySpecies>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        protected override bool HasSpawnedMarker(Entity entity)
        {
            return EntityManager.HasComponent<EnemyCompanionSpawned>(entity);
        }

        protected override void MarkSpawned(Entity entity)
        {
            EntityManager.AddComponent<EnemyCompanionSpawned>(entity);
        }

        protected override Unity.Behavior.BehaviorGraph GetBehaviorGraph(Entity entity)
        {
            return EntityManager.GetComponentData<EnemySpecies>(entity).BehaviorGraph.Value;
        }

        protected override MonoBehaviour AttachBridge(GameObject companion)
        {
            return companion.AddComponent<EnemyCompanionAgent>();
        }

        protected override string CompanionName(Entity entity) => $"Enemy Companion ({entity})";
    }
}
