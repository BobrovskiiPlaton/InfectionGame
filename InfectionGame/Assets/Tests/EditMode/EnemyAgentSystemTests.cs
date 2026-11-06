using InfectionGame.Enemies.ECS;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Enemies.Tests
{
    /// <summary>
    /// ECS-исполнитель врага. Состояние — enableable-теги, которые включает behavior tree;
    /// система только исполняет, никаких переходов в ней быть не должно.
    /// </summary>
    public class EnemyAgentSystemTests
    {
        const float kTolerance = 1e-3f;

        World _world;
        EntityManager _em;
        SystemHandle _system;

        [SetUp]
        public void SetUp()
        {
            _world = new World("EnemyTests");
            _em = _world.EntityManager;
            _system = _world.CreateSystem<EnemyAgentSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
                _world.Dispose();

            _world = null;
        }

        void Run(float deltaTime = 1f / 60f)
        {
            _world.SetTime(new TimeData(elapsedTime: 0.0, deltaTime: deltaTime));
            _system.Update(_world.Unmanaged);
        }

        Entity CreateEnemy(
            float3 position,
            EnemyState state = EnemyState.Idle,
            float moveSpeed = 4f,
            float attackDistance = 2f,
            float attackInterval = 1f)
        {
            var entity = _em.CreateEntity();

            _em.AddComponentData(entity, LocalTransform.FromPosition(position));
            _em.AddComponentData(entity, new EnemyTag());
            _em.AddComponentData(entity, new EnemySpecies
            {
                MoveSpeed = moveSpeed,
                PassiveMoveSpeed = 1f,
                AttackDistance = attackDistance,
                AttackInterval = attackInterval,
                AttackDamage = 10f,
            });
            _em.AddComponentData(entity, new EnemyAgent());
            _em.AddComponentData(entity, new EnemyWanderTarget());
            _em.AddComponentData(entity, new InfectionGame.Perception.DetectionState());

            _em.AddComponent<EnemyStateIdle>(entity);
            _em.AddComponent<EnemyStateChase>(entity);
            _em.AddComponent<EnemyStateAttack>(entity);
            SetState(entity, state);

            return entity;
        }

        void SetState(Entity entity, EnemyState state)
        {
            _em.SetComponentEnabled<EnemyStateIdle>(entity, state == EnemyState.Idle);
            _em.SetComponentEnabled<EnemyStateChase>(entity, state == EnemyState.Chase);
            _em.SetComponentEnabled<EnemyStateAttack>(entity, state == EnemyState.Attack);
        }

        EnemyState StateOf(Entity entity)
        {
            if (_em.IsComponentEnabled<EnemyStateAttack>(entity))
                return EnemyState.Attack;

            if (_em.IsComponentEnabled<EnemyStateChase>(entity))
                return EnemyState.Chase;

            return EnemyState.Idle;
        }

        void SetPerception(Entity enemy, float3 targetPosition)
        {
            _em.SetComponentData(enemy, new InfectionGame.Perception.DetectionState
            {
                Primary = new Entity { Index = 1, Version = 1 },
                PrimaryFaction = InfectionGame.Perception.TargetFaction.Player,
                LastKnownPosition = targetPosition,
                Distance = math.distance(_em.GetComponentData<LocalTransform>(enemy).Position, targetPosition),
                TimeSinceSensed = 0f,
            });
        }

        /// <summary>Цель потеряна: PerceptionPrimarySelectJob оставляет DetectionState нулевым.</summary>
        void ClearPerception(Entity enemy)
        {
            _em.SetComponentData(enemy, new InfectionGame.Perception.DetectionState());
        }

        float3 PositionOf(Entity entity) => _em.GetComponentData<LocalTransform>(entity).Position;
        EnemyAgent AgentOf(Entity entity) => _em.GetComponentData<EnemyAgent>(entity);

        // --------------------------------------------------------------- погоня

        [Test]
        public void Chase_MovesTowardPerceivedTarget()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Chase);
            SetPerception(enemy, new float3(10f, 0f, 0f));

            Run(deltaTime: 0.5f);

            float3 position = PositionOf(enemy);
            Assert.That(position.x, Is.GreaterThan(0f), "враг должен двигаться к цели");
            Assert.That(position.x, Is.EqualTo(4f * 0.5f).Within(kTolerance),
                "шаг = скорость * время");
            Assert.That(position.z, Is.EqualTo(0f).Within(kTolerance));
        }

        [Test]
        public void Chase_TurnsTowardTarget()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Chase);
            SetPerception(enemy, new float3(0f, 0f, 10f));

            Run(deltaTime: 0.25f);

            float3 forward = math.forward(_em.GetComponentData<LocalTransform>(enemy).Rotation);
            Assert.That(forward.z, Is.GreaterThan(0.9f), "враг должен смотреть на цель");
        }

        [Test]
        public void Chase_WithoutTarget_StopsMovingAndKeepsState()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Chase);
            ClearPerception(enemy);

            Run(deltaTime: 0.5f);

            Assert.That(PositionOf(enemy).x, Is.EqualTo(0f).Within(kTolerance),
                "без цели двигаться некуда");
            Assert.That(StateOf(enemy), Is.EqualTo(EnemyState.Chase),
                "переходы состояний делает behavior tree, а не ECS-система");
        }

        // ---------------------------------------------------------------- удар

        [Test]
        public void Attack_InRange_DoesNotMove()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Attack, attackDistance: 2f);
            SetPerception(enemy, new float3(1.5f, 0f, 0f));

            Run(deltaTime: 0.5f);

            Assert.That(PositionOf(enemy).x, Is.EqualTo(0f).Within(kTolerance),
                "в радиусе удара враг стоит и бьёт");
        }

        [Test]
        public void Attack_InRange_ConsumesCooldown()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Attack, attackDistance: 2f, attackInterval: 1f);
            SetPerception(enemy, new float3(1.5f, 0f, 0f));

            Run(deltaTime: 0.1f);

            Assert.That(AgentOf(enemy).AttackCooldown, Is.GreaterThan(0f),
                "после удара должен включиться кулдаун");
            Assert.That(AgentOf(enemy).TimeSinceAttack, Is.EqualTo(0f).Within(kTolerance));
        }

        [Test]
        public void Attack_WhenTargetMovesAway_KeepsStateAndStaysPut()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Attack, attackDistance: 2f);
            SetPerception(enemy, new float3(10f, 0f, 0f));

            Run();

            Assert.That(StateOf(enemy), Is.EqualTo(EnemyState.Attack),
                "выходить из атаки — работа behavior tree");
            Assert.That(PositionOf(enemy).x, Is.EqualTo(0f).Within(kTolerance),
                "в Attack враг не двигается");
        }

        // ------------------------------------------------------------- брожение

        [Test]
        public void Idle_WithWanderTarget_MovesThereSlowly()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Idle, moveSpeed: 4f);

            _em.SetComponentData(enemy, new EnemyWanderTarget
            {
                Position = new float3(10f, 0f, 0f),
                HasTarget = 1f,
            });

            Run(deltaTime: 0.5f);

            float3 position = PositionOf(enemy);
            Assert.That(position.x, Is.EqualTo(1f * 0.5f).Within(kTolerance),
                "в Idle скорость пассивная, не боевая");
        }

        [Test]
        public void Idle_WithoutWanderTarget_StandsStill()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Idle);

            Run(deltaTime: 1f);

            Assert.That(PositionOf(enemy).x, Is.EqualTo(0f).Within(kTolerance));
        }

        [Test]
        public void Idle_WithPerceivedTarget_KeepsStateUntilGraphDecides()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Idle);
            SetPerception(enemy, new float3(5f, 0f, 0f));

            Run();

            Assert.That(StateOf(enemy), Is.EqualTo(EnemyState.Idle),
                "самостоятельно переходить в Chase система не должна — это делает граф");
        }

        [Test]
        public void Idle_WanderTarget_AccumulatesTimeSinceAssigned()
        {
            // На этом времени держится узел Choose Wander Target: без него он перебирает
            // точку каждый тик и враг дёргается.
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Idle);

            _em.SetComponentData(enemy, new EnemyWanderTarget
            {
                Position = new float3(10f, 0f, 0f),
                HasTarget = 1f,
            });

            Run(deltaTime: 0.5f);

            var wander = _em.GetComponentData<EnemyWanderTarget>(enemy);
            Assert.That(wander.TimeSinceAssigned, Is.EqualTo(0.5f).Within(kTolerance));
        }

        [Test]
        public void Idle_OnArrival_ClearsWanderTargetAndResetsTimer()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Idle);

            _em.SetComponentData(enemy, new EnemyWanderTarget
            {
                Position = new float3(0.1f, 0f, 0f),
                HasTarget = 1f,
            });

            Run(deltaTime: 1f);

            var wander = _em.GetComponentData<EnemyWanderTarget>(enemy);
            Assert.That(wander.HasTarget, Is.EqualTo(0f).Within(kTolerance),
                "дойдя до точки, враг должен её сбросить");
            Assert.That(wander.TimeSinceAssigned, Is.EqualTo(0f).Within(kTolerance));
        }

        // ------------------------------------------- система не трогает состояние

        [Test]
        public void ChaseJob_NeverSwitchesStateTags()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Chase);
            SetPerception(enemy, new float3(10f, 0f, 0f));

            Run(deltaTime: 0.5f);

            Assert.That(StateOf(enemy), Is.EqualTo(EnemyState.Chase));
        }

        [Test]
        public void WanderJob_NeverSwitchesStateTags()
        {
            Entity enemy = CreateEnemy(float3.zero, EnemyState.Idle);

            _em.SetComponentData(enemy, new EnemyWanderTarget
            {
                Position = new float3(0.1f, 0f, 0f),
                HasTarget = 1f,
            });

            Run(deltaTime: 1f);

            Assert.That(StateOf(enemy), Is.EqualTo(EnemyState.Idle),
                "даже достигнув точки, враг остаётся в Idle — новое состояние выдаст граф");
        }
    }
}
