using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace HayChoriYPaty.Cloud
{
    [Serializable] public sealed class PlayerCloudOperation
    {
        public string id, snapshot, payloadHash;
        public long expectedRevision;
        public StreetMatchRecord match;
    }
    [Serializable] public sealed class PlayerLocalState
    {
        public int schemaVersion = 1;
        public string uid, snapshot, legacySourceHash, lastAcknowledgedMatchId;
        public long acknowledgedRevision;
        public bool migrationCommitted;
        public List<PlayerCloudOperation> pending = new List<PlayerCloudOperation>();
    }

    /// <summary>One durable account snapshot + outbox. Never writes the unowned PlayerPrefs v4 key.</summary>
    public sealed class PlayerLocalRepository
    {
        [Serializable] private sealed class CheckedFile { public string payload, hash; }
        private readonly string directory, path;
        public PlayerLocalState State { get; private set; }
        public PlayerLocalRepository(string root, string uid)
        {
            if (string.IsNullOrWhiteSpace(uid)) throw new ArgumentException("UID required");
            directory = Path.Combine(root, "Accounts", Hash(uid)); path = Path.Combine(directory, "state.json");
            State = File.Exists(path) ? Read(path) : new PlayerLocalState { uid = uid };
            if (State.uid != uid || State.schemaVersion != 1 || State.pending == null || State.acknowledgedRevision < 0)
                throw new InvalidDataException("Invalid account envelope");
            for (int i = 0; i < State.pending.Count; i++)
            {
                var op = State.pending[i];
                if (op.expectedRevision != State.acknowledgedRevision + i || op.payloadHash != Hash(op.snapshot))
                    throw new InvalidDataException("Invalid outbox chain");
            }
        }
        public static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant();
        }
        public static void ValidateSnapshot(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 450000) throw new InvalidDataException("Invalid snapshot size");
            var save = JsonUtility.FromJson<StreetGame.SaveData>(json);
            if (save == null || save.version < 1 || save.version > 4 || save.unlockedLevel < 0 || save.unlockedLevel > 10)
                throw new InvalidDataException("Invalid legacy version/progress");
            if (save.market != null)
            {
                if (save.market.balanceCents < 0 || save.market.stockUnits == null || save.market.stockUnits.Length != 10)
                    throw new InvalidDataException("Invalid economy/inventory");
                foreach (int units in save.market.stockUnits) if (units < 0) throw new InvalidDataException("Negative inventory");
            }
        }
        private static PlayerLocalState Read(string filename)
        {
            var record = JsonUtility.FromJson<CheckedFile>(File.ReadAllText(filename));
            if (record == null || record.hash != Hash(record.payload))
                throw new InvalidDataException("Account file damaged; recovery copy preserved");
            return JsonUtility.FromJson<PlayerLocalState>(record.payload);
        }
        private void Write(PlayerLocalState next)
        {
            Directory.CreateDirectory(directory);
            string payload = JsonUtility.ToJson(next);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CheckedFile { payload = payload, hash = Hash(payload) }));
            string temp = path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
            State = next;
        }
        private PlayerLocalState Copy() => JsonUtility.FromJson<PlayerLocalState>(JsonUtility.ToJson(State));
        public bool Commit(string snapshot, StreetMatchRecord match)
        {
            try
            {
                ValidateSnapshot(snapshot);
                if (match != null)
                {
                    bool alreadyQueued = State.pending.Exists(op => op.match != null && op.match.id == match.id);
                    if (alreadyQueued || State.lastAcknowledgedMatchId == match.id) match = null;
                }
                if (State.snapshot == snapshot && match == null) return true;
                // Bound disk growth, never drop pending economic operations to make room.
                if (State.pending.Count >= 512) return false;
                var next = Copy();
                next.pending.Add(new PlayerCloudOperation { id = Guid.NewGuid().ToString("N"), snapshot = snapshot,
                    payloadHash = Hash(snapshot), expectedRevision = next.acknowledgedRevision + next.pending.Count,
                    match = match == null ? null : JsonUtility.FromJson<StreetMatchRecord>(JsonUtility.ToJson(match)) });
                next.snapshot = snapshot; Write(next); return true;
            }
            catch (Exception) { return false; } // Preserve the prior durable state and fail the caller's command.
        }
        public void SetLegacySource(string hash)
        { var next = Copy(); next.legacySourceHash = hash; Write(next); }
        public void Acknowledge(string operationId, long revision)
        {
            if (State.pending.Count == 0 || State.pending[0].id != operationId || revision != State.pending[0].expectedRevision + 1)
                throw new InvalidDataException("Unexpected operation acknowledgment");
            var next = Copy();
            if (next.pending[0].match != null) next.lastAcknowledgedMatchId = next.pending[0].match.id;
            next.pending.RemoveAt(0); next.acknowledgedRevision = revision;
            if (!string.IsNullOrEmpty(next.legacySourceHash)) next.migrationCommitted = true;
            Write(next);
        }
        public void AdoptRemote(string snapshot, long revision, bool preserveBranch)
        {
            ValidateSnapshot(snapshot);
            if (revision < 1) throw new InvalidDataException("Invalid remote revision");
            if (State.pending.Count > 0 && !preserveBranch) throw new InvalidOperationException("Unsynced branch must be preserved");
            if (preserveBranch && File.Exists(path))
                File.Copy(path, Path.Combine(directory, "branch-" + Guid.NewGuid().ToString("N") + ".json"), false);
            var next = Copy(); next.snapshot = snapshot; next.acknowledgedRevision = revision; next.pending.Clear(); Write(next);
        }
        public static string BackupLegacy(string root, string legacyJson)
        {
            if (legacyJson == null) return null;
            string hash = Hash(legacyJson), folder = Path.Combine(root, "Legacy"); Directory.CreateDirectory(folder);
            string destination = Path.Combine(folder, hash + ".json");
            if (!File.Exists(destination))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(legacyJson);
                using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            }
            if (File.ReadAllText(destination) != legacyJson) throw new IOException("Legacy backup mismatch");
            return hash;
        }
        public static bool CanClaimLegacy(string root, string uid)
        { string file = Path.Combine(root, "Legacy", "owner"); return !File.Exists(file) || File.ReadAllText(file) == uid; }
        public static void ClaimLegacy(string root, string uid)
        {
            if (!CanClaimLegacy(root, uid)) throw new InvalidOperationException("Legacy already associated to another account");
            string folder = Path.Combine(root, "Legacy"); Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "owner");
            if (!File.Exists(file))
            {
                using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { byte[] bytes = Encoding.UTF8.GetBytes(uid); stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            }
        }
    }
}
