using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NPCRuntimeSet",
    menuName = "Game/NPC/NPC Runtime Set"
)]
public class NPCRuntimeSetSO : ScriptableObject
{
    private readonly List<NPC> npcs = new();

    public IReadOnlyList<NPC> NPCs => npcs;

    public void Add(NPC npc)
    {
        if (npc != null && !npcs.Contains(npc))
            npcs.Add(npc);
    }

    public void Remove(NPC npc)
    {
        npcs.Remove(npc);
    }
}