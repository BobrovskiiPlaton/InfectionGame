using NUnit.Framework;
using Unity.Mathematics;

namespace InfectionGame.Perception.Tests
{
    public class PerceptionMathTests
    {
        const float kTolerance = 1e-4f;
        const float kRange = 20f;

        static readonly float3 kForward = new float3(0f, 0f, 1f);
        static readonly float3 kEye = float3.zero;

        static float3 DirectionAtDegrees(float degreesFromForward)
        {
            float rad = math.radians(degreesFromForward);
            return new float3(math.sin(rad), 0f, math.cos(rad));
        }

        static float3 PointAt(float degreesFromForward, float distance)
        {
            return DirectionAtDegrees(degreesFromForward) * distance;
        }

        // ---------------------------------------------------------------- VisionCone

        [Test]
        public void VisionCone_FromDegrees_120_IsCosineOfHalfAngle()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.That(cone.HalfAngleCos,
                Is.EqualTo(math.cos(math.radians(60f))).Within(kTolerance));
        }

        [Test]
        public void VisionCone_FromDegrees_ClampsBelowZeroToZero()
        {
            Assert.That(PerceptionMath.VisionCone.FromDegrees(-90f).HalfAngleCos,
                Is.EqualTo(PerceptionMath.VisionCone.FromDegrees(0f).HalfAngleCos).Within(kTolerance));
        }

        [Test]
        public void VisionCone_FromDegrees_ClampsAbove360To360()
        {
            Assert.That(PerceptionMath.VisionCone.FromDegrees(400f).HalfAngleCos,
                Is.EqualTo(PerceptionMath.VisionCone.FromDegrees(360f).HalfAngleCos).Within(kTolerance));
        }

        // ------------------------------------------------------------- IsInsideCone

