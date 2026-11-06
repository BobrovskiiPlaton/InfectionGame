
using Unity.Entities;
using Unity.Mathematics;

public struct NPCMovementComponent : IComponentData
{
    public float MoveSpeed;
    public float3 Direction;
    public float FleeRadius;
    public float FleeSpeed;

    public byte IsFleeing;
}
