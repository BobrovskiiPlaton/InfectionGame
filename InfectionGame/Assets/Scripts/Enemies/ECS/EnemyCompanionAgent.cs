using InfectionGame.Actors;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Enemies.ECS
{
    /// <summary>
    /// Мост companion GameObject ↔ ECS для врага. Вся общая работа (поиск своей сущности,
    /// зеркалирование трансформа, точка брожения) — в <see cref="ActorCompanionAgentBase"/>;
    /// здесь только враговое: запрос поиска и переключение состояния.
    /// </summary>
    public class EnemyCompanionAgent : ActorCompanionAgentBase
    {
        protected override EntityQuery CreateLinkQuery(EntityManager entityManager)
        {
            return entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        /// <summary>Механика врага, не связанная с решениями (кулдауны, счётчики).</summary>
        public bool TryGetAgent(out EnemyAgent agent)
        {
            agent = default;

            if (!IsLinked)
                return false;

            agent = EcsWorld.EntityManager.GetComponentData<EnemyAgent>(Entity);
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

            var entityManager = EcsWorld.EntityManager;
            entityManager.SetComponentEnabled<EnemyStateIdle>(Entity, value == EnemyState.Idle);
            entityManager.SetComponentEnabled<EnemyStateChase>(Entity, value == EnemyState.Chase);
            entityManager.SetComponentEnabled<EnemyStateAttack>(Entity, value == EnemyState.Attack);
        }

        /// <summary>Текущее состояние врага — какой из трёх тегов сейчас включён.</summary>
        public bool TryGetState(out EnemyState state)
        {
            state = EnemyState.Idle;

            if (!IsLinked)
                return false;

            var entityManager = EcsWorld.EntityManager;

            if (entityManager.IsComponentEnabled<EnemyStateAttack>(Entity))
                state = EnemyState.Attack;
            else if (entityManager.IsComponentEnabled<EnemyStateChase>(Entity))
                state = EnemyState.Chase;

            return true;
        }
    }
}
