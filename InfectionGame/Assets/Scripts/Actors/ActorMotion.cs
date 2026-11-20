using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Actors
{
    public static class ActorMotion
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
}
