using UnityEngine;

namespace InfectionGame.Actors
{
    public class ActorSpeciesSO : ScriptableObject
    {
        public float visionRange = 20f;

        [Range(0f, 360f)] public float fieldOfViewDegrees = 120f;

        public Vector3 eyeOffsetLocal = new Vector3(0f, 1.6f, 0f);

        [Min(0f)] public float hearingRadius = 4f;

        [Min(0f)] public float moveSpeed = 4f;

        [Min(0f)] public float passiveMoveSpeed = 1f;

        [Min(0f)] public float memoryDuration = 3f;

        public Unity.Behavior.BehaviorGraph behaviorGraph;
    }
}
