using Unity.Entities;
using Unity.Mathematics;

public struct NPCFleeState : IComponentData
{
    public float3 Direction;
}

public struct NPCFleeThreatSnapshot : IBufferElementData
{
    public Entity Target;
    public float3 RelativePosition;
}