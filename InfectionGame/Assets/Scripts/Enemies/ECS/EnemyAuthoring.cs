using InfectionGame.Actors;
using InfectionGame.Enemies.Species;
using Unity.Entities;
using UnityEngine;

namespace InfectionGame.Enemies.ECS
{
    public class EnemyAuthoring : MonoBehaviour
    {
        [SerializeField] private EnemySpeciesSO species;

        sealed class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                EnemySpeciesSO species = authoring.species;

                if (species == null)
                {
                    Debug.LogError($"{nameof(EnemyAuthoring)} на '{authoring.name}': вид не назначен.", authoring);
                    return;
                }

                AddComponent(entity, new EnemyTag());
                AddComponent(entity, new EnemySpecies
                {
                    MoveSpeed = species.moveSpeed,
                    PassiveMoveSpeed = species.passiveMoveSpeed,
                    AttackDistance = species.attackDistance,
                    AttackInterval = species.attackInterval,
                    AttackDamage = species.attackDamage,
                    BehaviorGraph = species.behaviorGraph,
                });
                AddComponent(entity, new EnemyAgent());
                AddComponent(entity, new WanderTarget());

                // Состояние — enableable-теги. Стартуем в Idle, остальные выключены;
                // переключает их behavior tree через companion-мост.
                AddComponent<EnemyStateIdle>(entity);
                AddComponent<EnemyStateChase>(entity);
                AddComponent<EnemyStateAttack>(entity);
                SetComponentEnabled<EnemyStateChase>(entity, false);
                SetComponentEnabled<EnemyStateAttack>(entity, false);

                AddComponent(entity, new InfectionGame.Perception.DetectionTarget
                {
                    Faction = InfectionGame.Perception.TargetFaction.Enemy,
                });

                AddComponent(entity, new InfectionGame.Perception.DetectionSensor
                {
                    Targets = InfectionGame.Perception.TargetFaction.Player
                              | InfectionGame.Perception.TargetFaction.NPC,
                    MemoryDuration = species.memoryDuration,
                });

                AddComponent(entity, new InfectionGame.Perception.VisionSensor
                {
                    Range = species.visionRange,
                    FieldOfViewDegrees = species.fieldOfViewDegrees,
                    EyeOffsetLocal = species.eyeOffsetLocal,
                });

                AddComponent(entity, new InfectionGame.Perception.HearingSensor
                {
                    Radius = species.hearingRadius,
                });

                AddBuffer<InfectionGame.Perception.DetectedTarget>(entity);
                AddComponent(entity, new InfectionGame.Perception.DetectionState());
            }
        }
    }
}
