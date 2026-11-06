using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Perception.Tests
{
    /// <summary>
    /// Слух: всенаправленный сенсор по расстоянию. Именно он ловит цель, подкравшуюся
    /// со спины — зрение её не видит.
    /// </summary>
    public class HearingDetectionTests
    {
        const float kTolerance = 1e-3f;

        World _world;
        EntityManager _em;
        SystemHandle _system;

        [SetUp]
        public void SetUp()
        {
            _world = new World("HearingTests");
            _em = _world.EntityManager;
            _system = _world.CreateSystem<VisionDetectionSystem>();
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

        Entity CreateSensor(
            float hearingRadius,
            float visionRange = 0f,
            float fieldOfViewDegrees = 120f,
            float memoryDuration = 0f,
            TargetFaction targets = TargetFaction.All)
        {
            var entity = _em.CreateEntity();

            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            _em.AddComponentData(entity, new DetectionSensor
            {
                Targets = targets,
                MemoryDuration = memoryDuration,
            });
            _em.AddComponentData(entity, new VisionSensor
            {
                Range = visionRange,
                FieldOfViewDegrees = fieldOfViewDegrees,
                EyeOffsetLocal = float3.zero,
            });
            _em.AddComponentData(entity, new HearingSensor
            {
                Radius = hearingRadius,
            });
            _em.AddComponentData(entity, new DetectionState());
            _em.AddBuffer<DetectedTarget>(entity);

            return entity;
        }

        Entity CreateTarget(float3 position, TargetFaction faction = TargetFaction.NPC)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, LocalTransform.FromPosition(position));
            _em.AddComponentData(entity, new DetectionTarget { Faction = faction });

            return entity;
        }

        DynamicBuffer<DetectedTarget> DetectedOf(Entity sensor) => _em.GetBuffer<DetectedTarget>(sensor);
        DetectionState StateOf(Entity sensor) => _em.GetComponentData<DetectionState>(sensor);

        // ------------------------------------------------------- цель со спины

        [Test]
        public void TargetBehindEnemy_InsideHearingRadius_IsHeard()
        {
            // Сенсор смотрит в +Z, цель стоит ровно за спиной. Зрения нет вовсе.
            Entity sensor = CreateSensor(hearingRadius: 4f, visionRange: 0f);
            Entity target = CreateTarget(new float3(0f, 0f, -3f));

            Run();

            var detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].Target, Is.EqualTo(target));
            Assert.That(detected[0].SensedBy, Is.EqualTo(SenseKind.Hearing));
        }

        [Test]
        public void TargetBehindEnemy_InsideHearingRadius_IsHeardEvenWithVisionOn()
        {
            // Зрение включено, но цель ровно за спиной (вне конуса FOV 120) —
            // её приносит только слух.
            Entity sensor = CreateSensor(hearingRadius: 4f, visionRange: 20f, fieldOfViewDegrees: 120f);
            CreateTarget(new float3(0f, 0f, -3f));

            Run();

            var detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].SensedBy, Is.EqualTo(SenseKind.Hearing),
                "цель за спиной должна ловиться слухом, а не зрением");
        }

        [Test]
        public void TargetBehindEnemy_BeyondHearingRadius_IsNotHeard()
        {
            Entity sensor = CreateSensor(hearingRadius: 4f, visionRange: 20f, fieldOfViewDegrees: 120f);
            CreateTarget(new float3(0f, 0f, -6f));

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0),
                "дальше радиуса слуха и за спиной цель не должна замечаться");
        }

        [Test]
        public void TargetExactlyAtHearingRadius_IsHeard()
        {
            // Граница инклюзивная: ровно на радиусе цель ещё слышна.
            Entity sensor = CreateSensor(hearingRadius: 4f);
            CreateTarget(new float3(0f, 0f, -4f));

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(1));
        }

        [Test]
        public void ZeroHearingRadius_NeverDetects()
        {
            Entity sensor = CreateSensor(hearingRadius: 0f);
            CreateTarget(new float3(0f, 0f, 0.5f));

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0),
                "нулевой радиус = глухой сенсор");
        }

        // ------------------------------------------------- зрение + слух вместе

        [Test]
        public void TargetInFrontAndClose_IsMarkedBothSenses()
        {
            Entity sensor = CreateSensor(hearingRadius: 4f, visionRange: 20f, fieldOfViewDegrees: 120f);
            CreateTarget(new float3(0f, 0f, 2f));

            Run();

            var detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].SensedBy, Is.EqualTo(SenseKind.Vision | SenseKind.Hearing));
        }

        [Test]
        public void TargetInFrontButOutOfHearing_VisionOnly()
        {
            Entity sensor = CreateSensor(hearingRadius: 3f, visionRange: 20f, fieldOfViewDegrees: 120f);
            CreateTarget(new float3(0f, 0f, 10f));

            Run();

            var detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].SensedBy, Is.EqualTo(SenseKind.Vision));
        }

        [Test]
        public void HearingAndVision_AgreeOnDistance()
        {
            // Слух считает дистанцию до тела, зрение — до глаз. При нулевом смещении
            // глаз оба числа должны совпасть.
            Entity sensor = CreateSensor(hearingRadius: 5f, visionRange: 20f);
            CreateTarget(new float3(0f, 0f, 3f));

            Run();

            var detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].Distance, Is.EqualTo(3f).Within(kTolerance));
        }

        // ------------------------------------------------------------- фракции

        [Test]
        public void Hearing_RespectsFactionFilter()
        {
            Entity sensor = CreateSensor(hearingRadius: 4f, targets: TargetFaction.Player);
            CreateTarget(new float3(0f, 0f, -2f), TargetFaction.NPC);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0),
                "слух должен применять тот же фракционный фильтр, что и зрение");
        }

        // -------------------------------------------------------------- память

        [Test]
        public void HeardTarget_FadesFromMemory_AfterLeavingRadius()
        {
            Entity sensor = CreateSensor(hearingRadius: 4f, memoryDuration: 0.5f);
            Entity target = CreateTarget(new float3(0f, 0f, -2f));

            Run();
            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(1));

            // Уводим цель за радиус — остаётся в памяти.
            _em.SetComponentData(target, LocalTransform.FromPosition(new float3(0f, 0f, -50f)));
            Run(deltaTime: 0.1f);

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(1),
                "цель должна помниться после потери слуха");
            Assert.That(DetectedOf(sensor)[0].SensedBy, Is.EqualTo(SenseKind.None),
                "запомненная цель не считается услышанной в этом кадре");

            // Память истекла.
            Run(deltaTime: 1f);
            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        [Test]
        public void PrimaryTarget_PrefersTheCloserOne()
        {
            Entity sensor = CreateSensor(hearingRadius: 10f);
            CreateTarget(new float3(0f, 0f, -8f));
            CreateTarget(new float3(0f, 0f, -2f));

            Run();

            var state = StateOf(sensor);
            Assert.That(state.Distance, Is.EqualTo(2f).Within(kTolerance),
                "primary должна быть ближайшая цель");
        }
    }
}
