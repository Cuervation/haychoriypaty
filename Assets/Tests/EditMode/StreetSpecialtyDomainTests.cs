using System;
using System.Reflection;
using NUnit.Framework;

namespace HayChoriYPaty.Tests
{
    public sealed class StreetSpecialtyDomainTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private static Type TypeOf(string name) => Type.GetType("HayChoriYPaty." + name + ", Assembly-CSharp", true);
        private static object NewBalance() => Activator.CreateInstance(TypeOf("StreetBalance"));
        private static object Make(int level, int coins = 0, object balance = null) =>
            Activator.CreateInstance(TypeOf("StreetSimulation"), new[] { balance ?? NewBalance(), (object)level, 5f, coins, 1, 0 });
        private static object Get(object value, string property) =>
            value.GetType().GetProperty(property, PublicInstance).GetValue(value, null);
        private static object Call(object value, string method, params object[] args)
        {
            foreach (MethodInfo candidate in value.GetType().GetMethods(PublicInstance))
                if (candidate.Name == method && candidate.GetParameters().Length == args.Length)
                    return candidate.Invoke(value, args);
            throw new MissingMethodException(value.GetType().FullName, method);
        }
        private static object Role(string name) => Enum.Parse(TypeOf("StreetWorkerRole"), name);
        private static bool Has(object sim, string role) => (bool)Call(sim, "HasRole", Role(role));
        private static void Set(object value, string field, object contents) =>
            value.GetType().GetField(field, PublicInstance).SetValue(value, contents);
        private static void SetProperty(object value, string property, object contents) =>
            value.GetType().GetProperty(property, PublicInstance).GetSetMethod(true).Invoke(value, new[] { contents });

        [Test]
        public void ProductIdsMapToExactlyOneRoleAndPhysicalStation()
        {
            Type specialties = TypeOf("StreetSpecialties");
            MethodInfo roleFor = specialties.GetMethod("GetRequiredWorkerRole", BindingFlags.Public | BindingFlags.Static);
            MethodInfo stationFor = specialties.GetMethod("GetStation", BindingFlags.Public | BindingFlags.Static);
            string[] roles = { "Parrillero", "Parrillero", "ParrilleroPremium", "ParrilleroPremium",
                "Cocacolero", "Fernetero", "Cocacolero" };
            string[] stations = { "NormalGrill", "NormalGrill", "PremiumGrill", "PremiumGrill",
                "CocaBarrel", "FernetTable", "BeerBarrel" };
            for (int product = 0; product < roles.Length; product++)
            {
                Assert.AreEqual(roles[product], roleFor.Invoke(null, new object[] { product }).ToString(), "Product " + product);
                Assert.AreEqual(stations[product], stationFor.Invoke(null, new object[] { product }).ToString(), "Product " + product);
            }
            Assert.AreEqual(0, Convert.ToInt32(Role("Parrillero")));
            Assert.AreEqual(1, Convert.ToInt32(Role("Cocacolero")));
            Assert.AreEqual(2, Convert.ToInt32(Role("ParrilleroPremium")));
            Assert.AreEqual(3, Convert.ToInt32(Role("Fernetero")));
        }

        [Test]
        public void SpecialtyVisibilityFollowsProductsNotLevelIndex()
        {
            object balance = NewBalance();
            string[] ids = (string[])TypeOf("StreetBalance").GetField("levelProductIds", PublicInstance).GetValue(balance);
            ids[0] = "0,3,5";
            ids[10] = "0,3,5";
            foreach (int level in new[] { 0, 10 })
            {
                object synthetic = Make(level, balance: balance);
                Assert.IsTrue(Has(synthetic, "Parrillero"));
                Assert.IsTrue(Has(synthetic, "ParrilleroPremium"));
                Assert.IsTrue(Has(synthetic, "Fernetero"));
                Assert.IsFalse(Has(synthetic, "Cocacolero"));
                Assert.IsTrue((bool)Get(synthetic, "HasParrilleroPremium"));
                Assert.IsTrue((bool)Get(synthetic, "HasFernetero"));
                Assert.IsFalse((bool)Get(synthetic, "HasCocacolero"));
            }
        }

