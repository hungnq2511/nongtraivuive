using UnityEngine;

namespace Farm
{
    /// <summary>Building, animal house, machine, fruit tree, truck, sprinkler or decoration placed on the grid.</summary>
    public class PlaceView : Interactable
    {
        public PlaceData D;
        [System.NonSerialized] public PlaceDef Def;
        private WorldBubble bubble;
        private SpriteRenderer glow;
        private float refresh;
        private string shownSprite;

        public override bool Movable => true;

        public void Init(PlaceData d)
        {
            D = d;
            Def = Defs.Place(d.def);
            Sr = MakeSprite(Def.sprite, Def.scale);
            shownSprite = Def.sprite;
            if (Def.kind == PlaceKind.Machine || Def.kind == PlaceKind.Tree || Def.kind == PlaceKind.Delivery || Def.action == "market")
                bubble = WorldBubble.Create(transform, Def.size > 1 ? 2.9f : 1.0f + 1.4f * Def.scale);
            if (Def.id == "lamp")
            {
                var g = new GameObject("Glow");
                g.transform.SetParent(transform, false);
                g.transform.localPosition = new Vector3(0.12f, 1.25f, 0);
                g.transform.localScale = Vector3.one * 2.6f;
                glow = g.AddComponent<SpriteRenderer>();
                glow.sprite = ProcSprites.SoftCircle;
                glow.color = new Color(1f, 0.85f, 0.45f, 0f);
            }
            if (Def.kind == PlaceKind.Tree) Orchard.State(this);
            SetCell(d.gx, d.gy);
            RefreshHomeLook();
        }

        /// <summary>Farmhouse (B3.1): use the level art when it exists, otherwise grow the house a little per level.</summary>
        public void RefreshHomeLook()
        {
            if (Def.id != "farmhouse" || GameManager.I == null) return;
            int lvl = Mathf.Max(1, GameManager.I.Data.homeLevel);
            string sp = Defs.HomeSprite(lvl);
            shownSprite = sp;
            Sr.sprite = SpriteDB.Get(sp);
            float k = sp == Def.sprite ? 1f + 0.04f * (lvl - 1) : 1f;
            BaseScale = Vector3.one * Def.scale * k;
            Sr.transform.localScale = BaseScale;
        }

        public bool IsDecor => Def.kind == PlaceKind.Decor;

        public static Vector3 CenterOf(PlaceDef def, int gx, int gy)
        {
            float o = (def.size - 1) * 0.5f;
            return IsoGrid.ToWorld(gx + o, gy + o);
        }

        public void SetCell(int gx, int gy)
        {
            D.gx = gx;
            D.gy = gy;
            transform.position = CenterOf(Def, gx, gy);
            // Enclosures sort behind the back edge of their yard so the animals inside draw on top.
            Sr.sortingOrder = IsEnclosure
                ? IsoGrid.SortOrder(YardPoint(Def.yard[3]).y + 0.05f)
                : IsoGrid.SortOrder(transform.position.y);
            if (glow != null) glow.sortingOrder = Sr.sortingOrder + 1;
        }

        // ------------------------------------------------------------------ animal enclosure yard
        public bool IsEnclosure => Def.kind == PlaceKind.House && Def.yard != null && Def.yard.Length == 4;

        /// <summary>Yard corner (normalized, y down) → world position.</summary>
        public Vector3 YardPoint(Vector2 n)
        {
            float k = Def.size * IsoGrid.HalfW;
            return transform.position + new Vector3(n.x * k, -n.y * k, 0f);
        }

        /// <summary>Random point on the fenced yard floor (or near the building if it has no yard).</summary>
        public Vector3 RandomYardPoint()
        {
            if (!IsEnclosure)
                return transform.position + new Vector3(Random.Range(-1f, 1f), -IsoGrid.HalfH * 2.2f + Random.Range(-0.3f, 0.3f), 0);
            Vector2[] q = Def.yard; // left, front, right, back
            float a = Random.value, b = Random.value;
            Vector2 n = Vector2.Lerp(Vector2.Lerp(q[3], q[2], a), Vector2.Lerp(q[0], q[1], a), b);
            return YardPoint(n);
        }

