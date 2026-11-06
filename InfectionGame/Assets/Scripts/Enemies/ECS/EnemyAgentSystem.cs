using InfectionGame.Actors;
using InfectionGame.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Enemies.ECS
{
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
            if (perception.Primary == Entity.Null)
                return;

            ActorMotion.MoveTowards(
                ref transform, perception.LastKnownPosition, species.MoveSpeed, DeltaTime);
        }
    }

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
