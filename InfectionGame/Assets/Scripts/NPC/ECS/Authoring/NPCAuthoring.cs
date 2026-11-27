using InfectionGame.Actors;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Rendering;
using static InfectionComponent;

public class NPCAuthoring : MonoBehaviour
{
    [SerializeField] private NPCConfigSO config;

    public class Baker : Baker<NPCAuthoring>
    {
        public override void Bake(NPCAuthoring authoring)
        {
            if (authoring.config == null)
                return;
            DependsOn(authoring.config);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<NPCComponent>(entity);
            AddComponent(entity, new NPCMovementComponent
            {
               MoveSpeed = authoring.config.moveSpeed,
               Direction = Unity.Mathematics.float3.zero,
               FleeRadius = authoring.config.fleeRadius,
               FleeSpeed = authoring.config.fleeSpeed,
               IsFleeing = 0
            });

            AddComponent(entity, new HealthComponent
            {
               CurrentHealth = authoring.config.maxHealth,
               MaxHealth = authoring.config.maxHealth
            });

            AddComponent(entity, new InfectionComponent
            {
                State = InfectionState.Healthy,
                TimeRemaining = 0f,
                TreatmentProgress = authoring.config.treatmentRequired,
                TreatmentRequired = authoring.config.treatmentRequired,
                InfectionDuration = authoring.config.infectionDuration,
                ReceivedHits = 0,
                HitsToInfect = math.max(1, authoring.config.hitsToInfect)
            });

            AddComponent(entity, new NPCWanderComponent
            {
                ChangeDirectionTimer = 0f,
                RandomState = 0
            });

            Color healthy = authoring.config.normalColor;
            Color damaged = authoring.config.damagedColor;
            Color infected = authoring.config.infectedColor;

            float4 healthyColor = new float4(
                healthy.r, healthy.g, healthy.b, healthy.a
            );

            float4 infectedColor = new float4(
                infected.r, infected.g, infected.b, infected.a
            );

            float4 damagedColor = new float4(
                damaged.r, damaged.g, damaged.b, damaged.a
            );

            AddComponent(entity, new NPCVisualColors
            {
                HealthyColor = healthyColor,
                DamagedColor = damagedColor,
                InfectedColor = infectedColor
            });

            AddComponent(entity, new URPMaterialPropertyBaseColor
            {
                Value = healthyColor
            });

            // Буфер запросов удара: в него пишет AttackJob врагов
            AddBuffer<NPCInfectionHitRequest>(entity);

            AddComponent<NPCTreatmentRequest>(entity);
        }
    }

}
