using NUnit.Framework;
using System;
using System.Reflection;

namespace HayChoriYPaty.Tests
{
    /// <summary>Opt-in PlayerPrefs compatibility adapter for existing deterministic EditMode fixtures.</summary>
    [SetUpFixture]
    public sealed class CloudTestSetup
    {
        private FieldInfo initializationSeam;
        private bool prior;

        private static FieldInfo ResolveSeam()
        {
            Type gameType = Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true);
            return gameType.GetField("AllowEditorTestInitialization", BindingFlags.Public | BindingFlags.Static);
        }

        [OneTimeSetUp]
        public void EnableLegacyEditorAdapter()
        {
            initializationSeam = ResolveSeam();
            prior = (bool)initializationSeam.GetValue(null);
            initializationSeam.SetValue(null, true);
        }

        [OneTimeTearDown]
        public void RestoreLegacyEditorAdapter()
        {
            (initializationSeam ?? ResolveSeam()).SetValue(null, prior);
        }
    }
}
