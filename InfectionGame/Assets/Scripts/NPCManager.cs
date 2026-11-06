using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [SerializeField] private NPCSpawnConfigSO spawnConfig;
    [SerializeField] private NPC npcPrefab;

    private void Start()
    {
        SpawnNPCs();
    }

    private void SpawnNPCs()
    {
        for (int i = 0; i < spawnConfig.npcCount; i++)
        {
            Vector3 position = GetRandomSpawnPosition();

            Instantiate(
                npcPrefab,
                position,
                Quaternion.identity
            );
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 random =
            Random.insideUnitCircle * spawnConfig.spawnRadius;

        return transform.position +
               new Vector3(random.x, spawnConfig.spawnHeight, random.y);
    }
}