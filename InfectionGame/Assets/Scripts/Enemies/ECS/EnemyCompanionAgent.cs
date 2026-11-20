using InfectionGame.Actors;
using Unity.Entities;
using Unity.Transforms;

namespace InfectionGame.Enemies.ECS
{
    public class EnemyCompanionAgent : ActorCompanionAgentBase
    {
        protected override EntityQuery CreateLinkQuery(EntityManager entityManager)
        {
            return entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        public bool TryGetAgent(out EnemyAgent agent)
        {
            agent = default;

            if (!IsLinked)
                return false;

            agent = EcsWorld.EntityManager.GetComponentData<EnemyAgent>(Entity);
            return true;
        }

        public void SetState(EnemyState value)
        {
            if (!IsLinked)
                return;

            var entityManager = EcsWorld.EntityManager;
            entityManager.SetComponentEnabled<EnemyStateIdle>(Entity, value == EnemyState.Idle);
            entityManager.SetComponentEnabled<EnemyStateChase>(Entity, value == EnemyState.Chase);
            entityManager.SetComponentEnabled<EnemyStateAttack>(Entity, value == EnemyState.Attack);
        }

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
