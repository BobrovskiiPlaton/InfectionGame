using InfectionGame.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Enemies.ECS
{
    internal static class EnemyMotion
    {
        public static void MoveTowards(
            ref LocalTransform transform,
            float3 goal,
            float speed,
            float deltaTime)
        {
            if (speed <= 0f)
                return;

            float3 toGoal = goal - transform.Position;
            float distance = math.length(toGoal);

            if (distance <= 0.001f)
                return;

            float3 direction = toGoal / distance;
            float step = math.min(distance, speed * deltaTime);

            transform.Position += direction * step;
            transform.Rotation = FaceTowards(transform.Position, goal, transform.Rotation);
        }

        public static quaternion FaceTowards(float3 from, float3 goal, quaternion current)
        {
            float3 direction = goal - from;

            if (math.lengthsq(direction) < 1e-6f)
                return current;

            return quaternion.LookRotationSafe(direction, math.up());
        }
    }

    /// <summary>
    /// Исполняет решение behavior tree: три независимых job'а, по одному на состояние.
    /// Состояние — это enableable-тег (<see cref="EnemyStateIdle"/> / Chase / Attack),
    /// который включает граф через companion-мост.
    ///
    /// Здесь НЕТ переходов между состояниями и НЕТ switch: раньше тот же класс содержал
    /// state machine, который дублировал логику дерева поведения и перетирал её решения.
    /// Теперь единственным источником решений является граф.
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

            EnemyMotion.MoveTowards(
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

            transform.Rotation = EnemyMotion.FaceTowards(
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
    /// выдал новую.
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(EnemyStateIdle))]
    public partial struct WanderJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(
            ref LocalTransform transform,
            in EnemySpecies species,
            ref EnemyWanderTarget wander)
        {
            if (wander.HasTarget > 0.5f)
                wander.TimeSinceAssigned += DeltaTime;

            if (wander.HasTarget <= 0.5f)
                return;

            EnemyMotion.MoveTowards(
                ref transform, wander.Position, species.PassiveMoveSpeed, DeltaTime);

            if (math.distancesq(transform.Position, wander.Position) < 0.25f)
            {
                wander.HasTarget = 0f;
                wander.TimeSinceAssigned = 0f;
            }
        }
    }
}
