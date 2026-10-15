using System;
using System.IO;
using UnityEngine;

namespace Farm
{
    /// <summary>Gameplay events used by quests, UI and analytics hooks.</summary>
    public static class GameEvents
    {
        public static event Action<string, string, int> OnAction;
        public static void Raise(string key, string arg = "", int amount = 1) => OnAction?.Invoke(key, arg, amount);
        public static void Clear() => OnAction = null;
    }

    public static class SaveSystem
    {
        public static string FilePath => Path.Combine(Application.persistentDataPath, "farm_save.json");

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                SaveData d = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return d != null && d.initialized ? d : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Farm] Save load failed, starting new game: " + e.Message);
                return null;
            }
        }

        public static bool Save(SaveData d)
        {
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(d));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Farm] Save failed: " + e.Message);
                return false;
            }
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
    }

    /// <summary>Owns the save state: coins, XP/level, storage, virtual clock.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public SaveData Data { get; private set; }
        public bool IsNewGame { get; private set; }
        public bool MapWasReset { get; private set; }

        public event Action Changed;
        public event Action<int> LeveledUp;

        private float autosaveTimer;

        public static double RealNow => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        public double Now => RealNow + (Data != null ? Data.timeOffset : 0);

        public void Init()
        {
            I = this;
            Data = SaveSystem.Load();
            if (Data != null && Data.version < SaveData.CurrentVersion)
            {
                Debug.Log("[Farm] Old save layout (v" + Data.version + ") – starting a fresh farm with the new map.");
                Data = null;
                MapWasReset = true;
            }
            IsNewGame = Data == null;
            if (IsNewGame) Data = new SaveData();
        }

        private void Update()
        {
            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer > 15f) { autosaveTimer = 0; SaveNow(); }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) { SaveNow(); LocalNotifications.ScheduleAll(); }
            else LocalNotifications.CancelAll();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
            LocalNotifications.ScheduleAll();
        }

        public event Action BeforeSave;

        /// <summary>Write the save file now. Returns false when it could not be written.</summary>
        public bool SaveNow()
        {
            if (Data == null || !Data.initialized) return false;
            BeforeSave?.Invoke();
            Data.lastSave = RealNow;
            return SaveSystem.Save(Data);
        }

        public void NotifyChanged() => Changed?.Invoke();

        // ---------- Coins ----------
        public bool CanAfford(int n) => Data.coins >= n;

        public bool TrySpend(int n)
        {
            if (Data.coins < n) return false;
            Data.coins -= n;
            NotifyChanged();
            return true;
        }

        public void AddCoins(int n)
        {
            Data.coins += n;
            NotifyChanged();
        }

        // ---------- Gems ----------
        public bool TrySpendGems(int n)
        {
            if (Data.gems < n) return false;
            Data.gems -= n;
            NotifyChanged();
            return true;
        }

        public void AddGems(int n)
        {
            if (n <= 0) return;
            Data.gems += n;
            NotifyChanged();
        }

        // ---------- XP ----------
        public void AddXp(int n)
        {
            if (n <= 0) return;
            Data.xp += n;
            while (Data.level < Defs.MaxLevel && Data.xp >= Defs.XpToNext(Data.level))
            {
                Data.xp -= Defs.XpToNext(Data.level);
                Data.level++;
                Data.coins += Defs.LevelUpCoins(Data.level);
                Data.gems += Defs.LevelUpGems(Data.level);
                LeveledUp?.Invoke(Data.level);
                GameEvents.Raise("level", "", Data.level);
            }
            NotifyChanged();
        }

        // ---------- Storage ----------
        public int StorageCapacity => Defs.BaseStorage + Defs.StoragePerUpgrade * Data.storageLevel + Defs.HomeStoragePerLevel * (Mathf.Max(1, Data.homeLevel) - 1);
        public int StorageUpgradeCost => 150 * (Data.storageLevel + 1);

        public int StorageUsed
        {
            get
            {
                int n = 0;
                foreach (ItemStack s in Data.items) n += s.count;
                return n;
            }
        }

        public bool HasSpace(int n) => StorageUsed + n <= StorageCapacity;

        public int Count(string id)
        {
            ItemStack s = Data.items.Find(x => x.id == id);
            return s != null ? s.count : 0;
        }

        public bool AddItem(string id, int n)
        {
            if (!HasSpace(n)) return false;
            ItemStack s = Data.items.Find(x => x.id == id);
            if (s == null) Data.items.Add(new ItemStack(id, n));
            else s.count += n;
            NotifyChanged();
            return true;
        }

        public bool RemoveItem(string id, int n)
        {
            ItemStack s = Data.items.Find(x => x.id == id);
            if (s == null || s.count < n) return false;
            s.count -= n;
            if (s.count <= 0) Data.items.Remove(s);
            NotifyChanged();
            return true;
        }

        public bool TryUpgradeStorage()
        {
            if (!TrySpend(StorageUpgradeCost)) return false;
            Data.storageLevel++;
            AddXp(5);
            NotifyChanged();
            return true;
        }

        public int NextPlotCost => 20 + 15 * Data.plotsBought;
    }
}
