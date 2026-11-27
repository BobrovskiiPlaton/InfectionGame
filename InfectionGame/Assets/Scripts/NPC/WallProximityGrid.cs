
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class WallProximityGrid
{
    private const float CellSize = 5f;
    private const int MaxCells = 500000;

    private static WallProximityGrid instance;

    private readonly HashSet<long> wallCells = new();

    private bool initialized;
    private bool filteringEnabled;

    public static WallProximityGrid Instance =>
        instance ??= new WallProximityGrid();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        instance = null;
    }

    public void Initialize(
        int wallMask,
        float checkDistance,
        float checkRadius)
    {
        if (initialized)
            return;

        initialized = true;
        filteringEnabled = false;
        wallCells.Clear();

        Collider[] colliders =
            Object.FindObjectsByType<Collider>(
                FindObjectsSortMode.None
            );

        float margin = checkDistance + checkRadius + 0.1f;
        int wallCount = 0;

        foreach (Collider collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger)
                continue;

            if ((wallMask & (1 << collider.gameObject.layer)) == 0)
                continue;

            wallCount++;

            // Динамические препятствия нельзя кэшировать.
            if (!collider.gameObject.isStatic)
            {
                wallCells.Clear();
                return;
            }

            Bounds bounds = collider.bounds;

            int minX = ToCell(bounds.min.x - margin);
            int maxX = ToCell(bounds.max.x + margin);
            int minZ = ToCell(bounds.min.z - margin);
            int maxZ = ToCell(bounds.max.z + margin);

            long cellCount =
                (long)(maxX - minX + 1) *
                (maxZ - minZ + 1);

            if (cellCount > MaxCells ||
                wallCells.Count + cellCount > MaxCells)
            {
                wallCells.Clear();
                return;
            }

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    wallCells.Add(GetKey(x, z));
                }
            }
        }

        // Если стены ещё не найдены, оставляем
        // физические проверки для всех NPC.
        if (wallCount == 0)
            return;

        filteringEnabled = true;
    }

    public bool RequiresPhysicsCheck(float3 position)
    {
        if (!filteringEnabled)
            return true;

        int x = ToCell(position.x);
        int z = ToCell(position.z);

        return wallCells.Contains(GetKey(x, z));
    }

    private static int ToCell(float coordinate)
    {
        return (int)math.floor(coordinate / CellSize);
    }

    private static long GetKey(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }
}
