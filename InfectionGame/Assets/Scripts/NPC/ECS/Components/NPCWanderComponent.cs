
using Unity.Entities;

public struct NPCWanderComponent : IComponentData
{
    public float ChangeDirectionTimer;
    public uint RandomState;
}
