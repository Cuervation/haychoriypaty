using System;
using System.IO;
using NUnit.Framework;
using System.Reflection;

namespace HayChoriYPaty.Tests
{
    public sealed class PlayerLocalRepositoryTests
    {
        private string root;
        private static Type Repository => Type.GetType("HayChoriYPaty.Cloud.PlayerLocalRepository, Assembly-CSharp", true);
        private dynamic Repo(string uid) => Activator.CreateInstance(Repository, root, uid);
        private static string Save(long cents = 250) => "{\"version\":4,\"unlockedLevel\":2,\"market\":{\"balanceCents\":" + cents + ",\"stockUnits\":[0,0,0,0,0,0,0,0,0,0]}}";
        private static object Static(string name, params object[] args)
        {
            try { return Repository.GetMethod(name).Invoke(null, args); }
            catch (TargetInvocationException e) { throw e.InnerException; }
        }
        [SetUp] public void SetUp() { root = Path.Combine(Path.GetTempPath(), "hcyp-account-tests-" + Guid.NewGuid().ToString("N")); }
        [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); if (File.Exists(root)) File.Delete(root); }
        [Test] public void SnapshotAndStableOutboxSurviveRestart()
        {
            var repo = Repo("google-uid");
            Assert.IsTrue(repo.Commit(Save(), null));
            string id = repo.State.pending[0].id;
            repo = Repo("google-uid");
            Assert.AreEqual(id, repo.State.pending[0].id); Assert.AreEqual(0, repo.State.pending[0].expectedRevision);
            Assert.AreEqual(Save(), repo.State.snapshot);
            Assert.IsNull(repo.State.pending[0].match, "Restart must not create a phantom match from Unity inline serialization.");
        }
        [Test] public void CopyAndAckNeverTurnNoMatchOperationsIntoEmptyMatches()
        {
            var repo = Repo("uid"); Assert.IsTrue(repo.Commit(Save(), null)); Assert.IsTrue(repo.Commit(Save(300), null));
            Assert.IsNull(repo.State.pending[0].match);
            repo.Acknowledge(repo.State.pending[0].id, 1);
            Assert.IsNull(repo.State.pending[0].match);
            repo = Repo("uid"); Assert.IsNull(repo.State.pending[0].match);
        }
        [Test] public void DuplicateSnapshotDoesNotCreateOperations()
        {
            var repo = Repo("uid"); repo.Commit(Save(), null); repo.Commit(Save(), null);
            Assert.AreEqual(1, repo.State.pending.Count);
        }
        [Test] public void MatchIdentityIsNotRequeuedBeforeOrAfterAck()
        {
            var repo = Repo("uid");
            dynamic match = Activator.CreateInstance(Type.GetType("HayChoriYPaty.StreetMatchRecord, Assembly-CSharp", true)); match.id = Guid.NewGuid().ToString("N");
            repo.Commit(Save(), match); repo.Commit(Save(), match);
            Assert.AreEqual(1, repo.State.pending.Count);
            repo.Acknowledge(repo.State.pending[0].id, 1);
            repo.Commit(Save(), match); Assert.AreEqual(0, repo.State.pending.Count);
        }
        [Test] public void OrderedAckIsDurableAndNeverRemovesAnotherOperation()
        {
            var repo = Repo("uid"); repo.Commit(Save(), null); repo.Commit(Save(300), null);
            Assert.Throws<InvalidDataException>(() => repo.Acknowledge(repo.State.pending[1].id, 2));
            repo.Acknowledge(repo.State.pending[0].id, 1);
            repo = Repo("uid");
            Assert.AreEqual(1, repo.State.acknowledgedRevision); Assert.AreEqual(1, repo.State.pending.Count);
            Assert.AreEqual(1, repo.State.pending[0].expectedRevision);
        }
        [Test] public void AccountsNeverShareTheirSnapshot()
        {
            Repo("uid-a").Commit(Save(), null);
            var other = Repo("uid-b"); Assert.IsNull(other.State.snapshot); Assert.AreEqual(0, other.State.pending.Count);
        }
        [Test] public void LegacyBackupIsExactAndCannotBeClaimedByTwoAccounts()
        {
            string original = Save(); string hash = (string)Static("BackupLegacy", root, original);
            Assert.AreEqual(original, File.ReadAllText(Path.Combine(root, "Legacy", hash + ".json")));
            Static("ClaimLegacy", root, "uid-a");
            Assert.IsFalse((bool)Static("CanClaimLegacy", root, "uid-b"));
            Assert.Throws<InvalidOperationException>(() => Static("ClaimLegacy", root, "uid-b"));
            Assert.AreEqual(hash, (string)Static("BackupLegacy", root, original));
        }
        [Test] public void CorruptOrNegativeSnapshotsAreRejectedWithoutReplacingGoodState()
        {
            var repo = Repo("uid"); repo.Commit(Save(), null);
            Assert.IsFalse(repo.Commit("not-json", null)); Assert.IsFalse(repo.Commit(Save(-1), null));
            Assert.AreEqual(Save(), Repo("uid").State.snapshot);
        }
        [Test] public void DiskWriteFailureDoesNotAcknowledgeTheCommand()
        {
            File.WriteAllText(root, "not-a-directory"); var repo = Repo("uid");
            Assert.IsFalse(repo.Commit(Save(), null)); Assert.IsNull(repo.State.snapshot); Assert.AreEqual(0, repo.State.pending.Count);
        }
        [Test] public void ConflictRequiresPreservingTheEntireLocalBranch()
        {
            var repo = Repo("uid"); repo.Commit(Save(), null);
            Assert.Throws<InvalidOperationException>(() => repo.AdoptRemote(Save(500), 8, false));
            repo.AdoptRemote(Save(500), 8, true);
            Assert.AreEqual(8, repo.State.acknowledgedRevision); Assert.AreEqual(0, repo.State.pending.Count);
            Assert.AreEqual(1, Directory.GetFiles(Path.Combine(root, "Accounts", (string)Static("Hash", "uid")), "branch-*.json").Length);
        }
    }
}
