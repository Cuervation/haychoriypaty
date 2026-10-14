using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Unity lifecycle and isolated persistence wrapper for StreetSimulation.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetGame : MonoBehaviour
    {
        public const string ProgressKey = "HayChoriYPaty.StreetGame.v1";
        private const string SaveKey = ProgressKey;
        [SerializeField] private StreetBalance balance = new StreetBalance();
        [SerializeField, Range(0, 10)] private int selectedLevel;
        private StreetSimulation sim;
        private OrderDeliverySound deliverySound;
        private SaveData save;
        private bool resultSaved;
        private StreetMarket market;

        [System.Serializable]
        public sealed class SaveData
        {
            public int version = 4;
            public int unlockedLevel;
            public float price = 5f;
            public float[] prices;
            public int coins;
            public int staff = 1;
            public int speed;
            public int workerLevel = -1;
            public int parrilleros = -1;
            public int cocacoleros;
            public int premiumParrilleros;
            public int ferneteros;
            public StreetMarketData market;
        }

        public StreetSimulation Sim { get { return sim; } }
        public StreetBalance Balance { get { return balance; } }
        public int SelectedLevel { get { return selectedLevel; } }
        public int UnlockedLevel { get { return save == null ? 0 : save.unlockedLevel; } }
        public StreetMarket Market => market;
        public bool CanStartRound => sim != null && sim.Phase == RoundPhase.Ready &&
            market != null && !market.Data.attemptActive && market.HasRequiredStock(sim);

        private void OnEnable()
        {
            if (balance == null) balance = new StreetBalance();
            bool hasPriorSave = PlayerPrefs.HasKey(SaveKey);
            save = Load();
            bool hasPersistedMarket = save.market != null;
            market = new StreetMarket(save.market, balance);
            save.market = market.Data;
            if (!hasPriorSave) market.InitializeNewGameKit();
            else if (hasPersistedMarket) market.InitializeExistingSave(save.unlockedLevel);
            else market.Initialize(save.unlockedLevel); // One-time compatibility migration for pre-Mercado saves.
            market.RecoverAbandonedAttempt();
            selectedLevel = Mathf.Clamp(selectedLevel, 0, save.unlockedLevel);
            sim = new StreetSimulation(balance, selectedLevel, save.price);
            if(save.prices!=null && save.prices.Length==7)for(int i=0;i<7;i++)sim.SetProductPrice(i,save.prices[i]);
            InitializeDeliverySound();
            resultSaved = false;
            Save(); // Persist the one-time initialization/recovery, never grant it again on navigation.
        }

        private void Update()
        {
            if (sim == null) return;
            sim.Step(Time.deltaTime);
            ProcessRoundResult();
        }
        private void OnApplicationPause(bool paused) { if (paused) { ProcessRoundResult(); Save(); } }
        private void OnApplicationQuit() { FinishOrAbort(); }
        private void OnDisable() { FinishOrAbort(); }

        private void FinishOrAbort()
        {
            if (sim == null || market == null) return;
            ProcessRoundResult();
            if (sim.Phase == RoundPhase.Playing) market.AbortAttempt();
            Save();
        }

        public bool ProcessRoundResult()
        {
            if (sim == null || save == null || resultSaved ||
                (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost)) return false;
            market?.SettleAttempt(sim);
            if (market != null && market.Data.attemptActive) return false; // Refuse progression if settlement was rejected.
            int previousUnlocked = save.unlockedLevel;
            if (sim.Phase == RoundPhase.Won)
                save.unlockedLevel = Mathf.Max(save.unlockedLevel, Mathf.Min(StreetSimulation.LevelNames.Length - 1, sim.LevelIndex + 1));
            resultSaved = Save();
            if (!resultSaved) save.unlockedLevel = previousUnlocked;
            return resultSaved;
        }

        private bool EnsureResultSaved() => resultSaved ||
            (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost) || ProcessRoundResult();

        public bool SelectLevel(int level)
        {
            if (sim == null || sim.Phase == RoundPhase.Playing || level < 0 ||
                level >= StreetSimulation.LevelNames.Length || level > UnlockedLevel) return false;
            if (!EnsureResultSaved()) return false;
            selectedLevel = level;
            sim = NewReadySimulation(level); resultSaved = false;
            InitializeDeliverySound(); Save(); return true;
        }
        public void SetPrice(float value)
        {
            if (sim == null || sim.Phase != RoundPhase.Ready) return;
            sim.SetPrice(value);
        }
        public void SetProductPrice(int product,float value) { if(sim!=null && sim.Phase==RoundPhase.Ready)sim.SetProductPrice(product,value); }
        public void StartRound() { TryStartRound(); }
        public bool TryStartRound()
        {
            if (!CanStartRound || !market.BeginAttempt(sim)) return false;
            // Write the full pre-attempt snapshot before any delivery can consume stock.
            if (!Save()) { market.AbortAttempt(); return false; }
            sim.BindIngredientStock(market);
            resultSaved = false; sim.StartRound(); Save(); return true;
        }
        public bool TryBuyIngredient(int ingredient, int packs, long operationId = 0)
        {
            if (market == null || sim == null || sim.Phase != RoundPhase.Ready || UnlockedLevel == 0) return false;
            string before = JsonUtility.ToJson(market.Data);
            if (!market.TryBuy(sim, ingredient, packs, UnlockedLevel,
                operationId == 0 ? market.NextPurchaseId : operationId)) return false;
            if (Save()) return true;
            market = new StreetMarket(JsonUtility.FromJson<StreetMarketData>(before), balance);
            save.market = market.Data;
            return false;
        }
        public bool BuyMissingIngredient(int ingredient) => market != null &&
            TryBuyIngredient(ingredient, market.MissingPacks(sim, ingredient));
        public bool TryHire() { return TryHireParrillero(); }
        public bool TryHireParrillero() { return HireRole(StreetWorkerRole.Parrillero); }
        public bool TryHireCocacolero() { return HireRole(StreetWorkerRole.Cocacolero); }
        public bool TryHireParrilleroPremium() { return HireRole(StreetWorkerRole.ParrilleroPremium); }
        public bool TryHireFernetero() { return HireRole(StreetWorkerRole.Fernetero); }
        public bool TryHire(StreetWorkerRole role) { return HireRole(role); }
        private bool HireRole(StreetWorkerRole role) { if (sim == null || !sim.TryHire(role)) return false; Save(); return true; }
        public bool TryUpgradeSpeed() { if (sim == null || !sim.TryUpgradeSpeed()) return false; Save(); return true; }
        public bool NextLevel()
        {
            if (sim == null || sim.Phase != RoundPhase.Won || sim.LevelIndex + 1 >= StreetSimulation.LevelNames.Length) return false;
            if (!EnsureResultSaved()) return false;
            selectedLevel = sim.LevelIndex + 1; save.unlockedLevel = Mathf.Max(save.unlockedLevel, selectedLevel);
            sim = NewReadySimulation(selectedLevel); InitializeDeliverySound(); Save(); return true;
        }
        public bool Retry()
        {
            return false; // Retained API for old callers, not a retry mechanic or navigation action.
        }
        public bool ReturnToLevels()
        {
            if (sim == null || market == null) return false;
            if (!EnsureResultSaved()) return false;
            market.AbortAttempt();
            resultSaved = false; sim = NewReadySimulation(selectedLevel); InitializeDeliverySound(); Save(); return true;
        }

        private void InitializeDeliverySound()
        {
            if (deliverySound == null)
            {
                deliverySound = GetComponent<OrderDeliverySound>();
                if (deliverySound == null) deliverySound = gameObject.AddComponent<OrderDeliverySound>();
            }
            deliverySound.Initialize(sim);
        }

        private StreetSimulation NewReadySimulation(int level)
        {
            var next = new StreetSimulation(balance, level, sim.Price);
            for(int i=0;i<7;i++)next.SetProductPrice(i,sim.GetProductPrice(i));return next;
        }

        private SaveData Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return DefaultSave();
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                data = MigrateSaveData(data);
                if (data == null || data.version != 4) return DefaultSave();
                data.unlockedLevel = Mathf.Clamp(data.unlockedLevel, 0, StreetSimulation.LevelNames.Length - 1);
                data.price = Mathf.Clamp(data.price, balance.minPrice, balance.maxPrice);
                data.coins = 0; // Legacy balances are not transferable to a newly loaded attempt.
                data.staff = Mathf.Max(1, data.staff);
                data.speed = Mathf.Max(0, data.speed);
                return data;
            }
            catch { return DefaultSave(); }
        }

        private static SaveData MigrateSaveData(SaveData data)
        {
            if (data == null) return null;
            if (data.version == 1)
            {
                // Development started from an inflated save; retain its established migration.
                data.staff = 1;
                data.speed = 0;
                data.version = 2;
            }
            if (data.version == 2)
            {
                // Old saves only recorded the first two roles. Composition is never restored,
                // but retain their progress fields and initialize new role counters safely.
                data.premiumParrilleros = 0;
                data.ferneteros = 0;
                data.version = 3;
            }
            if (data.version == 3) data.version = 4; // Market.Initialize performs the one-time stock migration.
            return data;
        }


        private SaveData DefaultSave() { return new SaveData { price = balance.initialPrice }; }

        private bool Save()
        {
            if (sim == null || save == null) return false;
            if(save.prices==null || save.prices.Length!=7)save.prices=new float[7];
            for(int i=0;i<7;i++)save.prices[i]=sim.GetProductPrice(i);
            save.price = sim.Price; save.coins = sim.Coins; save.staff = sim.StaffCount; save.speed = sim.SpeedLevel;
            save.workerLevel = sim.LevelIndex; save.parrilleros = sim.ParrilleroCount; save.cocacoleros = sim.CocacoleroCount;
            save.premiumParrilleros = sim.ParrilleroPremiumCount; save.ferneteros = sim.FerneteroCount;
            save.unlockedLevel = Mathf.Max(save.unlockedLevel, sim.LevelIndex);
            save.market = market?.Data ?? save.market;
            try
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save();
                return true;
            }
            catch (System.Exception error)
            {
                Debug.LogError("No se pudo guardar Mercado y progreso: " + error.Message);
                return false;
            }
        }
    }
}
