using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace InfectionGame.Perception
{
    /// <summary>
    /// Помечает GameObject как цель для ECS-перцепции: создаёт ECS-сущность с
    /// <see cref="DetectionTarget"/> и каждый кадр синхронизирует в неё позицию и поворот
    /// этого объекта. Нужен только для ручного теста в сцене — в боевом коде цель должна
    /// создаваться Baker'ом через SubScene.
    /// </summary>
    [DisallowMultipleComponent]
    public class PerceptionDebugTarget : MonoBehaviour
    {
        [SerializeField] private TargetFaction faction = TargetFaction.Player;

        [Tooltip("Выключите, чтобы сделать объект невидимым для всех сенсоров, не убирая его из сцены.")]
        [SerializeField] private bool detectable = true;

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
                em.AddComponentData(_entity, new DetectionTarget { Faction = faction });
                _created = true;
            }

            Vector3 p = transform.position;
            Quaternion r = transform.rotation;
            em.SetComponentData(_entity,
                LocalTransform.FromPositionRotation(new float3(p.x, p.y, p.z), new quaternion(r.x, r.y, r.z, r.w)));
            em.SetComponentData(_entity, new DetectionTarget { Faction = faction });
            em.SetComponentEnabled<DetectionTarget>(_entity, detectable);
        }

        void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated && _created && world.EntityManager.Exists(_entity))
                world.EntityManager.DestroyEntity(_entity);
        }
    }
}
