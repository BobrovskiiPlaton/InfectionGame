/*using System.Collections.Generic;
using UnityEngine;

public class HealingField : MonoBehaviour
{
    [SerializeField]
    private TreatmentZoneConfigSO config;

    private readonly HashSet<NPCHealthState> npcsInside = new();

    private bool isRunning;

    private void OnEnable()
    {
        isRunning = true;
        HealingLoopAsync();
    }

    private void OnDisable()
    {
        isRunning = false;
        npcsInside.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        NPCHealthState npc = other.GetComponent<NPCHealthState>();

        if (npc == null)
            return;

        npcsInside.Add(npc);
    }

    private void OnTriggerExit(Collider other)
    {
        NPCHealthState npc = other.GetComponent<NPCHealthState>();

        if (npc == null)
            return;

        npcsInside.Remove(npc);
    }

    private async Awaitable HealingLoopAsync()
    {
        while (isRunning)
        {
            await Awaitable.WaitForSecondsAsync(
                config.treatmentInterval
            );

            HealNPCs();
        }
    }

    private void HealNPCs()
    {
        foreach (NPCHealthState npc in npcsInside)
        {
            if (npc == null)
                continue;

            if (config.treatInfected &&
                npc.CurrentState == NPCState.Infected)
            {
                npc.Treat(config.treatmentAmount);
            }
        }
    }
}*/