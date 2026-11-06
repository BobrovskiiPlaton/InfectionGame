using InfectionGame.Actors;
using UnityEngine;

namespace InfectionGame.Enemies.Species
{
    [CreateAssetMenu(menuName = "InfectionGame/Enemy Species", fileName = "EnemySpecies")]
    public class EnemySpeciesSO : ActorSpeciesSO
    {
        [Min(0f)] public float attackDistance = 2f;

        [Min(0f)] public float attackInterval = 1.5f;

        [Min(0f)] public float attackDamage = 10f;
    }
}
