using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    public class FarmTask
    {
        public Interactable owner;
        public Func<Vector3> target;
        public Func<bool> canDo;
        public Action doIt;
        public string pose = "hoe";
        public float duration = 0.35f;
        public float reach = 1.05f;
        public bool walkOnly;
    }

    /// <summary>Grid A* (8-way, no corner cutting).</summary>
    public static class Pathfinder
    {
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        public static List<Vector2Int> Find(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> walkable)
        {
            int n = IsoGrid.Size;
            var g = new float[n, n];
            var from = new Vector2Int[n, n];
            var closed = new bool[n, n];
            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++) g[x, y] = float.MaxValue;

            bool Ok(Vector2Int c) => IsoGrid.InBounds(c) && (c == goal || c == start || walkable(c));

            var open = new List<Vector2Int> { start };
            g[start.x, start.y] = 0;
            int guard = 0;
            while (open.Count > 0 && guard++ < 4000)
            {
                int bi = 0;
                float bf = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    Vector2Int o = open[i];
                    float f = g[o.x, o.y] + Vector2Int.Distance(o, goal);
                    if (f < bf) { bf = f; bi = i; }
                }
                Vector2Int cur = open[bi];
                open.RemoveAt(bi);
                if (cur == goal)
                {
                    var path = new List<Vector2Int>();
                    for (Vector2Int c = goal; c != start; c = from[c.x, c.y]) path.Add(c);
                    path.Reverse();
                    return path;
                }
                if (closed[cur.x, cur.y]) continue;
                closed[cur.x, cur.y] = true;
                foreach (Vector2Int d in Dirs)
                {
                    Vector2Int nb = cur + d;
                    if (!Ok(nb) || closed[nb.x, nb.y]) continue;
                    if (d.x != 0 && d.y != 0 && (!Ok(new Vector2Int(cur.x + d.x, cur.y)) || !Ok(new Vector2Int(cur.x, cur.y + d.y)))) continue;
                    float cost = g[cur.x, cur.y] + (d.x != 0 && d.y != 0 ? 1.414f : 1f);
                    if (cost < g[nb.x, nb.y])
                    {
                        g[nb.x, nb.y] = cost;
                        from[nb.x, nb.y] = cur;
                        open.Add(nb);
                    }
                }
            }
            return null;
        }
    }

    /// <summary>The player avatar: joystick movement, auto-walk to tasks, action poses.</summary>
    public class FarmerController : MonoBehaviour
    {
        public static FarmerController I { get; private set; }

        public const float Speed = 3.4f;
        private SpriteRenderer sr;
        private readonly List<FarmTask> queue = new List<FarmTask>();
        private FarmTask current;
        private List<Vector3> waypoints;
        private int wpIndex;
        private float actionTimer;
        private string actionPose;
        private float animTimer;
        private bool frameB;
        private bool faceLeft;
        private bool faceUp;
        private bool moving;

        public Vector2Int Cell => IsoGrid.ToCell(transform.position);
        public bool Busy => current != null || queue.Count > 0;

        public void Init(Vector3 pos)
        {
            I = this;
            transform.position = pos;
            var go = new GameObject("Sprite");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 0.8f;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDB.Get("farmer_idle_se");

            // soft shadow
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(transform, false);
            sh.transform.localScale = new Vector3(0.8f, 0.35f, 1f);
            var ssr = sh.AddComponent<SpriteRenderer>();
            ssr.sprite = ProcSprites.SoftCircle;
            ssr.color = new Color(0, 0, 0, 0.25f);
            ssr.sortingOrder = IsoGrid.GroundBase + 200;
            shadow = ssr;

            if (!WorldMap.I.Walkable(Cell))
                transform.position = IsoGrid.CellCenter(WorldMap.I.FindFreeCell(Cell, 1, true));
        }

        private SpriteRenderer shadow;

        public void Enqueue(FarmTask t)
        {
            queue.Add(t);
        }

        /// <summary>Walk to a point (tap on empty ground).</summary>
        public void WalkTo(Vector3 p)
        {
            CancelAll();
            queue.Add(new FarmTask { walkOnly = true, target = () => p, canDo = () => true, reach = 0.15f });
        }

        public void CancelAll()
        {
            if (current != null && current.owner != null) current.owner.Pending = false;
            foreach (FarmTask t in queue)
                if (t.owner != null) t.owner.Pending = false;
            queue.Clear();
            current = null;
            waypoints = null;
            actionTimer = 0;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector2 joy = Joystick.Value;
            moving = false;

            if (joy.sqrMagnitude > 0.02f)
            {
                if (Busy && actionTimer <= 0f) CancelAll();
                if (actionTimer <= 0f) MoveBy(new Vector3(joy.x, joy.y * 0.75f, 0) * Speed * dt);
            }
            else if (actionTimer > 0f)
            {
                actionTimer -= dt;
                if (actionTimer <= 0f) FinishAction();
            }
            else
            {
                ProcessTasks(dt);
            }

            Animate(dt);
            sr.sortingOrder = IsoGrid.SortOrder(transform.position.y, 1);
            shadow.sortingOrder = sr.sortingOrder - 1;
        }

        private void ProcessTasks(float dt)
        {
            if (current == null)
            {
                if (queue.Count == 0) return;
                current = queue[0];
                queue.RemoveAt(0);
                if ((current.owner == null && !current.walkOnly) || !current.canDo())
                {
                    if (current.owner != null) current.owner.Pending = false;
                    current = null;
                    return;
                }
                BuildPath();
                if (waypoints == null)
                {
                    UIManager.I.Toast(L.T("Không có đường đi tới đó!"));
                    if (current.owner != null) current.owner.Pending = false;
                    current = null;
                    return;
                }
            }

            if (current.owner == null && !current.walkOnly) { current = null; return; }
            Vector3 target = current.target();
            if (Vector2.Distance(transform.position, target) <= current.reach)
            {
                StartAction(target);
                return;
            }

            if (waypoints == null || wpIndex >= waypoints.Count)
            {
                // target moved (animals): re-path
                BuildPath();
                if (waypoints == null || waypoints.Count == 0)
                {
                    StartAction(target);
                    return;
                }
            }

            Vector3 wp = waypoints[wpIndex];
            Vector3 to = wp - transform.position;
            float step = Speed * dt;
            if (to.magnitude <= step)
            {
                transform.position = wp;
                wpIndex++;
            }
            else
            {
                transform.position += to.normalized * step;
                moving = true;
                Face(to);
            }
        }

        private void BuildPath()
        {
            Vector2Int goal = IsoGrid.ToCell(current.target());
            List<Vector2Int> cells = Pathfinder.Find(Cell, goal, WorldMap.I.Walkable);
            if (cells == null) { waypoints = null; return; }
            waypoints = new List<Vector3>();
            foreach (Vector2Int c in cells) waypoints.Add(IsoGrid.CellCenter(c));
            wpIndex = 0;
        }

        private void StartAction(Vector3 target)
        {
            if (current.walkOnly)
            {
                current = null;
                waypoints = null;
                return;
            }
            actionPose = current.pose;
            actionTimer = current.duration;
            actionDuration = current.duration;
            impacts = 0;
            if (Mathf.Abs(target.x - transform.position.x) > 0.05f) faceLeft = target.x < transform.position.x;
            faceUp = false; // action poses only exist facing the camera
            waypoints = null;
        }

        private float actionDuration;
        private int impacts;

        /// <summary>
        /// Procedural action animation on top of the single action frame:
        /// hoe / axe = wind-up (tool raised, body leaning back) then a fast strike with squash, dust and a haptic tick;
        /// water = gentle tilt; harvest = happy hop. If a dedicated "_up" frame exists (e.g. farmer_hoe_up_se) it is used
        /// for the wind-up instead of tilting the strike frame.
        /// </summary>
        private string AnimateAction(out float rot, out float sx, out float sy, out float lift)
        {
            rot = 0f; sx = 1f; sy = 1f; lift = 0f;
            string frame = "farmer_" + actionPose + "_se";
            float el = actionDuration - actionTimer;
            float sign = faceLeft ? -1f : 1f;
            switch (actionPose)
            {
                case "hoe":
                case "axe":
                {
                    bool hoe = actionPose == "hoe";
                    const float swing = 0.36f;
                    int idx = Mathf.FloorToInt(el / swing);
                    float k = (el - idx * swing) / swing;
                    string upFrame = "farmer_" + actionPose + "_up_se";
                    bool hasUp = SpriteDB.Exists(upFrame);
                    float back = hasUp ? -6f : (hoe ? -26f : -12f);   // lean-back angle at the top of the swing
                    float strike = hoe ? 4f : 16f;                       // forward angle at impact
                    if (k < 0.5f)
                    {
                        float u = Mathf.SmoothStep(0f, 1f, k / 0.5f);
                        if (hasUp) frame = upFrame;
                        rot = back * u;
                        lift = (hasUp ? 0.02f : 0.07f) * u;
                    }
                    else
                    {
                        float d = (k - 0.5f) / 0.5f;
                        float fast = Mathf.Min(1f, d * 5f);              // the strike itself is very quick
                        rot = Mathf.Lerp(back, strike, fast);
                        if (d > 0.5f) rot = Mathf.Lerp(strike, 0f, (d - 0.5f) / 0.5f);
                        if (d > 0.18f && d < 0.55f)
                        {
                            float q = 1f - Mathf.Abs(d - 0.3f) / 0.25f;
                            sx = 1f + 0.07f * q;
                            sy = 1f - 0.07f * q;
                        }
                        if (d > 0.2f && idx >= impacts)
                        {
                            impacts = idx + 1;
                            Vector3 tip = transform.position + new Vector3(sign * (hoe ? 0.55f : 0.65f), 0.05f, 0);
                            Fx.Dust(tip, hoe ? 6 : 8);
                            Haptics.Light();
                        }
                    }
                    break;
                }
                case "water":
                    rot = 5f + Mathf.Sin(el * 14f) * 3f;
                    break;
                case "harvest_happy":
                {
                    float k = Mathf.Clamp01(el / Mathf.Max(0.1f, actionDuration));
                    lift = Mathf.Sin(k * Mathf.PI) * 0.12f;
                    sy = 1f + Mathf.Sin(k * Mathf.PI) * 0.05f;
                    sx = 1f - Mathf.Sin(k * Mathf.PI) * 0.03f;
                    break;
                }
            }
            return frame;
        }

        private void FinishAction()
        {
            FarmTask t = current;
            current = null;
            actionPose = null;
            if (t == null) return;
            if (t.owner != null)
            {
                t.owner.Pending = false;
                if (t.canDo()) t.doIt();
            }
        }

        private void MoveBy(Vector3 delta)
        {
            Vector3 p = transform.position;
            Vector3 n = p + delta;
            if (WorldMap.I.Walkable(IsoGrid.ToCell(n))) transform.position = n;
            else if (WorldMap.I.Walkable(IsoGrid.ToCell(new Vector3(n.x, p.y, 0)))) transform.position = new Vector3(n.x, p.y, 0);
            else if (WorldMap.I.Walkable(IsoGrid.ToCell(new Vector3(p.x, n.y, 0)))) transform.position = new Vector3(p.x, n.y, 0);
            else return;
            moving = true;
            Face(delta);
        }

        /// <summary>Front frames ("_se") face down-right, back frames ("_sw") show the farmer from behind walking up-left.</summary>
        private void Face(Vector3 dir)
        {
            if (Mathf.Abs(dir.x) > 0.0001f) faceLeft = dir.x < 0f;
            if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x) * 0.2f) faceUp = dir.y > 0f;
        }

        // ------------------------------------------------------------------ walk cycle
        // The atlas only has two stride poses (walk_a / walk_b) plus idle. A natural gait is built from them:
        //   contact A → passing (legs together, body highest) → contact B → passing …
        // The cycle advances with the DISTANCE walked (not time), so each foot plants where it lands instead of
        // sliding; the body bobs down on each contact, rises while passing and leans a little into the motion.
        private const float StepLength = 0.62f;   // world units per step (one foot plant)
        private static readonly string[] Walk6Order = { "1", "3", "2", "4", "6", "5" };
        // Front view has a dedicated left-leg contact frame ("L"): right step 1→3→2, left step L→L→2.
        private static readonly string[] Walk6OrderLR = { "1", "3", "2", "L", "L", "2" };
        // Matched pair of stride frames (right leg / left leg forward): alternate one per step.
        private static readonly string[] Walk6OrderRL = { "R", "R", "R", "L", "L", "L" };
        private float walkPhase;                   // in steps
        private Vector3 lastAnimPos;

        private string WalkFrame(string dir, out float rot, out float sx, out float sy, out float lift)
        {
            float dist = Vector3.Distance(transform.position, lastAnimPos);
            walkPhase += Mathf.Min(dist, 0.5f) / StepLength;
            // Dedicated 6-frame walk art (Characters/Farmer_Walk_6x2.png): contact → down → passing, twice per cycle.
            if (SpriteDB.Exists("farmer_walk6_" + dir + "_1"))
            {
                int idx = Mathf.FloorToInt(walkPhase * 3f) % 6;
                float inS = (walkPhase * 3f) % 3f / 3f;        // 0..1 inside one step
                float b = Mathf.Sin(inS * Mathf.PI);
                lift = 0.02f + b * 0.035f;
                sx = 1f; sy = 1f + b * 0.015f;
                rot = 2f;
                var order = SpriteDB.Exists("farmer_walk6_" + dir + "_R") ? Walk6OrderRL
                          : SpriteDB.Exists("farmer_walk6_" + dir + "_L") ? Walk6OrderLR : Walk6Order;
                return "farmer_walk6_" + dir + "_" + order[idx];
            }
            float step = walkPhase % 2f;                  // 0..2 = one full cycle (two steps)
            float inStep = step % 1f;                     // 0 = foot lands, 0.5 = passing, 1 = next foot lands
            bool second = step >= 1f;
            // contact pose around the landing, passing pose (idle frame = legs together) in the middle
            string pose = inStep < 0.3f || inStep > 0.82f ? (second ? "walk_b_" : "walk_a_") : "idle_";
            if (inStep > 0.82f) pose = second ? "walk_a_" : "walk_b_";   // next foot reaching forward
            // bob: lowest at contact, highest when passing
            float bob = Mathf.Sin(inStep * Mathf.PI);
            lift = 0.035f + bob * 0.06f;
            float impact = Mathf.Max(0f, 1f - inStep / 0.18f);           // quick squash right after landing
            sx = 1f + impact * 0.035f;
            sy = 1f - impact * 0.045f + bob * 0.02f;
            // slight forward lean + side-to-side sway between steps
            rot = 3.5f + (second ? 1f : -1f) * Mathf.Sin(inStep * Mathf.PI) * 1.5f;
            return "farmer_" + pose + dir;
        }

        private void Animate(float dt)
        {
            string frame;
            bool back = false;
            float rot = 0f, sx = 1f, sy = 1f, lift = 0f;
            if (actionTimer > 0f && actionPose != null) frame = AnimateAction(out rot, out sx, out sy, out lift);
            else
            {
                back = faceUp;
                string dir = back ? "sw" : "se";
                if (moving) frame = WalkFrame(dir, out rot, out sx, out sy, out lift);
                else
                {
                    walkPhase = 0f;
                    frame = "farmer_idle_" + dir;
                }
            }
            lastAnimPos = transform.position;
            Sprite s = SpriteDB.Get(frame);
            if (s != null) sr.sprite = s;
            sr.flipX = back ? !faceLeft : faceLeft;
            // rot > 0 leans toward the facing direction (clockwise when facing right)
            Transform st = sr.transform;
            st.localRotation = Quaternion.Euler(0, 0, -rot * (faceLeft ? -1f : 1f));
            st.localScale = new Vector3(0.8f * sx, 0.8f * sy, 1f);
            st.localPosition = new Vector3(0, lift, 0);
        }
    }
}
