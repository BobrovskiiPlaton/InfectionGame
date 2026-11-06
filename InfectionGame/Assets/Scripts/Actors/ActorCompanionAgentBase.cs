using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Actors
{
    public abstract class ActorCompanionAgentBase : MonoBehaviour, IActorCompanionAgent
    {
        [Tooltip("Максимальная дистанция поиска своей ECS-сущности, метры.")]
        [SerializeField, Min(0f)] private float linkSearchRadius = 2f;

        private EntityQuery _linkQuery;
        private Unity.Entities.World _world;
        private Entity _entity;
        private bool _linked;

        protected Unity.Entities.World EcsWorld => _world;

        public Entity Entity => _linked ? _entity : Entity.Null;

        public bool IsLinked =>
            _linked && _world is { IsCreated: true } && _world.EntityManager.Exists(_entity);

        protected virtual void Awake()
        {
            _world = Unity.Entities.World.DefaultGameObjectInjectionWorld;

            if (_world == null || !_world.IsCreated)
            {
                Debug.LogWarning(
                    $"{GetType().Name}: default ECS world не создан, актор работать не будет.", this);
                return;
            }

            _linkQuery = CreateLinkQuery(_world.EntityManager);
        }

        protected virtual void LateUpdate()
        {
            if (!IsLinked)
            {
                TryLink();
                return;
            }

            // ECS-система двигает LocalTransform — зеркалим её в GameObject.
            var localTransform = _world.EntityManager.GetComponentData<LocalTransform>(_entity);
            transform.SetPositionAndRotation(localTransform.Position, localTransform.Rotation);
        }

        protected abstract EntityQuery CreateLinkQuery(EntityManager entityManager);

        public bool TryGetPerception(out InfectionGame.Perception.DetectionState state)
        {
            state = default;

            if (!IsLinked)
                return false;

            state = _world.EntityManager.GetComponentData<InfectionGame.Perception.DetectionState>(_entity);
            return true;
        }

        public bool TryGetWanderTarget(out WanderTarget wander)
        {
            wander = default;

            if (!IsLinked)
                return false;

            wander = _world.EntityManager.GetComponentData<WanderTarget>(_entity);
            return true;
        }

        public void SetWanderTarget(float3 position)
        {
            if (!IsLinked)
                return;

            _world.EntityManager.SetComponentData(_entity, WanderTargetMath.Create(position));
        }

        public void ClearWanderTarget()
        {
            if (!IsLinked)
                return;

            _world.EntityManager.SetComponentData(_entity, WanderTargetMath.Cleared());
        }

        // Костыль, ищет ближайшую entity, чтобы привязаться к ней
        private void TryLink()
        {
            if (_world == null || !_world.IsCreated)
                return;

            using NativeArray<Entity> entities = _linkQuery.ToEntityArray(Allocator.Temp);

            if (entities.Length == 0)
                return;

            float best = linkSearchRadius * linkSearchRadius;
            Entity bestEntity = Entity.Null;
            float3 self = transform.position;

            for (int i = 0; i < entities.Length; i++)
            {
                var lt = _world.EntityManager.GetComponentData<LocalTransform>(entities[i]);
                float distanceSq = math.distancesq(lt.Position, self);

                if (distanceSq <= best)
                {
                    best = distanceSq;
                    bestEntity = entities[i];
                }
            }

            if (bestEntity == Entity.Null)
                return;

            _entity = bestEntity;
            _linked = true;
        }
    }
}
