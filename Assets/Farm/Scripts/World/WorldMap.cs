using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>Terrain, land zones (unlocked by level), grid occupancy and spawning of every world entity.</summary>
    public class WorldMap : MonoBehaviour
    {
        public static WorldMap I { get; private set; }
        public const int N = IsoGrid.Size;
        public const int PathRow = 6;

        private static readonly Color LockedTint = new Color(0.52f, 0.62f, 0.48f, 1f);

        private readonly bool[,] water = new bool[N, N];
        private readonly bool[,] path = new bool[N, N];
        private readonly bool[,] border = new bool[N, N];
        private readonly bool[,] locked = new bool[N, N];
        private readonly int[,] zoneOf = new int[N, N];
        private readonly object[,] occ = new object[N, N];
        private readonly SpriteRenderer[,] ground = new SpriteRenderer[N, N];
        private readonly Dictionary<Vector2Int, GameObject> fog = new Dictionary<Vector2Int, GameObject>();

        public readonly List<PlotView> Plots = new List<PlotView>();
        public readonly List<PlaceView> Places = new List<PlaceView>();
        public readonly List<AnimalView> Animals = new List<AnimalView>();
        public readonly List<DebrisView> DebrisList = new List<DebrisView>();
        private readonly List<ZoneSignView> signs = new List<ZoneSignView>();

        private Transform groundRoot, objRoot;
        private float sprinklerTimer;

        public event System.Action ZonesChanged;

        private SaveData D => GameManager.I.Data;

        public void Build()
        {
            I = this;
            groundRoot = new GameObject("Ground").transform;
            groundRoot.SetParent(transform, false);
            objRoot = new GameObject("Objects").transform;
            objRoot.SetParent(transform, false);

            if (!D.initialized) SeedNewGame();
            if (!D.zones.Contains(0)) D.zones.Add(0);
            GenerateTerrain();
            foreach (PlaceData p in D.places) SpawnPlace(p);
            foreach (PlotData p in D.plots) SpawnPlot(p);
            foreach (DebrisData d in D.debris)
                if (!locked[d.gx, d.gy]) SpawnDebris(d);
            foreach (AnimalData a in D.animals) SpawnAnimal(a, null);
            SpawnFishingSpots();
            RefreshSigns();
        }

        private readonly List<FishingSpotView> fishingSpots = new List<FishingSpotView>();

        private void SpawnFishingSpots()
        {
            foreach (Vector2Int c in Defs.FishingSpots)
            {
                if (locked[c.x, c.y] || fishingSpots.Exists(f => f.Cell == c)) continue;
                FishingSpotView v = NewEntity<FishingSpotView>("FishingSpot");
                v.Init(c);
                fishingSpots.Add(v);
            }
        }

        public bool AnyFishingUnlocked => fishingSpots.Count > 0;

        public ZoneSignView ReadySign => signs.Find(x => x != null && GameManager.I.Data.level >= x.Zone.unlock);

        // ------------------------------------------------------------------ terrain
        private void GenerateTerrain()
        {
            for (int gx = 0; gx < N; gx++)
            for (int gy = 0; gy < N; gy++)
            {
                border[gx, gy] = gx < 2 || gy < 2 || gx > N - 3 || gy > N - 3;
                bool smallPond = gx >= 18 && gx <= 20 && gy >= 10 && gy <= 12 && !(gx == 20 && gy == 12);
                bool lake = gx >= 23 && gx <= 26 && gy >= 9 && gy <= 13 && !(gx == 23 && gy == 9) && !(gx == 26 && gy == 13) && !(gx == 26 && gy == 9);
                water[gx, gy] = smallPond || lake;
                path[gx, gy] = gy == PathRow && gx >= 2 && gx <= N - 3;
                LandZone z = Defs.ZoneAt(gx, gy);
                zoneOf[gx, gy] = z != null ? z.id : -1;
                locked[gx, gy] = z != null && !D.zones.Contains(z.id);
            }

            var rng = new System.Random(7);
            string[] trees = { "nature_pine_tree", "nature_oak_tree", "nature_apple_tree", "nature_bush", "nature_pine_tree", "nature_berry_bush" };
            for (int gx = 0; gx < N; gx++)
            for (int gy = 0; gy < N; gy++)
            {
                string tile;
                double r = rng.NextDouble();
                if (water[gx, gy])
                {
                    tile = r < 0.12 ? "water_lilies" : r < 0.2 ? "water_reeds" : "water_deep";
                    foreach (Vector2Int fs in Defs.FishingSpots)
                        if (fs.x == gx && fs.y == gy) tile = "water_fishing_spot";
                }
                else if (path[gx, gy]) tile = "ground_stone_straight_ne_sw";
                else if (border[gx, gy]) tile = r < 0.3 ? "ground_grass_flowers" : r < 0.45 ? "ground_grass_worn" : "ground_grass_plain";
                else tile = r < 0.07 ? "ground_grass_flowers" : r < 0.12 ? "ground_grass_worn" : "ground_grass_plain";

                var go = new GameObject($"T{gx}_{gy}");
                go.transform.SetParent(groundRoot, false);
                go.transform.position = IsoGrid.ToWorld(gx, gy);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteDB.Get(tile);
                sr.sortingOrder = IsoGrid.GroundOrder(gx, gy);
                if (locked[gx, gy]) sr.color = LockedTint;
                ground[gx, gy] = sr;

                // Forest: dense on the map border, thick "wilderness" on locked land
                double t = rng.NextDouble();
                bool wantTree = border[gx, gy] ? t < 0.6 : (locked[gx, gy] && !water[gx, gy] && !path[gx, gy] && t < 0.5);
                string kind = trees[rng.Next(trees.Length)];
                float jx = (float)(rng.NextDouble() - 0.5) * 0.4f, jy = (float)(rng.NextDouble() - 0.5) * 0.2f;
                float size = (float)rng.NextDouble();
                bool flip = rng.NextDouble() < 0.5;
                if (!wantTree) continue;

                var tg = new GameObject("Tree");
                tg.transform.SetParent(groundRoot, false);
                tg.transform.position = IsoGrid.ToWorld(gx, gy) + new Vector3(jx, jy, 0);
                bool small = kind.Contains("bush");
                tg.transform.localScale = Vector3.one * (small ? 0.8f : 1.1f + size * 0.25f);
                var tsr = tg.AddComponent<SpriteRenderer>();
                tsr.sprite = SpriteDB.Get(kind);
                tsr.flipX = flip;
                tsr.sortingOrder = IsoGrid.SortOrder(tg.transform.position.y);
                if (locked[gx, gy])
                {
                    tsr.color = new Color(0.8f, 0.86f, 0.78f, 1f);
                    fog[new Vector2Int(gx, gy)] = tg;
                }
            }
        }

        // ------------------------------------------------------------------ new game
        private void SeedNewGame()
        {
            SaveData d = D;
            d.version = SaveData.CurrentVersion;
            d.zones.Clear();
            d.zones.Add(0);
            void Place(string id, int gx, int gy) => d.places.Add(new PlaceData { uid = d.NewUid(), def = id, gx = gx, gy = gy });

            // Compact starter farm (zone 0 = cells 2..15 x 2..15)
            Place("farmhouse", 3, 3);
            Place("barn", 6, 3);
            Place("market", 9, 3);
            Place("shop", 12, 3);
            Place("orders", 14, 4);
            Place("lamp", 5, 7);
            Place("lamp", 11, 7);
            Place("bench", 2, 5);
            Place("flower_patch", 15, 5);
            Place("coop", 3, 12);
            Place("hay", 6, 13);
            Place("scarecrow", 9, 10);

            // 6 starter plots: 2 ripe wheat, 2 tilled, 2 untilled
            double now = GameManager.RealNow;
            int i = 0;
            for (int gy = 9; gy <= 10; gy++)
            for (int gx = 5; gx <= 7; gx++)
            {
                var p = new PlotData { uid = d.NewUid(), gx = gx, gy = gy };
                if (i < 2) { p.state = PlotState.Growing; p.crop = "wheat"; p.wateredAt = now - 1000; }
                else if (i < 4) p.state = PlotState.Tilled;
                else p.state = PlotState.Untilled;
                d.plots.Add(p);
                i++;
            }

            int coopUid = d.places.Find(x => x.def == "coop").uid;
            for (int k = 0; k < 2; k++)
                d.animals.Add(new AnimalData { uid = d.NewUid(), type = "chicken", houseUid = coopUid, state = AnimalState.Hungry });

            // Debris: a few in the starter area, many more on land that unlocks later
            var taken = new HashSet<Vector2Int>();
            foreach (PlaceData p in d.places)
            {
                PlaceDef def = Defs.Place(p.def);
                for (int x = -1; x <= def.size; x++)
                for (int y = -1; y <= def.size; y++) taken.Add(new Vector2Int(p.gx + x, p.gy + y));
            }
            foreach (PlotData p in d.plots) taken.Add(new Vector2Int(p.gx, p.gy));
            var rng = new System.Random(42);
            foreach (LandZone z in Defs.Zones)
            {
                int want = z.id == 0 ? 6 : Mathf.Clamp((z.x1 - z.x0 + 1) * (z.y1 - z.y0 + 1) / 9, 6, 18);
                int placed = 0, guard = 0;
                while (placed < want && guard++ < 400)
                {
                    var c = new Vector2Int(rng.Next(z.x0, z.x1 + 1), rng.Next(z.y0, z.y1 + 1));
                    if (z.id == 0 && c.x < 9 && c.y < 12) continue; // keep the starter yard clean
                    if (taken.Contains(c) || c.y == PathRow || IsWaterCell(c)) continue;
                    taken.Add(c);
                    Defs.DebrisDef dd = Defs.Debris[rng.Next(Defs.Debris.Count)];
                    d.debris.Add(new DebrisData { uid = d.NewUid(), sprite = dd.sprite, gx = c.x, gy = c.y });
                    placed++;
                }
            }

            Vector3 fp = IsoGrid.ToWorld(8, 8);
            d.farmerX = fp.x;
            d.farmerY = fp.y;
            d.coins = Defs.StartCoins;
            // a new farm always starts at 8:00 in the morning
            double len = Defs.DayLengthSec;
            d.dayOffset = (2.0 / 24.0) * len - (GameManager.I.Now % len);
            d.lastDailyDate = "";
            d.initialized = true;
        }

        private static bool IsWaterCell(Vector2Int c)
        {
            bool smallPond = c.x >= 18 && c.x <= 20 && c.y >= 10 && c.y <= 12;
            bool lake = c.x >= 23 && c.x <= 26 && c.y >= 9 && c.y <= 13;
            return smallPond || lake;
        }

        // ------------------------------------------------------------------ land zones
        public bool IsZoneUnlocked(int id) => D.zones.Contains(id);

        public bool IsFrontier(LandZone z)
        {
            if (IsZoneUnlocked(z.id)) return false;
            for (int x = z.x0; x <= z.x1; x++)
            for (int y = z.y0; y <= z.y1; y++)
            {
                if (UnlockedNeighbor(x + 1, y) || UnlockedNeighbor(x - 1, y) || UnlockedNeighbor(x, y + 1) || UnlockedNeighbor(x, y - 1))
                    return true;
            }
            return false;
        }

        private bool UnlockedNeighbor(int x, int y)
        {
            return x >= 2 && y >= 2 && x <= N - 3 && y <= N - 3 && !locked[x, y];
        }

        private void RefreshSigns()
        {
            foreach (ZoneSignView s in signs) if (s != null) Destroy(s.gameObject);
            signs.Clear();
            foreach (LandZone z in Defs.Zones)
            {
                if (!IsFrontier(z)) continue;
                // sign on the zone's border cell closest to the farm centre, preferably on the road
                Vector2Int best = new Vector2Int(z.x0, z.y0);
                float bestScore = float.MaxValue;
                for (int x = z.x0; x <= z.x1; x++)
                for (int y = z.y0; y <= z.y1; y++)
                {
                    if (water[x, y]) continue;
                    bool edge = UnlockedNeighbor(x + 1, y) || UnlockedNeighbor(x - 1, y) || UnlockedNeighbor(x, y + 1) || UnlockedNeighbor(x, y - 1);
                    if (!edge) continue;
                    float score = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8)) - (path[x, y] ? 4f : 0f);
                    if (score < bestScore) { bestScore = score; best = new Vector2Int(x, y); }
                }
                if (fog.TryGetValue(best, out GameObject tree) && tree != null) tree.SetActive(false);
                ZoneSignView v = NewEntity<ZoneSignView>("ZoneSign_" + z.id);
                v.Init(z, best);
                signs.Add(v);
            }
        }

        public void UnlockZone(LandZone z)
        {
            if (IsZoneUnlocked(z.id)) return;
            D.zones.Add(z.id);
            for (int x = z.x0; x <= z.x1; x++)
            for (int y = z.y0; y <= z.y1; y++)
            {
                locked[x, y] = false;
                ground[x, y].color = Color.white;
                var c = new Vector2Int(x, y);
                if (fog.TryGetValue(c, out GameObject tree))
                {
                    if (tree != null) Destroy(tree);
                    fog.Remove(c);
                }
            }
            foreach (DebrisData d in D.debris)
                if (zoneOf[d.gx, d.gy] == z.id && DebrisList.Find(v => v.D == d) == null) SpawnDebris(d);
            SpawnFishingSpots();
            RefreshSigns();
            ZonesChanged?.Invoke();
        }

        /// <summary>World-space rectangle covering all unlocked land (camera limits).</summary>
        public Bounds UnlockedBounds
        {
            get
            {
                bool any = false;
                var b = new Bounds();
                foreach (LandZone z in Defs.Zones)
                {
                    if (!IsZoneUnlocked(z.id)) continue;
                    Vector3[] corners =
                    {
                        IsoGrid.ToWorld(z.x0, z.y0), IsoGrid.ToWorld(z.x1, z.y1),
                        IsoGrid.ToWorld(z.x0, z.y1), IsoGrid.ToWorld(z.x1, z.y0),
                    };
                    foreach (Vector3 c in corners)
                    {
                        if (!any) { b = new Bounds(c, Vector3.zero); any = true; }
                        else b.Encapsulate(c);
                    }
                }
                b.Expand(new Vector3(IsoGrid.HalfW * 3f, IsoGrid.HalfH * 3f, 0));
                return b;
            }
        }

        // ------------------------------------------------------------------ spawning
        private T NewEntity<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(objRoot, false);
            return go.AddComponent<T>();
        }

        private PlaceView SpawnPlace(PlaceData p)
        {
            PlaceDef def = Defs.Place(p.def);
            if (def == null) return null;
            PlaceView v = NewEntity<PlaceView>(def.id);
            v.Init(p);
            Occupy(v, p.gx, p.gy, def.size);
            Places.Add(v);
            return v;
        }

        private PlotView SpawnPlot(PlotData p)
        {
            PlotView v = NewEntity<PlotView>("Plot");
            v.Init(p);
            Occupy(v, p.gx, p.gy, 1);
            Plots.Add(v);
            return v;
        }

        private void SpawnDebris(DebrisData d)
        {
            if (Defs.DebrisBySprite(d.sprite) == null) return;
            DebrisView v = NewEntity<DebrisView>("Debris");
            v.Init(d);
            Occupy(v, d.gx, d.gy, 1);
            DebrisList.Add(v);
        }

        private AnimalView SpawnAnimal(AnimalData a, Vector3? at)
        {
            AnimalDef def = Defs.Animal(a.type);
            if (def == null) return null;
            Vector3 pos;
            if (at.HasValue) pos = at.Value;
            else
            {
                PlaceView house = FindPlace(a.houseUid);
                Vector3 c = house != null ? house.transform.position : IsoGrid.ToWorld(8, 12);
                pos = c + new Vector3(Random.Range(-1f, 1f), -IsoGrid.HalfH * 2.4f + Random.Range(-0.3f, 0.3f), 0);
                if (!Walkable(IsoGrid.ToCell(pos))) pos = IsoGrid.CellCenter(FindFreeCell(IsoGrid.ToCell(c), 1, true));
            }
            AnimalView v = NewEntity<AnimalView>(def.id);
            v.Init(a, pos);
            Animals.Add(v);
            return v;
        }

        // ------------------------------------------------------------------ public API
        public PlotView AddPlot(int gx, int gy)
        {
            var p = new PlotData { uid = D.NewUid(), gx = gx, gy = gy, state = PlotState.Untilled };
            D.plots.Add(p);
            return SpawnPlot(p);
        }

        public PlaceView AddPlace(string defId, int gx, int gy)
        {
            var p = new PlaceData { uid = D.NewUid(), def = defId, gx = gx, gy = gy };
            D.places.Add(p);
            return SpawnPlace(p);
        }

        public AnimalView AddAnimal(string type, PlaceView house)
        {
            var a = new AnimalData { uid = D.NewUid(), type = type, houseUid = house.D.uid, state = AnimalState.Hungry };
            D.animals.Add(a);
            return SpawnAnimal(a, null);
        }

        public void RemoveDebris(DebrisView v)
        {
            Vacate(v.D.gx, v.D.gy, 1);
            D.debris.Remove(v.D);
            DebrisList.Remove(v);
            Destroy(v.gameObject);
        }

        public void MoveEntity(Interactable e, int gx, int gy)
        {
            if (e is PlaceView pv)
            {
                Vacate(pv.D.gx, pv.D.gy, pv.Def.size);
                pv.SetCell(gx, gy);
                Occupy(pv, gx, gy, pv.Def.size);
            }
            else if (e is PlotView plot)
            {
                Vacate(plot.D.gx, plot.D.gy, 1);
                plot.SetCell(gx, gy);
                Occupy(plot, gx, gy, 1);
            }
        }

        public PlaceView FindPlace(int uid) => Places.Find(p => p.D.uid == uid);

        public int AnimalsInHouse(int houseUid)
        {
            int n = 0;
            foreach (AnimalData a in D.animals) if (a.houseUid == houseUid) n++;
            return n;
        }

        public PlaceView FindHouseWithSpace(string houseDefId)
        {
            foreach (PlaceView p in Places)
                if (p.Def.id == houseDefId && AnimalsInHouse(p.D.uid) < HouseCapacity(p)) return p;
            return null;
        }

        public int HouseCapacity(PlaceView house) =>
            house.Def.capacity + Defs.HouseSlotsPerLevel * D.State(house.D.uid).level;

        public bool AnyDebrisUnlocked => DebrisList.Count > 0;

        public int CountPlaces(string defId) => Places.FindAll(p => p.Def.id == defId).Count;

        // ------------------------------------------------------------------ grid queries
        public static bool InBuildArea(Vector2Int c) => c.x >= 2 && c.y >= 2 && c.x <= N - 3 && c.y <= N - 3;

        public bool IsLocked(Vector2Int c) => IsoGrid.InBounds(c) && locked[c.x, c.y];
        public bool IsWater(Vector2Int c) => IsoGrid.InBounds(c) && water[c.x, c.y];

        public object OccupantAt(Vector2Int c) => IsoGrid.InBounds(c) ? occ[c.x, c.y] : null;
        public PlotView PlotAt(Vector2Int c) => OccupantAt(c) as PlotView;

        public bool Walkable(Vector2Int c)
        {
            if (!IsoGrid.InBounds(c) || water[c.x, c.y] || border[c.x, c.y] || locked[c.x, c.y]) return false;
            object o = occ[c.x, c.y];
            return o == null || o is PlotView;
        }

        public bool CanPlace(int size, int gx, int gy, object ignore)
        {
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                var c = new Vector2Int(gx + x, gy + y);
                if (!InBuildArea(c) || water[c.x, c.y] || path[c.x, c.y] || locked[c.x, c.y]) return false;
                object o = occ[c.x, c.y];
                if (o != null && o != ignore) return false;
            }
            return true;
        }

        public Vector2Int FindFreeCell(Vector2Int near, int size, bool walkableOnly = false)
        {
            for (int r = 0; r < N; r++)
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var c = new Vector2Int(near.x + dx, near.y + dy);
                if (walkableOnly ? Walkable(c) && occ[c.x, c.y] == null : CanPlace(size, c.x, c.y, null)) return c;
            }
            return near;
        }

        private void Occupy(object o, int gx, int gy, int size)
        {
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (IsoGrid.InBounds(new Vector2Int(gx + x, gy + y))) occ[gx + x, gy + y] = o;
        }

        private void Vacate(int gx, int gy, int size)
        {
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (IsoGrid.InBounds(new Vector2Int(gx + x, gy + y))) occ[gx + x, gy + y] = null;
        }

        // ------------------------------------------------------------------ sprinklers
        private void Update()
        {
            sprinklerTimer -= Time.deltaTime;
            if (sprinklerTimer > 0f) return;
            sprinklerTimer = 1f;
            foreach (PlaceView p in Places)
            {
                if (p.Def.kind != PlaceKind.Sprinkler) continue;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    PlotView plot = PlotAt(new Vector2Int(p.D.gx + dx, p.D.gy + dy));
                    if (plot != null && plot.D.state == PlotState.Planted && !plot.Pending) plot.Water();
                }
            }
        }

        public Bounds WorldBounds
        {
            get
            {
                Vector3 top = IsoGrid.ToWorld(0, 0), bottom = IsoGrid.ToWorld(N - 1, N - 1);
                Vector3 left = IsoGrid.ToWorld(0, N - 1), right = IsoGrid.ToWorld(N - 1, 0);
                var b = new Bounds(top, Vector3.zero);
                b.Encapsulate(bottom);
                b.Encapsulate(left);
                b.Encapsulate(right);
                return b;
            }
        }
    }

    /// <summary>Wooden sign at the edge of locked land: tap to see the requirement and expand the farm.</summary>
    public class ZoneSignView : Interactable
    {
        public LandZone Zone;
        private WorldBubble bubble;
        private float refresh;

        public void Init(LandZone z, Vector2Int cell)
        {
            Zone = z;
            transform.position = IsoGrid.CellCenter(cell);
            Sr = MakeSprite("decor_wooden_sign", 0.75f);
            Sr.sortingOrder = IsoGrid.SortOrder(transform.position.y);
            bubble = WorldBubble.Create(transform, 1.55f);
        }

        private void Update()
        {
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 0.5f;
            bool ready = GameManager.I.Data.level >= Zone.unlock;
            if (ready) bubble.Show(GameManager.I.CanAfford(Zone.cost) ? "hud_add" : "hud_coin", 0.3f);
            else bubble.Hide();
        }

        public override int HitOrder => base.HitOrder + 300;

        public override bool HitTest(Vector2 p)
        {
            return base.HitTest(p) || Vector2.Distance(p, transform.position + Vector3.up * 0.9f) < 0.9f;
        }

        public override void OnTap()
        {
            Punch();
            UIManager.I.OpenZone(Zone);
        }
    }
}
