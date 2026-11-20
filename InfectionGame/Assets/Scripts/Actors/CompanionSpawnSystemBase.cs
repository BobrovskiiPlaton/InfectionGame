using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Actors
{
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

            var behaviorAgent = go.AddComponent<Unity.Behavior.BehaviorGraphAgent>();
            behaviorAgent.Graph = GetBehaviorGraph(entity);

            AttachBridge(go);
        }

        protected abstract EntityQuery CreatePendingQuery();

        protected abstract bool HasSpawnedMarker(Entity entity);

        protected abstract void MarkSpawned(Entity entity);

        protected abstract Unity.Behavior.BehaviorGraph GetBehaviorGraph(Entity entity);

        protected abstract MonoBehaviour AttachBridge(GameObject companion);

        protected virtual string CompanionName(Entity entity) => $"Actor Companion ({entity})";
    }
}
