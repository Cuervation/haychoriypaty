using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    public sealed class HubBackgroundMusicTests
    {
        private const string ResourcePath = "Audio/Muchachos_16bit_20pct_mas_lento_LOOP";
        private const string AssetPath = "Assets/Resources/Audio/Muchachos_16bit_20pct_mas_lento_LOOP.wav";
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        [Test]
        public void BarrioTrackIsPresentCompressedAndKeepsItsStereoSourceFormat()
        {
            AudioClip clip = Resources.Load<AudioClip>(ResourcePath);
            Assert.NotNull(clip);
            Assert.AreEqual(2, clip.channels);
            Assert.AreEqual(32000, clip.frequency);
            Assert.That(clip.length, Is.InRange(43f, 45f));

            var importer = AssetImporter.GetAtPath(AssetPath) as AudioImporter;
            Assert.NotNull(importer);
            Assert.IsFalse(importer.forceToMono);
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, importer.defaultSampleSettings.loadType);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, importer.defaultSampleSettings.compressionFormat);
        }

        [Test]
        public void MusicGateAllowsOnlyTheVisibleHub()
        {
            MethodInfo gate = RuntimeView().GetMethod("ShouldPlayHubMusic", PrivateStatic);
            Assert.NotNull(gate);
            Assert.IsTrue((bool)gate.Invoke(null, new object[] { true, false, false, false, false, false, false }));
            Assert.IsFalse((bool)gate.Invoke(null, new object[] { false, false, false, false, false, false, false }));

            for (int excludedState = 1; excludedState < 7; excludedState++)
            {
                var state = new bool[7];
                state[0] = true;
                state[excludedState] = true;
                var arguments = new object[state.Length];
                for (int i = 0; i < state.Length; i++) arguments[i] = state[i];
                Assert.IsFalse((bool)gate.Invoke(null, arguments),
                    "Hub music must stop when state " + excludedState + " is active.");
            }
        }

        [Test]
        public void HubMusicPreferenceDefaultsOnAndCanBeChangedLocally()
        {
            System.Type view = RuntimeView();
            string key = (string)view.GetField("HubMusicEnabledPlayerPrefKey", PrivateStatic).GetRawConstantValue();
            MethodInfo read = view.GetMethod("HubMusicEnabled", PrivateStatic);
            MethodInfo write = view.GetMethod("SetHubMusicEnabled", PrivateStatic);
            bool hadValue = PlayerPrefs.HasKey(key);
            int oldValue = PlayerPrefs.GetInt(key, 1);
            try
            {
                PlayerPrefs.DeleteKey(key);
                Assert.IsTrue((bool)read.Invoke(null, null), "The existing hub music remains enabled by default.");
                write.Invoke(null, new object[] { false });
                Assert.IsFalse((bool)read.Invoke(null, null));
                Assert.AreEqual(0, PlayerPrefs.GetInt(key));
                write.Invoke(null, new object[] { true });
                Assert.IsTrue((bool)read.Invoke(null, null));
                Assert.AreEqual(1, PlayerPrefs.GetInt(key));
            }
            finally
            {
                if (hadValue) PlayerPrefs.SetInt(key, oldValue);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void HubControlIconsAreVisibleAndMusicStatesDiffer()
        {
            System.Type view = RuntimeView();
            System.Type iconKind = view.GetNestedType("HubControlIconKind", BindingFlags.NonPublic);
            MethodInfo createIcon = view.GetMethod("CreateHubControlIconTexture", PrivateStatic);
            Assert.NotNull(iconKind);
            Assert.NotNull(createIcon);
            Texture2D gear = null, music = null, muted = null;
            try
            {
                gear = (Texture2D)createIcon.Invoke(null, new[] { System.Enum.Parse(iconKind, "Settings") });
                music = (Texture2D)createIcon.Invoke(null, new[] { System.Enum.Parse(iconKind, "Music") });
                muted = (Texture2D)createIcon.Invoke(null, new[] { System.Enum.Parse(iconKind, "Muted") });
                Assert.Greater(CountVisiblePixels(gear), 100, "The gear should render as a clear icon, not an empty control.");
                Assert.Greater(CountVisiblePixels(music), 100, "The enabled speaker should render as a clear icon.");
                Assert.Greater(CountVisiblePixels(muted), 100, "The muted speaker should render as a clear icon.");

                Color[] enabledPixels = music.GetPixels(), mutedPixels = muted.GetPixels();
                int different = 0;
                for (int i = 0; i < enabledPixels.Length; i++)
                    if (Mathf.Abs(enabledPixels[i].r - mutedPixels[i].r) > .1f ||
                        Mathf.Abs(enabledPixels[i].g - mutedPixels[i].g) > .1f ||
                        Mathf.Abs(enabledPixels[i].b - mutedPixels[i].b) > .1f ||
                        Mathf.Abs(enabledPixels[i].a - mutedPixels[i].a) > .1f)
                        different++;
                Assert.Greater(different, 40, "Enabled sound waves and the mute slash must be visually distinguishable.");
            }
            finally
            {
                if (gear != null) Object.DestroyImmediate(gear);
                if (music != null) Object.DestroyImmediate(music);
                if (muted != null) Object.DestroyImmediate(muted);
            }
        }

        [Test]
        public void HubMusicAudioSourceIsLoopingAndTwoDimensional()
        {
            AudioClip clip = Resources.Load<AudioClip>(ResourcePath);
            var gameObject = new GameObject("Hub music test");
            try
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                MethodInfo configure = RuntimeView().GetMethod("ConfigureHubMusicSource", PrivateStatic);
                Assert.NotNull(configure);
                configure.Invoke(null, new object[] { source, clip });

                Assert.AreSame(clip, source.clip);
                Assert.IsTrue(source.loop);
                Assert.IsFalse(source.playOnAwake);
                Assert.AreEqual(0f, source.spatialBlend);
                Assert.AreEqual(.35f, source.volume);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static System.Type RuntimeView()
        {
            return System.Type.GetType("HayChoriYPaty.StreetView, Assembly-CSharp", true);
        }

        private static int CountVisiblePixels(Texture2D texture)
        {
            int visible = 0;
            foreach (Color pixel in texture.GetPixels()) if (pixel.a > .5f) visible++;
            return visible;
        }
    }
}
