using Unity.Mathematics;

namespace InfectionGame.Actors
{
    /// <summary>
    /// Контракт companion-моста любого актора (врага, NPC и т.д.).
    ///
    /// Unity Behavior работает только с GameObject, а данные живут в ECS, поэтому у каждого
    /// актора есть GameObject-компаньон, который читает перцепцию из ECS и пишет туда
    /// решения графа. Узлы поведения (<c>Has Perceived Target</c>, <c>Is Target In Range</c>,
    /// <c>Choose Wander Target</c>) работают именно с этим интерфейсом, а не с конкретным
    /// классом — тогда один и тот же узел переиспользуется всеми акторами без копирования.
    ///
    /// Состояние сюда намеренно не вынесено: enum состояния у каждого актора свой и он
    /// виден графу, поэтому переключение состояния оставляем в конкретном классе моста.
    /// </summary>
    public interface IActorCompanionAgent
    {
        /// <summary>Есть ли привязка к своей ECS-сущности.</summary>
        bool IsLinked { get; }

        /// <summary>Что актор сейчас воспринимает (зрение + слух + память).</summary>
        bool TryGetPerception(out InfectionGame.Perception.DetectionState state);

        /// <summary>Текущая точка брожения.</summary>
        bool TryGetWanderTarget(out WanderTarget wander);

        /// <summary>Задать точку брожения.</summary>
        void SetWanderTarget(float3 position);

        /// <summary>Снять точку брожения — актор остановится.</summary>
        void ClearWanderTarget();
    }
}
