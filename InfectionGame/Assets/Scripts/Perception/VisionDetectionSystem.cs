using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace InfectionGame.Perception
{
    public struct TargetSample
    {
        public Entity Entity;
        public TargetFaction Faction;
        public float3 Position;
    }

    [BurstCompile]
    public partial struct GatherPerceptionTargetsJob : IJobEntity
    {
        public NativeList<TargetSample> Targets;

        void Execute(Entity entity, in DetectionTarget target, in LocalTransform transform)
        {
            Targets.Add(new TargetSample
            {
                Entity = entity,
                Faction = target.Faction,
                Position = transform.Position,
            });
        }
    }

    [BurstCompile]
    public partial struct VisionDetectJob : IJobEntity
    {
        [ReadOnly] public NativeList<TargetSample> Targets;

        public float DeltaTime;

        void Execute(
            Entity self,
            in DetectionSensor sensor,
            in VisionSensor vision,
            in LocalTransform transform,
            ref DetectionState state,
            DynamicBuffer<DetectedTarget> detected)
        {
            float3 forward = math.forward(transform.Rotation);
            float3 eye = transform.Position + math.mul(transform.Rotation, vision.EyeOffsetLocal);
            PerceptionMath.VisionCone cone = PerceptionMath.VisionCone.FromDegrees(vision.FieldOfViewDegrees);

            // reverse проход, чтобы remove нормально работал
            for (int i = detected.Length - 1; i >= 0; i--)
            {
                DetectedTarget entry = detected[i];
                entry.SensedBy = SenseKind.None;

                bool stillKnown = PerceptionMath.TickMemory(
                    ref entry.TimeSinceSensed, false, DeltaTime, sensor.MemoryDuration);

                if (stillKnown)
                    detected[i] = entry;
                else
                    detected.RemoveAtSwapBack(i);
            }

            for (int t = 0; t < Targets.Length; t++)
            {
                TargetSample target = Targets[t];

                if (target.Entity == self)
                    continue;

                if (!PerceptionMath.IsInteresting(sensor.Targets, target.Faction))
                    continue;

                if (!PerceptionMath.CanSee(eye, forward, target.Position, vision.Range, cone))
                    continue;

                float distance = math.distance(eye, target.Position);
                int existing = FindIndex(detected, target.Entity);

                if (existing >= 0)
                {
                    DetectedTarget entry = detected[existing];
                    PerceptionMath.TickMemory(
                        ref entry.TimeSinceSensed, true, DeltaTime, sensor.MemoryDuration);

                    entry.SensedBy |= SenseKind.Vision;
                    entry.Faction = target.Faction;
                    entry.Distance = distance;
                    entry.LastKnownPosition = target.Position;

                    detected[existing] = entry;
                }
                else
                {
                    detected.Add(new DetectedTarget
                    {
                        Target = target.Entity,
                        SensedBy = SenseKind.Vision,
                        Faction = target.Faction,
                        Distance = distance,
                        LastKnownPosition = target.Position,
                        TimeSinceSensed = 0f,
                    });
                }
            }

            // Primary - ближайший или последний замеченный
            int primaryIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < detected.Length; i++)
            {
                DetectedTarget entry = detected[i];
                if (entry.TimeSinceSensed > 0f)
                    continue;

                if (entry.Distance < bestDistance)
                {
                    bestDistance = entry.Distance;
                    primaryIndex = i;
                }
            }

            if (primaryIndex < 0)
            {
                float bestTime = float.MaxValue;
                bestDistance = float.MaxValue;

                for (int i = 0; i < detected.Length; i++)
                {
                    DetectedTarget entry = detected[i];

                    bool better =
                        entry.TimeSinceSensed < bestTime ||
                        (entry.TimeSinceSensed == bestTime && entry.Distance < bestDistance);

                    if (better)
                    {
                        bestTime = entry.TimeSinceSensed;
                        bestDistance = entry.Distance;
                        primaryIndex = i;
                    }
                }
            }

            if (primaryIndex >= 0)
            {
                DetectedTarget primary = detected[primaryIndex];
                state = new DetectionState
                {
                    Primary = primary.Target,
                    PrimaryFaction = primary.Faction,
                    LastKnownPosition = primary.LastKnownPosition,
                    Distance = primary.Distance,
                    TimeSinceSensed = primary.TimeSinceSensed,
                };
            }
            else
            {
                state = default;
            }
        }

        static int FindIndex(DynamicBuffer<DetectedTarget> buffer, Entity target)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Target == target)
                    return i;
            }

            return -1;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct VisionDetectionSystem : ISystem
    {
        EntityQuery _targetQuery;

        public void OnCreate(ref SystemState state)
        {
            _targetQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<DetectionTarget>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var targets = new NativeList<TargetSample>(
                _targetQuery.CalculateEntityCount(), Allocator.TempJob);

            state.Dependency = new GatherPerceptionTargetsJob
            {
                Targets = targets,
            }.Schedule(state.Dependency);

            state.Dependency = new VisionDetectJob
            {
                Targets = targets,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = targets.Dispose(state.Dependency);
        }
    }
}
