using Unity.Entities;
using Unity.Mathematics;

public struct NPCSpawnerComponent : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float Radius;
    public float SpawnHeight;
    public float3 Center;
}
