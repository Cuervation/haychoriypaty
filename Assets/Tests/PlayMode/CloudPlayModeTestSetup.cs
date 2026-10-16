#if UNITY_EDITOR
using NUnit.Framework;
namespace HayChoriYPaty.Tests
{
    [SetUpFixture]
    public sealed class CloudPlayModeTestSetup
    {
        private bool previous;
        private static System.Reflection.FieldInfo Gate => System.Type.GetType("HayChoriYPaty.StreetGame, Assembly-CSharp", true).GetField("AllowEditorTestInitialization");
        [OneTimeSetUp] public void EnableLocalTestPersistence() { previous = (bool)Gate.GetValue(null); Gate.SetValue(null, true); }
        [OneTimeTearDown] public void RestoreProductionGate() { Gate.SetValue(null, previous); }
    }
}
#endif
