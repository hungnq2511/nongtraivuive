using System;
using System.Collections.Generic;

namespace Farm
{
    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;
        public ItemStack() { }
        public ItemStack(string id, int count) { this.id = id; this.count = count; }
    }

    public enum PlotState { Untilled = 0, Tilled = 1, Planted = 2, Growing = 3 }

    [Serializable]
    public class PlotData
    {
        public int uid, gx, gy;
        public PlotState state;
        public string crop;
        public double wateredAt;
    }

    public enum AnimalState { Hungry = 0, Producing = 1 }

    [Serializable]
    public class AnimalData
    {
        public int uid, houseUid;
        public string type;
        public AnimalState state;
        public double fedAt;
    }

    [Serializable]
    public class PlaceData
    {
        public int uid, gx, gy;
        public string def;
    }

    [Serializable]
    public class DebrisData
    {
        public int uid, gx, gy;
        public string sprite;
    }

    [Serializable]
    public class OrderData
    {
        public List<ItemStack> req = new List<ItemStack>();
        public int coins, xp;
        public double availableAt;
    }

    [Serializable]
    public class JobData
    {
        public string recipe;
        public double end;
    }

    [Serializable]
    public class MachineData
    {
        public int uid;
        public int slots = Defs.MachineBaseSlots;
        public List<JobData> jobs = new List<JobData>();
        public List<string> done = new List<string>();
    }

    /// <summary>Generic per-building state: tree harvests/ready time, house level, etc.</summary>
    [Serializable]
    public class PlaceState
    {
        public int uid, level, count;
        public double t;
    }

    [Serializable]
    public class CrateData
    {
        public string item;
        public int qty, coins;
        public bool filled;
    }

    [Serializable]
    public class DeliveryData
    {
        public List<CrateData> crates = new List<CrateData>();
        public int bonusCoins, xp;
        public double readyAt;
    }

    /// <summary>One stall slot at the farmers' market. item == null → empty.</summary>
    [Serializable]
    public class MarketSlotData
    {
        public string item;
        public int qty, price;
        public double soldAt;
    }

    [Serializable]
    public class FishSpotData
    {
        public int x, y, count = Defs.FishPerSpot;
        public double regenAt;
    }

    [Serializable]
    public class TaskData
    {
        public string stat, text;
        public int target, progress, coins, xp;
        public bool claimed;
    }

    [Serializable]
    public class StatEntry
    {
        public string key;
        public int value;
    }

    /// <summary>A visitor waiting on the road with a request (B2.1).</summary>
    [Serializable]
    public class VisitorData
    {
        public string npc;
        public int spot, coins, xp, gems;
        public List<ItemStack> req = new List<ItemStack>();
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;
        public int version = CurrentVersion;
        public bool initialized;
        public int nextUid = 1;
        public int coins = Defs.StartCoins;
        public int gems = Defs.StartGems;
        public bool haptics = true;
        public bool notifications = true;
        public bool notifAsked;
        public int level = 1;
        public int xp;
        public int storageLevel;
        public int plotsBought;
        public double timeOffset;
        public double dayOffset;
        public double lastSave;
        public string lastDailyDate = "";
        public int dailyStreak;
        public int questIndex;
        public int questProgress;
        public float farmerX, farmerY;
        public float zoom = 6f;

        public List<ItemStack> items = new List<ItemStack>();
        public List<PlotData> plots = new List<PlotData>();
        public List<AnimalData> animals = new List<AnimalData>();
        public List<PlaceData> places = new List<PlaceData>();
        public List<DebrisData> debris = new List<DebrisData>();
        public List<OrderData> orders = new List<OrderData>();
        public List<int> zones = new List<int> { 0 };
        public List<MachineData> machines = new List<MachineData>();
        public List<PlaceState> placeStates = new List<PlaceState>();
        public DeliveryData delivery = new DeliveryData();
        public List<FishSpotData> fishSpots = new List<FishSpotData>();
        public List<TaskData> tasks = new List<TaskData>();
        public string taskDate = "";
        public bool taskBonusClaimed;
        public List<StatEntry> stats = new List<StatEntry>();
        public List<StatEntry> achClaimed = new List<StatEntry>();
        public List<MarketSlotData> market = new List<MarketSlotData>();
        public int marketSlots = Defs.MarketBaseSlots;
        public long marketHotDay = -1;
        public List<string> marketHot = new List<string>();
        public List<VisitorData> visitors = new List<VisitorData>();
        public double nextVisitorAt;
        public List<string> pets = new List<string>();
        public int homeLevel = 1;
        /// <summary>Decorations put away in storage (def id → count); placing them again is free.</summary>
        public List<ItemStack> decorStore = new List<ItemStack>();
        /// <summary>One-time (non-consumable) store products already granted.</summary>
        public List<string> iapOwned = new List<string>();
        /// <summary>Transaction IDs already granted (stops double rewards if the store re-delivers).</summary>
        public List<string> iapOrders = new List<string>();
        public string adDay = "";
        public List<StatEntry> adCounts = new List<StatEntry>();

        public int Stat(string key)
        {
            StatEntry e = stats.Find(x => x.key == key);
            return e != null ? e.value : 0;
        }

        public void AddStat(string key, int n)
        {
            StatEntry e = stats.Find(x => x.key == key);
            if (e == null) stats.Add(new StatEntry { key = key, value = n });
            else e.value += n;
        }

        public PlaceState State(int uid)
        {
            PlaceState s = placeStates.Find(x => x.uid == uid);
            if (s == null)
            {
                s = new PlaceState { uid = uid };
                placeStates.Add(s);
            }
            return s;
        }

        public int NewUid() => nextUid++;
    }
}
