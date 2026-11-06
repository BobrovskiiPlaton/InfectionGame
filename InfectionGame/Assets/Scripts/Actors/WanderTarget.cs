using Unity.Mathematics;

namespace InfectionGame.Actors
{
    /// <summary>
    /// Точка, куда актор идёт, когда нет более важной задачи. Поведение пишет её,
    /// ECS-система движения ведёт туда актора.
    ///
    /// Общий для всех акторов: политика «не перебирать точку каждый тик» одинакова для
    /// бродящего врага и для уходящего от угрозы NPC, и держать её в одном месте дешевле,
    /// чем копировать на каждого.
    /// </summary>
    public struct WanderTarget : Unity.Entities.IComponentData
    {
        public float3 Position;

        /// <summary>1 — точка назначена, 0 — нет.</summary>
        public float HasTarget;

        /// <summary>Сколько секунд актор уже идёт к этой точке.</summary>
        public float TimeSinceAssigned;
    }
}
