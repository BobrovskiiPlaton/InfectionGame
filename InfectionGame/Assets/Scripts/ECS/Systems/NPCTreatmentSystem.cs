using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using static InfectionComponent;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(NPCInfectionSystem))]
public partial struct NPCTreatmentSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (infection, request) in
            SystemAPI.Query<
                RefRW<InfectionComponent>,
                RefRW<NPCTreatmentRequest>>()
                .WithAll<NPCComponent>())
        {
            float amount = request.ValueRO.Amount;

            if (amount <= 0f)
                continue;

            request.ValueRW.Amount = 0f;

            if (infection.ValueRO.State != InfectionState.Infected)
                continue;

            infection.ValueRW.TreatmentProgress =
                math.max(
                    0f,
                    infection.ValueRO.TreatmentProgress - amount
                );
        }
    }
}
