using NUnit.Framework;
using Unity.Scenes;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InfectionGame.Perception.Tests
{
    /// <summary>
    /// Проверяет ручную расстановку в главной сцене: что ECS SubScene подключён и что
    /// старые debug-пробники не мешают. Сценовые проверки PerceptionPlayground выпилены
    /// как stale (сцена переименована в Enemies_ECS_Subscene).
    /// </summary>
    public class PerceptionSceneTests
    {
        const string kMainScenePath = "Assets/HealingFieldScene.unity";

        [Test]
        public void HealingFieldScene_LoadsWithAnEcsSubScene()
        {
            var scene = EditorSceneManager.OpenScene(kMainScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.isLoaded, "основная сцена не открылась");

            int subScenes = 0;
            foreach (var root in scene.GetRootGameObjects())
                subScenes += root.GetComponentsInChildren<SubScene>(true).Length;

            Assert.That(subScenes, Is.GreaterThanOrEqualTo(1),
                "в HealingFieldScene нет SubScene-компонента — ECS-бэйкинга не будет");
        }

        [Test]
        public void HealingFieldScene_KeepsTheOldDebugProbesDisabled()
        {
            var scene = EditorSceneManager.OpenScene(kMainScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var probe in root.GetComponentsInChildren<PerceptionDebugProbe>(true))
                {
                    // Старые ручные пробники оставлены как запасной вариант — они должны
                    // быть выключены, иначе создадут дублирующие ECS-сущности рядом с
                    // забэйканными из SubScene.
                    Assert.That(probe.gameObject.activeSelf, Is.False,
                        $"{probe.name}: старый debug-пробник активен — будут дубли с SubScene");
                }
            }
        }
    }
}
