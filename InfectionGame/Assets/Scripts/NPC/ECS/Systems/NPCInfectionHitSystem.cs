
using Unity.Burst;
using Unity.Entities;
using static InfectionComponent;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(NPCInfectionSystem))]
public partial struct NPCInfectionHitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (infection, request) in
            SystemAPI.Query<
                RefRW<InfectionComponent>,
                RefRW<NPCInfectionHitRequest>>()
                .WithAll<NPCComponent>())
        {
            if (request.ValueRO.PendingHits <= 0)
                continue;

            request.ValueRW.PendingHits = 0;

            if (infection.ValueRO.State != InfectionState.Healthy)
                continue;

            infection.ValueRW.State = InfectionState.Infected;
            infection.ValueRW.TimeRemaining =
                infection.ValueRO.InfectionDuration;
        }
    }
}
