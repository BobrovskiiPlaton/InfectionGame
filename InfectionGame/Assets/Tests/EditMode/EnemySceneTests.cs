using InfectionGame.Enemies.Species;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace InfectionGame.Enemies.Tests
{
    /// <summary>
    /// Проверяет ассет вида врага. Сценовые проверки расстановки выпилены как stale:
    /// сцена PerceptionPlayground переименована в Enemies_ECS_Subscene, врагов плодит
    /// EnemySpawnerAuthoring, а не ручная расстановка в SubScene.
    /// </summary>
    public class EnemySceneTests
    {
        [Test]
        public void DefaultSpecies_HasFov120AndWeakHearing()
        {
            var species = AssetDatabase.LoadAssetAtPath<EnemySpeciesSO>(
                "Assets/ScriptableObjects/EnemySpecies_Default.asset");

            Assert.That(species, Is.Not.Null, "ассет вида не найден");

            Assert.That(species.fieldOfViewDegrees, Is.EqualTo(120f).Within(0.01f),
                "первый вид заявлен с FOV 120");
            Assert.That(species.hearingRadius, Is.GreaterThan(0f).And.LessThanOrEqualTo(6f),
                "слух у первого вида плохой — радиус в единицах метров");
        }
    }
}