        [Test]
        public void FourSpecialtiesHireIndependentlyOnTheSharedConfiguredCurve()
        {
            object sim = Make(4, 10000);
            string[] countNames = { "ParrilleroCount", "CocacoleroCount", "ParrilleroPremiumCount", "FerneteroCount" };
            string[] costNames = { "ParrilleroHireCost", "CocacoleroHireCost", "ParrilleroPremiumHireCost", "FerneteroHireCost" };
            string[] roles = { "Parrillero", "Cocacolero", "ParrilleroPremium", "Fernetero" };
            Assert.AreEqual(15, Get(sim, costNames[0]), "The free opening Parrillero leaves the first paid tier at $15.");
            for (int i = 1; i < roles.Length; i++) Assert.AreEqual(15, Get(sim, costNames[i]));
            for (int hiredRole = 0; hiredRole < roles.Length; hiredRole++)
            {
                int[] beforeHire = new int[countNames.Length];
                for (int role = 0; role < countNames.Length; role++) beforeHire[role] = (int)Get(sim, countNames[role]);
                Assert.IsTrue((bool)Call(sim, "TryHire", Role(roles[hiredRole])), roles[hiredRole]);
                for (int role = 0; role < roles.Length; role++)
                    Assert.AreEqual(beforeHire[role] + (role == hiredRole ? 1 : 0), Get(sim, countNames[role]),
                        roles[role] + " count changes only when that role is hired.");
            }
            for (int i = 0; i < roles.Length; i++)
                Assert.AreEqual(30, Get(sim, costNames[i]), roles[i] + " must advance only its own tier.");
            Assert.AreEqual(5, Get(sim, "StaffCount"));
        }


        [Test]
        public void EachRoleHasAnIndependentFifteenThirtySixtyHundredThenMaxTier()
        {
            object sim = Make(4, 100000);
            string[] roles = { "Parrillero", "Cocacolero", "ParrilleroPremium", "Fernetero" };
            string[] costProperties = { "ParrilleroHireCost", "CocacoleroHireCost", "ParrilleroPremiumHireCost", "FerneteroHireCost" };
            string[] countProperties = { "ParrilleroCount", "CocacoleroCount", "ParrilleroPremiumCount", "FerneteroCount" };
            int[] curve = { 15, 30, 60, 100 };
            for (int roleIndex = 0; roleIndex < roles.Length; roleIndex++)
            {
                int initialCount = (int)Get(sim, countProperties[roleIndex]);
                for (int tier = 0; tier < curve.Length; tier++)
                {
                    Assert.AreEqual(curve[tier], Get(sim, costProperties[roleIndex]), roles[roleIndex] + " tier " + tier);
                    int[] otherCosts = new int[costProperties.Length];
                    for (int other = 0; other < roles.Length; other++) otherCosts[other] = (int)Get(sim, costProperties[other]);
                    Assert.IsTrue((bool)Call(sim, "TryHire", Role(roles[roleIndex])));
                    Assert.AreEqual(initialCount + tier + 1, Get(sim, countProperties[roleIndex]));
                    for (int other = 0; other < roles.Length; other++)
                        if (other != roleIndex) Assert.AreEqual(otherCosts[other], Get(sim, costProperties[other]), roles[other] + " tier must not advance");
                }
                Assert.AreEqual(initialCount == 1 ? 5 : 4, Get(sim, countProperties[roleIndex]));
                Assert.AreEqual(0, Get(sim, costProperties[roleIndex]));
                Assert.IsFalse((bool)Get(sim, "CanHire" + roles[roleIndex]));
                Assert.AreEqual(5, Call(sim, "MaxWorkersForRole", Role(roles[roleIndex])));
            }
            Assert.AreEqual(17, Get(sim, "StaffCount"));
        }

        [Test]
        public void RoleSpecificHireCurvesCanInheritLevelProfileOrOverrideIndependently()
        {
            object balance = NewBalance();
            Type balanceType = TypeOf("StreetBalance");
            var costs = new[] { 15, 30, 60, 100 };
            var alternate = Activator.CreateInstance(TypeOf("StreetUpgradeCostProfile"));
            TypeOf("StreetUpgradeCostProfile").GetField("hireCosts", PublicInstance).SetValue(alternate, new[] { 17, 33, 66, 111 });
            Set(alternate, "profileId", "PREMIUM_TEST");
            Array profiles = Array.CreateInstance(TypeOf("StreetUpgradeCostProfile"), 2);
            profiles.SetValue(((Array)balanceType.GetField("upgradeCostProfiles", PublicInstance).GetValue(balance)).GetValue(0), 0);
            profiles.SetValue(alternate, 1);
            balanceType.GetField("upgradeCostProfiles", PublicInstance).SetValue(balance, profiles);
            int[] roleOverrides = (int[])balanceType.GetField("roleHireCostProfileIds", PublicInstance).GetValue(balance);
            roleOverrides[2] = 1;
            object sim = Make(4, 1000, balance);
            Assert.AreEqual(costs[0], Get(sim, "ParrilleroHireCost"));
            Assert.AreEqual(17, Get(sim, "ParrilleroPremiumHireCost"));
            Assert.IsTrue((bool)Call(sim, "TryHire", Role("ParrilleroPremium")));
            Assert.AreEqual(33, Get(sim, "ParrilleroPremiumHireCost"));
            Assert.AreEqual(costs[0], Get(sim, "ParrilleroHireCost"));
        }


