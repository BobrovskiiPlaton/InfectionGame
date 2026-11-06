using NUnit.Framework;
using Unity.Entities;

namespace InfectionGame.Perception.Tests
{
    /// <summary>
    /// Проверяет, что detection-система реально попадает в default ECS world — именно он
    /// поднимается при запуске игры. Без этого тесты зелёные, а в игре конус мёртвый.
    /// </summary>
    public class DefaultWorldBootTests
    {
        [Test]
        public void VisionDetectionSystem_IsAutoCreatedInTheDefaultWorld()
        {
            using (World world = DefaultWorldInitialization.Initialize("PerceptionDefaultWorldCheck"))
            {
                SystemHandle handle = world.GetExistingSystem<VisionDetectionSystem>();

                Assert.That(handle, Is.Not.EqualTo(default(SystemHandle)),
                    "VisionDetectionSystem не добавилась в default world — проверь [WorldSystemFilter] и сборку");
            }
        }

        [Test]
        public void DefaultWorld_CanRunTheVisionPipelineEndToEnd()
        {
            using (World world = DefaultWorldInitialization.Initialize("PerceptionEndToEnd"))
            {
                EntityManager em = world.EntityManager;

                Entity sensor = em.CreateEntity();
                em.AddComponentData(sensor, Unity.Transforms.LocalTransform.FromPosition(float3zero()));
                em.AddComponentData(sensor, new DetectionSensor { Targets = TargetFaction.Player, MemoryDuration = 0.5f });
                em.AddComponentData(sensor, new VisionSensor { Range = 20f, FieldOfViewDegrees = 120f, EyeOffsetLocal = float3zero() });
                em.AddComponentData(sensor, new DetectionState());
                em.AddBuffer<DetectedTarget>(sensor);

                Entity target = em.CreateEntity();
                em.AddComponentData(target, Unity.Transforms.LocalTransform.FromPosition(new Unity.Mathematics.float3(0f, 0f, 10f)));
                em.AddComponentData(target, new DetectionTarget { Faction = TargetFaction.Player });

                SystemHandle handle = world.GetExistingSystem<VisionDetectionSystem>();
                handle.Update(world.Unmanaged);

                var detected = em.GetBuffer<DetectedTarget>(sensor);
                Assert.That(detected.Length, Is.EqualTo(1),
                    "default world прогнал кадр, но цель не обнаружена");
            }
        }

        static Unity.Mathematics.float3 float3zero() => Unity.Mathematics.float3.zero;
    }
}
