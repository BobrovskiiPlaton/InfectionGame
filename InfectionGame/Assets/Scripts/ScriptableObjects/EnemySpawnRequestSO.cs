using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemySpawnRequest",
    menuName = "Game/Events/Enemy Spawn Request"
)]
public class EnemySpawnRequestSO : ScriptableObject
{
    public event Action<Vector3> OnSpawnRequested;

    public void Raise(Vector3 position)
    {
        OnSpawnRequested?.Invoke(position);
    }
}