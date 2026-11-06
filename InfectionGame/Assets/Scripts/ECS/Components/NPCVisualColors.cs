
using Unity.Entities;
using Unity.Mathematics;

public struct NPCVisualColors : IComponentData
{
    public float4 HealthyColor;
    public float4 InfectedColor;
}
