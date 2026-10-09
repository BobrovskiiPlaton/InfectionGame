using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemySpawnConfig",
    menuName = "Game/Enemy/Enemy Spawn Config"
)]
public class EnemySpawnConfigSO : ScriptableObject
{
    public int enemyCount = 100;

    public float spawnRadius = 20f;
}