        public bool InYard(Vector3 world)
        {
            if (!IsEnclosure) return true;
            Vector2[] q = Def.yard;
            float sign = 0f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = YardPoint(q[i]), b = YardPoint(q[(i + 1) % 4]);
                float cross = (b.x - a.x) * (world.y - a.y) - (b.y - a.y) * (world.x - a.x);
                if (Mathf.Abs(cross) < 1e-5f) continue;
                if (sign == 0f) sign = Mathf.Sign(cross);
                else if (Mathf.Sign(cross) != sign) return false;
            }
            return true;
        }

        /// <summary>Where the farmer stands to tend the animals: outside the gate (front-left fence, near the front corner).</summary>
        public Vector3 GatePoint
        {
            get
            {
                int n = Def.size;
                var candidates = new[]
                {
                    new Vector2Int(D.gx + n - 2, D.gy + n), new Vector2Int(D.gx + n - 1, D.gy + n), new Vector2Int(D.gx + n - 3, D.gy + n),
                    new Vector2Int(D.gx + n, D.gy + n - 1), new Vector2Int(D.gx + n, D.gy + n - 2), new Vector2Int(D.gx + n, D.gy + n),
                };
                foreach (Vector2Int c in candidates)
                    if (WorldMap.I.Walkable(c)) return IsoGrid.CellCenter(c);
                for (int i = -1; i <= n; i++)
                {
                    var a = new Vector2Int(D.gx + i, D.gy - 1);
                    var b = new Vector2Int(D.gx - 1, D.gy + i);
                    if (WorldMap.I.Walkable(a)) return IsoGrid.CellCenter(a);
                    if (WorldMap.I.Walkable(b)) return IsoGrid.CellCenter(b);
                }
                return IsoGrid.CellCenter(candidates[0]);
            }
        }

        public override bool HitTest(Vector2 p)
        {
            if (Def.size > 1 && IsoGrid.InDiamond(p, transform.position, Def.size)) return true;
            return base.HitTest(p);
        }

        private void Update()
        {
            if (glow != null && DayNight.I != null)
                glow.color = new Color(1f, 0.85f, 0.45f, DayNight.I.NightFactor * 0.55f);

            if (bubble == null) return;
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 0.4f;
            switch (Def.kind)
            {
                case PlaceKind.Machine:
                {
                    MachineData m = Production.Get(D.uid);
                    Production.Tick(m);
                    if (m.done.Count > 0) bubble.Show(Defs.Item(m.done[0]).icon, 0.28f);
                    else if (m.jobs.Count == 0) bubble.Show("hud_production", 0.3f);
                    else bubble.Hide();
                    break;
                }
                case PlaceKind.Tree:
                {
                    TreeDef t = Defs.Tree(Def.id);
                    bool dead = Orchard.IsDead(this);
                    string want = dead ? "nature_dead_tree" : Def.sprite;
                    if (want != shownSprite)
                    {
                        shownSprite = want;
                        Sr.sprite = SpriteDB.Get(want);
                        Sr.color = dead ? new Color(0.85f, 0.8f, 0.7f) : Color.white;
                    }
                    if (!dead && Orchard.IsReady(this)) bubble.Show(Defs.Item(t.fruit).icon, 0.28f);
                    else bubble.Hide();
                    break;
                }
                case PlaceKind.Special:
                    if (Def.action == "market")
                    {
                        if (Market.AnySold) bubble.Show("hud_coin", 0.3f);
                        else if (Market.ListedCount == 0) bubble.Show("hud_add", 0.3f);
                        else bubble.Hide();
                    }
                    break;
                case PlaceKind.Delivery:
                    if (!Delivery.Waiting && Delivery.AllFilled) bubble.Show("hud_confirm", 0.3f);
                    else if (!Delivery.Waiting) bubble.Show("hud_truck", 0.3f);
                    else bubble.Hide();
                    break;
            }
        }

