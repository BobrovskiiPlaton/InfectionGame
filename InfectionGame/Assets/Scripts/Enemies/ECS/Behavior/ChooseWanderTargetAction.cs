using System;
using InfectionGame.Actors;
using Unity.Behavior;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;

namespace InfectionGame.Enemies.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Choose Wander Target",
        description: "Keeps a wander destination, picks a new one only when there is none "
                     + "or the enemy has been stuck too long.",
        story: "Choose Wander Target In Radius [Radius] Restart After [RestartAfterSeconds]",
        category: "Action/Enemy",
        id: "b2c3d4e5f60718293a4b5c6d7e8f9012")]
    public partial class ChooseWanderTargetAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<float> Radius;

        [SerializeReference] public BlackboardVariable<float> RestartAfterSeconds;

        protected override Status OnUpdate()
        {
            if (GameObject == null)
                return Status.Failure;

            IActorCompanionAgent agent = GameObject.GetComponent<IActorCompanionAgent>();

            if (agent == null)
            {
                LogFailure($"На '{GameObject.name}' нет моста к ECS ({nameof(IActorCompanionAgent)}).");
                return Status.Failure;
            }

            agent.TryGetWanderTarget(out var wander);
            float timeout = RestartAfterSeconds?.Value ?? 0f;

            if (!WanderTargetMath.ShouldAllocate(wander, timeout))
                return Status.Success;

            float radius = math.max(0.5f, Radius?.Value ?? 5f);
            float3 target = WanderTargetMath.PointInDisk(
                GameObject.transform.position,
                UnityEngine.Random.insideUnitCircle,
                radius);

            agent.SetWanderTarget(target);
            return Status.Success;
        }
    }
}
