using System;
using InfectionGame.Enemies.ECS;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace InfectionGame.Enemies.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Set Enemy State",
        description: "Switches the enemy between Idle, Chase and Attack.",
        story: "Set Enemy State To [State]",
        category: "Action/Enemy",
        id: "a1b2c3d4e5f60718293a4b5c6d7e8f90")]
    public partial class SetEnemyStateAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<EnemyState> State;

        protected override Status OnUpdate()
        {
            if (GameObject == null)
                return Status.Failure;

            if (!GameObject.TryGetComponent<EnemyCompanionAgent>(out var agent))
            {
                LogFailure($"На '{GameObject.name}' нет {nameof(EnemyCompanionAgent)}.");
                return Status.Failure;
            }

            agent.SetState(State.Value);
            return Status.Success;
        }
    }
}