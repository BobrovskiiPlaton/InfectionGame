using InfectionGame.Enemies.ECS;
using InfectionGame.Perception;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InfectionGame.Enemies.Tests
{
    /// <summary>
    /// Сцена должна держать ровно одного NPC и одного нового врага на ECS помимо игрока.
    /// Старые спавнеры выключены: врагов они больше не плодят, NPC выдают одного.
    ///
    /// Конфиги спавна лежат в <c>Assembly-CSharp</c>, а тестовая сборка его не видит,
    /// поэтому поля читаются через <c>SerializedObject</c> по имени.
    /// </summary>
    public class ScenePopulationTests
    {
        const string kSubScenePath = "Assets/Scenes/PerceptionPlayground.unity";
        const string kEnemySpawnConfigPath = "Assets/ScriptableObjects/EnemySpawnConfig.asset";
        const string kNpcSpawnConfigPath = "Assets/ScriptableObjects/NPCSpawnConfig.asset";
        const string kNpcPrefabPath = "Assets/Prefabs/NPC.prefab";

        [Test]
        public void OldEnemySpawning_IsOff()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(kEnemySpawnConfigPath);
            Assert.That(asset, Is.Not.Null, "EnemySpawnConfig.asset не найден");

            var so = new SerializedObject(asset);
            var max = so.FindProperty("maxEnemyCount");

            Assert.That(max, Is.Not.Null,
                "в EnemySpawnConfig нет поля maxEnemyCount — ассет разошёлся со скриптом");
            Assert.That(max.intValue, Is.EqualTo(0),
                "старых врагов быть не должно — их заменяет один ECS-враг");
        }

        [Test]
        public void OldNpcSpawning_ProducesExactlyOne()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(kNpcSpawnConfigPath);
            Assert.That(asset, Is.Not.Null, "NPCSpawnConfig.asset не найден");

            var so = new SerializedObject(asset);
            var count = so.FindProperty("npcCount");

            Assert.That(count, Is.Not.Null, "в NPCSpawnConfig нет поля npcCount");
            Assert.That(count.intValue, Is.EqualTo(1),
                "в сцене должен быть ровно один NPC");
        }

        [Test]
        public void NpcPrefab_IsVisibleToEcsPerception()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kNpcPrefabPath);
            Assert.That(prefab, Is.Not.Null, "NPC.prefab не найден");

            // NPC — обычный GameObject, без зеркала в ECS его новый враг не увидит вообще.
            Assert.That(prefab.GetComponent<PerceptionDebugTarget>(), Is.Not.Null,
                "на NPC.prefab должен висеть PerceptionDebugTarget — иначе враг его не заметит");
        }

        [Test]
        public void SubScene_HasExactlyOneEnemy()
        {
            var scene = EditorSceneManager.OpenScene(kSubScenePath, OpenSceneMode.Single);

            int enemies = 0;
            foreach (var root in scene.GetRootGameObjects())
                enemies += root.GetComponentsInChildren<EnemyAuthoring>(true).Length;

            Assert.That(enemies, Is.EqualTo(1),
                "в SubScene должен быть ровно один новый враг");
        }
    }
}
