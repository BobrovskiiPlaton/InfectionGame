using Unity.Collections;
using Unity.Entities;
using static InfectionComponent;

// Счётчик заражённых NPC для HUD: читает состояние из ECS.
public static class ECSInfectionCounter
{
    private static World cachedWorld;
    private static EntityQuery infectedQuery;

    public static int GetInfectedCount()
    {
        World world = World.DefaultGameObjectInjectionWorld;

        if (world == null || !world.IsCreated)
            return 0;

        if (cachedWorld != world)
        {
            cachedWorld = world;
            infectedQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<InfectionComponent>(),
                ComponentType.ReadOnly<NPCComponent>());
        }

        NativeArray<InfectionComponent> infections =
            infectedQuery.ToComponentDataArray<InfectionComponent>(
                Allocator.Temp);

        int count = 0;

        foreach (InfectionComponent infection in infections)
        {
            if (infection.State == InfectionState.Infected)
            {
                count++;
            }
        }

        infections.Dispose();

        return count;
    }
}
