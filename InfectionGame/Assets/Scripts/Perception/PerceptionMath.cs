using Unity.Mathematics;

namespace InfectionGame.Perception
{
    public static class PerceptionMath
    {
        const float kDegenerateSqr = 1e-8f;

        public readonly struct VisionCone
        {
            public readonly float HalfAngleCos;

            public readonly bool IsFullCircle;

            public VisionCone(float halfAngleCos, bool isFullCircle)
            {
                HalfAngleCos = halfAngleCos;
                IsFullCircle = isFullCircle;
            }

            public static VisionCone FromDegrees(float fieldOfViewDegrees)
            {
                float clamped = math.clamp(fieldOfViewDegrees, 0f, 360f);
                return new VisionCone(
                    math.cos(math.radians(clamped) * 0.5f),
                    isFullCircle: clamped >= 360f);
            }
        }

        public static bool IsInteresting(TargetFaction interestMask, TargetFaction targetFaction)
        {
            return (interestMask & targetFaction) != 0;
        }

        public static bool IsInsideCone(float3 direction, float3 forward, in VisionCone cone)
        {
            float dirSqr = math.lengthsq(direction);
            if (dirSqr <= kDegenerateSqr)
                return false;

            float fwdSqr = math.lengthsq(forward);
            if (fwdSqr <= kDegenerateSqr)
                return false;

            if (cone.IsFullCircle)
                return true;

            float cos = math.dot(direction, forward) * math.rsqrt(dirSqr * fwdSqr);
            return cos >= cone.HalfAngleCos;
        }

        public static bool CanSee(
            float3 eye,
            float3 forward,
            float3 targetPosition,
            float range,
            in VisionCone cone)
        {
            if (range <= 0f)
                return false;

            float3 to = targetPosition - eye;
            float distSqr = math.lengthsq(to);

            if (distSqr > range * range)
                return false;

            if (distSqr <= kDegenerateSqr)
                return true;

            return IsInsideCone(to, forward, cone);
        }

        // Возвращает false когда target был забыт
        public static bool TickMemory(
            ref float timeSinceSensed,
            bool sensedNow,
            float deltaTime,
            float memoryDuration)
        {
            if (sensedNow)
            {
                timeSinceSensed = 0f;
                return true;
            }

            timeSinceSensed += math.max(0f, deltaTime);
            return timeSinceSensed <= math.max(0f, memoryDuration);
        }

        public static bool IsHearable(
            float3 listenerPosition,
            float3 targetPosition,
            float hearingRadius)
        {
            if (hearingRadius <= 0f)
                return false;

            return math.distancesq(listenerPosition, targetPosition)
                   <= hearingRadius * hearingRadius;
        }
    }
}
