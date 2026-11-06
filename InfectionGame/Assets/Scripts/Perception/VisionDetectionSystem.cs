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

    /// <summary>
    /// Общая работа с буфером <see cref="DetectedTarget"/>: добавить/обновить запись о цели,
    /// которую заметил один из сенсоров. Вынесена, чтобы Vision и Hearing job'ы не
    /// дублировали логику и вели себя одинаково.
    /// </summary>
    internal static class PerceptionBufferOps
    {
        public static void RecordSensed(
            DynamicBuffer<DetectedTarget> detected,
            in TargetSample target,
            float distance,
            SenseKind sense,
            float deltaTime,
            float memoryDuration)
        {
            int existing = FindIndex(detected, target.Entity);

            if (existing >= 0)
            {
                DetectedTarget entry = detected[existing];
                PerceptionMath.TickMemory(
                    ref entry.TimeSinceSensed, true, deltaTime, memoryDuration);

                entry.SensedBy |= sense;
                entry.Faction = target.Faction;
                entry.Distance = distance;
                entry.LastKnownPosition = target.Position;

                detected[existing] = entry;
            }
            else
            {
                float timeSince = 0f;
                PerceptionMath.TickMemory(
                    ref timeSince, true, deltaTime, memoryDuration);

                detected.Add(new DetectedTarget
                {
                    Target = target.Entity,
                    SensedBy = sense,
                    Faction = target.Faction,
                    Distance = distance,
                    LastKnownPosition = target.Position,
                    TimeSinceSensed = timeSince,
                });
            }
        }

        public static int FindIndex(DynamicBuffer<DetectedTarget> buffer, Entity target)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Target == target)
                    return i;
            }

            return -1;
        }
    }

    /// <summary>
    /// Тикает память по всем известным целям и удаляет забытых. Идёт первым, чтобы
    /// сенсорные job'ы ниже писали в уже очищенный буфер.
    /// </summary>
    [BurstCompile]
    public partial struct PerceptionMemoryTickJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(
            in DetectionSensor sensor,
            in LocalTransform transform,
            ref DetectionState state,
            DynamicBuffer<DetectedTarget> detected)
        {
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

            state = default;
        }
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

    /// <summary>
    /// Зрение: конус вокруг направления взгляда. Работает только при положительном
    /// <see cref="VisionSensor.Range"/>, поэтому враг без зрения не тратит на него время.
    /// </summary>
    [BurstCompile]
    public partial struct VisionSenseJob : IJobEntity
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
            if (vision.Range <= 0f)
                return;

            float3 forward = math.forward(transform.Rotation);
            float3 eye = transform.Position + math.mul(transform.Rotation, vision.EyeOffsetLocal);
            PerceptionMath.VisionCone cone = PerceptionMath.VisionCone.FromDegrees(vision.FieldOfViewDegrees);

            for (int t = 0; t < Targets.Length; t++)
            {
                TargetSample target = Targets[t];
                if (target.Entity == self)
                    continue;

                if (!PerceptionMath.IsInteresting(sensor.Targets, target.Faction))
                    continue;

                if (!PerceptionMath.CanSee(eye, forward, target.Position, vision.Range, cone))
                    continue;

                PerceptionBufferOps.RecordSensed(
                    detected, target,
                    math.distance(eye, target.Position),
                    SenseKind.Vision,
                    DeltaTime, sensor.MemoryDuration);
            }
        }
    }

    /// <summary>
    /// Слух: всенаправленный, только по расстоянию. Именно он ловит цель, подкравшуюся
    /// со спины на дистанцию <see cref="HearingSensor.Radius"/>, которую зрение не видит.
    /// </summary>
    [BurstCompile]
    public partial struct HearingSenseJob : IJobEntity
    {
        [ReadOnly] public NativeList<TargetSample> Targets;

        public float DeltaTime;

        void Execute(
            Entity self,
            in DetectionSensor sensor,
            in HearingSensor hearing,
            in LocalTransform transform,
            ref DetectionState state,
            DynamicBuffer<DetectedTarget> detected)
        {
            if (hearing.Radius <= 0f)
                return;

            for (int t = 0; t < Targets.Length; t++)
            {
                TargetSample target = Targets[t];
                if (target.Entity == self)
                    continue;

                if (!PerceptionMath.IsInteresting(sensor.Targets, target.Faction))
                    continue;

                if (!PerceptionMath.IsHearable(transform.Position, target.Position, hearing.Radius))
                    continue;

                PerceptionBufferOps.RecordSensed(
                    detected, target,
                    math.distance(transform.Position, target.Position),
                    SenseKind.Hearing,
                    DeltaTime, sensor.MemoryDuration);
            }
        }
    }

    /// <summary>
    /// Выбор primary-цели: ближайшая из замеченных в этом кадре, иначе — самая свежая
    /// из запомненных.
    /// </summary>
    [BurstCompile]
    public partial struct PerceptionPrimarySelectJob : IJobEntity
    {
        void Execute(
            in DetectionSensor sensor,
            in LocalTransform transform,
            ref DetectionState state,
            DynamicBuffer<DetectedTarget> detected)
        {
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

            if (primaryIndex < 0)
                return;

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

            // Job'ы идут строго последовательно: все пишут в DetectionState и
            // DetectedTarget одной и той же сущности, параллельно им быть нельзя.
            state.Dependency = new PerceptionMemoryTickJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule(state.Dependency);

            state.Dependency = new VisionSenseJob
            {
                Targets = targets,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule(state.Dependency);

            state.Dependency = new HearingSenseJob
            {
                Targets = targets,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule(state.Dependency);

            state.Dependency = new PerceptionPrimarySelectJob
            {
            }.Schedule(state.Dependency);

            state.Dependency = targets.Dispose(state.Dependency);
        }
    }
}
