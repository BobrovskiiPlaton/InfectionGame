using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyConfig",
    menuName = "Game/Enemy/Enemy Config"
)]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Chase")]
    public float moveSpeed = 3f;
    public float detectionRadius = 20f;

    [Header("Passive Movement")]
    public float passiveMoveSpeed = 1.5f;
    public float changeDirectionTime = 3f;
}