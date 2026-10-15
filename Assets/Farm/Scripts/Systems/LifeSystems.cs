using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>B2.1 – villagers arrive on the road with small requests; serving them builds friendship.</summary>
    public class VisitorSystem : MonoBehaviour
    {
        public static VisitorSystem I { get; private set; }
        public readonly List<VisitorView> Views = new List<VisitorView>();
        private float tick;

        private SaveData D => GameManager.I.Data;

        public void Init()
        {
            I = this;
            foreach (VisitorData v in D.visitors) Spawn(v, false);
            if (D.nextVisitorAt <= 0) D.nextVisitorAt = GameManager.I.Now + 40;
        }

        private void Update()
        {
            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick = 1f;
            Views.RemoveAll(v => v == null);
            GameManager gm = GameManager.I;
            if (D.visitors.Count >= Defs.MaxVisitors(D.level) || gm.Now < D.nextVisitorAt) return;
            if (DayNight.I != null && DayNight.I.NightFactor > 0.6f) return; // villagers sleep at night
            if (PlacementController.I != null && PlacementController.I.Active) return;
            Arrive();
        }

        private void Arrive()
        {
            var freeSpots = new List<int>();
            for (int i = 0; i < Defs.VisitorSpots.Length; i++)
                if (!D.visitors.Exists(v => v.spot == i) && !Views.Exists(v => v != null && v.D.spot == i)) freeSpots.Add(i);
            var npcs = Defs.Visitors.FindAll(n => !D.visitors.Exists(v => v.npc == n.id) && !Views.Exists(v => v != null && v.D.npc == n.id));
            if (freeSpots.Count == 0 || npcs.Count == 0) { D.nextVisitorAt = GameManager.I.Now + 30; return; }
            VisitorDef def = npcs[Random.Range(0, npcs.Count)];
            VisitorData d = Generate(def);
            if (d == null) { D.nextVisitorAt = GameManager.I.Now + 60; return; }
            d.spot = freeSpots[Random.Range(0, freeSpots.Count)];
            D.visitors.Add(d);
            Spawn(d, true);
            // a prettier farm attracts visitors more often (-5% waiting per beauty tier)
            float gap = Random.Range(Defs.VisitorMinGap, Defs.VisitorMaxGap) * (1f - 0.05f * Defs.BeautyTier(Defs.Beauty()));
            D.nextVisitorAt = GameManager.I.Now + gap;
            if (D.Stat("visitor_hint") == 0)
            {
                D.AddStat("visitor_hint", 1);
                UIManager.I.Toast(L.F("{0} ghé thăm nông trại! Chạm vào khách để xem họ cần gì.", def.name));
            }
        }

        private void Spawn(VisitorData d, bool arrive)
        {
            var go = new GameObject("Visitor_" + d.npc);
            go.transform.SetParent(transform, false);
            VisitorView v = go.AddComponent<VisitorView>();
            v.Init(d, arrive);
            Views.Add(v);
        }

        private static string Kind(string id)
        {
            if (Defs.Crop(id) != null) return "crop";
            if (Defs.Products.Exists(p => p.id == id)) return "animal";
            if (Defs.Recipe(id) != null) return "processed";
            return "other";
        }

        public VisitorData Generate(VisitorDef def)
        {
            int level = D.level;
            List<ItemDef> pool = ItemPool.Available(true);
            if (pool.Count == 0) return null;
            var d = new VisitorData { npc = def.id };
            int lines = Random.Range(1, Mathf.Clamp(1 + level / 4, 1, 3) + 1);
            float value = 0, xp = 0;
            for (int i = 0; i < lines && pool.Count > 0; i++)
            {
                // the first item follows the visitor's taste when possible
                List<ItemDef> pick = pool;
                if (i == 0 && def.likes != "any")
                {
                    List<ItemDef> liked = pool.FindAll(it => Kind(it.id) == def.likes);
                    if (liked.Count > 0) pick = liked;
                }
                ItemDef item = pick[Random.Range(0, pick.Count)];
                pool.Remove(item);
                bool raw = Defs.Crop(item.id) != null;
                int qty = raw ? Random.Range(2, Mathf.Min(6, 3 + level / 3) + 1) : Random.Range(1, Mathf.Min(3, 1 + level / 6) + 1);
                d.req.Add(new ItemStack(item.id, qty));
                value += item.sell * qty;
                xp += item.xp * qty;
            }
            float mult = 1.8f * (1f + FriendLevel(def.id) * Defs.FriendBonusPerLevel);
            d.coins = Mathf.CeilToInt(value * mult) + 10;
            d.xp = Mathf.CeilToInt(xp * 1.2f) + 3;
            d.gems = Random.value < Defs.VisitorGemChance ? 1 : 0;
            return d;
        }

        public int Friendship(string npc) => D.Stat("friend_" + npc);
        public int FriendLevel(string npc) => Mathf.Min(Defs.FriendMaxLevel, Friendship(npc) / Defs.FriendPerLevel);

        public bool CanServe(VisitorData d)
        {
            foreach (ItemStack r in d.req)
                if (GameManager.I.Count(r.id) < r.count) return false;
            return true;
        }

        public bool Serve(VisitorView v)
        {
            VisitorData d = v.D;
            if (!v.Waiting || !CanServe(d)) return false;
            GameManager gm = GameManager.I;
            foreach (ItemStack r in d.req) gm.RemoveItem(r.id, r.count);
            int before = FriendLevel(d.npc);
            D.AddStat("friend_" + d.npc, 1);
            int coins = Defs.WithBonus(d.coins);
            gm.AddCoins(coins);
            gm.AddXp(Defs.WithBonus(d.xp));
            gm.AddGems(d.gems);
            D.visitors.Remove(d);
            Vector3 p = v.transform.position + Vector3.up * 1.6f;
            UIManager.I.FloatText(p, "+" + coins, "hud_coin");
            // B3.1: villagers sometimes bring a building material as a gift
            if (Random.value < Defs.VisitorMaterialChance)
            {
                string mat = Defs.RandomMaterial();
                if (gm.AddItem(mat, 1)) UIManager.I.FloatText(p + Vector3.up * 1.0f, "+1", Defs.Item(mat).icon);
            }
            if (d.gems > 0) UIManager.I.FloatText(p + Vector3.up * 0.5f, "+" + d.gems, "hud_gem");
            Fx.Sparkle(v.transform.position, new Color(1f, 0.6f, 0.75f), 12);
            Haptics.Success();
            v.Leave(true);
            GameEvents.Raise("visitor", d.npc);
            int after = FriendLevel(d.npc);
            if (after > before) UIManager.I.Toast(L.F("Thân thiết với {0} lên cấp {1}! Thưởng của {2} +{3}%.", v.Def.name, after, v.Def.name, after * 5));
            return true;
        }

        public void Decline(VisitorView v)
        {
            D.visitors.Remove(v.D);
            v.Leave(false);
            D.nextVisitorAt = System.Math.Max(D.nextVisitorAt, GameManager.I.Now + 60);
        }
    }

    /// <summary>B2.3 – crows land on unprotected growing plots and pause them; tap (or the dog) chases them off.</summary>
    public class CrowSystem : MonoBehaviour
    {
        public static CrowSystem I { get; private set; }
        public readonly List<CrowView> Crows = new List<CrowView>();
        private float nextCrow;
        private float tick;

        public void Init()
        {
            I = this;
            nextCrow = Time.time + Random.Range(Defs.CrowMinGap * 0.5f, Defs.CrowMaxGap * 0.6f);
        }

        public static bool Protected(Vector2Int cell)
        {
            foreach (PlaceView p in WorldMap.I.Places)
            {
                if (p.Def.id != "scarecrow") continue;
                if (Mathf.Abs(p.D.gx - cell.x) <= Defs.ScarecrowRadius && Mathf.Abs(p.D.gy - cell.y) <= Defs.ScarecrowRadius) return true;
            }
            return false;
        }

        private void Update()
        {
            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick = 1f;
            Crows.RemoveAll(c => c == null);
            GameManager gm = GameManager.I;
            if (gm.Data.level < Defs.CrowUnlock || Time.time < nextCrow) return;
            int active = Crows.FindAll(c => c.Active).Count;
            if (active >= Defs.CrowMax) return;
            DayNight dn = DayNight.I;
            if (dn != null && (dn.NightFactor > 0.5f || dn.Raining)) { nextCrow = Time.time + 20f; return; }

            var candidates = new List<PlotView>();
            foreach (PlotView p in WorldMap.I.Plots)
            {
                if (p.D.state != PlotState.Growing || p.IsReady || p.SecondsLeft < 25 || p.Pending) continue;
                if (Protected(p.Cell) || Crows.Exists(c => c != null && c.Plot == p)) continue;
                candidates.Add(p);
            }
            nextCrow = Time.time + Random.Range(Defs.CrowMinGap, Defs.CrowMaxGap);
            if (candidates.Count == 0) return;
            PlotView target = candidates[Random.Range(0, candidates.Count)];
            var go = new GameObject("Crow");
            go.transform.SetParent(transform, false);
            CrowView crow = go.AddComponent<CrowView>();
            crow.Init(target);
            Crows.Add(crow);

            SaveData d = gm.Data;
            if (d.Stat("crow_hint") == 0)
            {
                d.AddStat("crow_hint", 1);
                UIManager.I.Toast(L.T("Quạ đang phá ruộng, cây ngừng lớn! Chạm vào quạ để đuổi. Đặt Bù nhìn để bảo vệ ruộng."));
            }
        }
    }

    /// <summary>B2.2 – owned pets (bought in the shop) follow the farmer around.</summary>
    public class PetSystem : MonoBehaviour
    {
        public static PetSystem I { get; private set; }
        public readonly List<PetView> Views = new List<PetView>();

        public void Init()
        {
            I = this;
            foreach (string id in GameManager.I.Data.pets) Spawn(id);
        }

        public static bool Has(string id) => GameManager.I != null && GameManager.I.Data.pets.Contains(id);

        public bool Buy(PetDef def)
        {
            GameManager gm = GameManager.I;
            if (Has(def.id) || !gm.TrySpend(def.cost)) return false;
            gm.Data.pets.Add(def.id);
            gm.AddXp(5);
            PetView v = Spawn(def.id);
            if (v != null) Fx.Sparkle(v.transform.position, new Color(1f, 0.6f, 0.75f), 14);
            GameEvents.Raise("buy_pet", def.id);
            return true;
        }

        /// <summary>Add a pet that was given (not bought), e.g. from the starter pack.</summary>
        public void SpawnFree(string id)
        {
            if (Views.Exists(v => v != null && v.Def.id == id)) return;
            PetView v2 = Spawn(id);
            if (v2 != null) Fx.Sparkle(v2.transform.position, new Color(1f, 0.6f, 0.75f), 14);
        }

        private PetView Spawn(string id)
        {
            PetDef def = Defs.Pet(id);
            if (def == null) return null;
            var go = new GameObject("Pet_" + id);
            go.transform.SetParent(transform, false);
            PetView v = go.AddComponent<PetView>();
            // pets live around the farmhouse and roam the farm from there
            PlaceView home = WorldMap.I.Places.Find(p => p.Def.id == "farmhouse");
            Vector2Int near = home != null ? new Vector2Int(home.D.gx + home.Def.size, home.D.gy + (id == "dog" ? 0 : 2)) : new Vector2Int(6, 7);
            v.Init(def, IsoGrid.CellCenter(WorldMap.I.FindFreeCell(near, 1, true)));
            Views.Add(v);
            return v;
        }
    }
}
