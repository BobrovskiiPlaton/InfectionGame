using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Enemies.ECS
{
    /// <summary>
    /// Companion GameObject для ECS-сущности врага. Unity Behavior умеет работать только
    /// с GameObject, поэтому поведение живёт здесь, а данные (перцепция, состояние) — в ECS.
    /// Этот класс и есть мост: читает <see cref="DetectionState"/> из ECS, пишет
    /// <see cref="EnemyAgent"/> и <see cref="EnemyWanderTarget"/> обратно.
    ///
    /// Сущность ищется по ближайшей позиции — для одного врага это однозначно. Если врагов
    /// станет много и они будут рождаться вплотную, стоит заменить на явную связку через
    /// <c>UnityObjectRef&lt;GameObject&gt;</c>.
    /// </summary>
    public class EnemyCompanionAgent : MonoBehaviour
    {
        [Tooltip("Максимальная дистанция поиска своей ECS-сущности, метры.")]
        [SerializeField, Min(0f)] private float linkSearchRadius = 2f;

        private EntityQuery _enemyQuery;
        private World _world;
        private Entity _entity;
        private bool _linked;

        /// <summary>Есть ли привязка к ECS-сущности.</summary>
        public bool IsLinked => _linked && _world is { IsCreated: true } && _world.EntityManager.Exists(_entity);

        /// <summary>Сущность врага в ECS. Entity.Null, если ещё не привязались.</summary>
        public Entity Entity => _linked ? _entity : Entity.Null;

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;

            if (_world == null || !_world.IsCreated)
            {
                Debug.LogWarning($"{nameof(EnemyCompanionAgent)}: default ECS world не создан, враг работать не будет.", this);
                return;
            }

            _enemyQuery = _world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        private void LateUpdate()
        {
            if (!IsLinked)
            {
                TryLink();
                return;
            }

            // ECS-система двигает LocalTransform — зеркалим её в GameObject.
            var localTransform = _world.EntityManager.GetComponentData<LocalTransform>(_entity);
            this.transform.SetPositionAndRotation(localTransform.Position, localTransform.Rotation);
        }

        private void TryLink()
        {
            if (_world == null || !_world.IsCreated)
                return;

            using var entities = _enemyQuery.ToEntityArray(Allocator.Temp);

            if (entities.Length == 0)
                return;

            float best = linkSearchRadius * linkSearchRadius;
            Entity bestEntity = Entity.Null;
            float3 self = this.transform.position;

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

        /// <summary>Что враг сейчас воспринимает (зрение + слух + память).</summary>
        public bool TryGetPerception(out InfectionGame.Perception.DetectionState state)
        {
            state = default;

            if (!IsLinked)
                return false;

            state = _world.EntityManager.GetComponentData<InfectionGame.Perception.DetectionState>(_entity);
            return true;
        }

        /// <summary>Текущее состояние врага.</summary>
        public bool TryGetAgent(out EnemyAgent agent)
        {
            agent = default;

            if (!IsLinked)
                return false;

            agent = _world.EntityManager.GetComponentData<EnemyAgent>(_entity);
            return true;
        }

        /// <summary>
        /// Переключить состояние (решение behavior tree). Состояние в ECS — это enableable-теги,
        /// поэтому переключение не меняет архетип и стоит почти даром.
        /// </summary>
        public void SetState(EnemyState value)
        {
            if (!IsLinked)
                return;

            var entityManager = _world.EntityManager;
            entityManager.SetComponentEnabled<EnemyStateIdle>(_entity, value == EnemyState.Idle);
            entityManager.SetComponentEnabled<EnemyStateChase>(_entity, value == EnemyState.Chase);
            entityManager.SetComponentEnabled<EnemyStateAttack>(_entity, value == EnemyState.Attack);
        }

        /// <summary>Текущее состояние врага — какой из трёх тегов сейчас включён.</summary>
        public bool TryGetState(out EnemyState state)
        {
            state = EnemyState.Idle;

            if (!IsLinked)
                return false;

            var entityManager = _world.EntityManager;

            if (entityManager.IsComponentEnabled<EnemyStateAttack>(_entity))
                state = EnemyState.Attack;
            else if (entityManager.IsComponentEnabled<EnemyStateChase>(_entity))
                state = EnemyState.Chase;

            return true;
        }

        /// <summary>Задать точку брожения, когда цели нет.</summary>
        public void SetWanderTarget(float3 position)
        {
            if (!IsLinked)
                return;

            _world.EntityManager.SetComponentData(_entity, new EnemyWanderTarget
            {
                Position = position,
                HasTarget = 1f,
                TimeSinceAssigned = 0f,
            });
        }

        /// <summary>Снять точку брожения — враг остановится.</summary>
        public void ClearWanderTarget()
        {
            if (!IsLinked)
                return;

            _world.EntityManager.SetComponentData(_entity, new EnemyWanderTarget
            {
                Position = float3.zero,
                HasTarget = 0f,
                TimeSinceAssigned = 0f,
            });
        }

        /// <summary>Текущая точка брожения. Нужна поведению, чтобы не перебирать её каждый тик.</summary>
        public bool TryGetWanderTarget(out EnemyWanderTarget wander)
        {
            wander = default;

            if (!IsLinked)
                return false;

            wander = _world.EntityManager.GetComponentData<EnemyWanderTarget>(_entity);
            return true;
        }
    }
}
