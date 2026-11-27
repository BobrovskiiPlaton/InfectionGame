using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace InfectionGame.Gameplay.Tests
{
    // Assembly-CSharp недоступна напрямую из тестовой asmdef.
    public abstract class GameplayTestBase
    {
        private readonly List<Object> objects = new();
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        protected static Type GameType(string name) =>
            Type.GetType(name + ", Assembly-CSharp", true);

        protected T Track<T>(T obj) where T : Object
        {
            objects.Add(obj);
            return obj;
        }

        protected Component CreatePlayer() =>
            Track(new GameObject("Test Player")).AddComponent(GameType("PlayerHealth"));

        protected static T Read<T>(Component component, string property) =>
            (T)component.GetType().GetProperty(property).GetValue(component);

        protected static object Call(Component component, string method, params object[] args) =>
            component.GetType().GetMethod(method).Invoke(component, args);

        protected static void SetField(Object obj, string field, object value) =>
            obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).SetValue(obj, value);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.Destroy(objects[i]);
            objects.Clear();
            Time.timeScale = previousTimeScale;
            yield return null;
        }
    }
}
