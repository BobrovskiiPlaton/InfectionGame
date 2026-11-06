using Unity.Entities;
using Unity.Mathematics;

namespace InfectionGame.Enemies.ECS
{
    /// <summary>Маркер врага. Сенсоры/движение настроены через <see cref="EnemySpecies"/>.</summary>
    public struct EnemyTag : IComponentData
    {
    }

    /// <summary>
    /// Ставится, когда для врага уже создан companion GameObject с behavior tree.
    /// Нужен, чтобы спавнер не порождал дубликаты каждый кадр.
    /// </summary>
    public struct EnemyCompanionSpawned : IComponentData
    {
    }

    /// <summary>
    /// Данные вида, развернутые из <c>EnemySpeciesSO</c> при бэйкинге. Хранятся как
    /// значение, чтобы ECS-системы не таскали ссылку на ScriptableObject.
    /// </summary>
    public struct EnemySpecies : IComponentData
    {
        public float MoveSpeed;
        public float PassiveMoveSpeed;
        public float AttackDistance;
        public float AttackInterval;
        public float AttackDamage;

        /// <summary>
        /// Behavior graph этого вида. Лежит в ECS как <c>UnityObjectRef</c>, чтобы companion-спавнер
        /// мог выдать его <c>BehaviorGraphAgent</c> без поиска ScriptableObject.
        /// </summary>
        public Unity.Entities.UnityObjectRef<Unity.Behavior.BehaviorGraph> BehaviorGraph;
    }

    // [BlackboardEnum] — не косметика: Unity Behavior собирает список поддерживаемых
    // enum'ов через TypeCache.GetTypesWithAttribute<BlackboardEnumAttribute>().
    // Без него BlackboardVariable<EnemyState> не создать, а IL2CPPGenericTypesGenerator
    // не сгенерирует инстанцирование и сборка упадёт уже на устройстве.
    [System.Serializable]
    [Unity.Behavior.BlackboardEnum]
    public enum EnemyState : byte
    {
        /// <summary>Цели нет — бродит/стоит.</summary>
        Idle = 0,

        /// <summary>Цель замечена, догоняем.</summary>
        Chase = 1,

        /// <summary>Цель в зоне удара — бьём.</summary>
        Attack = 2,
    }

    /// <summary>
    /// Текущее состояние врага. Это НЕ enum-поле, а три enableable-тега: состояние — это
    /// «какой из трёх job'ов сейчас работает над этой сущностью».
    ///
    /// Раньше состояние лежало в enum-поле, и в <c>EnemyAgentSystem</c> стоял switch,
    /// который сам переходил между состояниями. Получались два источника правды —
    /// behavior tree и switch, — и они перетирали решения друг друга. Теперь переходов
    /// в ECS нет вообще: граф включает нужный тег через companion-мост, а ECS исполняет
    /// ровно тот job, чей тег активен.
    /// </summary>
    public struct EnemyStateIdle : IComponentData, IEnableableComponent
    {
    }

    /// <summary>См. <see cref="EnemyStateIdle"/>.</summary>
    public struct EnemyStateChase : IComponentData, IEnableableComponent
    {
    }

    /// <summary>См. <see cref="EnemyStateIdle"/>.</summary>
    public struct EnemyStateAttack : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Механика врага, не связанная с решениями: кулдаун удара, цель, таймеры. Решения
    /// принимает behavior tree, сюда попадают только счётчики.
    /// </summary>
    public struct EnemyAgent : IComponentData
    {
        /// <summary>Сущность текущей цели. Entity.Null — цели нет.</summary>
        public Entity Target;

        /// <summary>Сколько секунд осталось до следующего удара.</summary>
        public float AttackCooldown;

        /// <summary>Сколько секунд назад был последний удар (для анимаций/эффектов).</summary>
        public float TimeSinceAttack;
    }

    /// <summary>
    /// Точка брожения у врага — общий <c>InfectionGame.Actors.WanderTarget</c>. Вынесена
    /// в общий слой, чтобы тот же компонент и ту же политику «не перебирать точку каждый
    /// тик» использовал NPC.
    /// </summary>
}
