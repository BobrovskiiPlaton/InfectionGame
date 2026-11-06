using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Perception
{
    [DisallowMultipleComponent]
    public class PerceptionDebugProbe : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float range = 20f;

        [SerializeField, Range(0f, 360f)] private float fieldOfViewDegrees = 120f;

        [SerializeField] private Vector3 eyeOffsetLocal = new Vector3(0f, 1.6f, 0f);

        [SerializeField] private TargetFaction targets = TargetFaction.Player;

        [SerializeField, Min(0f)] private float memoryDuration = 0.5f;

        [SerializeField] private bool visionEnabled = true;

        [SerializeField] private bool drawCone = true;
        [SerializeField, Range(3, 64)] private int coneSegments = 24;
        [SerializeField] private Color coneColor = new Color(0f, 1f, 1f, 0.35f);
        [SerializeField] private Color seenColor = Color.red;
        [SerializeField] private Color rememberedColor = Color.yellow;
        [SerializeField] private Color hiddenColor = new Color(1f, 1f, 1f, 0.25f);

        Entity _entity;
        bool _created;

        void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var em = world.EntityManager;
            if (!_created || !em.Exists(_entity))
            {
                _entity = em.CreateEntity();
                em.AddComponentData(_entity, LocalTransform.Identity);
                em.AddComponentData(_entity, new DetectionSensor());
                em.AddComponentData(_entity, new VisionSensor());
                em.AddComponentData(_entity, new DetectionState());
                em.AddBuffer<DetectedTarget>(_entity);
                _created = true;
            }

            Vector3 p = transform.position;
            Quaternion r = transform.rotation;
            em.SetComponentData(_entity,
                LocalTransform.FromPositionRotation(new float3(p.x, p.y, p.z), new quaternion(r.x, r.y, r.z, r.w)));
            em.SetComponentData(_entity, new DetectionSensor
            {
                Targets = targets,
                MemoryDuration = memoryDuration,
            });
            em.SetComponentData(_entity, new VisionSensor
            {
                Range = range,
                FieldOfViewDegrees = fieldOfViewDegrees,
                EyeOffsetLocal = new float3(eyeOffsetLocal.x, eyeOffsetLocal.y, eyeOffsetLocal.z),
            });
            em.SetComponentEnabled<VisionSensor>(_entity, visionEnabled);
        }

        void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated && _created && world.EntityManager.Exists(_entity))
                world.EntityManager.DestroyEntity(_entity);
        }

        void OnDrawGizmos()
        {
            if (!drawCone)
                return;

            DrawCone();
            DrawDetection();
        }

        Vector3 EyeWorld => transform.TransformPoint(eyeOffsetLocal);

        void DrawCone()
        {
            Vector3 eye = EyeWorld;
            float half = fieldOfViewDegrees * 0.5f;
            Vector3 axisUp = transform.up;
            Vector3 forward = transform.forward;

            Vector3 leftDir = Quaternion.AngleAxis(-half, axisUp) * forward;
            Vector3 rightDir = Quaternion.AngleAxis(half, axisUp) * forward;

            // Краевые лучи.
            UnityEngine.Debug.DrawLine(eye, eye + leftDir * range, coneColor);
            UnityEngine.Debug.DrawLine(eye, eye + rightDir * range, coneColor);

            // Дуга между краевыми лучами.
            Vector3 prev = eye + leftDir * range;
            for (int i = 1; i <= coneSegments; i++)
            {
                float t = (float)i / coneSegments;
                float angle = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.AngleAxis(angle, axisUp) * forward;
                Vector3 point = eye + dir * range;
                UnityEngine.Debug.DrawLine(prev, point, coneColor);
                prev = point;
            }

            // Ось взгляда, чтобы было видно направление.
            UnityEngine.Debug.DrawLine(eye, eye + forward * range, new Color(coneColor.r, coneColor.g, coneColor.b, 1f));
        }

        void DrawDetection()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var em = world.EntityManager;
            if (!_created || !em.Exists(_entity))
                return;

            Vector3 eye = EyeWorld;
            DynamicBuffer<DetectedTarget> detected = em.GetBuffer<DetectedTarget>(_entity);

            for (int i = 0; i < detected.Length; i++)
            {
                DetectedTarget t = detected[i];
                Vector3 targetPos = new Vector3(t.LastKnownPosition.x, t.LastKnownPosition.y, t.LastKnownPosition.z);

                // Видим сейчас — красный, помним — жёлтый.
                UnityEngine.Debug.DrawLine(eye, targetPos,
                    t.SensedBy == SenseKind.Vision ? seenColor : rememberedColor);

                UnityEngine.Debug.DrawRay(targetPos, Vector3.up * 0.5f,
                    t.SensedBy == SenseKind.Vision ? seenColor : rememberedColor);
            }
        }
    }
}
