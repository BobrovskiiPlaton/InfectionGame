using Unity.Entities;
using UnityEngine;

namespace InfectionGame.Perception
{
    public class PerceptionTargetECSBehavior : MonoBehaviour
    {
        [SerializeField] private TargetFaction faction = TargetFaction.NPC;

        // для infected можно выставить false здесь
        [SerializeField] private bool detectable = true;

        sealed class Baker : Baker<PerceptionTargetECSBehavior>
        {
            public override void Bake(PerceptionTargetECSBehavior authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new DetectionTarget
                {
                    Faction = authoring.faction,
                });

                if (!authoring.detectable)
                    SetComponentEnabled<DetectionTarget>(entity, false);
            }
        }
    }
}
