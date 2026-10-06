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
        [SerializeField, Range(0, 4)] private int selectedLevel;
        private StreetSimulation sim;
        private SaveData save;
        private bool resultSaved;

        [System.Serializable]
        public sealed class SaveData
        {
            public int version = 2;
            public int unlockedLevel;
            public float price = 5f;
            public float[] prices;
            public int coins;
            public int staff = 1;
            public int speed;
        }

        public StreetSimulation Sim { get { return sim; } }
        public StreetBalance Balance { get { return balance; } }
        public int SelectedLevel { get { return selectedLevel; } }
        public int UnlockedLevel { get { return save == null ? 0 : save.unlockedLevel; } }

        private void OnEnable()
        {
            if (balance == null) balance = new StreetBalance();
            save = Load();
            selectedLevel = Mathf.Clamp(selectedLevel, 0, save.unlockedLevel);
            sim = new StreetSimulation(balance, selectedLevel, save.price, save.coins, save.staff, save.speed);
            if(save.prices!=null && save.prices.Length==7)for(int i=0;i<7;i++)sim.SetProductPrice(i,save.prices[i]);
        }

        private void Update()
        {
            if (sim == null) return;
            sim.Step(Time.deltaTime);
            if (!resultSaved && (sim.Phase == RoundPhase.Won || sim.Phase == RoundPhase.Lost))
            {
                resultSaved = true;
                Save();
            }
        }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }

        public bool SelectLevel(int level)
        {
            if (sim == null || level < 0 || level > UnlockedLevel || !sim.SelectLevel(level)) return false;
            selectedLevel = level; Save(); return true;
        }
        public void SetPrice(float value)
        {
            if (sim == null || sim.Phase != RoundPhase.Ready) return;
            sim.SetPrice(value);
        }
        public void SetProductPrice(int product,float value) { if(sim!=null && sim.Phase==RoundPhase.Ready)sim.SetProductPrice(product,value); }
        public void StartRound() { if (sim == null || sim.Phase != RoundPhase.Ready) return; resultSaved = false; sim.StartRound(); Save(); }
        public bool TryHire() { if (sim == null || !sim.TryHire()) return false; Save(); return true; }
        public bool TryUpgradeSpeed() { if (sim == null || !sim.TryUpgradeSpeed()) return false; Save(); return true; }
        public bool NextLevel()
        {
            if (sim == null || !sim.NextLevel()) return false;
            selectedLevel = sim.LevelIndex; save.unlockedLevel = Mathf.Max(save.unlockedLevel, selectedLevel);
            sim = NewReadySimulation(selectedLevel); Save(); return true;
        }
        public bool Retry()
        {
            if (sim == null || (sim.Phase != RoundPhase.Won && sim.Phase != RoundPhase.Lost)) return false;
            resultSaved = false; sim = NewReadySimulation(selectedLevel); Save(); return true;
        }

        private StreetSimulation NewReadySimulation(int level)
        {
            var next = new StreetSimulation(balance, level, sim.Price, 0, sim.StaffCount, sim.SpeedLevel);
            for(int i=0;i<7;i++)next.SetProductPrice(i,sim.GetProductPrice(i));return next;
        }

        private SaveData Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return DefaultSave();
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                data = MigrateSaveData(data);
                if (data == null || data.version != 2) return DefaultSave();
                data.unlockedLevel = Mathf.Clamp(data.unlockedLevel, 0, 4);
                data.price = Mathf.Clamp(data.price, balance.minPrice, balance.maxPrice);
                data.coins = 0; // Legacy balances are not transferable to a newly loaded attempt.
                data.staff = Mathf.Clamp(data.staff, 1, balance.maxStaff);
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
                // Development started from an inflated save; reset only opening staff and speed.
                // Preserve unlocked locations, coins, prices, and the rest of player progress.
                data.staff = 1;
                data.speed = 0;
                data.version = 2;
            }
            return data;
        }


        private SaveData DefaultSave() { return new SaveData { price = balance.initialPrice }; }

        private void Save()
        {
            if (sim == null || save == null) return;
            if(save.prices==null || save.prices.Length!=7)save.prices=new float[7];
            for(int i=0;i<7;i++)save.prices[i]=sim.GetProductPrice(i);
            save.price = sim.Price; save.coins = sim.Coins; save.staff = sim.StaffCount; save.speed = sim.SpeedLevel;
            save.unlockedLevel = Mathf.Max(save.unlockedLevel, sim.LevelIndex);
            if (sim.Phase == RoundPhase.Won) save.unlockedLevel = Mathf.Max(save.unlockedLevel, Mathf.Min(4, sim.LevelIndex + 1));
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save();
        }
    }
}
