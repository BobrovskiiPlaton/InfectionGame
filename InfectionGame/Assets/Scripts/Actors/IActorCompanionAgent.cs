using Unity.Mathematics;

namespace InfectionGame.Actors
{
    public interface IActorCompanionAgent
    {
        bool IsLinked { get; }

        bool TryGetPerception(out InfectionGame.Perception.DetectionState state);

        bool TryGetWanderTarget(out WanderTarget wander);

        void SetWanderTarget(float3 position);

        void ClearWanderTarget();
    }
}
