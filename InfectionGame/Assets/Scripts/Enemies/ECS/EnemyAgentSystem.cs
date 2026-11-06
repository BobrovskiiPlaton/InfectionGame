using InfectionGame.Actors;
using InfectionGame.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Enemies.ECS
{
    /// <summary>
    /// Исполняет решение behavior tree: три независимых job'а, по одному на состояние.
    /// Состояние — это enableable-тег (<see cref="EnemyStateIdle"/> / Chase / Attack),
    /// который включает граф через companion-мост.
    ///
    /// Здесь НЕТ переходов между состояниями и НЕТ switch: раньше тот же класс содержал
    /// state machine, который дублировал логику дерева поведения и перетирал её решения.
    /// Теперь единственным источником решений является граф.
    ///
    /// Кинематика (<see cref="ActorMotion"/>) и работа с точкой брожения
    /// (<see cref="WanderTargetMath"/>) общие для всех акторов — NPC переиспользует их же.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(VisionDetectionSystem))]
    public partial struct EnemyAgentSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            state.Dependency = new ChaseMoveJob
            {
                DeltaTime = dt,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new AttackJob
            {
                DeltaTime = dt,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new WanderJob
            {
                DeltaTime = dt,
            }.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>Идти к последней известной позиции цели.</summary>
    [BurstCompile]
    [WithAll(typeof(EnemyStateChase))]
    public partial struct ChaseMoveJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(
            ref LocalTransform transform,
            in EnemySpecies species,
            in DetectionState perception)
        {
            // Цели нет — стоять на месте; следующее состояние выдаст граф.
            if (perception.Primary == Entity.Null)
                return;

            ActorMotion.MoveTowards(
                ref transform, perception.LastKnownPosition, species.MoveSpeed, DeltaTime);
        }
    }

    /// <summary>
    /// Стоять и бить. Кулдаун — чистая механика, урон наносится отдельно, когда у целей
    /// появится здоровье.
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(EnemyStateAttack))]
    public partial struct AttackJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(
            ref LocalTransform transform,
            in EnemySpecies species,
            in DetectionState perception,
            ref EnemyAgent agent)
        {
            agent.TimeSinceAttack += DeltaTime;

            if (agent.AttackCooldown > 0f)
                agent.AttackCooldown = math.max(0f, agent.AttackCooldown - DeltaTime);

            if (perception.Primary == Entity.Null)
                return;

            transform.Rotation = ActorMotion.FaceTowards(
                transform.Position, perception.LastKnownPosition, transform.Rotation);

            if (agent.AttackCooldown <= 0f)
            {
                agent.AttackCooldown = species.AttackInterval;
                agent.TimeSinceAttack = 0f;
            }
        }
    }

    /// <summary>
    /// Бродить к точке, которую выдал behavior tree. Дойдя — снимает точку, чтобы граф
    /// выдал новую. Сам враг точку никогда не перебирает: это делает только граф.
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(EnemyStateIdle))]
    public partial struct WanderJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(
            ref LocalTransform transform,
            in EnemySpecies species,
            ref WanderTarget wander)
        {
            WanderTargetMath.Tick(ref wander, DeltaTime);

            if (wander.HasTarget <= 0.5f)
                return;

            ActorMotion.MoveTowards(
                ref transform, wander.Position, species.PassiveMoveSpeed, DeltaTime);

            if (WanderTargetMath.HasArrived(transform.Position, wander))
                wander = WanderTargetMath.Cleared();
        }
    }
}
