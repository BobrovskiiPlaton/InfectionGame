using System;
using Unity.Entities;
using Unity.Mathematics;

namespace InfectionGame.Perception
{
    [Flags]
    public enum TargetFaction : byte
    {
        None = 0,
        Player = 1 << 0,
        NPC = 1 << 1,
        Enemy = 1 << 2,
        All = Player | NPC | Enemy,
    }

    [Flags]
    public enum SenseKind : byte
    {
        None = 0,
        Vision = 1 << 0,
        Hearing = 1 << 1,
    }

    public struct DetectionTarget : IComponentData, IEnableableComponent
    {
        public TargetFaction Faction;
    }

    public struct DetectionSensor : IComponentData
    {
        public TargetFaction Targets;

        public float MemoryDuration;
    }

    public struct VisionSensor : IComponentData, IEnableableComponent
    {
        // Дистанция видимости в метрах
        public float Range;

        // FOV
        public float FieldOfViewDegrees;

        // Локальная позиция глаз
        public float3 EyeOffsetLocal;
    }

    public struct HearingSensor : IComponentData
    {
        // Радиус слуха в метрах
        public float Radius;
    }

    public struct DetectedTarget : IBufferElementData
    {
        public Entity Target;

        // SenseKind.None, если мы только помним эту цель
        public SenseKind SensedBy;

        public TargetFaction Faction;

        public float Distance;

        public float3 LastKnownPosition;

        // Время в секундах с последнего момента видимости
        public float TimeSinceSensed;
    }

    public struct DetectionState : IComponentData
    {
        public Entity Primary;
        public TargetFaction PrimaryFaction;
        public float3 LastKnownPosition;
        public float Distance;
        public float TimeSinceSensed;
    }
}
