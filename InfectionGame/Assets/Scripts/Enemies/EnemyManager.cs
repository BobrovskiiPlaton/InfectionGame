using System.Collections;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private EnemySpawnConfigSO spawnConfig;
    [SerializeField] private Enemy enemyPrefab;

    [SerializeField]
    private EnemySpawnRequestSO spawnRequest;

    private int currentEnemyCount;
    private float currentSpawnInterval;

    private void OnEnable()
    {
        if (spawnRequest != null)
        {
            spawnRequest.OnSpawnRequested += SpawnEnemyAt;
        }
    }

    private void OnDisable()
    {
        if (spawnRequest != null)
        {
            spawnRequest.OnSpawnRequested -= SpawnEnemyAt;
        }
    }

    private void Start()
    {
        currentSpawnInterval =
            spawnConfig.initialSpawnInterval;

        StartCoroutine(
            SpawnEnemiesCoroutine()
        );
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        while (currentEnemyCount < spawnConfig.maxEnemyCount)
        {
            SpawnEnemy();

            yield return new WaitForSeconds(
                currentSpawnInterval
            );

            currentSpawnInterval -=
                spawnConfig.intervalDecrease;

            currentSpawnInterval =
                Mathf.Max(
                    currentSpawnInterval,
                    spawnConfig.minimumSpawnInterval
                );
        }
    }

    private void SpawnEnemy()
    {
        Vector3 position =
            GetRandomSpawnPosition();

        SpawnEnemyAt(position);
    }

    public void SpawnEnemyAt(Vector3 position)
    {
        Instantiate(
            enemyPrefab,
            position,
            Quaternion.identity
        );

        currentEnemyCount++;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomPoint =
            Random.insideUnitCircle *
            spawnConfig.spawnRadius;

        return transform.position +
               new Vector3(
                   randomPoint.x,
                   spawnConfig.spawnHeight,
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