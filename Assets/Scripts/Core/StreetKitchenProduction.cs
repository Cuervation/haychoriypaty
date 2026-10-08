using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum StreetFoodState { Raw, Cooking, Cooked, Burned }
    public enum StreetFoodLocation { Grill, Table, Carried }

    [Serializable]
    public sealed class StreetKitchenBalance
    {
        [Min(1)] public int normalTableCapacity = 48;
        [Min(1)] public int premiumTableCapacity = 36;
        [Min(1)] public int fernetTableCapacity = 45;
        [Min(1)] public int barrelCapacity = 12;
        [Min(.1f)] public float cookSeconds = 4f;
        [Min(.1f)] public float burnAfterSeconds = 20f;
        [Min(.1f)] public float discardBurnedAfterSeconds = 1f;
        [Min(.01f)] public float drinkPrepareSeconds = .28f;
    }

    /// <summary>A real, individually identified item in the active street simulation.</summary>
    [Serializable]
    public sealed class StreetFoodUnit
    {
        public int Id { get; internal set; }
        public int Product { get; internal set; }
        public StreetFoodState State { get; internal set; }
        public StreetFoodLocation Location { get; internal set; }
        public int Slot { get; internal set; }
        public int WorkerId { get; internal set; }
        public float Progress { get; internal set; }
        internal float CookedAge;
        internal float BurnedAge;
    }

    /// <summary>Single-simulation food production and inventory; Step is advanced by StreetSimulation.</summary>
    public sealed class StreetKitchenProduction
    {
        private readonly StreetKitchenBalance balance;
        private readonly List<StreetFoodUnit> units = new List<StreetFoodUnit>();
        private readonly int[] tableCapacity = new int[7];
        private readonly int[] grillCapacity = new int[7];
        private readonly float[] drinkProgress = new float[7];
        private int nextId = 1;

        public IReadOnlyList<StreetFoodUnit> Units { get { return units; } }
        public StreetKitchenBalance Balance { get { return balance; } }
        public StreetKitchenProduction(StreetKitchenBalance balance, int[] availableProducts)
        {
            this.balance = balance ?? new StreetKitchenBalance();
            Initialize(availableProducts ?? new int[0]);
        }

        public int TableCount(int product)
        {
            int count = 0;
            for (int i = 0; i < units.Count; i++) if (units[i].Product == product && units[i].Location == StreetFoodLocation.Table && units[i].State == StreetFoodState.Cooked) count++;
            return count;
        }
        public int TableCapacity(int product) { return product >= 0 && product < 7 ? tableCapacity[product] : 0; }
        public int GrillCapacity(int product) { return product >= 0 && product < 7 ? grillCapacity[product] : 0; }
        public StreetFoodUnit Find(int id)
        {
            for (int i = 0; i < units.Count; i++) if (units[i].Id == id) return units[i];
            return null;
        }

        public StreetFoodUnit TryTake(int product, StreetWorkerRole role, int workerId)
        {
            if (product < 0 || product >= 7 || workerId <= 0 || !StreetSpecialties.IsResponsible(role, product)) return null;
            for (int i = 0; i < units.Count; i++)
            {
                StreetFoodUnit unit = units[i];
                if (unit.Product != product || unit.Location != StreetFoodLocation.Table || unit.State != StreetFoodState.Cooked) continue;
                unit.Location = StreetFoodLocation.Carried;
                unit.WorkerId = workerId;
                return unit;
            }
            return null;
        }

        public bool Consume(int id, int product, int workerId)
        {
            StreetFoodUnit unit = Find(id);
            if (unit == null || unit.Product != product || unit.Location != StreetFoodLocation.Carried || unit.WorkerId != workerId || unit.State != StreetFoodState.Cooked) return false;
            units.Remove(unit);
            return true;
        }

        public void ReturnCarried(int id, int workerId)
        {
            StreetFoodUnit unit = Find(id);
            if (unit == null || unit.Location != StreetFoodLocation.Carried || unit.WorkerId != workerId) return;
            unit.WorkerId = 0;
            if (TableCount(unit.Product) < tableCapacity[unit.Product])
            {
                unit.Location = StreetFoodLocation.Table;
                unit.Slot = FirstFreeTableSlot(unit.Product);
            }
            else units.Remove(unit);
        }

        public void Advance(float delta, float workRate, bool normalCook, bool premiumCook, bool cocaWorker, bool fernetero)
        {
            if (delta <= 0f) return;
            for (int product = 4; product <= 6; product++)
            {
                bool preparedByHiredRole = product == 5 ? fernetero : cocaWorker;
                if (!preparedByHiredRole || tableCapacity[product] <= 0 || TableCount(product) >= tableCapacity[product]) continue;
                drinkProgress[product] += delta * Mathf.Max(.01f, workRate);
                if (drinkProgress[product] >= Mathf.Max(.01f, balance.drinkPrepareSeconds))
                {
                    drinkProgress[product] = 0f;
                    Add(product, StreetFoodState.Cooked, StreetFoodLocation.Table, FirstFreeTableSlot(product));
                }
            }
            for (int i = units.Count - 1; i >= 0; i--)
            {
                StreetFoodUnit unit = units[i];
                if (unit.Location == StreetFoodLocation.Carried) continue;
                bool cookingEnabled = unit.Product <= 1 ? normalCook : unit.Product <= 3 ? premiumCook : unit.Product == 5 ? fernetero : cocaWorker;
                if (unit.Location == StreetFoodLocation.Table)
                {
                    continue;
                }
                if (unit.State == StreetFoodState.Raw)
                {
                    if (!cookingEnabled) continue;
                    unit.State = StreetFoodState.Cooking;
                    unit.Progress = 0f;
                }
                if (unit.State == StreetFoodState.Cooking)
                {
                    if (!cookingEnabled) continue;
                    unit.Progress += delta * Mathf.Max(.01f, workRate) / Mathf.Max(.1f, balance.cookSeconds);
                    if (unit.Progress >= 1f)
                    {
                        unit.Progress = 1f;
                        unit.State = StreetFoodState.Cooked;
                        int slot = FirstFreeTableSlot(unit.Product);
                        if (slot >= 0)
                        {
                            int grillSlot = unit.Slot;
                            unit.Location = StreetFoodLocation.Table;
                            unit.Slot = slot;
                            unit.CookedAge = 0f;
                            Add(unit.Product, StreetFoodState.Raw, StreetFoodLocation.Grill, grillSlot);
                        }
                    }
                    continue;
                }
                if (unit.State == StreetFoodState.Cooked)
                {
                    int slot = FirstFreeTableSlot(unit.Product);
                    if (slot >= 0)
                    {
                        int grillSlot = unit.Slot;
                        unit.Location = StreetFoodLocation.Table;
                        unit.Slot = slot;
                        unit.CookedAge = 0f;
                        Add(unit.Product, StreetFoodState.Raw, StreetFoodLocation.Grill, grillSlot);
                        continue;
                    }
                    unit.CookedAge += delta;
                    if (unit.CookedAge >= balance.burnAfterSeconds)
                    {
                        unit.State = StreetFoodState.Burned;
                        unit.Progress = 1f;
                        unit.BurnedAge = 0f;
                    }
                }
                else if (unit.State == StreetFoodState.Burned)
                {
                    unit.BurnedAge += delta;
                    if (unit.BurnedAge >= balance.discardBurnedAfterSeconds)
                    {
                        units.RemoveAt(i);
                        Add(unit.Product, StreetFoodState.Raw, StreetFoodLocation.Grill, unit.Slot);
                    }
                }
            }
        }

        private void Initialize(int[] products)
        {
            bool[] available = new bool[7];
            for (int i = 0; i < products.Length; i++) if (products[i] >= 0 && products[i] < 7) available[products[i]] = true;
            SetGroupCapacities(available, 0, 1, Mathf.Max(1, balance.normalTableCapacity));
            SetGroupCapacities(available, 2, 3, Mathf.Max(1, balance.premiumTableCapacity));
            tableCapacity[5] = available[5] ? Mathf.Max(1, balance.fernetTableCapacity) : 0;
            tableCapacity[4] = available[4] ? Mathf.Max(1, balance.barrelCapacity) : 0;
            tableCapacity[6] = available[6] ? Mathf.Max(1, balance.barrelCapacity) : 0;
            SetGrillCapacities(available, 0, 1, 18, 12);
            SetGrillCapacities(available, 2, 3, 7, 4);
            for (int product = 0; product < 7; product++)
            {
                int capacity = tableCapacity[product];
                if (capacity <= 0) continue;
                if (product == 4 || product == 6) capacity = Mathf.Min(capacity, Mathf.Max(1, balance.barrelCapacity));
                for (int slot = 0; slot < capacity; slot++) Add(product, StreetFoodState.Cooked, StreetFoodLocation.Table, slot);
            }
            for (int product = 0; product < 4; product++)
                for (int slot = 0; slot < grillCapacity[product]; slot++) Add(product, StreetFoodState.Raw, StreetFoodLocation.Grill, slot);
        }
        private void SetGroupCapacities(bool[] available, int first, int second, int capacity)
        {
            int count = (available[first] ? 1 : 0) + (available[second] ? 1 : 0);
            if (count == 0) return;
            int each = capacity / count, remainder = capacity % count;
            if (available[first]) tableCapacity[first] = each + (remainder-- > 0 ? 1 : 0);
            if (available[second]) tableCapacity[second] = each + (remainder > 0 ? 1 : 0);
        }
        private void SetGrillCapacities(bool[] available, int first, int second, int total, int firstDefault)
        {
            int count = (available[first] ? 1 : 0) + (available[second] ? 1 : 0);
            if (count == 0) return;
            if (count == 1) grillCapacity[available[first] ? first : second] = total;
            else { grillCapacity[first] = firstDefault; grillCapacity[second] = total - firstDefault; }
        }
        private int FirstFreeTableSlot(int product)
        {
            // Full prepared tables are the common case: avoid scanning every slot
            // against every live item on each held cooked grill unit/substep.
            if (TableCount(product) >= tableCapacity[product]) return -1;
            for (int slot = 0; slot < tableCapacity[product]; slot++)
            {
                bool occupied = false;
                for (int i = 0; i < units.Count; i++) if (units[i].Product == product && units[i].Location == StreetFoodLocation.Table && units[i].Slot == slot) { occupied = true; break; }
                if (!occupied) return slot;
            }
            return -1;
        }
        private void Add(int product, StreetFoodState state, StreetFoodLocation location, int slot)
        {
            if (slot < 0 && location == StreetFoodLocation.Table) return;
            units.Add(new StreetFoodUnit { Id = nextId++, Product = product, State = state, Location = location, Slot = slot });
        }
    }
}
