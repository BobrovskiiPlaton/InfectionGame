using Unity.Mathematics;

namespace InfectionGame.Actors
{
    public static class WanderTargetMath
    {
        public const float ArriveDistanceSq = 0.25f;

        public static bool ShouldAllocate(in WanderTarget target, float restartAfterSeconds)
        {
            if (target.HasTarget <= 0.5f)
                return true;

            if (restartAfterSeconds <= 0f)
                return false;

            return target.TimeSinceAssigned >= restartAfterSeconds;
        }

        public static void Tick(ref WanderTarget target, float deltaTime)
        {
            if (target.HasTarget > 0.5f)
                target.TimeSinceAssigned += deltaTime;
        }

        public static bool HasArrived(float3 self, in WanderTarget target)
        {
            return target.HasTarget > 0.5f
                   && math.distancesq(self, target.Position) < ArriveDistanceSq;
        }

        public static WanderTarget Create(float3 position)
        {
            return new WanderTarget
            {
                Position = position,
                HasTarget = 1f,
                TimeSinceAssigned = 0f,
            };
        }

        public static WanderTarget Cleared()
        {
            return new WanderTarget
            {
                Position = float3.zero,
                HasTarget = 0f,
                TimeSinceAssigned = 0f,
            };
        }

        public static float3 PointInDisk(float3 center, float2 unitCircleOffset, float radius)
        {
            return new float3(
                center.x + unitCircleOffset.x * radius,
                center.y,
                center.z + unitCircleOffset.y * radius);
        }
    }
}
