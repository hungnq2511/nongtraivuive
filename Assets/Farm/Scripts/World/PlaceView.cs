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
            if (Def.kind == PlaceKind.Machine || Def.kind == PlaceKind.Tree || Def.kind == PlaceKind.Delivery)
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
        }

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
            Sr.sortingOrder = IsoGrid.SortOrder(transform.position.y);
            if (glow != null) glow.sortingOrder = Sr.sortingOrder + 1;
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
                        case "storage": ui.OpenStorage(false); break;
                        case "market": ui.OpenStorage(true); break;
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
                    ui.ShowTooltip(transform.position + Vector3.up, "Vòi phun tự tưới\ncác ô đất xung quanh");
                    break;
                default:
                    ui.ShowTooltip(transform.position + Vector3.up * (0.6f + Def.scale), Def.name);
                    break;
            }
        }

        private void TapTree()
        {
            TreeDef t = Defs.Tree(Def.id);
            PlaceState s = Orchard.State(this);
            if (s.count <= 0)
            {
                int cost = Def.cost / 2;
                UIManager.I.Confirm("Cây đã già", $"{Def.name} đã cho hết quả.\nTrồng lại với giá {cost} xu?", () =>
                {
                    if (!GameManager.I.TrySpend(cost)) { UIManager.I.Toast("Không đủ xu!"); return; }
                    s.count = t.harvests;
                    s.t = GameManager.I.Now + t.cycleSec;
                    refresh = 0;
                });
                return;
            }
            if (!Orchard.IsReady(this))
            {
                UIManager.I.ShowTooltip(transform.position + Vector3.up * (0.8f + Def.scale),
                    $"{Def.name}: còn {s.count} lần thu\nQuả chín sau {Defs.FormatTime(s.t - GameManager.I.Now)}");
                return;
            }
            if (!GameManager.I.HasSpace(t.yield)) { UIManager.I.Toast("Kho đầy! Hãy bán bớt hoặc nâng cấp kho."); return; }
            if (Pending) return;
            Pending = true;
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                target = () => transform.position,
                pose = "harvest_happy",
                reach = 1.3f,
                canDo = () => Orchard.IsReady(this) && GameManager.I.HasSpace(t.yield),
                doIt = () =>
                {
                    ItemDef fruit = Defs.Item(t.fruit);
                    GameManager.I.AddItem(fruit.id, t.yield);
                    GameManager.I.AddXp(fruit.xp);
                    s.count--;
                    s.t = GameManager.I.Now + t.cycleSec;
                    UIManager.I.FloatText(transform.position + Vector3.up * 1.2f, "+" + t.yield, fruit.icon);
                    GameEvents.Raise("harvest_fruit", fruit.id, t.yield);
                    if (s.count <= 0) UIManager.I.Toast($"{Def.name} đã cho hết quả – chạm để trồng lại.");
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

        private void Update()
        {
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 1f;
            if (Fishing.Spot(Cell).count > 0) bubble.Show("icon_fish_carp", 0.26f);
            else bubble.Hide();
        }

        public override void OnTap()
        {
            FishSpotData s = Fishing.Spot(Cell);
            if (s.count <= 0)
            {
                UIManager.I.ShowTooltip(transform.position + Vector3.up, $"Cá đã hết\nCá mới sau {Defs.FormatTime(s.regenAt - GameManager.I.Now)}");
                return;
            }
            if (Pending) return;
            Pending = true;
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                target = () => transform.position,
                pose = "water",
                reach = 1.7f,
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
                    $"Dọn {Def.name}: {Def.cost} xu\nChạm lần nữa để dọn");
                return;
            }
            armedUntil = 0;
            if (!GameManager.I.CanAfford(Def.cost))
            {
                UIManager.I.Toast("Không đủ xu để dọn!");
                return;
            }
            Pending = true;
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                target = () => transform.position,
                pose = "axe",
                duration = 0.6f,
                canDo = () => this != null && GameManager.I.CanAfford(Def.cost),
                doIt = () =>
                {
                    if (!GameManager.I.TrySpend(Def.cost)) return;
                    GameManager.I.AddXp(Def.cost / 3 + 1);
                    UIManager.I.FloatText(transform.position + Vector3.up, "-" + Def.cost, "hud_coin");
                    GameEvents.Raise("clear", D.sprite);
                    WorldMap.I.RemoveDebris(this);
                }
            });
        }
    }
}
