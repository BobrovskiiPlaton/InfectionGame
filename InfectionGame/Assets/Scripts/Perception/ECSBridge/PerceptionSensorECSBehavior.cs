using Unity.Entities;
using UnityEngine;

namespace InfectionGame.Perception
{
    public class PerceptionSensorECSBehavior : MonoBehaviour
    {
        [SerializeField] private TargetFaction targets = TargetFaction.Player | TargetFaction.NPC;

        [SerializeField, Min(0f)] private float memoryDuration = 0.5f;

        [SerializeField] private bool visionEnabled = true;

        [SerializeField, Min(0f)] private float visionRange = 20f;

        [SerializeField, Range(0f, 360f)] private float fieldOfViewDegrees = 120f;

        [SerializeField] private Vector3 eyeOffsetLocal = new Vector3(0f, 1.6f, 0f);

        sealed class Baker : Baker<PerceptionSensorECSBehavior>
        {
            public override void Bake(PerceptionSensorECSBehavior authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new DetectionSensor
                {
                    Targets = authoring.targets,
                    MemoryDuration = authoring.memoryDuration,
                });

                AddComponent(entity, new VisionSensor
                {
                    Range = authoring.visionRange,
                    FieldOfViewDegrees = authoring.fieldOfViewDegrees,
                    EyeOffsetLocal = authoring.eyeOffsetLocal,
                });

                SetComponentEnabled<VisionSensor>(entity, authoring.visionEnabled);

                AddBuffer<DetectedTarget>(entity);
                AddComponent(entity, new DetectionState());
            }
        }
    }
}
