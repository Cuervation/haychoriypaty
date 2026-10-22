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
        [Min(.01f)] public float activeCookSeconds = 2f;
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
        internal bool ReservedTableCapacity;
        internal long TableDepositSequence;
        internal bool PendingTableReturn;
        internal float CookedAge;
        internal float BurnedAge;
    }

    /// <summary>Single-simulation food production and inventory; Step is advanced by StreetSimulation.</summary>
    public sealed class StreetKitchenProduction
    {
        private readonly StreetKitchenBalance balance;
        private readonly IStreetKitchenStock ingredientStock;
        private readonly List<StreetFoodUnit> units = new List<StreetFoodUnit>();
        private readonly int[] tableCapacity = new int[7];
        private readonly int[] grillCapacity = new int[7];
        private readonly float[] drinkProgress = new float[7];
        private int nextId = 1;
        private long nextTableDepositSequence;
        public bool ActiveCooks { get; private set; }

        public IReadOnlyList<StreetFoodUnit> Units { get { return units; } }
        public StreetKitchenBalance Balance { get { return balance; } }
        public StreetKitchenProduction(StreetKitchenBalance balance, int[] availableProducts)
            : this(balance, availableProducts, null) { }

        public StreetKitchenProduction(StreetKitchenBalance balance, int[] availableProducts,
            IStreetKitchenStock ingredientStock)
            : this(balance, availableProducts, ingredientStock, false) { }

        public StreetKitchenProduction(StreetKitchenBalance balance, int[] availableProducts,
            IStreetKitchenStock ingredientStock, bool activeCooks)
        {
            this.balance = balance ?? new StreetKitchenBalance();
            this.ingredientStock = ingredientStock;
            ActiveCooks = activeCooks;
            Initialize(availableProducts ?? new int[0]);
        }

        public bool HasSupply(int product)
        {
            if (ingredientStock == null || (ActiveCooks && product >= 0 && product < 4)) return true;
            for (int i = 0; i < units.Count; i++)
                if (units[i].Product == product && units[i].State != StreetFoodState.Burned) return true;
            return ingredientStock.CanPrepareFood(product);
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
            if (product < 0 || product >= 7 || workerId <= 0 || !StreetSpecialties.IsDeliveryResponsible(role, product, ActiveCooks)) return null;
            for (int i = 0; i < units.Count; i++)
            {
                StreetFoodUnit unit = units[i];
                if (unit.Product != product || unit.Location != StreetFoodLocation.Table || unit.State != StreetFoodState.Cooked ||
                    (unit.WorkerId != 0 && unit.WorkerId != workerId) ||
                    (ActiveCooks && product < 4 && unit.WorkerId != workerId)) continue;
                unit.Location = StreetFoodLocation.Carried;
                unit.WorkerId = workerId;
                if (ActiveCooks && product < 4)
                {
                    // The sandwich no longer occupies a physical table cell once the Mozo lifts it.
                    // Keep WorkerId as the exclusive item claim, but make its old slot reusable.
                    unit.Slot = -1;
                    unit.ReservedTableCapacity = false;
                    CompactTableSlots(product);
                }
                return unit;
            }
            return null;
        }

        public int SupplyCount(int product)
        {
            int count = 0;
            for (int i = 0; i < units.Count; i++)
                if (units[i].Product == product && units[i].State != StreetFoodState.Burned) count++;
            return count;
        }
        public int AvailableTableCount(int product, int workerId = 0)
        {
            int count = 0;
            foreach (StreetFoodUnit unit in units)
                if (unit.Product == product && unit.Location == StreetFoodLocation.Table &&
                    unit.State == StreetFoodState.Cooked && (unit.WorkerId == 0 || unit.WorkerId == workerId)) count++;
            return count;
        }
        public StreetFoodUnit ReserveTableUnit(int product, int workerId)
        {
            if (!ActiveCooks || product < 0 || product > 3 || workerId <= 0) return null;
            // Idempotent for dispatch after an assignment, without reserving a second sandwich.
            foreach (StreetFoodUnit unit in units)
                if (unit.WorkerId == workerId)
                    return unit.Location == StreetFoodLocation.Table && unit.Product == product ? unit : null;
            // Cook creation order can differ from arrival order: claim the last actual deposit.
            StreetFoodUnit latest = null;
            foreach (StreetFoodUnit unit in units)
                if (unit.Location == StreetFoodLocation.Table && unit.Product == product &&
                    unit.State == StreetFoodState.Cooked && unit.WorkerId == 0 &&
                    (latest == null || unit.TableDepositSequence > latest.TableDepositSequence)) latest = unit;
            if (latest != null) latest.WorkerId = workerId;
            return latest;
        }
        public void ReleaseTableReservation(int workerId)
        {
            foreach (StreetFoodUnit unit in units)
                if (unit.Location == StreetFoodLocation.Table && unit.WorkerId == workerId) unit.WorkerId = 0;
        }
        public bool IsGrillSlotFree(int product, int slot)
        {
            if (product < 0 || product >= grillCapacity.Length || slot < 0 || slot >= grillCapacity[product]) return false;
            foreach (StreetFoodUnit unit in units)
                if (unit.Product == product && unit.Location == StreetFoodLocation.Grill && unit.Slot == slot) return false;
            return true;
        }
        private int FreeGrillSlot(int product)
        {
            for (int slot = 0; slot < GrillCapacity(product); slot++)
                if (IsGrillSlotFree(product, slot)) return slot;
            return -1;
        }
        public bool CanReserveCook(int product) => ActiveCooks && product >= 0 && product < 4 &&
            !HasPendingTableReturn(product) && TableCapacityInUse(product) < tableCapacity[product] &&
            FreeGrillSlot(product) >= 0;

        public StreetFoodUnit ReserveCook(int product, int workerId) => ReserveCook(product, workerId, -1);
        public StreetFoodUnit ReserveCook(int product, int workerId, int preferredGrillSlot)
        {
            if (workerId <= 0 || !CanReserveCook(product)) return null;
            foreach (StreetFoodUnit unit in units)
                if (unit.WorkerId == workerId) return null;
            int grillSlot = IsGrillSlotFree(product, preferredGrillSlot) ? preferredGrillSlot : FreeGrillSlot(product);
            StreetFoodUnit created = Add(product, StreetFoodState.Raw, StreetFoodLocation.Grill, grillSlot);
            if (created != null)
            {
                created.WorkerId = workerId;
                created.ReservedTableCapacity = true;
            }
            return created;
        }
        public bool AdvanceCook(int id, int workerId, float delta, float workRate)
        {
            StreetFoodUnit unit = Find(id);
            if (!ActiveCooks || unit == null || unit.Location != StreetFoodLocation.Grill || unit.WorkerId != workerId || delta <= 0f) return false;
            if (unit.State == StreetFoodState.Cooked) return true;
            if (unit.State != StreetFoodState.Raw && unit.State != StreetFoodState.Cooking) return false;
            unit.State = StreetFoodState.Cooking;
            unit.Progress = Mathf.Min(1f, unit.Progress + delta * Mathf.Max(.01f, workRate) / Mathf.Max(.01f, balance.activeCookSeconds));
            if (unit.Progress >= 1f - .00001f) { unit.Progress = 1f; unit.State = StreetFoodState.Cooked; return true; }
            return false;
        }
        public bool TakeCooked(int id, int workerId)
        {
            StreetFoodUnit unit = Find(id);
            if (!ActiveCooks || unit == null || unit.Location != StreetFoodLocation.Grill ||
                unit.State != StreetFoodState.Cooked || unit.WorkerId != workerId) return false;
            unit.Location = StreetFoodLocation.Carried;
            unit.Slot = -1;
            return true;
        }
        public bool DepositCooked(int id, int workerId)
        {
            StreetFoodUnit unit = Find(id);
            if (!ActiveCooks || unit == null || unit.Location != StreetFoodLocation.Carried ||
                unit.State != StreetFoodState.Cooked || unit.WorkerId != workerId || !unit.ReservedTableCapacity) return false;
            CompactTableSlots(unit.Product);
            int slot = FirstFreeTableSlot(unit.Product);
            if (slot < 0) return false;
            unit.Location = StreetFoodLocation.Table;
            unit.Slot = slot;
            unit.ReservedTableCapacity = false;
            unit.WorkerId = 0;
            unit.TableDepositSequence = ++nextTableDepositSequence;
            return true;
        }
        public void CancelCook(int workerId)
        {
            if (!ActiveCooks || workerId <= 0) return;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                StreetFoodUnit unit = units[i];
                if (unit.WorkerId != workerId) continue;
                if (unit.State == StreetFoodState.Cooked)
                {
                    if (unit.Location != StreetFoodLocation.Table)
                    {
                        CompactTableSlots(unit.Product);
                        int capacityUsedWithoutThisUnit = TableCapacityInUse(unit.Product) -
                            (unit.ReservedTableCapacity ? 1 : 0);
                        int slot = capacityUsedWithoutThisUnit < tableCapacity[unit.Product]
                            ? FirstFreeTableSlot(unit.Product) : -1;
                        if (slot >= 0) PlaceReturnedSandwich(unit, slot);
                        else
                        {
                            unit.Location = StreetFoodLocation.Carried;
                            unit.Slot = -1;
                            unit.ReservedTableCapacity = false;
                            unit.PendingTableReturn = true;
                        }
                    }
                    else unit.ReservedTableCapacity = false;
                    unit.WorkerId = 0;
                }
                else { ingredientStock?.ReleaseFood(unit.Id); units.RemoveAt(i); }
            }
        }

        public bool Consume(int id, int product, int workerId)
        {
            StreetFoodUnit unit = Find(id);
            if (unit == null || unit.Product != product || unit.Location != StreetFoodLocation.Carried || unit.WorkerId != workerId || unit.State != StreetFoodState.Cooked) return false;
            // Active sandwich products (0–3) are not backed by Market/Fridge ingredients.
            // Other products and legacy automatic levels retain their reserved-stock transaction.
            if (!(ActiveCooks && product >= 0 && product < 4) &&
                ingredientStock != null && !ingredientStock.ConsumeReservedFood(id, product)) return false;
            units.Remove(unit);
            return true;
        }

        public void ReturnCarried(int id, int workerId)
        {
            StreetFoodUnit unit = Find(id);
            if (unit == null || unit.Location != StreetFoodLocation.Carried || unit.WorkerId != workerId) return;
            unit.WorkerId = 0;
            if (ActiveCooks && unit.Product < 4)
            {
                CompactTableSlots(unit.Product);
                int slot = TableCapacityInUse(unit.Product) < tableCapacity[unit.Product]
                    ? FirstFreeTableSlot(unit.Product) : -1;
                if (slot >= 0) PlaceReturnedSandwich(unit, slot);
                else
                {
                    // Preserve canceled food without colliding with a slot now used by another cook.
                    // Pending returns take priority over starting more cooks until a cell opens.
                    unit.Location = StreetFoodLocation.Carried;
                    unit.Slot = -1;
                    unit.ReservedTableCapacity = false;
                    unit.PendingTableReturn = true;
                }
                return;
            }
            if (TableCount(unit.Product) < tableCapacity[unit.Product])
            {
                unit.Location = StreetFoodLocation.Table;
                unit.Slot = FirstFreeTableSlot(unit.Product);
            }
            else
            {
                ingredientStock?.ReleaseFood(unit.Id);
                units.Remove(unit);
            }
        }

        public void Advance(float delta, float workRate, bool normalCook, bool premiumCook, bool cocaWorker, bool fernetero)
        {
            // Retain the original shared-rate API for existing callers/tests.
            Advance(delta, workRate, workRate, normalCook, premiumCook, cocaWorker, fernetero);
        }

        public void Advance(float delta, float cookWorkRate, float beverageWorkRate,
            bool normalCook, bool premiumCook, bool cocaWorker, bool fernetero)
        {
            if (delta <= 0f) return;
            cookWorkRate = Mathf.Max(.01f, cookWorkRate);
            beverageWorkRate = Mathf.Max(.01f, beverageWorkRate);
            if (ActiveCooks)
                for (int product = 0; product < 4; product++) RestorePendingTableReturns(product);

            for (int product = 4; product <= 6; product++)
            {
                bool preparedByHiredRole = product == 5 ? fernetero : cocaWorker;
                if (!preparedByHiredRole || tableCapacity[product] <= 0 || TableCount(product) >= tableCapacity[product]) continue;
                drinkProgress[product] += delta * beverageWorkRate;
                if (drinkProgress[product] >= Mathf.Max(.01f, balance.drinkPrepareSeconds))
                {
                    drinkProgress[product] = 0f;
                    Add(product, StreetFoodState.Cooked, StreetFoodLocation.Table, FirstFreeTableSlot(product));
                }
            }
            for (int i = units.Count - 1; i >= 0; i--)
            {
                StreetFoodUnit unit = units[i];
                if (unit.Location == StreetFoodLocation.Carried || (ActiveCooks && unit.Product < 4)) continue;
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
                    unit.Progress += delta * cookWorkRate / Mathf.Max(.1f, balance.cookSeconds);
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
                        ingredientStock?.ReleaseFood(unit.Id);
                        Add(unit.Product, StreetFoodState.Raw, StreetFoodLocation.Grill, unit.Slot);
                    }
                }
            }
            // A failed ingredient reservation leaves the slot empty, not permanently disabled.
            if (ingredientStock != null && !ActiveCooks)
            for (int product = 0; product < 4; product++)
            {
                if (!(product <= 1 ? normalCook : premiumCook)) continue;
                for (int slot = 0; slot < grillCapacity[product]; slot++)
                {
                    bool occupied = false;
                    for (int i = 0; i < units.Count; i++)
                        if (units[i].Location == StreetFoodLocation.Grill && units[i].Product == product && units[i].Slot == slot)
                        { occupied = true; break; }
                    if (!occupied) Add(product, StreetFoodState.Raw, StreetFoodLocation.Grill, slot);
                }
            }
        }

        private void Initialize(int[] products)
        {
            bool[] available = new bool[7];
            for (int i = 0; i < products.Length; i++) if (products[i] >= 0 && products[i] < 7) available[products[i]] = true;
            // The serialized normal/Premium balances still divide Levels 1-5 starting stock
            // across available products. Physical sandwich table capacity is now 48 per product
            // in every level; Racing+ still starts with no prepared sandwich inventory.
            SetGroupCapacities(available, 0, 1, Mathf.Max(1, balance.normalTableCapacity));
            SetGroupCapacities(available, 2, 3, Mathf.Max(1, balance.premiumTableCapacity));
            int[] initialSandwichStock = new int[4];
            Array.Copy(tableCapacity, initialSandwichStock, initialSandwichStock.Length);
            for (int product = 0; product < 4; product++)
                tableCapacity[product] = available[product] ? 48 : 0;
            tableCapacity[5] = available[5] ? Mathf.Max(1, balance.fernetTableCapacity) : 0;
            tableCapacity[4] = available[4] ? Mathf.Max(1, balance.barrelCapacity) : 0;
            tableCapacity[6] = available[6] ? Mathf.Max(1, balance.barrelCapacity) : 0;
            SetGrillCapacities(available, 0, 1, 18, 12);
            SetGrillCapacities(available, 2, 3, 7, 4);
            for (int product = 0; product < 7; product++)
            {
                int capacity = tableCapacity[product];
                if (capacity <= 0 || (ActiveCooks && product < 4)) continue;
                if (product == 4 || product == 6) capacity = Mathf.Min(capacity, Mathf.Max(1, balance.barrelCapacity));
                int initialStock = product < 4 ? Mathf.Min(initialSandwichStock[product], capacity) : capacity;
                for (int slot = 0; slot < initialStock; slot++) Add(product, StreetFoodState.Cooked, StreetFoodLocation.Table, slot);
            }
            if (!ActiveCooks)
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
                for (int i = 0; i < units.Count; i++) if (units[i].Product == product &&
                    units[i].Location == StreetFoodLocation.Table && units[i].Slot == slot) { occupied = true; break; }
                if (!occupied) return slot;
            }
            return -1;
        }
        private bool HasPendingTableReturn(int product)
        {
            foreach (StreetFoodUnit unit in units)
                if (unit.Product == product && unit.PendingTableReturn) return true;
            return false;
        }
        private void RestorePendingTableReturns(int product)
        {
            CompactTableSlots(product);
            for (int i = 0; i < units.Count; i++)
            {
                StreetFoodUnit unit = units[i];
                if (unit.Product != product || !unit.PendingTableReturn) continue;
                if (TableCapacityInUse(product) >= tableCapacity[product]) return;
                int slot = FirstFreeTableSlot(product);
                if (slot < 0) return;
                PlaceReturnedSandwich(unit, slot);
            }
        }
        private void PlaceReturnedSandwich(StreetFoodUnit unit, int slot)
        {
            unit.Location = StreetFoodLocation.Table;
            unit.Slot = slot;
            unit.ReservedTableCapacity = false;
            unit.PendingTableReturn = false;
            unit.TableDepositSequence = ++nextTableDepositSequence;
        }

        private int TableCapacityInUse(int product)
        {
            int count = TableCount(product);
            if (!ActiveCooks || product < 0 || product > 3) return count;
            for (int i = 0; i < units.Count; i++)
                if (units[i].Product == product && units[i].ReservedTableCapacity) count++;
            return count;
        }

        private void CompactTableSlots(int product)
        {
            if (!ActiveCooks || product < 0 || product > 3) return;
            List<StreetFoodUnit> tableItems = new List<StreetFoodUnit>();
            for (int i = 0; i < units.Count; i++)
            {
                StreetFoodUnit unit = units[i];
                if (unit.Product == product && unit.Location == StreetFoodLocation.Table &&
                    unit.State == StreetFoodState.Cooked) tableItems.Add(unit);
            }
            tableItems.Sort((a, b) =>
            {
                int slotOrder = a.Slot.CompareTo(b.Slot);
                if (slotOrder != 0) return slotOrder;
                int depositOrder = a.TableDepositSequence.CompareTo(b.TableDepositSequence);
                return depositOrder != 0 ? depositOrder : a.Id.CompareTo(b.Id);
            });
            for (int slot = 0; slot < tableItems.Count; slot++) tableItems[slot].Slot = slot;
        }
        private StreetFoodUnit Add(int product, StreetFoodState state, StreetFoodLocation location, int slot)
        {
            if (slot < 0 && location == StreetFoodLocation.Table) return null;
            // Racing+ real sandwiches intentionally bypass ingredient reservations; drinks keep theirs.
            if (!(ActiveCooks && product >= 0 && product < 4) &&
                ingredientStock != null && !ingredientStock.TryReserveFood(nextId, product)) return null;
            var unit = new StreetFoodUnit { Id = nextId++, Product = product, State = state, Location = location,
                Slot = slot };
            if (ActiveCooks && product < 4 && location == StreetFoodLocation.Table)
                unit.TableDepositSequence = ++nextTableDepositSequence;
            units.Add(unit);
            CompactTableSlots(product);
            return unit;
        }
    }
}
