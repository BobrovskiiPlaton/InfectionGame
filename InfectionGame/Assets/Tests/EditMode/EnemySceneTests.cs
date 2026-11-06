using System.Linq;
using InfectionGame.Enemies.ECS;
using InfectionGame.Enemies.Species;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InfectionGame.Enemies.Tests
{
    /// <summary>
    /// Враги должны лежать в SubScene, чтобы Baker развернул их в ECS-сущности. Тест ловит
    /// случай, когда объект врага случайно вынесли из SubScene или отвалился вид.
    /// </summary>
    public class EnemySceneTests
    {
        const string kSubScenePath = "Assets/Scenes/PerceptionPlayground.unity";

        [Test]
        public void SubScene_ContainsEnemyAuthoring()
        {
            var scene = EditorSceneManager.OpenScene(kSubScenePath, OpenSceneMode.Single);

            var authors = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemyAuthoring>(true))
                .ToArray();

            Assert.That(authors.Length, Is.GreaterThanOrEqualTo(1),
                "в SubScene должен быть хотя бы один объект с EnemyAuthoring");
        }

        [Test]
        public void EnemyAuthoring_HasSpeciesAssigned()
        {
            var scene = EditorSceneManager.OpenScene(kSubScenePath, OpenSceneMode.Single);

            var authors = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnemyAuthoring>(true))
                .ToArray();

            foreach (var authoring in authors)
            {
                var species = GetSpecies(authoring);

                Assert.That(species, Is.Not.Null,
                    $"{authoring.name}: у EnemyAuthoring не назначен вид (EnemySpeciesSO)");

                Assert.That(species.fieldOfViewDegrees, Is.InRange(0f, 360f),
                    $"{authoring.name}: FOV вне диапазона");
                Assert.That(species.visionRange, Is.GreaterThanOrEqualTo(0f),
                    $"{authoring.name}: дальность зрения не может быть отрицательной");
                Assert.That(species.hearingRadius, Is.GreaterThanOrEqualTo(0f),
                    $"{authoring.name}: радиус слуха не может быть отрицательным");
                Assert.That(species.memoryDuration, Is.GreaterThanOrEqualTo(0f),
                    $"{authoring.name}: память не может быть отрицательной");
            }
        }

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

        static EnemySpeciesSO GetSpecies(EnemyAuthoring authoring)
        {
            var field = typeof(EnemyAuthoring).GetField(
                "species",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            return field?.GetValue(authoring) as EnemySpeciesSO;
        }
    }
}
