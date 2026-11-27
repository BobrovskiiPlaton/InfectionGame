using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace InfectionGame.Gameplay.Tests
{
    public class MedkitStationTests : GameplayTestBase
    {
        private Component CreateStation()
        {
            var config = Track(ScriptableObject.CreateInstance(GameType("MedkitConfigSO")));
            SetField(config, "playerHealAmount", 30f);
            SetField(config, "cooldownDuration", 0.2f);
            SetField(config, "interactionRadius", 3f);
            var station = Track(new GameObject("Test Medkit")).AddComponent(GameType("MedkitStation"));
            SetField(station, "config", config);
            return station;
        }

        private static bool TryHeal(Component station, Component player) =>
            (bool)Call(station, "TryHeal", player);

        [Test]
        public void TryHeal_RejectsFullHealthAndDistantPlayer()
        {
            var player = CreatePlayer();
            var station = CreateStation();
            Assert.That(TryHeal(station, player), Is.False);
            Call(player, "TakeDamage", 60f);
            player.transform.position = Vector3.right * 4f;
            Assert.That(TryHeal(station, player), Is.False);
            Assert.That(Read<float>(player, "CurrentHealth"), Is.EqualTo(40f));
            Assert.That(Read<bool>(station, "IsReady"), Is.True);
        }

        [UnityTest]
        public IEnumerator Cooldown_BlocksHealingAndRestoresAvailability()
        {
            var player = CreatePlayer();
            var station = CreateStation();
            Call(player, "TakeDamage", 80f);
            Assert.That(TryHeal(station, player), Is.True);
            Assert.That(Read<float>(player, "CurrentHealth"), Is.EqualTo(50f));
            Assert.That(Read<bool>(station, "IsReady"), Is.False);
            Assert.That(TryHeal(station, player), Is.False);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!Read<bool>(station, "IsReady") && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(Read<bool>(station, "IsReady"), Is.True, "Cooldown did not finish");
            Assert.That(Read<float>(station, "RemainingCooldown"), Is.Zero);
            Assert.That(TryHeal(station, player), Is.True);
            Assert.That(Read<float>(player, "CurrentHealth"), Is.EqualTo(80f));
        }

        [UnityTest]
        public IEnumerator Cooldown_StopsWhenDisabled()
        {
            var player = CreatePlayer();
            var station = CreateStation();
            Call(player, "TakeDamage", 60f);
            Assert.That(TryHeal(station, player), Is.True);
            station.gameObject.SetActive(false);
            float remaining = Read<float>(station, "RemainingCooldown");
            Assert.That(remaining, Is.GreaterThan(0f));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Read<float>(station, "RemainingCooldown"), Is.EqualTo(remaining));
        }
    }
}
