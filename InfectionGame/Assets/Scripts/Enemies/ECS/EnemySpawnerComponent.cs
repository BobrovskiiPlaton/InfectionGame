
using Unity.Entities;
using Unity.Mathematics;

public struct EnemySpawnerComponent : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float Radius;
    public float SpawnHeight;
    public float3 Center;

    public float InitialSpawnInterval;
    public float MinimumSpawnInterval;
    public float IntervalDecrease;

    public int SpawnedCount;
    public float CurrentInterval;
    public float SpawnTimer;
}
