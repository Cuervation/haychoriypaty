using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum RoundPhase
    {
        Ready,
        Playing,
        Won,
        Lost
    }

    /// <summary>Small single-round simulation for the first Floresta playable slice.</summary>
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private GameBalance balance = new GameBalance();

        private readonly List<CustomerOrder> guests = new List<CustomerOrder>();
        private float elapsed;
        private float nextArrival;
        private float nextHandoff;
        private float crowdPatience;
        private int coins;
        private int staffCount;
        private int speedUpgrades;
        private int served;
        private int departed;
        private int nextGuestId;

        public RoundPhase Phase { get; private set; } = RoundPhase.Ready;
        public IList<CustomerOrder> Guests { get { return guests; } }
        public GameBalance Balance { get { return balance; } }
        public int Coins { get { return coins; } }
        public int StaffCount { get { return staffCount; } }
        public int SpeedUpgrades { get { return speedUpgrades; } }
        public int Served { get { return served; } }
        public int Departed { get { return departed; } }
        public float CrowdPatience { get { return crowdPatience; } }
        public float TimeRemaining { get { return Mathf.Max(0f, balance.roundDurationSeconds - elapsed); } }
        public float WorkRate { get { return 1f + Mathf.Max(0, staffCount - 1) * balance.helperCookBonus + speedUpgrades * balance.speedUpgradeCookBonus; } }
        public CustomerOrder CookingOrder
        {
            get
            {
                for (int i = 0; i < guests.Count; i++)
                    if (guests[i].State == GuestState.Cooking) return guests[i];
                return null;
            }
        }
        public int CookingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < guests.Count; i++)
                    if (guests[i].State == GuestState.Cooking) count++;
                return count;
            }
        }

        private void Awake()
        {
            if (balance == null) balance = new GameBalance();
            PrototypeView view = GetComponent<PrototypeView>();
            if (view == null) gameObject.AddComponent<PrototypeView>();
            ResetRound();
        }

        private void OnEnable()
        {
            ResetRound();
            Phase = RoundPhase.Ready;
        }

        private void Update()
        {
            AdvanceRound(Time.deltaTime);
        }

        // The same step drives real frames and focused editor acceptance tests.
        private void AdvanceRound(float delta)
        {
            if (Phase != RoundPhase.Playing || delta <= 0f) return;
            elapsed += delta;
            if (elapsed >= balance.roundDurationSeconds)
            {
                Phase = served >= balance.customersToWin ? RoundPhase.Won : RoundPhase.Lost;
                return;
            }

            if (elapsed >= nextArrival) SpawnGuest();
            AdvanceOrders(delta);
            if (guests.Count >= 3) crowdPatience -= balance.crowdPressurePerSecond * (guests.Count - 2) * delta;

            crowdPatience = Mathf.Clamp(crowdPatience, 0f, 100f);
            if (crowdPatience <= 0f) Phase = RoundPhase.Lost;
            else if (served >= balance.customersToWin) Phase = RoundPhase.Won;
        }

        public void StartRound()
        {
            ResetRound();
            Phase = RoundPhase.Playing;
            nextArrival = 0.6f;
        }

        public bool TryHire()
        {
            if (Phase != RoundPhase.Playing || staffCount >= 2 || coins < balance.hireCost) return false;
            coins -= balance.hireCost;
            staffCount++;
            return true;
        }

        public bool TryUpgradeSpeed()
        {
            if (Phase != RoundPhase.Playing || speedUpgrades >= balance.maxSpeedUpgrades || coins < balance.speedUpgradeCost) return false;
            coins -= balance.speedUpgradeCost;
            speedUpgrades++;
            return true;
        }

        public float PatienceFraction(CustomerOrder guest)
        {
            return Mathf.Clamp01(guest.PatienceRemaining / Mathf.Max(1f, balance.customerPatienceSeconds));
        }

        private void ResetRound()
        {
            guests.Clear();
            elapsed = 0f;
            nextArrival = 0f;
            nextHandoff = 0f;
            crowdPatience = balance.startingCrowdPatience;
            coins = balance.startingCoins;
            staffCount = 1;
            speedUpgrades = 0;
            served = 0;
            departed = 0;
            nextGuestId = 1;
        }

        private void SpawnGuest()
        {
            if (guests.Count < balance.maxQueueCapacity)
            {
                float roll = Random.value;
                OrderKind kind = roll < 0.27f ? OrderKind.CocaOnly : (roll < 0.62f ? OrderKind.ChoriAndCoca : OrderKind.Chori);
                guests.Add(new CustomerOrder(nextGuestId++, kind, balance.customerPatienceSeconds));
            }
            nextArrival = elapsed + balance.arrivalIntervalSeconds;
        }

        private void AdvanceOrders(float delta)
        {
            // Automatic cooking with inspector-configurable parallel grill slots.
            int activeGrillOrders = CookingCount;
            for (int i = 0; i < guests.Count; i++)
            {
                if (guests[i].State != GuestState.Waiting) continue;
                CustomerOrder guest = guests[i];
                if (guest.HasFood && activeGrillOrders < Mathf.Max(1, balance.grillCapacity))
                {
                    guest.State = GuestState.Cooking;
                    guest.WorkRemaining = balance.choriCookSeconds;
                    activeGrillOrders++;
                }
                else if (!guest.HasFood)
                {
                    guest.State = GuestState.Seasoning;
                    guest.WorkRemaining = balance.condimentSeconds * 0.65f;
                }
            }

            for (int i = guests.Count - 1; i >= 0; i--)
            {
                CustomerOrder guest = guests[i];
                guest.PatienceRemaining -= delta;
                if (guest.PatienceRemaining <= 0f)
                {
                    guests.RemoveAt(i);
                    departed++;
                    crowdPatience -= balance.crowdDeparturePenalty;
                    continue;
                }

                if (guest.State == GuestState.Cooking)
                {
                    guest.WorkRemaining -= delta * WorkRate;
                    if (guest.WorkRemaining <= 0f)
                    {
                        guest.State = GuestState.Seasoning;
                        guest.WorkRemaining = balance.condimentSeconds;
                    }
                }
                else if (guest.State == GuestState.Seasoning)
                {
                    guest.WorkRemaining -= delta * WorkRate;
                    if (guest.WorkRemaining <= 0f) guest.State = GuestState.Ready;
                }
            }

            CustomerOrder ready = FirstReadyOrder();
            if (ready != null && elapsed >= nextHandoff)
            {
                guests.Remove(ready);
                served++;
                coins += balance.coinsPerOrder;
                nextHandoff = elapsed + balance.handoffSeconds / Mathf.Max(1f, staffCount + speedUpgrades * balance.speedUpgradeHandoffBonus);
            }
        }

        private CustomerOrder FirstReadyOrder()
        {
            for (int i = 0; i < guests.Count; i++)
                if (guests[i].State == GuestState.Ready) return guests[i];
            return null;
        }
    }
}