        [Test]
        public void FourRolesFulfillOneMixedTicketWithoutCrossSpecialtyHandoffs()
        {
            object balance = NewBalance();
            object sim = Make(10, balance: balance);
            Call(sim, "StartRound");
            SetProperty(sim, "Coins", 10000);
            Assert.IsTrue((bool)Call(sim, "TryHire", Role("Cocacolero")));
            Assert.IsTrue((bool)Call(sim, "TryHire", Role("ParrilleroPremium")));
            Assert.IsTrue((bool)Call(sim, "TryHire", Role("Fernetero")));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0, 2, 4, 5, 6 }, new[] { 1, 1, 1, 1, 1 }));
            object customer = ((System.Collections.IList)Get(sim, "Customers"))[0];
            SetProperty(customer, "State", Enum.Parse(TypeOf("StreetCustomerState"), "Waiting"));
            SetProperty(customer, "Position", Get(customer, "Target"));
            Set(balance, "workerSpeed", 100000f);
            Set(balance, "pickupSeconds", .01f);

            for (int i = 0; i < 300 && (int)Get(sim, "Delivered") < 5; i++) Call(sim, "Step", .05f);
            Assert.AreEqual(5, Get(sim, "Delivered"));
            Assert.AreEqual(39, Get(sim, "CoinsEarned"), "Actual handoffs for IDs 0, 2, 4, 5 and 6 earn $5+$10+$5+$12+$7.");
            Assert.AreEqual(0, Get(customer, "PendingOrderLineCount"));
        }

        [Test]
        public void ExpirationReleasesAllFourRoleReservationsBeforeDelivery()
        {
            object balance = NewBalance();
            Set(balance, "customerPatienceSeconds", .15f);
            Set(balance, "maxCustomers", 1);
            object sim = Make(10, balance: balance);
            Call(sim, "StartRound");
            SetProperty(sim, "Coins", 10000);
            Call(sim, "TryHire", Role("Cocacolero"));
            Call(sim, "TryHire", Role("ParrilleroPremium"));
            Call(sim, "TryHire", Role("Fernetero"));
            Assert.IsTrue((bool)Call(sim, "SpawnCustomerWithProducts", new[] { 0, 2, 4, 5 }, new[] { 1, 1, 1, 1 }));
            object customer = ((System.Collections.IList)Get(sim, "Customers"))[0];
            SetProperty(customer, "State", Enum.Parse(TypeOf("StreetCustomerState"), "Waiting"));
            SetProperty(customer, "Position", Get(customer, "Target"));
            Call(sim, "Step", .01f);
            Assert.Greater((int)Get(customer, "Reserved"), 0);
            Call(sim, "Step", .5f);
            Assert.AreEqual(0, Get(sim, "Delivered"));
            Assert.AreEqual(0, Get(customer, "Reserved"));
            for (int i = 0; i < ((System.Collections.IList)Get(customer, "OrderLines")).Count; i++)
                Assert.AreEqual(0, Get(((System.Collections.IList)Get(customer, "OrderLines"))[i], "OwnerWorkerId"));
        }

        [Test]
        public void LegacySaveVersionsMigrateWithoutLosingUnlocksAndNeverRestoreHires()
        {
            Type game = TypeOf("StreetGame");
            Type saveType = game.GetNestedType("SaveData", BindingFlags.Public);
            MethodInfo migrate = game.GetMethod("MigrateSaveData", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (int version in new[] { 1, 2 })
            {
                object save = Activator.CreateInstance(saveType);
                saveType.GetField("version").SetValue(save, version);
                saveType.GetField("unlockedLevel").SetValue(save, 7);
                saveType.GetField("parrilleros").SetValue(save, 3);
                saveType.GetField("cocacoleros").SetValue(save, 2);
                object migrated = migrate.Invoke(null, new[] { save });
                Assert.AreEqual(3, saveType.GetField("version").GetValue(migrated));
                Assert.AreEqual(7, saveType.GetField("unlockedLevel").GetValue(migrated));
                Assert.AreEqual(0, saveType.GetField("premiumParrilleros").GetValue(migrated));
                Assert.AreEqual(0, saveType.GetField("ferneteros").GetValue(migrated));
            }
            object fresh = Make(4);
            Assert.AreEqual(1, Get(fresh, "ParrilleroCount"));
            Assert.AreEqual(0, Get(fresh, "CocacoleroCount"));
            Assert.AreEqual(0, Get(fresh, "ParrilleroPremiumCount"));
            Assert.AreEqual(0, Get(fresh, "FerneteroCount"));
            Assert.AreEqual(0, Get(fresh, "SpeedLevel"));
        }
    }
}
