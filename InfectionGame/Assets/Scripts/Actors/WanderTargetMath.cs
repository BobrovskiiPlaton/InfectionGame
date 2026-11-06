using Unity.Mathematics;

namespace InfectionGame.Actors
{
    /// <summary>
    /// Политика работы с <see cref="WanderTarget"/>. Чистая математика без ECS и Random —
    /// поэтому её можно покрыть тестами и переиспользовать и в job'е движения, и в узле
    /// поведения, не рискуя расхождением между ними.
    ///
    /// Главное правило здесь: подбирать точку можно только когда её нет. Граф стоит под
    /// <c>Repeat</c> и вызывает узел каждый тик; если перебирать точку безусловно, актор
    /// получает ~60 новых целей в секунду и дёргается на каждом кадре.
    /// </summary>
    public static class WanderTargetMath
    {
        /// <summary>Дистанция в квадрате, при которой точка считается достигнутой.</summary>
        public const float ArriveDistanceSq = 0.25f;

        /// <summary>
        /// Нужно ли подбирать новую точку.
        /// </summary>
        /// <param name="target">Текущая точка.</param>
        /// <param name="restartAfterSeconds">
        /// Через сколько секунд безрезультатного подхода выбросить точку (спасение от
        /// актора, упёршегося в стену). 0 или меньше — никогда, ждать достижения бесконечно.
        /// </param>
        public static bool ShouldAllocate(in WanderTarget target, float restartAfterSeconds)
        {
            // Точки нет — бери новую.
            if (target.HasTarget <= 0.5f)
                return true;

            // Точка есть, таймаут не настроен — не трогаем.
            if (restartAfterSeconds <= 0f)
                return false;

            // Точка есть, но застряли — перебираем.
            return target.TimeSinceAssigned >= restartAfterSeconds;
        }

        /// <summary>Тикает таймер точки. Вызывать только когда точка назначена.</summary>
        public static void Tick(ref WanderTarget target, float deltaTime)
        {
            if (target.HasTarget > 0.5f)
                target.TimeSinceAssigned += deltaTime;
        }

        /// <summary>Достигнута ли точка.</summary>
        public static bool HasArrived(float3 self, in WanderTarget target)
        {
            return target.HasTarget > 0.5f
                   && math.distancesq(self, target.Position) < ArriveDistanceSq;
        }

        /// <summary>Новая назначенная точка с обнулённым таймером.</summary>
        public static WanderTarget Create(float3 position)
        {
            return new WanderTarget
            {
                Position = position,
                HasTarget = 1f,
                TimeSinceAssigned = 0f,
            };
        }

        /// <summary>Сброшенная точка — актор стоит на месте.</summary>
        public static WanderTarget Cleared()
        {
            return new WanderTarget
            {
                Position = float3.zero,
                HasTarget = 0f,
                TimeSinceAssigned = 0f,
            };
        }

        /// <summary>
        /// Точка в диске вокруг центра. <paramref name="unitCircleOffset"/> — смещение из
        /// <c>Random.insideUnitCircle</c> (или любое внутри единичной окружности), радиус
        /// прикладывается здесь. Высота центра сохраняется.
        /// </summary>
        public static float3 PointInDisk(float3 center, float2 unitCircleOffset, float radius)
        {
            return new float3(
                center.x + unitCircleOffset.x * radius,
                center.y,
                center.z + unitCircleOffset.y * radius);
        }
    }
}
