using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class ClubVisualThemeTests
    {
        private static System.Type SimulationType
        {
            get { return System.Type.GetType("HayChoriYPaty.StreetSimulation, Assembly-CSharp", true); }
        }

        private static System.Type ThemeType
        {
            get { return System.Type.GetType("HayChoriYPaty.ClubVisualTheme, Assembly-CSharp", true); }
        }

        [Test]
        public void EveryCurrentLevelResolvesItsOwnClubTheme()
        {
            string[] names = (string[])SimulationType.GetField("LevelNames", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            MethodInfo forLevel = ThemeType.GetMethod("ForLevel", BindingFlags.Public | BindingFlags.Static);
            var resolvedClubs = new HashSet<string>();

            for (int i = 0; i < names.Length; i++)
            {
                object theme = forLevel.Invoke(null, new object[] { i });
                string clubName = (string)ThemeType.GetProperty("ClubName").GetValue(theme, null);
                Assert.AreEqual(names[i], clubName, "Level " + i);
                Assert.IsTrue(resolvedClubs.Add(clubName), "Each live level must resolve to a distinct club palette.");
            }
        }

        [Test]
        public void ChicagoStartsGreenAndAlternatesWithBlack()
        {
            object chicago = ForClub("Nueva Chicago");
            Color primary = GetColor(chicago, "PrimaryColor");
            Color secondary = GetColor(chicago, "SecondaryColor");

            AssertColor(new Color32(0, 122, 61, 255), primary);
            AssertColor(new Color32(0, 0, 0, 255), secondary);
            AssertColor(primary, PennantColor(chicago, 0));
            AssertColor(secondary, PennantColor(chicago, 1));
            AssertColor(primary, PennantColor(chicago, 2));
            AssertColor(secondary, PennantColor(chicago, 3));
        }

        [Test]
        public void AllBoysThemeRetainsItsMonochromeClubPalette()
        {
            object allBoys = ForClub("Floresta / All Boys");
            AssertColor(new Color32(245, 242, 234, 255), GetColor(allBoys, "PrimaryColor"));
            AssertColor(new Color32(0, 0, 0, 255), GetColor(allBoys, "SecondaryColor"));
        }

        [Test]
        public void LaterLevelsUseTheirConfiguredClubPalettes()
        {
            AssertPalette("Liniers - Velez Sarsfield", new Color32(8, 48, 132, 255), new Color32(245, 242, 234, 255));
            AssertPalette("Ferro Carril Oeste", new Color32(0, 105, 63, 255), new Color32(245, 242, 234, 255));
            AssertPalette("Independiente de Avellaneda", new Color32(211, 29, 39, 255), new Color32(245, 242, 234, 255));
        }

        [Test]
        public void PennantStripUsesOneSharedTextureSize()
        {
            object theme = ThemeType.GetMethod("ForLevel", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { 1 });
            System.Type renderer = System.Type.GetType("HayChoriYPaty.ClubPennantRenderer, Assembly-CSharp", true);
            Texture2D texture = (Texture2D)renderer.GetMethod("CreateTexture", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { theme, true });
            try
            {
                Assert.AreEqual((int)renderer.GetField("TextureWidth").GetValue(null), texture.width);
                Assert.AreEqual((int)renderer.GetField("TextureHeight").GetValue(null), texture.height);
                Assert.AreEqual(HideFlags.HideAndDontSave, texture.hideFlags);
                Assert.Greater((int)renderer.GetField("PennantCount").GetValue(null), 1);

                Color32 primary = GetColor32(theme, "PrimaryColor");
                Color32 secondary = GetColor32(theme, "SecondaryColor");
                Color32[] pixels = texture.GetPixels32();
                int primaryPixels = 0, secondaryPixels = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (IsNearColor(pixels[i], primary, 32)) primaryPixels++;
                    if (IsNearColor(pixels[i], secondary, 32)) secondaryPixels++;
                }
                Assert.Greater(primaryPixels, 100, "The generated fabric visibly contains the primary sRGB color.");
                Assert.Greater(secondaryPixels, 100, "The generated fabric visibly contains the secondary sRGB color.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static bool IsNearColor(Color32 pixel, Color32 expected, int tolerance)
        {
            return pixel.a == 255
                && Mathf.Abs(pixel.r - expected.r) <= tolerance
                && Mathf.Abs(pixel.g - expected.g) <= tolerance
                && Mathf.Abs(pixel.b - expected.b) <= tolerance;
        }

        private static object ForClub(string name)
        {
            return ThemeType.GetMethod("ForClub", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { name });
        }

        private static Color GetColor(object theme, string property)
        {
            return (Color)ThemeType.GetProperty(property).GetValue(theme, null);
        }

        private static Color32 GetColor32(object theme, string property)
        {
            return (Color32)GetColor(theme, property);
        }

        private static Color PennantColor(object theme, int index)
        {
            return (Color)ThemeType.GetMethod("GetPennantColor").Invoke(theme, new object[] { index });
        }

        private static void AssertPalette(string club, Color32 primary, Color32 secondary)
        {
            object theme = ForClub(club);
            Assert.AreEqual(club, ThemeType.GetProperty("ClubName").GetValue(theme, null));
            AssertColor(primary, GetColor(theme, "PrimaryColor"));
            AssertColor(secondary, GetColor(theme, "SecondaryColor"));
            AssertColor(primary, PennantColor(theme, 0));
            AssertColor(secondary, PennantColor(theme, 1));
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.005f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.005f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.005f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.005f));
        }
    }
}
