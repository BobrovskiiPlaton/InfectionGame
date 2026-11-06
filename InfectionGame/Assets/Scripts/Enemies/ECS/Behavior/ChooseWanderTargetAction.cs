using System;
using InfectionGame.Actors;
using Unity.Behavior;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;

namespace InfectionGame.Enemies.Behavior
{
    /// <summary>
    /// Гарантирует, что у актора есть куда идти. Новую точку выбирает ТОЛЬКО когда её нет —
    /// либо актор уже дошёл (ECS сбрасывает <c>HasTarget</c>), либо слишком долго топчется
    /// на одном месте (<c>RestartAfterSeconds</c>, спасение от упёршегося в стену).
    ///
    /// Раньше узел безусловно перебирал точку на каждом проходе, а корень графа стоит под
    /// Repeat — то есть ~60 новых случайных целей в секунду. Враг на каждом тике разворачивался
    /// в новую сторону и дёргался.
    ///
    /// Через <see cref="IActorCompanionAgent"/> и <see cref="WanderTargetMath"/> узел служит
    /// любому актору (враг, NPC) и не дублируется. Состояние он сознательно не проверяет:
    /// решать, когда бродить, — работа графа, а не узла.
    /// </summary>
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
        [Tooltip("Радиус вокруг врага, в котором выбирается точка, метры.")]
        [SerializeReference] public BlackboardVariable<float> Radius;

        [Tooltip("Через сколько секунд безрезультатного подхода выбросить точку и взять новую. "
                 + "0 — никогда, ждать достижения точки бесконечно.")]
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

            // Точка ещё жива — оставляем как есть. Именно это убирает дёрганье.
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
