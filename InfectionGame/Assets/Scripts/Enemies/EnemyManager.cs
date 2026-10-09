using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Scriptable Objects")]
    [SerializeField]
    private EnemySpawnConfigSO spawnConfig;

    [Header("Prefab")]
    [SerializeField]
    private Enemy enemyPrefab;

    private void Start()
    {
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < spawnConfig.enemyCount; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Vector3 position = GetRandomSpawnPosition();

        Instantiate(
            enemyPrefab,
            position,
            Quaternion.identity
        );
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 circle =
            Random.insideUnitCircle *
            spawnConfig.spawnRadius;

        return transform.position +
               new Vector3(
                   circle.x,
                   0f,
                   circle.y
               );
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnConfig == null)
            return;

        Gizmos.DrawWireSphere(
            transform.position,
            spawnConfig.spawnRadius
        );
    }
}