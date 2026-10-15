using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>Ghost preview + grid snapping for buying or moving buildings/plots.</summary>
    public class PlacementController : MonoBehaviour
    {
        public static PlacementController I { get; private set; }

        public bool Active { get; private set; }
        public bool Grabbed { get; private set; }

        private PlaceDef def;
        private int size;
        private Interactable moving;
        private Action<int, int> onConfirm;
        private int gx, gy;
        private SpriteRenderer ghost;
        private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        private Vector2 grabOffset;

        public void Init()
        {
            I = this;
            var g = new GameObject("Ghost");
            g.transform.SetParent(transform, false);
            ghost = g.AddComponent<SpriteRenderer>();
            ghost.sortingOrder = IsoGrid.OverlayOrder - 10;
            g.SetActive(false);
        }

        public bool Valid => WorldMap.I.CanPlace(size, gx, gy, moving);

        /// <summary>The decoration being moved, if any (the placement bar then offers store / sell).</summary>
        public PlaceView MovingDecor => moving is PlaceView pv && pv.IsDecor ? pv : null;

        public void BeginNew(PlaceDef d, Action<int, int> confirm)
        {
            Cancel();
            def = d;
            size = d.size;
            moving = null;
            onConfirm = confirm;
            ghost.sprite = SpriteDB.Get(d.kind == PlaceKind.Plot ? "ground_soil_tilled_dry" : d.sprite);
            ghost.transform.localScale = Vector3.one * (d.kind == PlaceKind.Plot ? 1f : d.scale);
            Vector2Int near = IsoGrid.ToCell(CameraController.I.transform.position);
            Vector2Int c = WorldMap.I.FindFreeCell(near, size);
            Begin(c.x, c.y);
        }

        public void BeginMove(Interactable e)
        {
            Cancel();
            moving = e;
            onConfirm = null;
            if (e is PlaceView pv)
            {
                def = pv.Def;
                size = def.size;
                ghost.sprite = pv.Sr.sprite;
                ghost.transform.localScale = Vector3.one * def.scale;
                Begin(pv.D.gx, pv.D.gy);
            }
            else if (e is PlotView plot)
            {
                def = Defs.Place("plot");
                size = 1;
                ghost.sprite = plot.Sr.sprite;
                ghost.transform.localScale = plot.Sr.transform.localScale;
                Begin(plot.D.gx, plot.D.gy);
            }
            e.Sr.enabled = false;
        }

        private void Begin(int x, int y)
        {
            Active = true;
            ghost.gameObject.SetActive(true);
            EnsureTiles(size * size);
            SetCell(x, y);
            UIManager.I.ShowPlacementBar(true, moving == null ? L.T(def.name) : L.T("Di chuyển: ") + L.T(def.name));
            CameraController.I.FocusOn(ghost.transform.position);
        }

        private void EnsureTiles(int n)
        {
            while (tiles.Count < n)
            {
                var t = new GameObject("Tile");
                t.transform.SetParent(transform, false);
                var sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = ProcSprites.Diamond;
                sr.sortingOrder = IsoGrid.OverlayOrder - 20;
                tiles.Add(sr);
            }
            for (int i = 0; i < tiles.Count; i++) tiles[i].gameObject.SetActive(i < n);
        }

        private void SetCell(int x, int y)
        {
            gx = Mathf.Clamp(x, 0, IsoGrid.Size - size);
            gy = Mathf.Clamp(y, 0, IsoGrid.Size - size);
            Vector3 center = PlaceView.CenterOf(def, gx, gy);
            ghost.transform.position = center;
            bool ok = Valid;
            ghost.color = ok ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 0.55f, 0.55f, 0.7f);
            Color tc = ok ? new Color(0.3f, 1f, 0.35f, 0.55f) : new Color(1f, 0.25f, 0.2f, 0.55f);
            int i = 0;
            for (int dx = 0; dx < size; dx++)
            for (int dy = 0; dy < size; dy++)
            {
                tiles[i].transform.position = IsoGrid.ToWorld(gx + dx, gy + dy);
                tiles[i].color = tc;
                i++;
            }
            UIManager.I.SetPlacementValid(ok);
        }

        public void PointerDown(Vector2 world)
        {
            Vector3 c = PlaceView.CenterOf(def, gx, gy);
            Grabbed = Vector2.Distance(world, c) < 1.2f * size;
            grabOffset = (Vector2)c - world;
        }

        public void Drag(Vector2 world)
        {
            Vector2 center = world + grabOffset;
            float o = (size - 1) * 0.5f;
            Vector2 g = IsoGrid.ToGrid(center);
            int x = Mathf.RoundToInt(g.x - o), y = Mathf.RoundToInt(g.y - o);
            if (x != gx || y != gy) SetCell(x, y);
        }

        public void PointerUp() => Grabbed = false;

        public void TapAt(Vector2 world)
        {
            float o = (size - 1) * 0.5f;
            Vector2 g = IsoGrid.ToGrid(world);
            SetCell(Mathf.RoundToInt(g.x - o), Mathf.RoundToInt(g.y - o));
        }

        public void Confirm()
        {
            if (!Active) return;
            if (!Valid)
            {
                UIManager.I.Toast(L.T("Không thể đặt ở đây!"));
                return;
            }
            if (moving != null)
            {
                WorldMap.I.MoveEntity(moving, gx, gy);
                moving.Sr.enabled = true;
                moving = null;
            }
            else onConfirm?.Invoke(gx, gy);
            End();
        }

        public void Cancel()
        {
            if (moving != null && moving.Sr != null) moving.Sr.enabled = true;
            moving = null;
            if (Active) End();
        }

        private void End()
        {
            Active = false;
            Grabbed = false;
            ghost.gameObject.SetActive(false);
            foreach (SpriteRenderer t in tiles) t.gameObject.SetActive(false);
            UIManager.I.ShowPlacementBar(false, null);
        }
    }
}
