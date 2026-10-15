using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>B2.1 – a villager who walks in along the road, waits with a request bubble and leaves after being served.</summary>
    public class VisitorView : Interactable
    {
        public VisitorData D;
        public VisitorDef Def;
        public string Line;

        private enum S { Arriving, Waiting, Leaving }
        private S state;
        private Vector3 target;
        private WorldBubble bubble;
        private SpriteRenderer shadow;
        private float animT, waveT, nextWave, alpha, bobT;
        private bool frameB, faceLeft;

        private const float WalkSpeed = 1.5f;

        public bool Waiting => state == S.Waiting;
        public static Vector3 EntryPos => IsoGrid.CellCenter(Defs.VisitorEntry);
        public Vector3 SpotPos => IsoGrid.CellCenter(Defs.VisitorSpots[Mathf.Clamp(D.spot, 0, Defs.VisitorSpots.Length - 1)]);

        public void Init(VisitorData d, bool arrive)
        {
            D = d;
            Def = Defs.Visitor(d.npc) ?? Defs.Visitors[0];
            Line = Def.hello[Random.Range(0, Def.hello.Length)];
            Sr = MakeSprite(Def.Frame("idle"), Def.scale);
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(transform, false);
            sh.transform.localScale = new Vector3(0.75f, 0.3f, 1f);
            shadow = sh.AddComponent<SpriteRenderer>();
            shadow.sprite = ProcSprites.SoftCircle;
            shadow.color = new Color(0, 0, 0, 0.22f);
            bubble = WorldBubble.Create(transform, 2.35f * Def.scale / 0.75f);
            nextWave = Random.Range(4f, 9f);
            if (arrive)
            {
                transform.position = EntryPos;
                target = SpotPos;
                state = S.Arriving;
                alpha = 0f;
            }
            else
            {
                transform.position = SpotPos;
                state = S.Waiting;
                alpha = 1f;
            }
        }

        public void Leave(bool happy)
        {
            state = S.Leaving;
            target = EntryPos;
            waveT = happy ? 1.4f : 0f;
            bubble.Hide();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            string frame;
            bool walking = false;
            if (waveT > 0f)
            {
                waveT -= dt;
                frame = Def.Frame("wave");
            }
            else if (state == S.Waiting)
            {
                nextWave -= dt;
                if (nextWave <= 0f) { waveT = 1.2f; nextWave = Random.Range(7f, 14f); }
                frame = Def.Frame("idle");
            }
            else
            {
                Vector3 to = target - transform.position;
                float step = WalkSpeed * dt;
                if (to.magnitude <= step)
                {
                    transform.position = target;
                    if (state == S.Arriving) { state = S.Waiting; waveT = 1.2f; }
                    else { Destroy(gameObject); return; }
                }
                else
                {
                    transform.position += to.normalized * step;
                    if (Mathf.Abs(to.x) > 0.01f) faceLeft = to.x < 0f;
                    walking = true;
                }
                animT += dt;
                if (animT > 0.2f) { animT = 0; frameB = !frameB; }
                frame = Def.Frame(frameB ? "walk_b" : "walk_a");
            }

            // fade in at the forest edge, fade out when leaving
            float fadeTarget = state == S.Leaving && Vector2.Distance(transform.position, EntryPos) < 1.2f ? 0f : 1f;
            alpha = Mathf.MoveTowards(alpha, fadeTarget, dt * 2.5f);

            Sr.sprite = SpriteDB.Get(frame);
            Sr.flipX = faceLeft;
            bobT += dt * (walking ? 11f : 0f);
            Sr.transform.localPosition = new Vector3(0, walking ? Mathf.Abs(Mathf.Sin(bobT)) * 0.05f : 0f, 0);
            Sr.color = new Color(1, 1, 1, alpha);
            shadow.color = new Color(0, 0, 0, 0.22f * alpha);
            Sr.sortingOrder = IsoGrid.SortOrder(transform.position.y, 1);
            shadow.sortingOrder = Sr.sortingOrder - 1;

            if (state == S.Waiting && D.req.Count > 0) bubble.Show(Defs.Item(D.req[0].id).icon, 0.26f);
            else bubble.Hide();
        }

        public override int HitOrder => base.HitOrder + 600;

        public override void OnTap()
        {
            Punch();
            if (state == S.Waiting) UIManager.I.OpenVisitor(this);
            else if (state == S.Arriving) UIManager.I.Toast(L.F("{0} đang tới…", Def.name));
        }
    }

    /// <summary>B2.3 – a crow that lands on a growing plot and pauses its growth until it is chased away.</summary>
    public class CrowView : Interactable
    {
        public PlotView Plot { get; private set; }

        private enum S { FlyIn, Perch, Flee }
        private S state;
        private Vector3 from, to;
        private float t, dur, stay, frameT, peckT;
        private int flyFrame;
        private bool pecking;
        private SpriteRenderer shadow;
        private const float Scale = 0.55f;

        public bool Perched => state == S.Perch;
        public bool Active => state != S.Flee;

        public void Init(PlotView plot)
        {
            Plot = plot;
            to = plot.transform.position + new Vector3(Random.Range(-0.15f, 0.15f), 0.12f, 0);
            float side = Random.value < 0.5f ? -1f : 1f;
            from = to + new Vector3(side * 8f, 6f, 0);
            transform.position = from;
            dur = Vector3.Distance(from, to) / 4.5f;
            Sr = MakeSprite("crow_fly_1", Scale);
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(transform, false);
            shadow = sh.AddComponent<SpriteRenderer>();
            shadow.sprite = ProcSprites.SoftCircle;
            state = S.FlyIn;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            string frame;
            switch (state)
            {
                case S.FlyIn:
                {
                    t += dt / dur;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
                    frame = FlyFrame(dt);
                    Sr.flipX = to.x < from.x;
                    if (t >= 1f)
                    {
                        transform.position = to;
                        state = S.Perch;
                        Fx.Leaves(to, new Color(0.3f, 0.3f, 0.35f));
                    }
                    break;
                }
                case S.Perch:
                {
                    if (Plot == null || Plot.D.state != PlotState.Growing || Plot.IsReady) { Flee(); frame = "crow_flee"; break; }
                    stay += dt;
                    Plot.D.wateredAt += dt; // the crow is eating: growth is paused while it sits here
                    if (stay >= Defs.CrowMaxStay) { Flee(); frame = "crow_flee"; break; }
                    peckT -= dt;
                    if (peckT <= 0f)
                    {
                        pecking = !pecking;
                        peckT = pecking ? Random.Range(0.25f, 0.45f) : Random.Range(0.6f, 1.6f);
                        if (pecking && Random.value < 0.35f) Sr.flipX = !Sr.flipX;
                    }
                    frame = pecking ? "crow_peck" : "crow_stand";
                    break;
                }
                default:
                {
                    t += dt / dur;
                    if (t < 0.15f) frame = "crow_flee";
                    else
                    {
                        float k = (t - 0.15f) / 0.85f;
                        transform.position = Vector3.Lerp(to, from, k * k);
                        frame = FlyFrame(dt);
                    }
                    float a = Mathf.Clamp01((1f - t) * 3f);
                    Sr.color = new Color(1, 1, 1, a);
                    if (t >= 1f) { Destroy(gameObject); return; }
                    break;
                }
            }
            Sr.sprite = SpriteDB.Get(frame);

            // shadow on the ground under the bird
            Vector3 ground = state == S.FlyIn ? Vector3.Lerp(from, to, Mathf.Clamp01(t)) : state == S.Flee ? to : transform.position;
            float h = Mathf.Max(0f, transform.position.y - ground.y);
            shadow.transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y, ground.y + 0.02f), 0);
            float s = Mathf.Lerp(0.42f, 0.15f, Mathf.Clamp01(h / 4f));
            shadow.transform.localScale = new Vector3(s, s * 0.45f, 1f);
            shadow.color = new Color(0, 0, 0, state == S.Flee ? 0.18f * Mathf.Clamp01(1f - t) : 0.2f);

            bool flying = state != S.Perch;
            Sr.sortingOrder = flying ? 14000 : IsoGrid.SortOrder(transform.position.y, 6);
            shadow.sortingOrder = IsoGrid.SortOrder(ground.y, 2);
        }

        private string FlyFrame(float dt)
        {
            frameT += dt;
            if (frameT > 0.08f) { frameT = 0; flyFrame = (flyFrame + 1) % 4; }
            int[] seq = { 1, 2, 3, 2 };
            return "crow_fly_" + seq[flyFrame];
        }

        private void Flee()
        {
            if (state == S.Flee) return;
            state = S.Flee;
            to = transform.position;
            float side = Random.value < 0.5f ? -1f : 1f;
            from = to + new Vector3(side * 9f, 7f, 0);
            Sr.flipX = side > 0f; // the flee frame faces left
            t = 0f;
            dur = 1.6f;
        }

        /// <summary>Chase the crow away. Returns false when it was already leaving.</summary>
        public bool Scare(string by)
        {
            if (state == S.Flee) return false;
            Flee();
            GameManager gm = GameManager.I;
            gm.AddCoins(Defs.CrowCoins);
            gm.AddXp(1);
            gm.Data.AddStat("crow", 1);
            UIManager.I.FloatText(transform.position + Vector3.up * 0.6f, "+" + Defs.CrowCoins, "hud_coin");
            Fx.Burst(transform.position + Vector3.up * 0.3f, new Color(0.18f, 0.18f, 0.24f), 7, ProcSprites.Diamond, 2.2f, 1.6f, -3f, 0.14f, 0.9f, 360f);
            Haptics.Light();
            GameEvents.Raise("crow", by);
            return true;
        }

        public override int HitOrder => 16000;

        public override bool HitTest(Vector2 p)
        {
            if (state == S.Flee || Sr == null || Sr.sprite == null) return false;
            Bounds b = Sr.bounds;
            // generous touch target: crows are small
            return Mathf.Abs(p.x - b.center.x) <= Mathf.Max(0.6f, b.extents.x) && Mathf.Abs(p.y - b.center.y) <= Mathf.Max(0.6f, b.extents.y);
        }

        public override void OnTap() => Scare("tap");
    }

    /// <summary>B2.2 – a dog or cat that roams the farm on its own (pathfinding over walkable land),
    /// naps at night near the farmhouse; the dog runs to chase crows anywhere on the farm.</summary>
    public class PetView : Interactable
    {
        public PetDef Def;
        private SpriteRenderer shadow;
        private string pose;
        private float poseT, animT, idleT, bobT, chaseCooldown;
        private bool frameB, faceLeft, faceUp;
        private CrowView chase;
        private List<Vector3> path;
        private int pathIndex;
        private bool goingToBed;

        private const float WalkSpeed = 1.25f, RunSpeed = 4.6f;

        public void Init(PetDef def, Vector3 pos)
        {
            Def = def;
            transform.position = pos;
            Sr = MakeSprite(def.Frame("sit_se"), def.scale);
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(transform, false);
            sh.transform.localScale = new Vector3(0.6f, 0.25f, 1f);
            shadow = sh.AddComponent<SpriteRenderer>();
            shadow.sprite = ProcSprites.SoftCircle;
            shadow.color = new Color(0, 0, 0, 0.22f);
            idleT = Random.Range(1f, 4f);
        }

        private string HappyPose => Def.id == "cat" ? "stretch" : "happy";
        private bool Night => DayNight.I != null && DayNight.I.NightFactor > 0.6f;
        private Vector2Int Cell => IsoGrid.ToCell(transform.position);

        /// <summary>Walkable cell near the farmhouse where pets sleep.</summary>
        private Vector2Int BedCell()
        {
            PlaceView home = WorldMap.I.Places.Find(p => p.Def.id == "farmhouse");
            Vector2Int near = home != null ? new Vector2Int(home.D.gx + home.Def.size, home.D.gy + home.Def.size - 1 + (Def.id == "dog" ? 0 : 1)) : new Vector2Int(6, 7);
            return WorldMap.I.FindFreeCell(near, 1, true);
        }

        private Vector2Int RandomCell()
        {
            Vector2Int here = Cell;
            for (int i = 0; i < 25; i++)
            {
                var c = new Vector2Int(here.x + Random.Range(-7, 8), here.y + Random.Range(-7, 8));
                if (WorldMap.I.Walkable(c) && WorldMap.I.OccupantAt(c) == null) return c;
            }
            return here;
        }

        private bool PathTo(Vector2Int goal)
        {
            Vector2Int start = Cell;
            if (start == goal) { path = new List<Vector3> { IsoGrid.CellCenter(goal) }; pathIndex = 0; return true; }
            List<Vector2Int> cells = Pathfinder.Find(start, goal, WorldMap.I.Walkable);
            if (cells == null || cells.Count == 0) { path = null; return false; }
            path = new List<Vector3>();
            // a little jitter so the pet does not walk exactly on the cell centres
            foreach (Vector2Int c in cells) path.Add(IsoGrid.CellCenter(c) + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.1f, 0.1f), 0));
            pathIndex = 0;
            return true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (WorldMap.I == null) return;

            // the dog spots perched crows anywhere nearby and runs to chase them
            if (chaseCooldown > 0f) chaseCooldown -= dt;
            if (chase != null && !chase.Active) { chase = null; path = null; }
            if (Def.id == "dog" && chase == null && chaseCooldown <= 0f && CrowSystem.I != null)
            {
                chaseCooldown = 0.6f;
                foreach (CrowView c in CrowSystem.I.Crows)
                    if (c != null && c.Perched && Vector2.Distance(c.transform.position, transform.position) < Defs.DogChaseRadius)
                    {
                        if (PathTo(c.Plot != null ? c.Plot.Cell : IsoGrid.ToCell(c.transform.position))) { chase = c; pose = null; goingToBed = false; }
                        break;
                    }
            }

            if (poseT > 0f) poseT -= dt;
            else pose = null;

            bool walking = false;
            float speed = chase != null ? RunSpeed : WalkSpeed;
            if (path != null && pose == null)
            {
                Vector3 wp = pathIndex < path.Count ? path[pathIndex] : transform.position;
                if (chase != null && pathIndex >= path.Count - 1) wp = chase.transform.position;
                Vector3 to = wp - transform.position;
                float step = speed * dt;
                if (to.magnitude <= step)
                {
                    transform.position = wp;
                    pathIndex++;
                    if (pathIndex >= path.Count && chase == null)
                    {
                        path = null;
                        idleT = goingToBed ? 999f : Random.Range(3f, 8f);
                    }
                }
                else
                {
                    transform.position += to.normalized * step;
                    if (Mathf.Abs(to.x) > 0.02f) faceLeft = to.x < 0f;
                    faceUp = to.y > Mathf.Abs(to.x) * 0.35f;
                    walking = true;
                }
                animT += dt * (chase != null ? 1.8f : 1f);
                if (animT > 0.17f) { animT = 0; frameB = !frameB; }

                if (chase != null && Vector2.Distance(transform.position, chase.transform.position) < 0.7f)
                {
                    if (chase.Scare("dog")) UIManager.I.FloatText(transform.position + Vector3.up * 1.0f, L.T("Gâu!"), null);
                    chase = null;
                    path = null;
                    pose = HappyPose;
                    poseT = 1.2f;
                    idleT = Random.Range(2f, 4f);
                }
            }
            else if (pose == null)
            {
                faceUp = false;
                if (Night)
                {
                    // head home to sleep
                    if (!goingToBed)
                    {
                        goingToBed = true;
                        Vector2Int bed = BedCell();
                        if (Cell != bed && PathTo(bed)) { }
                        else path = null;
                    }
                }
                else
                {
                    if (goingToBed) { goingToBed = false; idleT = Random.Range(1f, 3f); } // morning: wake up
                    idleT -= dt;
                    if (idleT <= 0f)
                    {
                        if (Random.value < 0.25f) { pose = HappyPose; poseT = 1.3f; idleT = 1.5f; }
                        else if (!PathTo(RandomCell())) idleT = 2f;
                    }
                }
            }

            string frame;
            bool sleeping = !walking && pose == null && Night && path == null;
            if (pose != null) frame = Def.Frame(pose);
            else if (walking) frame = Def.Frame((frameB ? "walk_b_" : "walk_a_") + (faceUp ? "nw" : "se"));
            else if (sleeping) frame = Def.Frame("sleep");
            else frame = Def.Frame("sit_se");

            Sr.sprite = SpriteDB.Get(frame);
            // front frames face right, back frames face up-left
            if (frame.EndsWith("_nw")) Sr.flipX = !faceLeft;
            else if (frame.EndsWith("sleep")) Sr.flipX = false;
            else Sr.flipX = faceLeft;
            bobT += dt * (walking ? (chase != null ? 22f : 12f) : 0f);
            Sr.transform.localPosition = new Vector3(0, walking ? Mathf.Abs(Mathf.Sin(bobT)) * 0.04f : 0f, 0);
            Sr.sortingOrder = IsoGrid.SortOrder(transform.position.y, 1);
            shadow.sortingOrder = Sr.sortingOrder - 1;
        }

        /// <summary>Put the pet back on walkable ground (spawn, or after the map changed under it).</summary>
        public void Relocate(Vector2Int near)
        {
            transform.position = IsoGrid.CellCenter(WorldMap.I.FindFreeCell(near, 1, true));
            path = null;
        }

        public override int HitOrder => base.HitOrder + 550;

        public override void OnTap()
        {
            Punch();
            pose = HappyPose;
            poseT = 1.4f;
            path = null;
            idleT = Random.Range(2f, 4f);
            Fx.Sparkle(transform.position, new Color(1f, 0.6f, 0.75f), 8);
            Haptics.Light();
            UIManager.I.ShowTooltip(transform.position + Vector3.up * 1.1f, L.T(Def.name) + "\n" + L.T(Def.perk));
        }
    }
}
