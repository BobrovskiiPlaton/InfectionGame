using Unity.Entities;

namespace InfectionGame.Actors
{
    /// <summary>
    /// Запрос удара по NPC. Буфер висит на NPC-сущности: AttackJob врага добавляет
    /// сюда элемент при попадании, NPCInfectionHitSystem подсчитывает и очищает.
    /// Лежит в общей сборке Actors, чтобы враг (InfectionGame.Enemies.ECS) мог писать.
    /// </summary>
    public struct NPCInfectionHitRequest : IBufferElementData
    {
        // Кто ударил (сущность врага)
        public Entity Source;

        // Сила удара
        public float Amount;
    }
}
