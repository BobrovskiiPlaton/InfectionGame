using Unity.Entities;
using UnityEngine;

public struct InfectionComponent : IComponentData
{
    public InfectionState State;
    public float TimeRemaining;
    public float TreatmentProgress;


    public enum InfectionState : byte
    {
        Healthy,
        Infected,
        Recovered,
        Immune
    }
}
