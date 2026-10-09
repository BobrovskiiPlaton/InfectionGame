using System.Collections;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private EnemySpawnConfigSO spawnConfig;
    [SerializeField] private Enemy enemyPrefab;

    private int currentEnemyCount;

    private void Start()
    {
        StartCoroutine(SpawnEnemiesCoroutine());
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        while (currentEnemyCount < spawnConfig.maxEnemyCount)
        {
            SpawnEnemy();

            currentEnemyCount++;

            yield return new WaitForSeconds(
                spawnConfig.spawnInterval
            );
        }
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();

        Instantiate(
            enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomPoint =
            Random.insideUnitCircle * spawnConfig.spawnRadius;

        return transform.position + new Vector3(
            randomPoint.x,
            0f,
            randomPoint.y
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