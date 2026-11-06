using System;
using InfectionGame.Enemies.ECS;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace InfectionGame.Enemies.Behavior
{
    /// <summary>
    /// Условие: воспринятая цель ближе заданной дистанции. Используется, чтобы переключиться
    /// между преследованием и атакой.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Target In Range",
        description: "True if the perceived target is closer than the given distance.",
        story: "Is Target In Range [Distance]",
        category: "Condition/Enemy",
        id: "c3d4e5f60718293a4b5c6d7e8f901234")]
    public partial class IsTargetInRangeCondition : Unity.Behavior.Condition
    {
        [Tooltip("Дистанция в метрах.")]
        [SerializeReference] public BlackboardVariable<float> Distance;

        public override bool IsTrue()
        {
            if (GameObject == null)
                return false;

            if (!GameObject.TryGetComponent<EnemyCompanionAgent>(out var agent))
                return false;

            if (!agent.TryGetPerception(out var perception))
                return false;

            if (perception.Primary == Unity.Entities.Entity.Null)
                return false;

            return perception.Distance <= (Distance?.Value ?? 0f);
        }
    }
}