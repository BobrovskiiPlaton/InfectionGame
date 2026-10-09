using UnityEngine;

[CreateAssetMenu(
    fileName = "NPCSpawnConfig",
    menuName = "Game/NPC/NPC Spawn Config"
)]
public class NPCSpawnConfigSO : ScriptableObject
{
    public int npcCount = 100;
    public float spawnRadius = 30f;
    public float spawnHeight = 1f;
}