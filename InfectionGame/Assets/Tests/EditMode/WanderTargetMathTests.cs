using InfectionGame.Actors;
using NUnit.Framework;
using Unity.Mathematics;

namespace InfectionGame.Actors.Tests
{
    /// <summary>
    /// Политика работы с <see cref="WanderTarget"/> — общий для врага и NPC. Здесь живёт
    /// защита от дёрганья (точка не перебирается, пока жива), поэтому именно эти тесты
    /// ловят возврат старого поведения.
    /// </summary>
    public class WanderTargetMathTests
    {
        const float kTolerance = 1e-4f;

        static WanderTarget Active(float timeSinceAssigned = 0f) => new WanderTarget
        {
            Position = new float3(10f, 0f, 0f),
            HasTarget = 1f,
            TimeSinceAssigned = timeSinceAssigned,
        };

        // ------------------------------------------------- нужно ли брать новую точку

        [Test]
        public void ShouldAllocate_WithoutTarget_Allocates()
        {
            var target = WanderTargetMath.Cleared();
            Assert.That(WanderTargetMath.ShouldAllocate(target, 10f), Is.True);
        }

        [Test]
        public void ShouldAllocate_WithLiveTarget_KeepsIt()
        {
            // Это и есть анти-дёрганье: граф вызывает узел каждый тик, но точку трогать нельзя.
            var target = Active(timeSinceAssigned: 1f);
            Assert.That(WanderTargetMath.ShouldAllocate(target, 10f), Is.False,
                "живую точку перебирать нельзя — актор будет дёргаться каждый кадр");
        }

        [Test]
        public void ShouldAllocate_AfterTimeout_Reallocates()
        {
            var target = Active(timeSinceAssigned: 10f);
            Assert.That(WanderTargetMath.ShouldAllocate(target, 10f), Is.True,
                "застоявшегося у стены актора надо освобождать");
        }

        [Test]
        public void ShouldAllocate_ZeroTimeout_NeverRestarts()
        {
            var target = Active(timeSinceAssigned: 1000f);
            Assert.That(WanderTargetMath.ShouldAllocate(target, 0f), Is.False,
                "0 — ждать достижения точки бесконечно");
            Assert.That(WanderTargetMath.ShouldAllocate(target, -1f), Is.False);
        }

        // ------------------------------------------------------------------ таймер

        [Test]
        public void Tick_WithTarget_Accumulates()
        {
            var target = Active();
            WanderTargetMath.Tick(ref target, 0.5f);
            Assert.That(target.TimeSinceAssigned, Is.EqualTo(0.5f).Within(kTolerance));
        }

        [Test]
        public void Tick_WithoutTarget_DoesNothing()
        {
            var target = WanderTargetMath.Cleared();
            WanderTargetMath.Tick(ref target, 0.5f);
            Assert.That(target.TimeSinceAssigned, Is.EqualTo(0f).Within(kTolerance));
        }

        // ---------------------------------------------------------------- прибытие

        [Test]
        public void HasArrived_CloseEnough_True()
        {
            var target = new WanderTarget { Position = new float3(0.1f, 0f, 0f), HasTarget = 1f };
            Assert.That(WanderTargetMath.HasArrived(float3.zero, target), Is.True);
        }

        [Test]
        public void HasArrived_Far_False()
        {
            var target = Active();
            Assert.That(WanderTargetMath.HasArrived(float3.zero, target), Is.False);
        }

        [Test]
        public void HasArrived_WithoutTarget_False()
        {
            var target = WanderTargetMath.Cleared();
            Assert.That(WanderTargetMath.HasArrived(float3.zero, target), Is.False,
                "без назначенной точки прибытия не бывает");
        }

        // ------------------------------------------------------- создание/сброс

        [Test]
        public void Create_SetsTargetAndZeroesTimer()
        {
            var target = WanderTargetMath.Create(new float3(3f, 0f, 4f));

            Assert.That(target.HasTarget, Is.EqualTo(1f).Within(kTolerance));
            Assert.That(target.TimeSinceAssigned, Is.EqualTo(0f).Within(kTolerance));
            Assert.That(target.Position.x, Is.EqualTo(3f).Within(kTolerance));
        }

        [Test]
        public void Cleared_ResetsEverything()
        {
            var target = WanderTargetMath.Cleared();

            Assert.That(target.HasTarget, Is.EqualTo(0f).Within(kTolerance));
            Assert.That(target.TimeSinceAssigned, Is.EqualTo(0f).Within(kTolerance));
        }

        // ------------------------------------------------------------- геометрия

        [Test]
        public void PointInDisk_AppliesRadiusAndKeepsHeight()
        {
            float3 point = WanderTargetMath.PointInDisk(
                new float3(1f, 5f, 2f), new float2(0.5f, -0.25f), radius: 4f);

            Assert.That(point.x, Is.EqualTo(1f + 2f).Within(kTolerance));
            Assert.That(point.y, Is.EqualTo(5f).Within(kTolerance), "высота не должна меняться");
            Assert.That(point.z, Is.EqualTo(2f - 1f).Within(kTolerance));
        }
    }
}