        public override void OnTap()
        {
            Punch();
            UIManager ui = UIManager.I;
            switch (Def.kind)
            {
                case PlaceKind.Special:
                    switch (Def.action)
                    {
                        case "storage": ui.OpenStorage(); break;
                        case "market":
                        {
                            int coins = Market.CollectAll();
                            if (coins > 0)
                            {
                                ui.FloatText(transform.position + Vector3.up * 1.5f, "+" + coins, "hud_coin");
                                ui.Toast(L.F("Khách đã mua hàng: +{0} xu", coins));
                            }
                            ui.OpenMarket();
                            break;
                        }
                        case "shop": ui.OpenShop(null); break;
                        case "orders": ui.OpenOrders(); break;
                        case "farmhouse": ui.OpenFarmhouse(); break;
                    }
                    break;
                case PlaceKind.House:
                    ui.OpenHouse(this);
                    break;
                case PlaceKind.Machine:
                {
                    MachineData m = Production.Get(D.uid);
                    if (Production.Collect(m, transform.position) == 0) ui.OpenMachine(this);
                    break;
                }
                case PlaceKind.Tree:
                    TapTree();
                    break;
                case PlaceKind.Delivery:
                    ui.OpenDelivery();
                    break;
                case PlaceKind.Sprinkler:
                    ui.ShowTooltip(transform.position + Vector3.up, L.T("Vòi phun tự tưới\ncác ô đất xung quanh"));
                    break;
                default:
                    if (IsDecor) ui.OpenDecor(this);
                    else ui.ShowTooltip(transform.position + Vector3.up * (0.6f + Def.scale), Def.name);
                    break;
            }
        }

