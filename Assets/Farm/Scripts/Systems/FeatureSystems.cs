using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>Which goods the player can currently produce (used by orders and the truck).</summary>
    public static class ItemPool
    {
        public static List<ItemDef> Available(bool includeProcessed)
        {
            GameManager gm = GameManager.I;
            SaveData d = gm.Data;
            var pool = new List<ItemDef>();
            foreach (CropDef c in Defs.Crops)
                if (c.unlock <= d.level) pool.Add(Defs.Item(c.id));
            foreach (AnimalDef a in Defs.Animals)
                if (d.animals.Exists(x => x.type == a.id)) pool.Add(Defs.Item(a.product));
            foreach (TreeDef t in Defs.Trees)
                if (d.places.Exists(p => p.def == t.place)) pool.Add(Defs.Item(t.fruit));
            if (includeProcessed)
            {
                foreach (RecipeDef r in Defs.Recipes)
                    if (r.unlock <= d.level && d.places.Exists(p => p.def == r.machine) && InputsObtainable(r, pool))
                        pool.Add(Defs.Item(r.output));
                if (d.Stat("fish") > 0) pool.Add(Defs.Item("fish_crucian"));
            }
            return pool;
        }

        private static bool InputsObtainable(RecipeDef r, List<ItemDef> pool)
        {
            foreach (ItemStack s in r.inputs)
                if (!pool.Exists(i => i.id == s.id)) return false;
            return true;
        }

        public static bool IsRaw(string id) => Defs.Crop(id) != null;
    }

    // ===================================================================== production
    public static class Production
    {
        private static SaveData D => GameManager.I.Data;

        public static MachineData Get(int uid)
        {
            MachineData m = D.machines.Find(x => x.uid == uid);
            if (m == null)
            {
                m = new MachineData { uid = uid };
                D.machines.Add(m);
            }
            return m;
        }

        /// <summary>Move finished jobs (time-based, works offline) to the done tray.</summary>
        public static void Tick(MachineData m)
        {
            double now = GameManager.I.Now;
            while (m.jobs.Count > 0 && m.jobs[0].end <= now)
            {
                m.done.Add(m.jobs[0].recipe);
                m.jobs.RemoveAt(0);
            }
        }

        public static bool HasInputs(RecipeDef r)
        {
            foreach (ItemStack s in r.inputs)
                if (GameManager.I.Count(s.id) < s.count) return false;
            return true;
        }

        public static string Start(MachineData m, RecipeDef r)
        {
            Tick(m);
            if (D.level < r.unlock) return L.F("Mở khoá ở cấp {0}.", r.unlock);
            if (m.jobs.Count >= m.slots) return L.T("Hàng đợi đã đầy. Nâng cấp thêm chỗ hoặc chờ máy chạy xong.");
            if (!HasInputs(r)) return L.T("Không đủ nguyên liệu.");
            foreach (ItemStack s in r.inputs) GameManager.I.RemoveItem(s.id, s.count);
            double now = GameManager.I.Now;
            double start = m.jobs.Count > 0 ? System.Math.Max(now, m.jobs[m.jobs.Count - 1].end) : now;
            m.jobs.Add(new JobData { recipe = r.output, end = start + r.sec });
            return null;
        }

        /// <summary>Collect finished goods into storage. Returns how many were collected.</summary>
        public static int Collect(MachineData m, Vector3 worldPos)
        {
            Tick(m);
            int n = 0;
            while (m.done.Count > 0)
            {
                ItemDef item = Defs.Item(m.done[0]);
                if (!GameManager.I.AddItem(item.id, 1))
                {
                    UIManager.I.Toast(L.T("Kho đầy! Mang hàng ra Chợ bán hoặc nâng cấp kho."));
                    break;
                }
                GameManager.I.AddXp(item.xp);
                UIManager.I.FloatText(worldPos + Vector3.up * (1f + n * 0.3f), "+1", item.icon);
                if (n == 0) { Fx.Sparkle(worldPos + Vector3.up * 0.6f, new Color(1f, 0.95f, 0.75f), 12); Haptics.Light(); }
                m.done.RemoveAt(0);
                GameEvents.Raise("produce", item.id);
                n++;
            }
            return n;
        }

        /// <summary>Gem speed-up: the running job finishes now and the rest of the queue moves up.</summary>
        public static void FinishCurrent(MachineData m)
        {
            double left = CurrentJobLeft(m);
            if (left <= 0) { Tick(m); return; }
            foreach (JobData j in m.jobs) j.end -= left;
            Tick(m);
        }

        public static double CurrentJobLeft(MachineData m)
        {
            if (m.jobs.Count == 0) return 0;
            return m.jobs[0].end - GameManager.I.Now;
        }
    }

    // ===================================================================== delivery truck
    public static class Delivery
    {
        private static SaveData D => GameManager.I.Data;

        public static bool Waiting => D.delivery != null && D.delivery.crates.Count == 0 && GameManager.I.Now < D.delivery.readyAt;

        public static DeliveryData Current
        {
            get
            {
                if (D.delivery == null) D.delivery = new DeliveryData();
                if (D.delivery.crates.Count == 0 && GameManager.I.Now >= D.delivery.readyAt) Generate();
                return D.delivery;
            }
        }

        public static void Generate()
        {
            DeliveryData dd = D.delivery;
            dd.crates.Clear();
            List<ItemDef> pool = ItemPool.Available(true);
            int total = 0;
            for (int i = 0; i < Defs.DeliveryCrates && pool.Count > 0; i++)
            {
                ItemDef item = pool[Random.Range(0, pool.Count)];
                bool raw = ItemPool.IsRaw(item.id);
                int qty = raw ? Random.Range(3, 7) : Random.Range(1, 4);
                int coins = Mathf.CeilToInt(item.sell * qty * 1.25f);
                dd.crates.Add(new CrateData { item = item.id, qty = qty, coins = coins });
                total += coins;
            }
            dd.bonusCoins = Mathf.CeilToInt(total * 0.35f) + 20;
            dd.xp = 10 + D.level * 3;
        }

        public static string Fill(int index)
        {
            CrateData c = Current.crates[index];
            if (c.filled) return null;
            if (GameManager.I.Count(c.item) < c.qty) return L.T("Chưa đủ hàng cho thùng này.");
            GameManager.I.RemoveItem(c.item, c.qty);
            c.filled = true;
            GameManager.I.AddCoins(Defs.WithBonus(c.coins));
            GameManager.I.AddXp(2);
            return null;
        }

        public static bool AllFilled => Current.crates.Count > 0 && Current.crates.TrueForAll(c => c.filled);

        public static void Ship()
        {
            DeliveryData dd = Current;
            GameManager.I.AddCoins(dd.bonusCoins);
            GameManager.I.AddGems(Defs.TruckGems);
            GameManager.I.AddXp(dd.xp);
            dd.crates.Clear();
            dd.readyAt = GameManager.I.Now + Defs.DeliveryCooldownSec;
            GameEvents.Raise("truck");
        }

        public static void Reroll()
        {
            if (Current.crates.Exists(c => c.filled)) return;
            Generate();
        }
    }

    // ===================================================================== fishing
    public static class Fishing
    {
        private static SaveData D => GameManager.I.Data;

        public static FishSpotData Spot(Vector2Int c)
        {
            FishSpotData s = D.fishSpots.Find(x => x.x == c.x && x.y == c.y);
            if (s == null)
            {
                s = new FishSpotData { x = c.x, y = c.y, count = Defs.FishPerSpot, regenAt = GameManager.I.Now + Defs.FishRegenSec };
                D.fishSpots.Add(s);
            }
            Regen(s);
            return s;
        }

        public static void Regen(FishSpotData s)
        {
            double now = GameManager.I.Now;
            while (s.count < Defs.FishPerSpot && now >= s.regenAt)
            {
                s.count++;
                s.regenAt += Defs.FishRegenSec;
            }
            if (s.count >= Defs.FishPerSpot && s.regenAt < now) s.regenAt = now + Defs.FishRegenSec;
        }

        public static FishDef Roll()
        {
            float r = Random.value, acc = 0f;
            foreach (FishDef f in Defs.Fish)
            {
                acc += f.chance;
                if (r <= acc) return f;
            }
            return Defs.Fish[0];
        }
    }

    // ===================================================================== fruit trees
    public static class Orchard
    {
        public static PlaceState State(PlaceView v)
        {
            TreeDef t = Defs.Tree(v.Def.id);
            PlaceState s = GameManager.I.Data.State(v.D.uid);
            if (s.level == 0)
            {
                // first time: fresh tree
                s.level = 1;
                s.count = t.harvests;
                s.t = GameManager.I.Now + t.cycleSec;
            }
            return s;
        }

        public static bool Endless(PlaceView v) => Defs.Tree(v.Def.id).harvests < 0;

        public static bool IsReady(PlaceView v)
        {
            PlaceState s = State(v);
            return (Endless(v) || s.count > 0) && GameManager.I.Now >= s.t;
        }

        public static bool IsDead(PlaceView v) => !Endless(v) && State(v).count <= 0;

        /// <summary>Bees pollinate fruit trees near a beehive: +1 fruit per harvest.</summary>
        public static bool Pollinated(PlaceView v)
        {
            if (v.Def.id == "beehive") return false;
            foreach (PlaceView p in WorldMap.I.Places)
            {
                if (p.Def.id != "beehive") continue;
                if (Mathf.Abs(p.D.gx - v.D.gx) <= Defs.BeehiveRadius && Mathf.Abs(p.D.gy - v.D.gy) <= Defs.BeehiveRadius) return true;
            }
            return false;
        }

        public static int Yield(PlaceView v) => Defs.Tree(v.Def.id).yield + (Pollinated(v) ? 1 : 0);
    }
}
