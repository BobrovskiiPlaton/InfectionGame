using UnityEngine;

[CreateAssetMenu(
    fileName = "MedkitConfig",
    menuName = "InfectionGame/Medkit Config"
)]
public class MedkitConfigSO : ScriptableObject
{
    [Header("Healing")]
    [Min(0f)]
    public float playerHealAmount = 30f;

    [Header("Cooldown")]
    [Min(0.1f)]
    public float cooldownDuration = 10f;

    [Header("Interaction")]
    [Min(0.1f)]
    public float interactionRadius = 3f;
}