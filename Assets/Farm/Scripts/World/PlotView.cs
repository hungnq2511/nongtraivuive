using UnityEngine;

namespace Farm
{
    /// <summary>One farm plot: till → plant → water → grow (real time) → harvest.</summary>
    public class PlotView : Interactable
    {
        public PlotData D;
        private WorldBubble bubble;
        private float harvestedTimer;
        private string harvestedSprite;
        private float refreshTimer;
        private string shownSprite;

        public CropDef Crop => Defs.Crop(D.crop);
        public Vector2Int Cell => new Vector2Int(D.gx, D.gy);
        public override bool Movable => D.state <= PlotState.Tilled;

        private SpriteRenderer plantSr;
        private static readonly Color DrySoil = Color.white;
        private static readonly Color WetSoil = new Color(0.72f, 0.64f, 0.6f, 1f);

        public void Init(PlotData d)
        {
            D = d;
            transform.position = IsoGrid.ToWorld(d.gx, d.gy);
            Sr = MakeSprite("ground_soil_bare", 1f);
            var pg = new GameObject("Plant");
            pg.transform.SetParent(transform, false);
            pg.transform.localScale = Vector3.one * PlantScale;
            plantSr = pg.AddComponent<SpriteRenderer>();
            bubble = WorldBubble.Create(transform, 1.1f);
            Refresh(true);
        }

        private const float PlantScale = 0.9f;

        public void SetCell(int gx, int gy)
        {
            D.gx = gx;
            D.gy = gy;
            transform.position = IsoGrid.ToWorld(gx, gy);
            Refresh(true);
        }

        public double Progress
        {
            get
            {
                if (D.state != PlotState.Growing || Crop == null) return 0;
                return (GameManager.I.Now - D.wateredAt) / Crop.growSec;
            }
        }

        public bool IsReady => D.state == PlotState.Growing && Progress >= 1.0;
        public double SecondsLeft => D.state == PlotState.Growing && Crop != null ? D.wateredAt + Crop.growSec - GameManager.I.Now : 0;

        private void Update()
        {
            if (harvestedTimer > 0f)
            {
                harvestedTimer -= Time.deltaTime;
                if (harvestedTimer <= 0f) Refresh(true);
            }
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.25f;
                Refresh(false);
            }
        }

        public void Refresh(bool force)
        {
            // Soil is a flat ground tile (flush with the grass); the plant is drawn on top of it.
            string soil = D.state == PlotState.Untilled ? "ground_soil_bare" : "ground_soil_tilled_dry";
            string plant = null;
            CropDef c = Crop;
            if (D.state == PlotState.Tilled && harvestedTimer > 0f) plant = harvestedSprite;
            else if (D.state == PlotState.Growing && c != null)
            {
                double p = Progress;
                if (p >= 1.0) plant = c.Stage("ready");
                else if (p >= 0.85) plant = c.Stage("mature");
                else if (p >= 0.6) plant = c.Stage("medium");
                else if (p >= 0.35) plant = c.Stage("young");
                else if (p >= 0.12) plant = c.Stage("sprout");
            }

            string key = soil + "|" + plant + "|" + D.state;
            if (force || key != shownSprite)
            {
                shownSprite = key;
                Sr.sprite = SpriteDB.Get(soil);
                Sr.color = D.state == PlotState.Growing ? WetSoil : DrySoil;
                Sr.sortingOrder = IsoGrid.GroundOrder(D.gx, D.gy, 1);
                plantSr.sprite = plant != null ? SpriteDB.Get(plant) : null;
                plantSr.sortingOrder = IsoGrid.SortOrder(transform.position.y);
            }

            if (D.state == PlotState.Planted) bubble.Show("tool_watering_can", 0.28f);
            else bubble.Hide();
        }

        public override int HitOrder => plantSr != null && plantSr.sprite != null ? plantSr.sortingOrder : Sr.sortingOrder;

        // ---------------- Interaction ----------------

        public string PrimaryAction()
        {
            switch (D.state)
            {
                case PlotState.Untilled: return "till";
                case PlotState.Tilled: return InteractionController.SelectedSeed != null ? "plant" : "seedbar";
                case PlotState.Planted: return "water";
                default: return IsReady ? "harvest" : "info";
            }
        }

