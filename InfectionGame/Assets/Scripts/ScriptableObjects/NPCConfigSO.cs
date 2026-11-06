using UnityEngine;

[CreateAssetMenu(
    fileName = "NPCConfig",
    menuName = "Game/NPC/NPC Config"
)]
public class NPCConfigSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float fleeSpeed = 4f;
    public float fleeRadius = 10f;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Infection")]
    public int hitsToInfect = 1;
    public float infectionDuration = 30f;
    public float treatmentRequired = 100f;

    [Header("Colors")]
    public Color normalColor = Color.blue;
    public Color infectedColor = Color.red;
}