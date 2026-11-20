
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using static InfectionComponent;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NPCInfectionSystem))]
public partial struct NPCVisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (infection, colors, materialColor) in
            SystemAPI.Query<
                RefRO<InfectionComponent>,
                RefRO<NPCVisualColors>,
                RefRW<URPMaterialPropertyBaseColor>>()
                .WithAll<NPCComponent>())
        {
            float4 targetColor =
                infection.ValueRO.State == InfectionState.Infected
                ? colors.ValueRO.InfectedColor
                : colors.ValueRO.HealthyColor;

            if (math.all(materialColor.ValueRO.Value == targetColor))
                continue;

            materialColor.ValueRW.Value = targetColor;
        }
    }
}
