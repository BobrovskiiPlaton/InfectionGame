using System;
using InfectionGame.Actors;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace InfectionGame.Enemies.Behavior
{
    /// <summary>
    /// Условие: актор сейчас воспринимает цель (зрение, слух или память о ней).
    /// Работает с <see cref="IActorCompanionAgent"/>, а не с конкретным мостом — поэтому
    /// узел одинаково служит и врагу, и NPC, и не требует копирования.
    /// Читает <c>DetectionState</c> из ECS через companion-мост.
    /// </summary>
    // Условия регистрируются ТОЛЬКО через [Condition(...)]: ConditionUtility.GetConditionTypes
    // собирает список через TypeCache.GetTypesWithAttribute<ConditionAttribute>(). [NodeDescription]
    // — атрибут Action/Modifier/Composite, условие с ним в Assign Condition не появится.
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Has Perceived Target",
        description: "True if the enemy currently senses or remembers a target.",
        story: "Has Perceived Target With Memory [MemoryGraceSeconds]",
        category: "Condition/Enemy",
        id: "f1a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8")]
    public partial class HasPerceivedTargetCondition : Unity.Behavior.Condition
    {
        /// <summary>Сколько секунд с момента потери цели ещё считать её "замеченной".</summary>
        [SerializeReference] public BlackboardVariable<float> MemoryGraceSeconds;

        public override bool IsTrue()
        {
            if (GameObject == null)
                return false;

            IActorCompanionAgent agent = GameObject.GetComponent<IActorCompanionAgent>();

            if (agent == null)
                return false;

            if (!agent.TryGetPerception(out var perception))
                return false;

            if (perception.Primary == Unity.Entities.Entity.Null)
                return false;

            float grace = MemoryGraceSeconds?.Value ?? 0f;
            return perception.TimeSinceSensed <= grace;
        }
    }
}