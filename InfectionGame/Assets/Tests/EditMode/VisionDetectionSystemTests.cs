using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Perception.Tests
{
    public class VisionDetectionSystemTests
    {
        const float kTolerance = 1e-3f;

        World _world;
        EntityManager _em;
        SystemHandle _system;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PerceptionTests");
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
            TargetFaction targets,
            float range = 20f,
            float fieldOfViewDegrees = 120f,
            float memoryDuration = 0f,
            float3 eyeOffset = default)
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
                Range = range,
                FieldOfViewDegrees = fieldOfViewDegrees,
                EyeOffsetLocal = eyeOffset,
            });
            _em.AddComponentData(entity, new DetectionState());
            _em.AddBuffer<DetectedTarget>(entity);

            return entity;
        }

        Entity CreateTarget(float3 position, TargetFaction faction)
        {
            var entity = _em.CreateEntity();

            _em.AddComponentData(entity, LocalTransform.FromPosition(position));
            _em.AddComponentData(entity, new DetectionTarget { Faction = faction });

            return entity;
        }

        DynamicBuffer<DetectedTarget> DetectedOf(Entity sensor)
        {
            return _em.GetBuffer<DetectedTarget>(sensor);
        }

        DetectionState StateOf(Entity sensor)
        {
            return _em.GetComponentData<DetectionState>(sensor);
        }

        static float3 PointAt(float degreesFromForward, float distance)
        {
            float rad = math.radians(degreesFromForward);
            return new float3(math.sin(rad), 0f, math.cos(rad)) * distance;
        }

        // ------------------------------------------------------------- angle / range

        [Test]
        public void TargetInsideCone_IsDetected()
        {
            Entity sensor = CreateSensor(TargetFaction.All);
            Entity target = CreateTarget(new float3(0f, 0f, 10f), TargetFaction.NPC);

            Run();

            DynamicBuffer<DetectedTarget> detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].Target, Is.EqualTo(target));
            Assert.That(detected[0].Distance, Is.EqualTo(10f).Within(kTolerance));
            Assert.That(detected[0].SensedBy, Is.EqualTo(SenseKind.Vision));
            Assert.That(detected[0].TimeSinceSensed, Is.EqualTo(0f).Within(kTolerance));
        }

        [Test]
        public void TargetBehindSensor_IsNotDetected()
        {
            Entity sensor = CreateSensor(TargetFaction.All);
            CreateTarget(new float3(0f, 0f, -10f), TargetFaction.NPC);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
            Assert.That(StateOf(sensor).Primary, Is.EqualTo(Entity.Null));
        }

        [Test]
        public void TargetJustInsideTheCone_IsDetected()
        {
            Entity sensor = CreateSensor(TargetFaction.All, fieldOfViewDegrees: 120f);
            Entity target = CreateTarget(PointAt(59.9f, 10f), TargetFaction.NPC);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(1));
            Assert.That(DetectedOf(sensor)[0].Target, Is.EqualTo(target));
        }

        [Test]
        public void TargetJustOutsideTheCone_IsNotDetected()
        {
            Entity sensor = CreateSensor(TargetFaction.All, fieldOfViewDegrees: 120f);
            CreateTarget(PointAt(60.1f, 10f), TargetFaction.NPC);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        [Test]
        public void FullCircleVision_DetectsTargetBehind()
        {
            Entity sensor = CreateSensor(TargetFaction.All, fieldOfViewDegrees: 360f);
            CreateTarget(new float3(0f, 0f, -10f), TargetFaction.NPC);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(1));
        }

        [Test]
        public void TargetBeyondRange_IsNotDetected()
        {
            Entity sensor = CreateSensor(TargetFaction.All, range: 20f);
            CreateTarget(new float3(0f, 0f, 20.1f), TargetFaction.NPC);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        [Test]
        public void EyeOffset_MovesTheObservationPoint()
        {
            Entity sensor = CreateSensor(
                TargetFaction.All, range: 20f, fieldOfViewDegrees: 120f,
                memoryDuration: 0f, eyeOffset: new float3(0f, 0f, 5f));

            CreateTarget(new float3(0f, 0f, 3f), TargetFaction.NPC);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        // ------------------------------------------------------------------- filters

        [Test]
        public void FactionFilter_IgnoresUnwantedTeams()
        {
            Entity sensor = CreateSensor(TargetFaction.NPC);
            CreateTarget(new float3(0f, 0f, 10f), TargetFaction.Enemy);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        [Test]
        public void FactionFilter_WatchesSeveralTeamsAtOnce()
        {
            Entity sensor = CreateSensor(TargetFaction.Player | TargetFaction.NPC);
            Entity npc = CreateTarget(new float3(2f, 0f, 10f), TargetFaction.NPC);
            Entity player = CreateTarget(new float3(-2f, 0f, 10f), TargetFaction.Player);
            CreateTarget(new float3(0f, 0f, 8f), TargetFaction.Enemy);

            Run();

            DynamicBuffer<DetectedTarget> detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(2));

            var seen = new Entity[detected.Length];
            for (int i = 0; i < detected.Length; i++)
                seen[i] = detected[i].Target;

            Assert.That(seen, Does.Contain(npc));
            Assert.That(seen, Does.Contain(player));
        }

        [Test]
        public void DisabledTarget_IsInvisibleToEverySensor()
        {
            Entity sensor = CreateSensor(TargetFaction.All);
            Entity target = CreateTarget(new float3(0f, 0f, 10f), TargetFaction.NPC);
            _em.SetComponentEnabled<DetectionTarget>(target, false);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        [Test]
        public void DisabledVisionSensor_DetectsNothing()
        {
            Entity sensor = CreateSensor(TargetFaction.All);
            CreateTarget(new float3(0f, 0f, 10f), TargetFaction.NPC);
            _em.SetComponentEnabled<VisionSensor>(sensor, false);

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
            Assert.That(StateOf(sensor).Primary, Is.EqualTo(Entity.Null));
        }

        [Test]
        public void Sensor_NeverPerceivesItself()
        {
            Entity sensor = CreateSensor(TargetFaction.All, fieldOfViewDegrees: 360f);
            _em.AddComponentData(sensor, new DetectionTarget { Faction = TargetFaction.Enemy });

            Run();

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        // --------------------------------------------------------------- primary pick

        [Test]
        public void Primary_IsTheNearestCurrentlyPerceivedTarget()
        {
            Entity sensor = CreateSensor(TargetFaction.All);
            CreateTarget(new float3(0f, 0f, 12f), TargetFaction.NPC);
            Entity nearest = CreateTarget(new float3(0f, 0f, 4f), TargetFaction.Player);

            Run();

            DetectionState state = StateOf(sensor);
            Assert.That(state.Primary, Is.EqualTo(nearest));
            Assert.That(state.PrimaryFaction, Is.EqualTo(TargetFaction.Player));
            Assert.That(state.Distance, Is.EqualTo(4f).Within(kTolerance));
        }

        [Test]
        public void Primary_IsNullWhenNothingIsKnown()
        {
            Entity sensor = CreateSensor(TargetFaction.All);

            Run();

            DetectionState state = StateOf(sensor);
            Assert.That(state.Primary, Is.EqualTo(Entity.Null));
            Assert.That(state.PrimaryFaction, Is.EqualTo(TargetFaction.None));
        }

        // -------------------------------------------------------------------- memory

        [Test]
        public void Memory_KeepsLostTargetUntilTheDurationExpires()
        {
            Entity sensor = CreateSensor(TargetFaction.All, memoryDuration: 0.5f);
            Entity target = CreateTarget(new float3(0f, 0f, 10f), TargetFaction.NPC);

            Run(deltaTime: 0.1f);
            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(1));

            // The target leaves the cone; the sensor should keep remembering it.
            _em.SetComponentData(target, LocalTransform.FromPosition(new float3(0f, 0f, -10f)));

            Run(deltaTime: 0.1f);
            DynamicBuffer<DetectedTarget> detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].SensedBy, Is.EqualTo(SenseKind.None));
            Assert.That(detected[0].TimeSinceSensed, Is.EqualTo(0.1f).Within(kTolerance));

            // ...and keep it right up to the memory boundary (0.5s is still remembered)...
            Run(deltaTime: 0.1f);
            Run(deltaTime: 0.1f);
            Run(deltaTime: 0.1f);
            Run(deltaTime: 0.1f);

            detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].TimeSinceSensed, Is.EqualTo(0.5f).Within(kTolerance));

            // ...and forget it once the memory runs out (0.6s > 0.5s).
            Run(deltaTime: 0.1f);

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
        }

        [Test]
        public void Memory_LostTargetRemainsThePrimaryWhileItIsRemembered()
        {
            Entity sensor = CreateSensor(TargetFaction.All, memoryDuration: 1f);
            Entity target = CreateTarget(new float3(0f, 0f, 10f), TargetFaction.NPC);

            Run(deltaTime: 0.1f);
            _em.SetComponentData(target, LocalTransform.FromPosition(new float3(0f, 0f, -10f)));

            Run(deltaTime: 0.1f);

            DetectionState state = StateOf(sensor);
            Assert.That(state.Primary, Is.EqualTo(target));
            Assert.That(state.TimeSinceSensed, Is.EqualTo(0.1f).Within(kTolerance));
            // Last known position is where it was last perceived, not where it went.
            Assert.That(state.LastKnownPosition.z, Is.EqualTo(10f).Within(kTolerance));
        }

        [Test]
        public void Memory_ZeroDuration_ForgetsWithoutMemory()
        {
            Entity sensor = CreateSensor(TargetFaction.All, memoryDuration: 0f);
            Entity target = CreateTarget(new float3(0f, 0f, 10f), TargetFaction.NPC);

            Run(deltaTime: 0.1f);
            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(1));

            _em.SetComponentData(target, LocalTransform.FromPosition(new float3(0f, 0f, -10f)));

            Run(deltaTime: 0.1f);

            Assert.That(DetectedOf(sensor).Length, Is.EqualTo(0));
            Assert.That(StateOf(sensor).Primary, Is.EqualTo(Entity.Null));
        }

        [Test]
        public void Memory_TargetComingBackResetsTheClock()
        {
            Entity sensor = CreateSensor(TargetFaction.All, memoryDuration: 1f);
            Entity target = CreateTarget(new float3(0f, 0f, 10f), TargetFaction.NPC);

            Run(deltaTime: 0.1f);
            _em.SetComponentData(target, LocalTransform.FromPosition(new float3(0f, 0f, -10f)));
            Run(deltaTime: 0.1f);
            _em.SetComponentData(target, LocalTransform.FromPosition(new float3(0f, 0f, 10f)));

            Run(deltaTime: 0.1f);

            DynamicBuffer<DetectedTarget> detected = DetectedOf(sensor);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].TimeSinceSensed, Is.EqualTo(0f).Within(kTolerance));
            Assert.That(detected[0].SensedBy, Is.EqualTo(SenseKind.Vision));
        }

        // ---------------------------------------------------------------- reusability

        [Test]
        public void SameComponentsWorkForAnNpcFleeingFromEnemies()
        {
            // Nothing enemy-specific: an NPC is just a sensor with a different interest mask.
            Entity npc = CreateSensor(TargetFaction.Enemy, range: 15f, fieldOfViewDegrees: 360f);
            Entity enemy = CreateTarget(new float3(0f, 0f, 8f), TargetFaction.Enemy);
            CreateTarget(new float3(0f, 0f, 6f), TargetFaction.NPC);

            Run();

            DynamicBuffer<DetectedTarget> detected = DetectedOf(npc);
            Assert.That(detected.Length, Is.EqualTo(1));
            Assert.That(detected[0].Target, Is.EqualTo(enemy));
            Assert.That(StateOf(npc).Primary, Is.EqualTo(enemy));
        }
    }
}
