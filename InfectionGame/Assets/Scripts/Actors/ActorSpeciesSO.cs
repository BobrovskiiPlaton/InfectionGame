using UnityEngine;

namespace InfectionGame.Actors
{
    /// <summary>
    /// База описания любого актора: что он воспринимает, как двигается, сколько помнит
    /// и какой graph им управляет. Общее для врага и NPC вынесено сюда, чтобы виды не
    /// расходились по сенсорам и памяти.
    ///
    /// Наследники добавляют только своё: <c>EnemySpeciesSO</c> — атака,
    /// <c>NpcSpeciesSO</c> — бегство. Один ассет = один вид; новый вид — это копия ассета
    /// с другими числами, код менять не нужно.
    ///
    /// Имена полей наследовать нельзя переименовывать: на них завязаны Baker'ы и уже
    /// созданные ассеты видов.
    /// </summary>
    public class ActorSpeciesSO : ScriptableObject
    {
        [Header("Зрение")]
        [Tooltip("Дальность обзора в метрах.")]
        public float visionRange = 20f;

        [Tooltip("Полный угол конуса обзора в градусах. 360 — круговой обзор.")]
        [Range(0f, 360f)] public float fieldOfViewDegrees = 120f;

        [Tooltip("Локальное смещение глаз относительно центра объекта.")]
        public Vector3 eyeOffsetLocal = new Vector3(0f, 1.6f, 0f);

        [Header("Слух")]
        [Tooltip("Радиус слуха в метрах. Всенаправленный, направление взгляда не важно. "
                 + "0 — глухой. Малое значение даёт 'плохой слух': цель замечена только "
                 + "если подобралась вплотную — в том числе со спины.")]
        [Min(0f)] public float hearingRadius = 4f;

        [Header("Движение")]
        [Tooltip("Скорость при активном поведении (погоня, бегство), м/с.")]
        [Min(0f)] public float moveSpeed = 4f;

        [Tooltip("Скорость бесцельного брожения, м/с.")]
        [Min(0f)] public float passiveMoveSpeed = 1f;

        [Header("Память")]
        [Tooltip("Сколько секунд цель помнится после потери из виду / из слуха.")]
        [Min(0f)] public float memoryDuration = 3f;

        [Header("Поведение")]
        [Tooltip("Behavior graph, который управляет этим видом. Собирается в редакторе "
                 + "Unity Behavior (Create > Behavior > Behavior Graph) и назначается сюда.")]
        public Unity.Behavior.BehaviorGraph behaviorGraph;
    }
}
