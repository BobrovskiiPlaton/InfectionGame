using Unity.Mathematics;

namespace InfectionGame.Actors
{
    public struct WanderTarget : Unity.Entities.IComponentData
    {
        public float3 Position;

        public float HasTarget;

        public float TimeSinceAssigned;
    }
}
