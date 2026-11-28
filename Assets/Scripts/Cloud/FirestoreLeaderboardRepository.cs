using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;

namespace HayChoriYPaty.Cloud
{
    /// <summary>One row of the public ranking: only an apodo and Liga points (no Google name, mail or game state).</summary>
    public sealed class LeaderboardEntry
    {
        public string uid, nickname;
        public long points, wonLevels;
    }

    public sealed class LeaderboardSnapshot
    {
        public const int TopSize = 10;
        public List<LeaderboardEntry> top = new List<LeaderboardEntry>();
        /// <summary>The player's own published row, or null while it has no apodo yet.</summary>
        public LeaderboardEntry me;
        /// <summary>1-based position of <see cref="me"/>; 0 when unknown (no apodo or the count could not be read).</summary>
        public long myRank;
    }

    /// <summary>Public ranking collection <c>leaderboard/{uid}</c>; the rules allow each player to write only its own row.</summary>
    public sealed class FirestoreLeaderboardRepository
    {
        public const string Collection = "leaderboard";
        private readonly FirebaseFirestore db;
        private readonly string uid;
        public FirestoreLeaderboardRepository(FirebaseFirestore firestore, string ownerUid) { db = firestore; uid = ownerUid; }

        public async Task<LeaderboardEntry> ReadOwnAsync()
        {
            DocumentSnapshot own = await db.Collection(Collection).Document(uid).GetSnapshotAsync(Source.Server);
            return own.Exists ? Entry(own) : null;
        }

        public async Task<bool> PublishAsync(string nickname, long points, long wonLevels)
        {
            if (!StreetLeague.TryNormalizeNickname(nickname, out string clean)) throw new ArgumentException("Invalid apodo");
            points = Math.Max(0L, Math.Min(StreetLeague.MaxPoints, points));
            wonLevels = Math.Max(0L, Math.Min(StreetLeague.MaxWonLevels, wonLevels));
            var fields = new Dictionary<string, object> {
                { "schemaVersion", 1L }, { "uid", uid }, { "nickname", clean },
                { "points", points }, { "wonLevels", wonLevels }, { "updatedAt", FieldValue.ServerTimestamp }
            };
            await db.Collection(Collection).Document(uid).SetAsync(fields);
            return true;
        }

        public async Task<LeaderboardSnapshot> ReadAsync()
        {
            var result = new LeaderboardSnapshot();
            Query ranked = db.Collection(Collection).OrderByDescending("points");
            QuerySnapshot top = await ranked.Limit(LeaderboardSnapshot.TopSize).GetSnapshotAsync(Source.Server);
            foreach (DocumentSnapshot document in top.Documents) result.top.Add(Entry(document));
            result.me = await ReadOwnAsync();
            if (result.me == null) return result;
            int inTop = result.top.FindIndex(e => e.uid == uid);
            if (inTop >= 0) { result.myRank = inTop + 1; return result; }
            try
            {
                // Position = players with more points + 1 (ties share the best position).
                AggregateQuerySnapshot above = await db.Collection(Collection).WhereGreaterThan("points", result.me.points)
                    .Count.GetSnapshotAsync(AggregateSource.Server);
                result.myRank = above.Count + 1;
            }
            catch (Exception) { result.myRank = 0; }
            return result;
        }

        private static LeaderboardEntry Entry(DocumentSnapshot document) => new LeaderboardEntry {
            uid = document.GetValue<string>("uid"), nickname = document.GetValue<string>("nickname"),
            points = document.GetValue<long>("points"), wonLevels = document.GetValue<long>("wonLevels")
        };
    }
}
