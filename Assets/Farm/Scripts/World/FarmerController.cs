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
                    UIManager.I.Toast("Không có đường đi tới đó!");
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
            if (Mathf.Abs(target.x - transform.position.x) > 0.05f) faceLeft = target.x < transform.position.x;
            faceUp = false; // action poses only exist facing the camera
            waypoints = null;
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

        private void Animate(float dt)
        {
            string frame;
            bool back = false;
            if (actionTimer > 0f && actionPose != null) frame = "farmer_" + actionPose + "_se";
            else
            {
                back = faceUp;
                string dir = back ? "sw" : "se";
                if (moving)
                {
                    animTimer += dt;
                    if (animTimer > 0.13f) { animTimer = 0f; frameB = !frameB; }
                    frame = (frameB ? "farmer_walk_b_" : "farmer_walk_a_") + dir;
                }
                else frame = "farmer_idle_" + dir;
            }
            Sprite s = SpriteDB.Get(frame);
            if (s != null) sr.sprite = s;
            sr.flipX = back ? !faceLeft : faceLeft;
        }
    }
}
