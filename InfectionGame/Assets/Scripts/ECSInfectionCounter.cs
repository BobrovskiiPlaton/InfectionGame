using UnityEngine;

public static class ECSInfectionCounter
{
    public static int GetInfectedCount()
    {
        NPCHealthState[] npcs =
            Object.FindObjectsByType<NPCHealthState>(
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (NPCHealthState npc in npcs)
        {
            if (npc.CurrentState == NPCState.Infected)
            {
                count++;
            }
        }

        return count;
    }
}