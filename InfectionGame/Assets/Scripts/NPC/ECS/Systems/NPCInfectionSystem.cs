using InfectionGame.Perception;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static InfectionComponent;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct NPCInfectionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        ComponentLookup<DetectionTarget> detectionLookup =
            SystemAPI.GetComponentLookup<DetectionTarget>(false);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (infection, entity) in
            SystemAPI.Query<RefRW<InfectionComponent>>()
                .WithAll<NPCComponent>()
                .WithNone<NPCConversionRequest>()
                .WithEntityAccess())
        {
            if (infection.ValueRO.State != InfectionState.Infected)
                continue;

            // Долечили — выздоровление
            if (infection.ValueRO.TreatmentProgress <= 0f)
            {
                infection.ValueRW.State = InfectionState.Recovered;
                infection.ValueRW.TimeRemaining = 0f;
                infection.ValueRW.ReceivedHits = 0;
                infection.ValueRW.TreatmentProgress = 0f;

                // Выздоровевший NPC снова виден врагам
                if (detectionLookup.HasComponent(entity))
                    detectionLookup.SetComponentEnabled(entity, true);

                continue;
            }

            // Отсчёт времени заражения
            infection.ValueRW.TimeRemaining = math.max(
                0f,
                infection.ValueRO.TimeRemaining - deltaTime
            );

            // Время вышло — пора конвертироваться
            if (infection.ValueRO.TimeRemaining <= 0f)
            {
                ecb.AddComponent<NPCConversionRequest>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
