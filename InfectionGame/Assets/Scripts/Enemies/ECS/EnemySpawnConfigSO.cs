using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemySpawnConfig",
    menuName = "Game/Enemy/Enemy Spawn Config"
)]
public class EnemySpawnConfigSO : ScriptableObject
{
    public int maxEnemyCount = 100;

    public float spawnRadius = 30f;

    public float spawnHeight = 1f;

    [Header("Spawn Speed")]

    public float initialSpawnInterval = 10f;

    public float minimumSpawnInterval = 0.5f;

    public float intervalDecrease = 0.25f;
}