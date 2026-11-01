using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>Huracán regressions use reflection to preserve the test asmdef's runtime boundary.</summary>
    public sealed class StreetBonusTrackTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private Type simType, balanceType, stateType, gameType, saveType, ticketType, matchType, roleType, phaseType;
        private readonly List<GameObject> hosts = new List<GameObject>();
        private bool oldEditorInitialization, oldStartupGrant;
        private string latestJson;
        private object lastMatch;

        [SetUp]
        public void SetUp()
        {
            simType = Runtime("StreetSimulation");
            balanceType = Runtime("StreetBalance");
            stateType = Runtime("StreetBonusTrackState");
            gameType = Runtime("StreetGame");
            saveType = gameType.GetNestedType("SaveData", BindingFlags.Public);
            ticketType = Runtime("StreetTicketData");
            matchType = Runtime("StreetMatchRecord");
            roleType = Runtime("StreetWorkerRole");
            phaseType = Runtime("RoundPhase");
            latestJson = null;
            lastMatch = null;
            FieldInfo editorInitialization = gameType.GetField("AllowEditorTestInitialization", AnyStatic);
            oldEditorInitialization = (bool)editorInitialization.GetValue(null);
            editorInitialization.SetValue(null, false);
            FieldInfo startupGrant = gameType.GetField("temporaryStartupTicketGrantAppliedThisProcess", AnyStatic);
            oldStartupGrant = (bool)startupGrant.GetValue(null);
            startupGrant.SetValue(null, false);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject host in hosts) if (host != null) UnityEngine.Object.DestroyImmediate(host);
            hosts.Clear();
            gameType.GetField("AllowEditorTestInitialization", AnyStatic).SetValue(null, oldEditorInitialization);
            gameType.GetField("temporaryStartupTicketGrantAppliedThisProcess", AnyStatic).SetValue(null, oldStartupGrant);
        }

        [Test]
        public void CampaignOrderInsertsHuracanWithoutShiftingStableLevelIds()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 11, 5, 6, 7, 8, 9, 10 },
                (int[])simType.GetField("CampaignOrder", BindingFlags.Public | BindingFlags.Static).GetValue(null));
            Assert.AreEqual(11, CallStatic(simType, "CampaignLevelAt", 5));
            Assert.AreEqual(5, CallStatic(simType, "NextCampaignLevelId", 11));
            Assert.AreEqual(11, CallStatic(simType, "NextCampaignLevelId", 4));
            Assert.AreEqual(10, CallStatic(simType, "NextCampaignLevelId", 9));
            Assert.AreEqual(-1, CallStatic(simType, "NextCampaignLevelId", 10));
        }

        [Test]
        public void LegacyV4RacingProgressAlsoUnlocksHuracanWithoutMovingTheFrontier()
        {
            object legacy = Activator.CreateInstance(saveType);
            Put(legacy, "version", 4);
            Put(legacy, "unlockedLevel", 7);
            object migrated = gameType.GetMethod("MigrateSaveData", AnyStatic).Invoke(null, new[] { legacy });

            Assert.AreEqual(5, Read(migrated, "version"));
            Assert.AreEqual(7, Read(migrated, "unlockedLevel"));
            Assert.IsTrue((bool)Read(migrated, "legacyRacingUnlocked"));
            Assert.IsTrue((bool)Read(migrated, "huracanUnlocked"));
            Assert.IsFalse((bool)Read(migrated, "racingUnlockedByHuracan"));
            Assert.NotNull(Read(migrated, "bonusTrack"));
        }

        [Test]
        public void BonusStartsWithFreeTeamAndPausesAtFiveExactChoiceDeadlines()
        {
            object sim = NewSim(11);
            Assert.IsTrue((bool)Call(sim, "StartBonusRound", 4097));
            Assert.AreEqual(2, Read(sim, "ParrilleroCount"));
            Assert.AreEqual(1, Read(sim, "CocacoleroCount"));
            Assert.AreEqual(1, Read(sim, "MozoCount"));
            Assert.AreEqual(0, Read(sim, "ParrilleroPremiumCount"));
            Assert.AreEqual(0, Read(sim, "FerneteroCount"));
            Assert.AreEqual(0, Read(sim, "Coins"));
            Assert.IsFalse((bool)Read(sim, "GoalReached"));

            for (int choice = 0; choice < 5; choice++)
            {
                float deadline = (choice + 1) * 20f;
                AdvanceTo(sim, deadline);
                object state = Read(sim, "BonusTrack");
                Assert.AreEqual(deadline, (float)Read(sim, "Elapsed"), .0001f, "Choice " + (choice + 1) + " must land on its exact deadline.");
                Assert.IsTrue((bool)Read(state, "SelectionPending"));
                Assert.AreEqual(3, ((ICollection)Read(state, "CurrentCards")).Count);
                Assert.AreEqual(choice == 2, (bool)Read(state, "IsSuperBonusPending"));
                Call(sim, "Step", 6f);
                Assert.AreEqual(deadline, (float)Read(sim, "Elapsed"), .0001f, "Effective time must stop while the card is open.");
                Assert.IsTrue((bool)Call(sim, "ChooseBonusCard", 0));
                state = Read(sim, "BonusTrack");
                if ((bool)Read(state, "EmergencyRoleSelectionPending"))
                    Assert.IsTrue((bool)Call(sim, "ChooseBonusEmergencyRole", First(Read(state, "EmergencyRoles"))));
                Assert.IsFalse((bool)Read(state, "SelectionPending"));
                Assert.AreEqual(choice + 1, Read(state, "ChoiceCount"));
            }

            AdvanceTo(sim, 120f);
            Assert.AreEqual("Won", Read(sim, "Phase").ToString());
            Assert.AreEqual(120f, (float)Read(sim, "Elapsed"), .0001f);
        }

        [Test]
        public void AguanteRequiresTenDeliveredUnitsAndExpiresWithoutPermanentSpeedChange()
        {
            object bonus = Activator.CreateInstance(stateType, new object[] { 81 });
            for (int i = 0; i < 9; i++) Call(bonus, "RecordDeliveredUnit");
            Assert.IsFalse((bool)Read(bonus, "AguanteReady"));
            Assert.AreEqual(900, Read(bonus, "Score"));
            Call(bonus, "RecordDeliveredUnit");
            Assert.IsTrue((bool)Read(bonus, "AguanteReady"));
            Assert.IsTrue((bool)Call(bonus, "ActivateAguante"));
            object parrillero = Enum.Parse(roleType, "Parrillero");
            object mozo = Enum.Parse(roleType, "Mozo");
            Assert.AreEqual(1.25f, (float)Call(bonus, "ProductionMultiplier", parrillero), .0001f);
            Assert.AreEqual(1.25f, (float)Call(bonus, "MovementMultiplier", mozo), .0001f);
            Call(bonus, "Advance", 8f);
            Assert.IsFalse((bool)Read(bonus, "AguanteActive"));
            Assert.AreEqual(1f, (float)Call(bonus, "ProductionMultiplier", parrillero), .0001f);
            Assert.AreEqual(0f, (float)Read(bonus, "AguanteProgress"));
        }

        [Test]
        public void OnlyRealSimulationHandoffsAddBonusPointsAndNeverAddCurrency()
        {
            object sim = NewSim(11);
            Assert.IsTrue((bool)Call(sim, "StartBonusRound", 1234));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomer", 0, 1));
            int unitEvents = 0;
            EventInfo delivered = simType.GetEvent("ProductUnitDelivered", PublicInstance);
            Action handler = () => unitEvents++;
            delivered.AddEventHandler(sim, handler);

            object state = Read(sim, "BonusTrack");
            for (int guard = 0; guard < 3000 && (int)Read(state, "Deliveries") == 0; guard++)
            {
                if ((bool)Read(state, "SelectionPending"))
                {
                    Assert.IsTrue((bool)Call(sim, "ChooseBonusCard", 0));
                    state = Read(sim, "BonusTrack");
                    if ((bool)Read(state, "EmergencyRoleSelectionPending"))
                        Assert.IsTrue((bool)Call(sim, "ChooseBonusEmergencyRole", First(Read(state, "EmergencyRoles"))));
                }
                else Call(sim, "Step", .05f);
            }

            int deliveries = (int)Read(state, "Deliveries");
            int completed = (int)Read(state, "CompletedOrders");
            Assert.Greater(deliveries, 0);
            Assert.AreEqual(deliveries, unitEvents);
            Assert.GreaterOrEqual((int)Read(state, "Score"), 100 * deliveries + 150 * completed);
            Assert.AreEqual(0, Read(sim, "Coins"));
            Assert.AreEqual(0, Read(sim, "CoinsEarned"));
            Assert.AreEqual(0, Read(sim, "CustomersLost"));
        }

        [Test]
        public void BonusTicketGrantIsIdempotentAndThreeStarExtraIsOneTime()
        {
            object save = Activator.CreateInstance(saveType);
            Put(save, "version", 5);
            Put(save, "unlockedLevel", 4);
            Put(save, "huracanUnlocked", true);
            object tickets = Activator.CreateInstance(ticketType);
            Put(tickets, "migrationApplied", true);
            Put(save, "tickets", tickets);
            object game = CreateGame(JsonUtility.ToJson(save));

            Assert.AreEqual(0L, Read(game, "TicketBalance"));
            Assert.IsTrue((bool)Call(game, "SelectLevel", 11));
            Assert.IsTrue((bool)Call(game, "TryStartRound"));
            CompleteRound(Read(game, "Sim"));
            SetScore(Read(Read(game, "Sim"), "BonusTrack"), 3000);
            Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(1L, Read(game, "TicketBalance"));
            Assert.AreEqual(1, Read(lastMatch, "bonusTickets"));
            Assert.IsFalse((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(1L, Read(game, "TicketBalance"), "Repeating the terminal callback cannot award a second ticket.");

            Assert.IsTrue((bool)Call(game, "Retry"));
            CompleteRound(Read(game, "Sim"));
            SetScore(Read(Read(game, "Sim"), "BonusTrack"), 10000);
            Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(3L, Read(game, "TicketBalance"), "The second run earns one regular ticket and the first-time three-star extra.");
            Assert.AreEqual(2, Read(lastMatch, "bonusTickets"));
            Assert.IsTrue((bool)Read(Read(game, "BonusTrackProgress"), "threeStarTicketAwarded"));
            Assert.IsTrue((bool)Call(game, "IsLevelUnlocked", 5));

            Assert.IsTrue((bool)Call(game, "Retry"));
            CompleteRound(Read(game, "Sim"));
            SetScore(Read(Read(game, "Sim"), "BonusTrack"), 10000);
            Assert.IsTrue((bool)Call(game, "ProcessRoundResult"));
            Assert.AreEqual(4L, Read(game, "TicketBalance"), "Later three-star runs earn only the repeatable regular ticket.");
            Assert.AreEqual(1, Read(Read(game, "BonusTrackProgress"), "lastTickets"));
            Assert.IsNotEmpty(latestJson);
        }

        private object CreateGame(string json)
        {
            var host = new GameObject("Huracán bonus-track isolated test");
            host.SetActive(false);
            hosts.Add(host);
            Component game = host.AddComponent(gameType);
            host.SetActive(true);
            Type callbackType = typeof(Func<,,>).MakeGenericType(typeof(string), matchType, typeof(bool));
            MethodInfo callbackMethod = GetType().GetMethod("Persist", PrivateInstance).MakeGenericMethod(matchType);
            Delegate persist = callbackMethod.CreateDelegate(callbackType, this);
            Assert.IsTrue((bool)Call(game, "ActivatePlayer", json, persist));
            return game;
        }

        private bool Persist<T>(string json, T match)
        {
            latestJson = json;
            if (match != null) lastMatch = match;
            return true;
        }

        private object NewSim(int level) => Activator.CreateInstance(simType,
            new object[] { Activator.CreateInstance(balanceType), level, 5f, 0, 1, 0 });

        private void AdvanceTo(object sim, float deadline)
        {
            for (int guard = 0; guard < 32 && (string)Read(sim, "Phase").ToString() == "Playing" && (float)Read(sim, "Elapsed") + .0001f < deadline; guard++)
            {
                float remaining = deadline - (float)Read(sim, "Elapsed");
                Call(sim, "Step", Mathf.Min(10f, remaining));
                if ((bool)Read(Read(sim, "BonusTrack"), "SelectionPending")) return;
            }
        }

        private void CompleteRound(object sim)
        {
            object state = Read(sim, "BonusTrack");
            for (int guard = 0; guard < 3000 && Read(sim, "Phase").ToString() == "Playing"; guard++)
            {
                if ((bool)Read(state, "SelectionPending"))
                {
                    Assert.IsTrue((bool)Call(sim, "ChooseBonusCard", 0));
                    state = Read(sim, "BonusTrack");
                    if ((bool)Read(state, "EmergencyRoleSelectionPending"))
                        Assert.IsTrue((bool)Call(sim, "ChooseBonusEmergencyRole", First(Read(state, "EmergencyRoles"))));
                }
                else Call(sim, "Step", .25f);
            }
            Assert.AreEqual("Won", Read(sim, "Phase").ToString());
        }

        private void SetScore(object state, int score) => stateType.GetField("score", AnyInstance).SetValue(state, score);

        private static Type Runtime(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);

        private static object Read(object instance, string name)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance), "Cannot read " + name);
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(name, AnyInstance);
            if (property != null) return property.GetValue(instance, null);
            FieldInfo field = type.GetField(name, AnyInstance);
            if (field != null) return field.GetValue(instance);
            throw new ArgumentException("No readable member " + name + " on " + type.FullName);
        }

        private static void Put(object instance, string name, object value) => instance.GetType().GetField(name, AnyInstance).SetValue(instance, value);

        private static object Call(object instance, string name, params object[] args)
        {
            foreach (MethodInfo method in instance.GetType().GetMethods(AnyInstance))
            {
                if (method.Name != name) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool compatible = true;
                for (int i = 0; i < parameters.Length; i++)
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) { compatible = false; break; }
                if (compatible) return method.Invoke(instance, args);
            }
            throw new MissingMethodException(instance.GetType().FullName, name);
        }

        private static object CallStatic(Type type, string name, params object[] args) => type.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);

        private static object First(object sequence)
        {
            foreach (object value in (IEnumerable)sequence) return value;
            throw new InvalidOperationException("Expected at least one role option.");
        }
    }
}
