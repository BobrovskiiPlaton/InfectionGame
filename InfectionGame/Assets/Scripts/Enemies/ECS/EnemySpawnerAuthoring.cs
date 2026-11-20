
using Unity.Entities;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemySpawnConfigSO config;

    public class Baker : Baker<EnemySpawnerAuthoring>
    {
        public override void Bake(EnemySpawnerAuthoring authoring)
        {
            if (authoring.enemyPrefab == null ||
                authoring.config == null)
                return;

            DependsOn(authoring.config);

            Entity entity = GetEntity(
                TransformUsageFlags.None
            );

            Entity prefab = GetEntity(
                authoring.enemyPrefab,
                TransformUsageFlags.Dynamic
            );

            AddComponent(entity, new EnemySpawnerComponent
            {
                Prefab = prefab,
                Count = authoring.config.maxEnemyCount,
                Radius = authoring.config.spawnRadius,
                SpawnHeight = authoring.config.spawnHeight,
                Center = authoring.transform.position,

                InitialSpawnInterval =
                    authoring.config.initialSpawnInterval,

                MinimumSpawnInterval =
                    authoring.config.minimumSpawnInterval,

                IntervalDecrease =
                    authoring.config.intervalDecrease,

                SpawnedCount = 0,
                CurrentInterval =
                    authoring.config.initialSpawnInterval,

                SpawnTimer = 0f
            });
        }
    }
}