        private void TapTree()
        {
            TreeDef t = Defs.Tree(Def.id);
            PlaceState s = Orchard.State(this);
            bool endless = Orchard.Endless(this);
            ItemDef fruit = Defs.Item(t.fruit);
            if (Orchard.IsDead(this))
            {
                int cost = Def.cost / 2;
                UIManager.I.Confirm(L.T("Cây đã già"), L.F("{0} đã cho hết quả.\nTrồng lại với giá {1} xu?", Def.name, cost), () =>
                {
                    if (!GameManager.I.TrySpend(cost)) { UIManager.I.Toast(L.T("Không đủ xu!")); return; }
                    s.count = t.harvests;
                    s.t = GameManager.I.Now + t.cycleSec;
                    refresh = 0;
                });
                return;
            }
            if (!Orchard.IsReady(this))
            {
                string info = endless
                    ? L.F("{0}: {1} sau {2}\nCây ăn quả gần tổ ong được +1 quả", Def.name, fruit.name, Defs.FormatTime(s.t - GameManager.I.Now))
                    : L.F("{0}: còn {1} lần thu", Def.name, s.count) + (Orchard.Pollinated(this) ? L.T(" · có ong thụ phấn +1") : "") +
                      L.F("\nQuả chín sau {0}", Defs.FormatTime(s.t - GameManager.I.Now));
                UIManager.I.ShowSpeedUp(transform.position + Vector3.up * (0.8f + Def.scale), info, s.t - GameManager.I.Now,
                    () => { s.t = GameManager.I.Now; refresh = 0; });
                return;
            }
            int amount = Orchard.Yield(this);
            if (!GameManager.I.HasSpace(amount)) { UIManager.I.Toast(L.T("Kho đầy! Mang hàng ra Chợ bán hoặc nâng cấp kho.")); return; }
            if (Pending) return;
            Pending = true;
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                target = () => transform.position,
                pose = "harvest_happy",
                reach = 1.3f,
                canDo = () => Orchard.IsReady(this) && GameManager.I.HasSpace(Orchard.Yield(this)),
                doIt = () =>
                {
                    int n = Orchard.Yield(this);
                    GameManager.I.AddItem(fruit.id, n);
                    GameManager.I.AddXp(fruit.xp);
                    if (!endless) s.count--;
                    s.t = GameManager.I.Now + t.cycleSec;
                    UIManager.I.FloatText(transform.position + Vector3.up * 1.2f, "+" + n, fruit.icon);
                    if (endless) Fx.Sparkle(transform.position, Fx.Gold, 10);
                    else Fx.Leaves(transform.position + Vector3.up * 0.8f, Color.white);
                    Haptics.Light();
                    GameEvents.Raise("harvest_fruit", fruit.id, n);
                    if (!endless && s.count <= 0) UIManager.I.Toast(L.F("{0} đã cho hết quả – chạm để trồng lại.", Def.name));
                    refresh = 0;
                }
            });
        }
    }

    /// <summary>A water cell where the player can fish (mini-game).</summary>
    public class FishingSpotView : Interactable
    {
        public Vector2Int Cell;
        private WorldBubble bubble;
        private float refresh;

        public void Init(Vector2Int c)
        {
            Cell = c;
            transform.position = IsoGrid.CellCenter(c);
            bubble = WorldBubble.Create(transform, 0.9f);
        }

        public override bool HitTest(Vector2 p) => IsoGrid.InDiamond(p, transform.position);
        public override int HitOrder => IsoGrid.SortOrder(transform.position.y);

        /// <summary>Closest walkable land cell around the spot (nearest ring first, then nearest to the farmer).</summary>
        private Vector3 ShorePoint()
        {
            Vector2 farmer = FarmerController.I.transform.position;
            for (int r = 1; r <= 4; r++)
            {
                bool found = false;
                Vector2Int best = Cell;
                float bd = float.MaxValue;
                for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                    var c = new Vector2Int(Cell.x + dx, Cell.y + dy);
                    if (!WorldMap.I.Walkable(c)) continue;
                    float d = Vector2.Distance(IsoGrid.CellCenter(c), farmer);
                    if (d < bd) { bd = d; best = c; found = true; }
                }
                if (found) return IsoGrid.CellCenter(best);
            }
            return transform.position;
        }

        private void Update()
        {
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 1f;
            if (Fishing.Spot(Cell).count > 0) bubble.Show("hud_fishing", 0.3f);
            else bubble.Hide();
        }

        public override void OnTap()
        {
            FishSpotData s = Fishing.Spot(Cell);
            if (s.count <= 0)
            {
                UIManager.I.ShowTooltip(transform.position + Vector3.up, L.F("Cá đã hết\nCá mới sau {0}", Defs.FormatTime(s.regenAt - GameManager.I.Now)));
                return;
            }
            if (Pending) return;
            Pending = true;
            Vector3 shore = ShorePoint();
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                // stand on the nearest bank cell: spots in the middle of a pond have no walkable neighbour
                target = () => shore,
                pose = "water",
                reach = 0.3f,
                duration = 0.2f,
                canDo = () => Fishing.Spot(Cell).count > 0,
                doIt = () => UIManager.I.OpenFishing(Cell),
            });
        }
    }

    /// <summary>Stumps, rocks and bushes that block land until cleared.</summary>
    public class DebrisView : Interactable
    {
        public DebrisData D;
        [System.NonSerialized] public Defs.DebrisDef Def;
        private float armedUntil;

        public void Init(DebrisData d)
        {
            D = d;
            Def = Defs.DebrisBySprite(d.sprite);
            transform.position = IsoGrid.ToWorld(d.gx, d.gy);
            Sr = MakeSprite(d.sprite, Def.scale);
            Sr.sortingOrder = IsoGrid.SortOrder(transform.position.y);
        }

        public override void OnTap()
        {
            Punch();
            if (Pending) return;
            if (Time.time > armedUntil)
            {
                armedUntil = Time.time + 3f;
                UIManager.I.ShowTooltip(transform.position + Vector3.up * 1.2f,
                    L.F("Dọn {0}: {1} xu\nChạm lần nữa để dọn", Def.name, Def.cost));
                return;
            }
            armedUntil = 0;
            if (!GameManager.I.CanAfford(Def.cost))
            {
                UIManager.I.Toast(L.T("Không đủ xu để dọn!"));
                return;
            }
            Pending = true;
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                target = () => transform.position,
                pose = "axe",
                duration = 0.76f,
                canDo = () => this != null && GameManager.I.CanAfford(Def.cost),
                doIt = () =>
                {
                    if (!GameManager.I.TrySpend(Def.cost)) return;
                    GameManager.I.AddXp(Def.cost / 3 + 1);
                    UIManager.I.FloatText(transform.position + Vector3.up, "-" + Def.cost, "hud_coin");
                    GameEvents.Raise("clear", D.sprite);
                    Fx.Dust(transform.position, 16);
                    // B3.1: clearing land sometimes yields a building material
                    if (Random.value < Defs.DebrisMaterialChance)
                    {
                        string mat = Defs.RandomMaterial();
                        if (GameManager.I.AddItem(mat, 1))
                            UIManager.I.FloatText(transform.position + Vector3.up * 1.6f, "+1", Defs.Item(mat).icon);
                    }
                    Haptics.Light();
                    WorldMap.I.RemoveDebris(this);
                }
            });
        }
    }
}
