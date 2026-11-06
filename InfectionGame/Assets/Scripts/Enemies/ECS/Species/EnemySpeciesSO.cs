using InfectionGame.Actors;
using UnityEngine;

namespace InfectionGame.Enemies.Species
{
    /// <summary>
    /// Вид врага. Описывает и сенсоры (что враг воспринимает), и поведение (как двигается
    /// и атакует). Один ассет — один вид; чтобы завести новый вид, создай копию ассета
    /// с другими числами, код менять не нужно.
    /// </summary>
    [CreateAssetMenu(menuName = "InfectionGame/Enemy Species", fileName = "EnemySpecies")]
    public class EnemySpeciesSO : ActorSpeciesSO
    {
        [Header("Атака")]
        [Tooltip("На какой дистанции враг переходит к атаке, метры.")]
        [Min(0f)] public float attackDistance = 2f;

        [Tooltip("Пауза между атаками, секунды.")]
        [Min(0f)] public float attackInterval = 1.5f;

        [Tooltip("Урон за одну атаку.")]
        [Min(0f)] public float attackDamage = 10f;
    }
}
