using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyRuntimeSet",
    menuName = "Game/Enemy/Enemy Runtime Set"
)]
public class EnemyRuntimeSetSO : ScriptableObject
{
    private readonly List<Enemy> enemies = new();

    public IReadOnlyList<Enemy> Enemies => enemies;

    public void Add(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public void Remove(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemies.Remove(enemy);
    }
}