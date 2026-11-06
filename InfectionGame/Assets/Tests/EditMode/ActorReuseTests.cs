using InfectionGame.Actors;
using InfectionGame.Enemies.ECS;
using InfectionGame.Enemies.Species;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace InfectionGame.Enemies.Tests
{
    /// <summary>
    /// Общий слой <c>InfectionGame.Actors</c> выделен под переделку NPC: враг не должен
    /// владеть кинематикой, точкой брожения, мостом и описанием вида в одиночку. Эти тесты
    /// ловят случай, когда общий слой разъезжается с врагом.
    /// </summary>
    public class ActorReuseTests
    {
        const string kSpeciesAssetPath = "Assets/ScriptableObjects/EnemySpecies_Default.asset";

        [Test]
        public void EnemyCompanionAgent_ImplementsTheSharedBridgeContract()
        {
            Assert.That(typeof(IActorCompanionAgent).IsAssignableFrom(typeof(EnemyCompanionAgent)),
                Is.True,
                "мост врага должен реализовывать IActorCompanionAgent — иначе узлы поведения " +
                "нельзя переиспользовать для NPC");
        }

        [Test]
        public void EnemyCompanionAgent_DerivesFromTheSharedAgentBase()
        {
            Assert.That(typeof(ActorCompanionAgentBase).IsAssignableFrom(typeof(EnemyCompanionAgent)),
                Is.True,
                "общая работа моста должна жить в ActorCompanionAgentBase");
        }

        [Test]
        public void EnemySpeciesSO_DerivesFromTheSharedSpeciesBase()
        {
            Assert.That(typeof(ActorSpeciesSO).IsAssignableFrom(typeof(EnemySpeciesSO)), Is.True,
                "общие поля вида (зрение, слух, движение, память, граф) должны быть в ActorSpeciesSO");
        }

        [Test]
        public void EnemySpeciesDefaultAsset_KeepsSharedSpeciesFields()
        {
            // Перенос полей в базовый класс не должен терять значения: Unity ищет поля по
            // имени, но если поле исчезло — данные молча пропадут из ассета.
            var asset = AssetDatabase.LoadAssetAtPath<Object>(kSpeciesAssetPath);
            Assert.That(asset, Is.Not.Null, "EnemySpecies_Default.asset не найден");

            var so = new SerializedObject(asset);

            foreach (string field in new[]
                     {
                         "visionRange", "fieldOfViewDegrees", "eyeOffsetLocal",
                         "hearingRadius", "moveSpeed", "passiveMoveSpeed",
                         "memoryDuration", "behaviorGraph",
                         "attackDistance", "attackInterval", "attackDamage",
                     })
            {
                Assert.That(so.FindProperty(field), Is.Not.Null,
                    $"поле '{field}' пропало из вида — проверь перенос в ActorSpeciesSO");
            }

            Assert.That(so.FindProperty("visionRange").floatValue, Is.GreaterThan(0f));
            Assert.That(so.FindProperty("fieldOfViewDegrees").floatValue, Is.InRange(0f, 360f));
            Assert.That(so.FindProperty("hearingRadius").floatValue, Is.GreaterThanOrEqualTo(0f));
            Assert.That(so.FindProperty("moveSpeed").floatValue, Is.GreaterThan(0f));
            Assert.That(so.FindProperty("passiveMoveSpeed").floatValue, Is.GreaterThan(0f));
        }

        [Test]
        public void EnemySystems_AreAutoCreatedInTheDefaultWorld()
        {
            using (var world = Unity.Entities.DefaultWorldInitialization.Initialize("ActorReuseCheck"))
            {
                // EnemyAgentSystem — неуправляемый ISystem, у него другой способ получить handle.
                Assert.That(world.Unmanaged.GetExistingUnmanagedSystem<EnemyAgentSystem>(),
                    Is.Not.EqualTo(default(Unity.Entities.SystemHandle)),
                    "EnemyAgentSystem не поднялась в default world");
                Assert.That(world.GetExistingSystem<EnemyCompanionSpawnSystem>(),
                    Is.Not.EqualTo(default(Unity.Entities.SystemHandle)),
                    "EnemyCompanionSpawnSystem не поднялась в default world");
            }
        }
    }
}
