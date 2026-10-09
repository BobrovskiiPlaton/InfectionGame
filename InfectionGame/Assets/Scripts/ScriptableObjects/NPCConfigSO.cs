using UnityEngine;

[CreateAssetMenu(
    fileName = "NPCConfig",
    menuName = "Game/NPC/NPC Config"
)]
public class NPCConfigSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float fleeRadius = 10f;

    [Header("Infection")]
    public int hitsToInfect = 3;
    public float infectionDuration = 30f;

    [Header("Colors")]
    public Color normalColor = Color.blue;
    public Color infectedColor = Color.purple;
}