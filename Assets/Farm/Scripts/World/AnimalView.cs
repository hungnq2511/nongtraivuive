using UnityEngine;

namespace Farm
{
    /// <summary>Farm animal: wanders near its house, feed it a crop, collect its product later.</summary>
    public class AnimalView : Interactable
    {
        public AnimalData D;
        [System.NonSerialized] public AnimalDef Def;
        private WorldBubble bubble;
        private Vector3 walkTarget;
        private bool walking;
        private float idleTimer;
        private float animTimer;
        private bool frameB;
        private bool faceLeft;
        private bool faceUp;
        private string pose;
        private float poseTimer;
        private float refreshTimer;

        public bool IsReady => D.state == AnimalState.Producing && GameManager.I.Now >= D.fedAt + Def.produceSec;
        public double SecondsLeft => D.fedAt + Def.produceSec - GameManager.I.Now;

        public void Init(AnimalData d, Vector3 pos)
        {
            D = d;
            Def = Defs.Animal(d.type);
            transform.position = pos;
            Sr = MakeSprite(Def.Frame("idle_se"), Def.scale);
            bubble = WorldBubble.Create(transform, 2.2f * Def.scale + 0.45f);
            idleTimer = Random.Range(0.5f, 3f);
            faceLeft = Random.value > 0.5f;
        }

        private PlaceView House => WorldMap.I.FindPlace(D.houseUid);

        private void Update()
        {
            float dt = Time.deltaTime;
            bool ready = IsReady;

            if (poseTimer > 0f) poseTimer -= dt;
            else pose = null;

            if (!ready && !Pending && pose == null) Wander(dt);
            else walking = false;

            // frame
            string frame;
            if (pose != null) frame = pose;
            else if (ready) frame = "product_ready";
            else if (walking)
            {
                animTimer += dt;
                if (animTimer > 0.18f) { animTimer = 0; frameB = !frameB; }
                frame = (frameB ? "walk_b_" : "walk_a_") + (faceUp ? Def.back : "se");
            }
            else frame = D.state == AnimalState.Hungry && idleTimer > 3.5f ? "sleep" : "idle_" + (faceUp ? Def.back : "se");

            Sr.sprite = SpriteDB.Get(Def.Frame(frame));
            // front frames ("_se") face right; back frames face up-left
            if (frame.EndsWith("_se")) Sr.flipX = faceLeft;
            else if (frame.EndsWith("_" + Def.back)) Sr.flipX = !faceLeft;
            else Sr.flipX = false;
            Sr.sortingOrder = IsoGrid.SortOrder(transform.position.y);

            refreshTimer -= dt;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.3f;
                if (ready) bubble.Show(Defs.Item(Def.product).icon, 0.26f);
                else if (D.state == AnimalState.Hungry) bubble.Show(Defs.Crop(Def.feed).Icon, 0.26f);
                else bubble.Hide();
            }
        }

        private void Wander(float dt)
        {
            if (walking)
            {
                Vector3 p = transform.position;
                Vector3 to = walkTarget - p;
                float step = 0.75f * dt;
                if (to.magnitude <= step)
                {
                    transform.position = walkTarget;
                    walking = false;
                    idleTimer = Random.Range(1.5f, 5f);
                    if (D.state == AnimalState.Producing && Random.value < 0.4f) { pose = "eat"; poseTimer = 1.5f; }
                }
                else
                {
                    Vector3 next = p + to.normalized * step;
                    if (!WorldMap.I.Walkable(IsoGrid.ToCell(next)))
                    {
                        walking = false;
                        idleTimer = 0.5f;
                        return;
                    }
                    transform.position = next;
                    if (Mathf.Abs(to.x) > 0.01f) faceLeft = to.x < 0f;
                    if (Mathf.Abs(to.y) > Mathf.Abs(to.x) * 0.2f) faceUp = to.y > 0f;
                }
                return;
            }

            idleTimer -= dt;
            if (idleTimer > 0f) return;
            PlaceView house = House;
            Vector3 center = house != null ? house.transform.position + new Vector3(0, -IsoGrid.HalfH * 2.2f, 0) : transform.position;
            for (int i = 0; i < 8; i++)
            {
                Vector2 r = Random.insideUnitCircle * 2.2f;
                Vector3 cand = center + new Vector3(r.x, r.y * 0.6f, 0);
                Vector2Int c = IsoGrid.ToCell(cand);
                if (WorldMap.I.Walkable(c) && WorldMap.I.PlotAt(c) == null)
                {
                    walkTarget = cand;
                    walking = true;
                    return;
                }
            }
            idleTimer = 1f;
        }

        public override int HitOrder => base.HitOrder + 500; // animals take priority over what's behind

        public override void OnTap()
        {
            string a = Action();
            if (a == "collect" || a == "feed") Queue(a);
            else if (D.state == AnimalState.Hungry)
                UIManager.I.Toast($"Cần {Defs.Crop(Def.feed).name} để cho {Def.name} ăn. Hãy trồng thêm nhé!");
            else
                UIManager.I.ShowTooltip(transform.position + Vector3.up * 1.4f,
                    $"{Def.name} đang tạo {Defs.Item(Def.product).name}\nCòn {Defs.FormatTime(SecondsLeft)}");
            Punch();
        }

        private string Action()
        {
            if (IsReady) return "collect";
            if (D.state == AnimalState.Hungry && GameManager.I.Count(Def.feed) > 0) return "feed";
            return null;
        }

        public override string DragKey() => Action();

        public override void OnDragApply(string key)
        {
            if (Action() == key) Queue(key);
        }

        private void Queue(string action)
        {
            if (Pending) return;
            if (action == "collect" && !GameManager.I.HasSpace(1))
            {
                UIManager.I.Toast("Kho đầy! Hãy bán bớt hoặc nâng cấp kho.");
                return;
            }
            Pending = true;
            FarmerController.I.Enqueue(new FarmTask
            {
                owner = this,
                target = () => transform.position,
                pose = "harvest_happy",
                canDo = () => Action() == action,
                doIt = () => Do(action),
            });
        }

        private void Do(string action)
        {
            GameManager gm = GameManager.I;
            if (action == "feed")
            {
                if (!gm.RemoveItem(Def.feed, 1)) return;
                D.state = AnimalState.Producing;
                D.fedAt = gm.Now;
                gm.AddXp(1);
                pose = "eat";
                poseTimer = 1.6f;
                UIManager.I.FloatText(transform.position + Vector3.up, "-1", Defs.Crop(Def.feed).Icon);
                GameEvents.Raise("feed", Def.id);
            }
            else if (action == "collect")
            {
                ItemDef prod = Defs.Item(Def.product);
                if (!gm.AddItem(prod.id, 1))
                {
                    UIManager.I.Toast("Kho đầy! Hãy bán bớt hoặc nâng cấp kho.");
                    return;
                }
                gm.AddXp(prod.xp);
                D.state = AnimalState.Hungry;
                pose = "happy";
                poseTimer = 1.2f;
                UIManager.I.FloatText(transform.position + Vector3.up, "+1", prod.icon);
                GameEvents.Raise("collect", prod.id);
            }
            refreshTimer = 0;
        }
    }
}
