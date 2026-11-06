using System;
using InfectionGame.Enemies.ECS;
using Unity.Behavior;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;

namespace InfectionGame.Enemies.Behavior
{
    /// <summary>
    /// Гарантирует, что у врага есть куда идти. Новую точку выбирает ТОЛЬКО когда её нет —
    /// либо враг уже дошёл (ECS сбрасывает <c>HasTarget</c>), либо слишком долго топчется
    /// на одном месте (<c>RestartAfterSeconds</c>, спасение от упёршегося в стену).
    ///
    /// Раньше узел безусловно перебирал точку на каждом проходе, а корень графа стоит под
    /// Repeat — то есть ~60 новых случайных целей в секунду. Враг на каждом тике разворачивался
    /// в новую сторону и дёргался.
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

            if (!GameObject.TryGetComponent<EnemyCompanionAgent>(out var agent))
            {
                LogFailure($"На '{GameObject.name}' нет {nameof(EnemyCompanionAgent)}.");
                return Status.Failure;
            }

            // Точка ещё жива — оставляем как есть. Именно это убирает дёрганье.
            if (agent.TryGetWanderTarget(out var wander) && wander.HasTarget > 0.5f)
            {
                float timeout = RestartAfterSeconds?.Value ?? 0f;

                if (timeout <= 0f || wander.TimeSinceAssigned < timeout)
                    return Status.Success;
            }

            float radius = math.max(0.5f, Radius?.Value ?? 5f);

            // Случайная точка в диске вокруг врага.
            float2 offset = UnityEngine.Random.insideUnitCircle * radius;
            float3 target = new float3(
                GameObject.transform.position.x + offset.x,
                GameObject.transform.position.y,
                GameObject.transform.position.z + offset.y);

            agent.SetWanderTarget(target);
            return Status.Success;
        }
    }
}
