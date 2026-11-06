using NUnit.Framework;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InfectionGame.Perception.Tests
{
    /// <summary>
    /// Проверяет ручную расстановку в сценах: что ECS SubScene подключён, что в нём лежат
    /// authoring-компоненты (Baker), и что старые debug-пробники не мешают. Запускать
    /// перед тем, как гонять игру — это единственная защита от сломанного YAML.
    /// </summary>
    public class PerceptionSceneTests
    {
        const string kMainScenePath = "Assets/HealingFieldScene.unity";
        const string kSubScenePath = "Assets/Scenes/PerceptionPlayground.unity";

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
        public void PerceptionPlayground_LoadsWithAuthoringComponents()
        {
            var scene = EditorSceneManager.OpenScene(kSubScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.isLoaded, "SubScene-сцена не открылась");

            int sensors = 0;
            int enemies = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                sensors += root.GetComponentsInChildren<PerceptionSensorECSBehavior>(true).Length;
                enemies += root.GetComponentsInChildren<InfectionGame.Enemies.ECS.EnemyAuthoring>(true).Length;
            }

            Assert.That(sensors, Is.GreaterThanOrEqualTo(1),
                "в SubScene нет PerceptionSensorECSBehavior — сенсоры не забэйкаются");
            Assert.That(enemies, Is.GreaterThanOrEqualTo(1),
                "в SubScene нет EnemyAuthoring — нового врага на ECS не будет");
        }

        [Test]
        public void PerceptionPlayground_SensorParamsAreSane()
        {
            var scene = EditorSceneManager.OpenScene(kSubScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var sensor in root.GetComponentsInChildren<PerceptionSensorECSBehavior>(true))
                {
                    var so = new UnityEditor.SerializedObject(sensor);
                    float range = so.FindProperty("visionRange").floatValue;
                    float fov = so.FindProperty("fieldOfViewDegrees").floatValue;
                    float memory = so.FindProperty("memoryDuration").floatValue;

                    Assert.That(range, Is.GreaterThan(0f), $"{sensor.name}: visionRange должен быть положительным");
                    Assert.That(fov, Is.InRange(0f, 360f), $"{sensor.name}: fieldOfViewDegrees вне 0..360");
                    Assert.That(memory, Is.GreaterThanOrEqualTo(0f), $"{sensor.name}: memoryDuration < 0");
                }
            }
        }

        [Test]
        public void PerceptionPlayground_DebugProbesAreInactive()
        {
            var scene = EditorSceneManager.OpenScene(kSubScenePath, OpenSceneMode.Single);

            // Сцена держит ровно одного нового врага и одного NPC. Старые vision-пробники
            // остаются в файле как заготовки, но выключены — иначе это лишние агенты,
            // которые бэйкатся в сущности и мешают тестировать реальную перцепцию.
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var probe in root.GetComponentsInChildren<PerceptionSensorECSBehavior>(true))
                {
                    if (probe.name.Contains("DISABLED"))
                        Assert.That(probe.gameObject.activeSelf, Is.False,
                            $"{probe.name}: пробник должен быть выключен");
                }
            }
        }

        [Test]
        public void HealingFieldScene_SubScenePointsAtARealSceneAsset()
        {
            var scene = EditorSceneManager.OpenScene(kMainScenePath, OpenSceneMode.Single);

            SubScene found = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                found = root.GetComponentInChildren<SubScene>(true);
                if (found != null)
                    break;
            }

            Assert.That(found, Is.Not.Null, "SubScene-компонент не найден");

            Assert.That(found.SceneAsset, Is.Not.Null,
                "SubScene не ссылается на ассет сцены — _SceneAsset пустой или битый");
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(found.SceneAsset),
                Is.EqualTo(kSubScenePath),
                "SubScene ссылается не на ту сцену — проверь guid в _SceneAsset");
            Assert.That(found.AutoLoadScene, Is.True,
                "AutoLoadScene выключен — сцена не загрузится при входе в Play");
        }

        [Test]
        public void PerceptionPlayground_MaterialsSupportDotsInstancing()
        {
            var scene = EditorSceneManager.OpenScene(kSubScenePath, OpenSceneMode.Single);

            int renderers = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderers++;
                    foreach (var material in renderer.sharedMaterials)
                    {
                        Assert.That(material, Is.Not.Null,
                            $"{renderer.name}: материал не назначен — рендер сломается");
                        Assert.That(material.shader, Is.Not.Null,
                            $"{renderer.name}: у материала нет шейдера");

                        string shaderName = material.shader.name;
                        Assert.That(shaderName, Does.StartWith("Universal Render Pipeline").Or.StartWith("HDRP"),
                            $"{renderer.name}: шейдер '{shaderName}' не умеет DOTS_INSTANCING_ON. "
                            + "Entities Graphics требует URP/HDRP материал — Standard даст "
                            + "'BatchDrawCommand ... does not define a DOTS_INSTANCING_ON variant'");
                    }
                }
            }

            Assert.That(renderers, Is.GreaterThanOrEqualTo(3),
                "в SubScene должно быть минимум 3 рендерера (2 сенсора + цель)");
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
