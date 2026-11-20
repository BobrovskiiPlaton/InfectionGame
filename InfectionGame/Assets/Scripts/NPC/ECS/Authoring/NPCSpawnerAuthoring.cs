using Unity.Entities;
using UnityEngine;

public class NPCSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private NPCSpawnConfigSO config;

    public class Baker : Baker<NPCSpawnerAuthoring>
    {
        public override void Bake(NPCSpawnerAuthoring authoring)
        {
            if (authoring.npcPrefab == null ||
                authoring.config == null)
                return;

            DependsOn(authoring.config);

            Entity entity = GetEntity(
                TransformUsageFlags.None
            );

            Entity prefab = GetEntity(
                authoring.npcPrefab,
                TransformUsageFlags.Dynamic
            );

            AddComponent(entity, new NPCSpawnerComponent
            {
                Prefab = prefab,
                Count = authoring.config.npcCount,
                Radius = authoring.config.spawnRadius,
                SpawnHeight = authoring.config.spawnHeight,
                Center = authoring.transform.position
            });
        }
    }
}
