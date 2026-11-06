using Unity.Entities;
using UnityEngine;

public struct HealthComponent : IComponentData
{
    public float CurrentHealth;
    public float MaxHealth;
}
