using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Actors
{
    /// <summary>
    /// Спавнит companion GameObject для каждой ECS-сущности актора. Unity Behavior работает
    /// только с GameObject, поэтому рядом с каждой сущностью рождается объект с
    /// <c>BehaviorGraphAgent</c> и мостом к ECS (см. <see cref="IActorCompanionAgent"/>).
    ///
    /// Общий для всех акторов; наследник описывает только запрос, маркер «уже заспавнен»,
    /// источник behavior graph и конкретный мост. Сущность помечается маркером, чтобы не
    /// спавнить дубликаты каждый кадр.
    /// </summary>
    public abstract partial class CompanionSpawnSystemBase : SystemBase
    {
        private EntityQuery _pendingQuery;

        protected override void OnCreate()
        {
            _pendingQuery = CreatePendingQuery();
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> entities = _pendingQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];

                if (HasSpawnedMarker(entity))
                    continue;

                SpawnCompanion(entity);
                MarkSpawned(entity);
            }
        }

        private void SpawnCompanion(Entity entity)
        {
            LocalTransform localTransform = EntityManager.GetComponentData<LocalTransform>(entity);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = CompanionName(entity);
            go.transform.SetPositionAndRotation(localTransform.Position, localTransform.Rotation);

            // Компаньон не участвует в физике — двигает его ECS-система, а колайдер
            // только мешал бы проходу других персонажей.
            if (go.TryGetComponent<Collider>(out var collider))
                Object.Destroy(collider);

            var behaviorAgent = go.AddComponent<Unity.Behavior.BehaviorGraphAgent>();
            behaviorAgent.Graph = GetBehaviorGraph(entity);

            AttachBridge(go);
        }

        /// <summary>Сущности, которым ещё нужен companion.</summary>
        protected abstract EntityQuery CreatePendingQuery();

        /// <summary>Уже заспавнен ли companion для этой сущности.</summary>
        protected abstract bool HasSpawnedMarker(Entity entity);

        /// <summary>Пометить сущность как обработанную.</summary>
        protected abstract void MarkSpawned(Entity entity);

        /// <summary>Behavior graph этого актора.</summary>
        protected abstract Unity.Behavior.BehaviorGraph GetBehaviorGraph(Entity entity);

        /// <summary>Повесить мост ECS ↔ GameObject и вернуть его.</summary>
        protected abstract MonoBehaviour AttachBridge(GameObject companion);

        /// <summary>Имя companion-объекта в иерархии.</summary>
        protected virtual string CompanionName(Entity entity) => $"Actor Companion ({entity})";
    }
}
