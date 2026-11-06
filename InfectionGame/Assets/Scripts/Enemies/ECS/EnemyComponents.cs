using Unity.Entities;
using Unity.Mathematics;

namespace InfectionGame.Enemies.ECS
{
    public struct EnemyTag : IComponentData
    {
    }

    public struct EnemyCompanionSpawned : IComponentData
    {
    }

    public struct EnemySpecies : IComponentData
    {
        public float MoveSpeed;
        public float PassiveMoveSpeed;
        public float AttackDistance;
        public float AttackInterval;
        public float AttackDamage;

        // object ref, потому что нельзя ссылку использовать
        public Unity.Entities.UnityObjectRef<Unity.Behavior.BehaviorGraph> BehaviorGraph;
    }

    [System.Serializable]
    [Unity.Behavior.BlackboardEnum]
    public enum EnemyState : byte
    {
        Idle = 0,

        Chase = 1,

        Attack = 2,
    }

    public struct EnemyStateIdle : IComponentData, IEnableableComponent
    {
    }

    public struct EnemyStateChase : IComponentData, IEnableableComponent
    {
    }

    public struct EnemyStateAttack : IComponentData, IEnableableComponent
    {
    }

    public struct EnemyAgent : IComponentData
    {
        public Entity Target;

        public float AttackCooldown;

        public float TimeSinceAttack;
    }
}
