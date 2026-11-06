using UnityEngine;

namespace InfectionGame.Enemies.Species
{
    /// <summary>
    /// Вид врага. Описывает и сенсоры (что враг воспринимает), и поведение (как двигается
    /// и атакует). Один ассет — один вид; чтобы завести новый вид, создай копию ассета
    /// с другими числами, код менять не нужно.
    /// </summary>
    [CreateAssetMenu(menuName = "InfectionGame/Enemy Species", fileName = "EnemySpecies")]
    public class EnemySpeciesSO : ScriptableObject
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
        [Tooltip("Скорость преследования, м/с.")]
        [Min(0f)] public float moveSpeed = 4f;

        [Tooltip("Скорость бесцельного брожения, м/с.")]
        [Min(0f)] public float passiveMoveSpeed = 1f;

        [Header("Атака")]
        [Tooltip("На какой дистанции враг переходит к атаке, метры.")]
        [Min(0f)] public float attackDistance = 2f;

        [Tooltip("Пауза между атаками, секунды.")]
        [Min(0f)] public float attackInterval = 1.5f;

        [Tooltip("Урон за одну атаку.")]
        [Min(0f)] public float attackDamage = 10f;

        [Header("Память")]
        [Tooltip("Сколько секунд цель помнится после потери из виду / из слуха.")]
        [Min(0f)] public float memoryDuration = 3f;

        [Header("Поведение")]
        [Tooltip("Behavior graph, который управляет этим видом. Собирается в редакторе "
                 + "Unity Behavior (Create > Behavior > Behavior Graph) и назначается сюда.")]
        public Unity.Behavior.BehaviorGraph behaviorGraph;
    }
}
