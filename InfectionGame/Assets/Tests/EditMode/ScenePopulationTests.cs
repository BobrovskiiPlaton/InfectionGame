using InfectionGame.Perception;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace InfectionGame.Enemies.Tests
{
    /// <summary>
    /// Проверки популяции сцены. Старые проверки счётчиков спавна и расстановки врага
    /// выпилены как stale: спавном рулит NPCSpawnerAuthoring/EnemySpawnerAuthoring,
    /// а конфиги давно не фиксируют «ровно одного NPC».
    /// </summary>
    public class ScenePopulationTests
    {
        const string kNpcPrefabPath = "Assets/Prefabs/NPC.prefab";

        [Test]
        public void NpcPrefab_IsVisibleToEcsPerception()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kNpcPrefabPath);
            Assert.That(prefab, Is.Not.Null, "NPC.prefab не найден");

            // NPC — обычный GameObject, без зеркала в ECS его новый враг не увидит вообще.
            Assert.That(prefab.GetComponent<PerceptionDebugTarget>(), Is.Not.Null,
                "на NPC.prefab должен висеть PerceptionDebugTarget — иначе враг его не заметит");
        }
    }
}
