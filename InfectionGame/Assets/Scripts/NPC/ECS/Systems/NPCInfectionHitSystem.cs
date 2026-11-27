using InfectionGame.Actors;
using InfectionGame.Enemies.ECS;
using InfectionGame.Perception;
using Unity.Burst;
using Unity.Entities;
using static InfectionComponent;

// Учитывает удары, которые AttackJob врагов пишет в буфер NPCInfectionHitRequest.
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemyAgentSystem))]
[UpdateBefore(typeof(NPCInfectionSystem))]
public partial struct NPCInfectionHitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ComponentLookup<DetectionTarget> detectionLookup =
            SystemAPI.GetComponentLookup<DetectionTarget>(false);

        foreach (var (infection, hits, entity) in
            SystemAPI.Query<
                RefRW<InfectionComponent>,
                DynamicBuffer<NPCInfectionHitRequest>>()
                .WithAll<NPCComponent>()
                .WithEntityAccess())
        {
            if (hits.Length == 0)
                continue;

            int hitCount = hits.Length;
            hits.Clear();

            if (infection.ValueRO.State == InfectionState.Infected ||
                infection.ValueRO.State == InfectionState.Immune)
                continue;

            infection.ValueRW.ReceivedHits += hitCount;

            if (infection.ValueRO.ReceivedHits >= infection.ValueRO.HitsToInfect)
            {
                infection.ValueRW.State = InfectionState.Infected;
                infection.ValueRW.ReceivedHits = 0;
                infection.ValueRW.TimeRemaining =
                    infection.ValueRO.InfectionDuration;
                infection.ValueRW.TreatmentProgress =
                    infection.ValueRO.TreatmentRequired;

                // Заражённого NPC враги игнорируют
                if (detectionLookup.HasComponent(entity))
                    detectionLookup.SetComponentEnabled(entity, false);
            }
            else
            {
                infection.ValueRW.State = InfectionState.Damaged;
            }
        }
    }
}
