using UnityEngine;

[CreateAssetMenu(
    fileName = "TreatmentZoneConfig",
    menuName = "Game/Treatment Zone Config")]
public class TreatmentZoneConfigSO : ScriptableObject
{
    [Header("Treatment")]
    public float treatmentInterval = 1f;

    public float treatmentAmount = 20f;

    [Header("Zone")]
    public bool treatInfected = true;
}