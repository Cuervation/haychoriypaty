using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace HayChoriYPaty.Cloud
{
    public sealed class CloudRevisionConflict : Exception { public CloudRevisionConflict() : base("Remote revision changed") { } }
    public sealed class RemotePlayerState { public string snapshot, operationId; public long revision; }

    /// <summary>Real Firestore transactions. A single revision serializes an account's bounded state.</summary>
    public sealed class FirestorePlayerRepository
    {
        private readonly FirebaseFirestore db;
        private readonly string uid;
        private DocumentReference Current => db.Document("players/" + uid + "/state/current");
        public FirestorePlayerRepository(FirebaseFirestore firestore, string ownerUid)
        { db = firestore; uid = ownerUid; }
        public async Task<RemotePlayerState> ReadAsync()
        {
            var value = await Current.GetSnapshotAsync(Source.Server);
            if (!value.Exists) return null;
            if (value.GetValue<string>("uid") != uid || value.GetValue<long>("schemaVersion") != 1)
                throw new InvalidOperationException("Unsupported remote schema");
            string snapshot = value.GetValue<string>("snapshot");
            if (value.GetValue<string>("payloadHash") != PlayerLocalRepository.Hash(snapshot))
                throw new InvalidOperationException("Remote checksum mismatch");
            PlayerLocalRepository.ValidateSnapshot(snapshot);
            return new RemotePlayerState { snapshot = snapshot, operationId = value.GetValue<string>("operationId"), revision = value.GetValue<long>("revision") };
        }
        public Task<long> CommitAsync(PlayerCloudOperation operation)
        {
            PlayerLocalRepository.ValidateSnapshot(operation.snapshot);
            if (operation.payloadHash != PlayerLocalRepository.Hash(operation.snapshot))
                throw new InvalidOperationException("Operation checksum mismatch");
            var save = JsonUtility.FromJson<StreetGame.SaveData>(operation.snapshot);
            var receipt = db.Document("players/" + uid + "/operations/" + operation.id);
            var state = new Dictionary<string, object> {
                { "schemaVersion", 1L }, { "uid", uid }, { "revision", operation.expectedRevision + 1 },
                { "operationId", operation.id }, { "payloadHash", operation.payloadHash }, { "snapshot", operation.snapshot },
                { "unlockedLevel", (long)save.unlockedLevel }, { "balanceCents", save.market.balanceCents },
                { "stockUnits", Values(save.market.stockUnits) }, { "updatedAt", FieldValue.ServerTimestamp }
            };
            var marker = new Dictionary<string, object> {
                { "schemaVersion", 1L }, { "uid", uid }, { "operationId", operation.id },
                { "revision", operation.expectedRevision + 1 }, { "previousRevision", operation.expectedRevision },
                { "payloadHash", operation.payloadHash }, { "matchId", operation.match == null ? "" : MatchId(operation.match) },
                { "createdAt", FieldValue.ServerTimestamp }
            };
            Dictionary<string, object> matchFields = null;
            DocumentReference matchDocument = null;
            if (operation.match != null)
            {
                matchFields = Fields(operation.match);
                matchFields["uid"] = uid; matchFields["operationId"] = operation.id;
                matchFields["schemaVersion"] = 1L; matchFields["revision"] = operation.expectedRevision + 1;
                matchFields["committedAt"] = FieldValue.ServerTimestamp;
                matchDocument = db.Document("players/" + uid + "/matches/" + MatchId(operation.match));
            }
            // No domain mutation or Unity callbacks inside this retryable transaction callback.
            return db.RunTransactionAsync(async transaction => {
                var priorReceipt = await transaction.GetSnapshotAsync(receipt);
                if (priorReceipt.Exists)
                {
                    if (priorReceipt.GetValue<string>("payloadHash") != operation.payloadHash ||
                        priorReceipt.GetValue<long>("revision") != operation.expectedRevision + 1 ||
                        priorReceipt.GetValue<string>("matchId") != (operation.match == null ? "" : MatchId(operation.match)))
                        throw new InvalidOperationException("Operation identity reused with different payload");
                    return priorReceipt.GetValue<long>("revision");
                }
                var before = await transaction.GetSnapshotAsync(Current);
                long revision = before.Exists ? before.GetValue<long>("revision") : 0;
                if (revision != operation.expectedRevision) throw new CloudRevisionConflict();
                if (matchDocument != null)
                {
                    var priorMatch = await transaction.GetSnapshotAsync(matchDocument);
                    if (priorMatch.Exists) throw new InvalidOperationException("Match already committed by another operation");
                }
                transaction.Set(Current, state); transaction.Set(receipt, marker);
                if (matchDocument != null) transaction.Set(matchDocument, matchFields);
                return revision + 1;
            });
        }
        private static string MatchId(StreetMatchRecord match)
        {
            // Public serialized identity remains compatible with the domain DTO without SDK attributes.
            FieldInfo identity = typeof(StreetMatchRecord).GetField("id") ?? typeof(StreetMatchRecord).GetField("matchId");
            string value = identity == null ? null : identity.GetValue(match) as string;
            Guid parsed;
            if (!Guid.TryParse(value, out parsed)) throw new InvalidOperationException("Stable match UUID required");
            return parsed.ToString("N");
        }
        private static Dictionary<string, object> Fields(object data)
        {
            var fields = new Dictionary<string, object>();
            foreach (var field in data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                fields[field.Name] = Value(field.GetValue(data));
            return fields;
        }
        private static List<object> Values(IEnumerable data)
        { var result = new List<object>(); foreach (var item in data) result.Add(Value(item)); return result; }
        private static object Value(object data)
        {
            if (data == null || data is string || data is bool || data is long || data is double) return data;
            if (data is float) return (double)(float)data;
            if (data is int) return (long)(int)data;
            if (data is IEnumerable) return Values((IEnumerable)data);
            return Fields(data);
        }
    }
}
