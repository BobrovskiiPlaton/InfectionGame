using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace InfectionGame.Gameplay.Tests
{
    public class PlayerHealthTests : GameplayTestBase
    {
        [Test]
        public void Awake_InitializesFullHealth()
        {
            var player = CreatePlayer();
            Assert.That(Read<float>(player, "CurrentHealth"), Is.EqualTo(Read<float>(player, "MaxHealth")));
        }

        [TestCase(30f, 70f)]
        [TestCase(150f, 0f)]
        [TestCase(0f, 100f)]
        [TestCase(-10f, 100f)]
        public void TakeDamage_RespectsHealthBounds(float damage, float expected)
        {
            var player = CreatePlayer();
            Call(player, "TakeDamage", damage);
            Assert.That(Read<float>(player, "CurrentHealth"), Is.EqualTo(expected));
        }

        [TestCase(20f, 60f)]
        [TestCase(100f, 100f)]
        [TestCase(0f, 40f)]
        [TestCase(-10f, 40f)]
        public void Heal_RespectsHealthBounds(float amount, float expected)
        {
            var player = CreatePlayer();
            Call(player, "TakeDamage", 60f);
            Call(player, "Heal", amount);
            Assert.That(Read<float>(player, "CurrentHealth"), Is.EqualTo(expected));
        }

        [Test]
        public void HealthChanged_ReportsUpdatedValues()
        {
            var player = CreatePlayer();
            int calls = 0;
            float current = -1f, maximum = -1f;
            Action<float, float> listener = (c, m) => { calls++; current = c; maximum = m; };
            var evt = player.GetType().GetEvent("OnHealthChanged");
            evt.AddEventHandler(player, listener);
            try
            {
                Call(player, "TakeDamage", 30f);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(current, Is.EqualTo(70f));
                Assert.That(maximum, Is.EqualTo(100f));
                Call(player, "Heal", 10f);
                Assert.That(calls, Is.EqualTo(2));
                Assert.That(current, Is.EqualTo(80f));
                Call(player, "Heal", 0f);
                Call(player, "TakeDamage", -1f);
                Assert.That(calls, Is.EqualTo(2));
            }
            finally { evt.RemoveEventHandler(player, listener); }
        }

        [UnityTest]
        public IEnumerator Start_NotifiesHealthOnFirstFrame()
        {
            var player = CreatePlayer();
            int calls = 0;
            float current = -1f, maximum = -1f;
            Action<float, float> listener = (c, m) => { calls++; current = c; maximum = m; };
            var evt = player.GetType().GetEvent("OnHealthChanged");
            evt.AddEventHandler(player, listener);
            try
            {
                Assert.That(calls, Is.Zero);
                yield return null;
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(current, Is.EqualTo(100f));
                Assert.That(maximum, Is.EqualTo(100f));
            }
            finally { evt.RemoveEventHandler(player, listener); }
        }
    }
}