        public override bool HitTest(Vector2 p)
        {
            if (IsoGrid.InDiamond(p, transform.position)) return true;
            // allow tapping the plant itself (it rises above the tile)
            if (plantSr != null && plantSr.sprite != null)
            {
                Bounds b = plantSr.bounds;
                return Mathf.Abs(p.x - b.center.x) < b.extents.x * 0.5f && p.y > transform.position.y && p.y < b.max.y;
            }
            return false;
        }

        public override void OnTap()
        {
            string a = PrimaryAction();
            switch (a)
            {
                case "seedbar":
                    UIManager.I.OpenSeedBar();
                    break;
                case "info":
                    UIManager.I.ShowTooltip(transform.position + Vector3.up * 1.2f,
                        $"{Crop.name}\nCòn {Defs.FormatTime(SecondsLeft)}");
                    break;
                default:
                    Queue(a, true);
                    break;
            }
        }

        public override string DragKey()
        {
            string a = PrimaryAction();
            return a == "till" || a == "plant" || a == "water" || a == "harvest" ? a : null;
        }

        public override void OnDragApply(string key)
        {
            if (PrimaryAction() == key) Queue(key, false);
        }

        private void Queue(string action, bool verbose)
        {
            if (Pending) return;
            if (action == "plant")
            {
                CropDef seed = Defs.Crop(InteractionController.SelectedSeed);
                if (seed == null) return;
                if (!GameManager.I.CanAfford(seed.seedCost))
                {
                    UIManager.I.Toast("Không đủ xu để mua hạt giống!");
                    return;
                }
            }
            if (action == "harvest" && !GameManager.I.HasSpace(Crop.yield))
            {
                UIManager.I.Toast("Kho đầy! Hãy bán bớt hoặc nâng cấp kho.");
                return;
            }

            string pose = action == "water" ? "water" : action == "harvest" ? "harvest_happy" : "hoe";
            string plantSeed = InteractionController.SelectedSeed;
            Pending = true;
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                target = () => transform.position,
                pose = pose,
                canDo = () => PrimaryAction() == action || (action == "plant" && D.state == PlotState.Tilled),
                doIt = () => Do(action, plantSeed),
            });
        }

        private void Do(string action, string seedId)
        {
            GameManager gm = GameManager.I;
            switch (action)
            {
                case "till":
                    D.state = PlotState.Tilled;
                    gm.AddXp(1);
                    GameEvents.Raise("till");
                    break;
                case "plant":
                    CropDef seed = Defs.Crop(seedId);
                    if (seed == null) return;
                    if (!gm.TrySpend(seed.seedCost))
                    {
                        UIManager.I.Toast("Không đủ xu để mua hạt giống!");
                        return;
                    }
                    D.crop = seed.id;
                    D.state = PlotState.Planted;
                    UIManager.I.FloatText(transform.position + Vector3.up * 0.6f, "-" + seed.seedCost, "hud_coin");
                    GameEvents.Raise("plant", seed.id);
                    break;
                case "water":
                    Water();
                    GameEvents.Raise("water");
                    break;
                case "harvest":
                    CropDef c = Crop;
                    if (!gm.AddItem(c.id, c.yield))
                    {
                        UIManager.I.Toast("Kho đầy! Hãy bán bớt hoặc nâng cấp kho.");
                        return;
                    }
                    gm.AddXp(c.xp);
                    UIManager.I.FloatText(transform.position + Vector3.up * 0.8f, "+" + c.yield, c.Icon);
                    harvestedSprite = c.Stage("harvested");
                    harvestedTimer = 0.6f;
                    D.state = PlotState.Tilled;
                    D.crop = null;
                    GameEvents.Raise("harvest", c.id);
                    break;
            }
            Refresh(true);
        }

        public void Water()
        {
            if (D.state != PlotState.Planted) return;
            D.state = PlotState.Growing;
            D.wateredAt = GameManager.I.Now;
            Refresh(true);
        }
    }
}
