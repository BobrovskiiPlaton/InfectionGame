using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemySpawnConfig",
    menuName = "Game/Enemy/Enemy Spawn Config"
)]
public class EnemySpawnConfigSO : ScriptableObject
{
    public int maxEnemyCount = 10000;
    public float spawnRadius = 30f;
    public float spawnInterval = 1f;
}