        [Test]
        public void IsInsideCone_StraightAhead_Passes()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.IsTrue(PerceptionMath.IsInsideCone(kForward, kForward, cone));
        }

        [Test]
        public void IsInsideCone_JustInsideHalfAngle_Passes()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.IsTrue(PerceptionMath.IsInsideCone(DirectionAtDegrees(59.9f), kForward, cone));
        }

        [Test]
        public void IsInsideCone_JustOutsideHalfAngle_Fails()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.IsFalse(PerceptionMath.IsInsideCone(DirectionAtDegrees(60.1f), kForward, cone));
        }

        [Test]
        public void IsInsideCone_Behind_Fails()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.IsFalse(PerceptionMath.IsInsideCone(DirectionAtDegrees(180f), kForward, cone));
        }

        [Test]
        public void IsInsideCone_FullCircle_SeesStraightBehind()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(360f);

            Assert.IsTrue(PerceptionMath.IsInsideCone(DirectionAtDegrees(180f), kForward, cone));
        }

        [Test]
        public void IsInsideCone_ZeroDegrees_OnlyExactForwardPasses()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(0f);

            Assert.IsTrue(PerceptionMath.IsInsideCone(kForward, kForward, cone));
            Assert.IsFalse(PerceptionMath.IsInsideCone(DirectionAtDegrees(0.1f), kForward, cone));
        }

        [Test]
        public void IsInsideCone_DegenerateForward_NeverPasses()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(360f);

            Assert.IsFalse(PerceptionMath.IsInsideCone(kForward, float3.zero, cone));
        }

        [Test]
        public void IsInsideCone_DegenerateDirection_NeverPasses()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(360f);

            Assert.IsFalse(PerceptionMath.IsInsideCone(float3.zero, kForward, cone));
        }

        [Test]
        public void IsInsideCone_IsScaleInvariant()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(90f);

            float3 direction = DirectionAtDegrees(44f);

            Assert.IsTrue(PerceptionMath.IsInsideCone(direction, kForward, cone));
            Assert.IsTrue(PerceptionMath.IsInsideCone(direction * 1000f, kForward * 0.001f, cone));
        }

        // ------------------------------------------------------------------ CanSee

        [Test]
        public void CanSee_TargetInConeAtRange_Passes()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.IsTrue(PerceptionMath.CanSee(kEye, kForward, PointAt(0f, kRange), kRange, cone));
        }

        [Test]
        public void CanSee_TargetBeyondRange_Fails()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(360f);

            Assert.IsFalse(PerceptionMath.CanSee(kEye, kForward, PointAt(0f, kRange + 0.01f), kRange, cone));
        }

        [Test]
        public void CanSee_TargetBehindButInRange_Fails()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.IsFalse(PerceptionMath.CanSee(kEye, kForward, PointAt(180f, 5f), kRange, cone));
        }

        [Test]
        public void CanSee_ZeroRange_SeesNothing()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(360f);

            Assert.IsFalse(PerceptionMath.CanSee(kEye, kForward, kForward, 0f, cone));
        }

        [Test]
        public void CanSee_TargetSittingOnTheEye_Passes()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);

            Assert.IsTrue(PerceptionMath.CanSee(kEye, kForward, kEye, kRange, cone));
        }

        [Test]
        public void CanSee_UsesTheEyePositionNotTheOrigin()
        {
            var cone = PerceptionMath.VisionCone.FromDegrees(120f);
            float3 eye = new float3(0f, 0f, 10f);

            // 5 units in front of the eye, but 15 units behind the world origin.
            Assert.IsTrue(PerceptionMath.CanSee(eye, kForward, new float3(0f, 0f, 15f), kRange, cone));
            // 5 units behind the eye.
            Assert.IsFalse(PerceptionMath.CanSee(eye, kForward, new float3(0f, 0f, 5f), kRange, cone));
        }

        // ------------------------------------------------------------ IsInteresting

        [Test]
        public void IsInteresting_MatchingFaction_IsTrue()
        {
            Assert.IsTrue(PerceptionMath.IsInteresting(TargetFaction.NPC, TargetFaction.NPC));
        }

        [Test]
        public void IsInteresting_OtherFaction_IsFalse()
        {
            Assert.IsFalse(PerceptionMath.IsInteresting(TargetFaction.NPC, TargetFaction.Enemy));
        }

        [Test]
        public void IsInteresting_EmptyMask_MatchesNothing()
        {
            Assert.IsFalse(PerceptionMath.IsInteresting(TargetFaction.None, TargetFaction.Player));
        }

        [Test]
        public void IsInteresting_CombinedMask_MatchesEverySelectedFaction()
        {
            var mask = TargetFaction.Player | TargetFaction.NPC;

            Assert.IsTrue(PerceptionMath.IsInteresting(mask, TargetFaction.Player));
            Assert.IsTrue(PerceptionMath.IsInteresting(mask, TargetFaction.NPC));
            Assert.IsFalse(PerceptionMath.IsInteresting(mask, TargetFaction.Enemy));
        }

        [Test]
        public void IsInteresting_AllMask_MatchesEveryFaction()
        {
            Assert.IsTrue(PerceptionMath.IsInteresting(TargetFaction.All, TargetFaction.Player));
            Assert.IsTrue(PerceptionMath.IsInteresting(TargetFaction.All, TargetFaction.NPC));
            Assert.IsTrue(PerceptionMath.IsInteresting(TargetFaction.All, TargetFaction.Enemy));
        }

        // ------------------------------------------------------------- TickMemory

        [Test]
        public void TickMemory_SensedNow_ResetsClockAndKeeps()
        {
            float timeSince = 3.5f;

            bool kept = PerceptionMath.TickMemory(ref timeSince, true, 0.1f, 0.5f);

            Assert.IsTrue(kept);
            Assert.That(timeSince, Is.EqualTo(0f).Within(kTolerance));
        }

        [Test]
        public void TickMemory_NotSensed_AccumulatesElapsedTime()
        {
            float timeSince = 0f;

            PerceptionMath.TickMemory(ref timeSince, false, 0.1f, 10f);
            PerceptionMath.TickMemory(ref timeSince, false, 0.2f, 10f);

            Assert.That(timeSince, Is.EqualTo(0.3f).Within(kTolerance));
        }

        [Test]
        public void TickMemory_KeepsTargetUpToAndIncludingTheDuration()
        {
            float timeSince = 0f;

            Assert.IsTrue(PerceptionMath.TickMemory(ref timeSince, false, 0.5f, 0.5f));
        }

        [Test]
        public void TickMemory_DropsTargetOnceTheDurationIsExceeded()
        {
            float timeSince = 0f;

            PerceptionMath.TickMemory(ref timeSince, false, 0.5f, 0.5f);
            bool kept = PerceptionMath.TickMemory(ref timeSince, false, 0.1f, 0.5f);

            Assert.IsFalse(kept);
            Assert.That(timeSince, Is.EqualTo(0.6f).Within(kTolerance));
        }

        [Test]
        public void TickMemory_ZeroDuration_ForgetsWithoutMemory()
        {
            float timeSince = 0f;

            PerceptionMath.TickMemory(ref timeSince, false, 0.01f, 0f);

            Assert.IsFalse(PerceptionMath.TickMemory(ref timeSince, false, 0.01f, 0f));
        }

        [Test]
        public void TickMemory_NegativeDeltaTime_IsClampedToZero()
        {
            float timeSince = 0.2f;

            bool kept = PerceptionMath.TickMemory(ref timeSince, false, -5f, 1f);

            Assert.IsTrue(kept);
            Assert.That(timeSince, Is.EqualTo(0.2f).Within(kTolerance));
        }
    }
}
