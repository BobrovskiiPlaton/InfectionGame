using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy/Enemy Config")]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Detection")]
    public float detectionRadius = 5f;
}
