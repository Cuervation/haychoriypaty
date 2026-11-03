using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class TowerDefenseModularFenceGeneratorTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [Test]
        public void GeneratesIndependentFenceSpritesAndTwoContinuousColliderRails()
        {
            Type configType = Type.GetType("HayChoriYPaty.TowerDefenseLevelConfig, Assembly-CSharp", true);
            Type generatorType = Type.GetType("HayChoriYPaty.TowerDefenseModularFenceGenerator, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            var owner = new GameObject("TowerDefense fence test");
            var texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 128f, 48f), new Vector2(.5f, .5f), 100f);

            try
            {
                Component generator = owner.AddComponent(generatorType);
                Set(generator, "levelConfig", config);
                Set(generator, "fencePanelSprite", sprite);
                bool generated = (bool)generatorType.GetMethod("Generate", PublicInstance).Invoke(generator, null);
                Assert.IsTrue(generated, Get<string>(generator, "LatestError"));
                Assert.Greater(Get<int>(generator, "GeneratedPieceCount"), 10);

                Transform generatedRoot = owner.transform.GetChild(0);
                SpriteRenderer[] pieces = generatedRoot.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.Greater(pieces.Length, 10, "Each short straight/turn piece should remain a separate SpriteRenderer object.");
                int straightCount = 0, turnCount = 0, terminalCount = 0;
                foreach (SpriteRenderer piece in pieces)
                {
                    Assert.IsNotNull(piece.GetComponent<BoxCollider2D>(), "Every visible fence segment has a physical collider.");
                    Component metadata = piece.GetComponent(Type.GetType("HayChoriYPaty.TowerDefenseFencePiece, Assembly-CSharp", true));
                    Assert.IsNotNull(metadata);
                    object kind = metadata.GetType().GetProperty("Kind", PublicInstance).GetValue(metadata, null);
                    string label = kind.ToString();
                    if (label == "Straight") straightCount++;
                    else if (label == "Turn") turnCount++;
                    else if (label == "Terminal") terminalCount++;
                }
                Assert.Greater(straightCount, 0);
                Assert.Greater(turnCount, 0, "The curved turn receives individually tagged fence pieces.");
                Assert.AreEqual(4, terminalCount, "Each rail has independent entry and exit terminators.");

                EdgeCollider2D[] rails = generatedRoot.GetComponentsInChildren<EdgeCollider2D>(true);
                Assert.AreEqual(2, rails.Length);
                Assert.Greater(rails[0].pointCount, 2);
                Assert.Greater(rails[1].pointCount, 2);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void InvalidRouteDoesNotCreateAnyFencePieces()
        {
            Type configType = Type.GetType("HayChoriYPaty.TowerDefenseLevelConfig, Assembly-CSharp", true);
            Type routeType = Type.GetType("HayChoriYPaty.TowerDefenseRouteGeometryConfig, Assembly-CSharp", true);
            Type generatorType = Type.GetType("HayChoriYPaty.TowerDefenseModularFenceGenerator, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            object route = Activator.CreateInstance(routeType);
            routeType.GetField("PlayableBounds", PublicInstance).SetValue(route, new Rect(-1f, -1f, 2f, 2f));
            configType.GetField("route", PrivateInstance).SetValue(config, route);
            var owner = new GameObject("Invalid TowerDefense fence test");
            var texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 128f, 48f), new Vector2(.5f, .5f), 100f);

            try
            {
                Component generator = owner.AddComponent(generatorType);
                Set(generator, "levelConfig", config);
                Set(generator, "fencePanelSprite", sprite);
                bool generated = (bool)generatorType.GetMethod("Generate", PublicInstance).Invoke(generator, null);
                Assert.IsFalse(generated);
                Assert.That(Get<string>(generator, "LatestError"), Does.Contain("PlayableBounds"));
                Assert.AreEqual(0, Get<int>(generator, "GeneratedPieceCount"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static void Set(Component component, string name, object value)
        {
            FieldInfo field = component.GetType().GetField(name, PrivateInstance);
            Assert.IsNotNull(field, "Expected serialized field " + name + ".");
            field.SetValue(component, value);
        }

        private static T Get<T>(Component component, string name)
        {
            PropertyInfo property = component.GetType().GetProperty(name, PublicInstance);
            Assert.IsNotNull(property, "Expected property " + name + ".");
            return (T)property.GetValue(component, null);
        }
    }
}
