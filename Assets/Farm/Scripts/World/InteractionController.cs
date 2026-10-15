using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Farm
{
    /// <summary>
    /// Routes touch / mouse input: tap on world objects, drag-to-repeat actions (Hay Day style),
    /// drag on empty ground to pan, pinch / scroll to zoom, and placement-mode dragging.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController I { get; private set; }
        public static string SelectedSeed;

        public bool BuildMode { get; private set; }

        private bool pointerDown;
        private bool overUI;
        private bool dragging;
        private Vector2 startPos, lastPos;
        private string paintKey;
        private bool pinching;
        private float pinchStartDist, pinchStartZoom;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private PointerEventData ped;

        private float DragThreshold => Mathf.Max(12f, Screen.dpi > 0 ? Screen.dpi * 0.08f : 12f);

        public void Init()
        {
            I = this;
        }

        public void SetBuildMode(bool on)
        {
            BuildMode = on;
            if (on) SelectedSeed = null;
        }

        private void Update()
        {
            if (UIManager.I == null || CameraController.I == null) return;
            if (UIManager.I.ModalOpen)
            {
                pointerDown = false;
                pinching = false;
                return;
            }

            var touches = ETouch.activeTouches;
            if (touches.Count >= 2)
            {
                HandlePinch(touches[0].screenPosition, touches[1].screenPosition);
                return;
            }
            if (pinching)
            {
                pinching = false;
                pointerDown = false; // swallow the rest of this gesture
                return;
            }

            if (touches.Count == 1)
            {
                ETouch t = touches[0];
                switch (t.phase)
                {
                    case ETouchPhase.Began:
                        Down(t.screenPosition);
                        break;
                    case ETouchPhase.Moved:
                    case ETouchPhase.Stationary: Move(t.screenPosition); break;
                    case ETouchPhase.Ended: Up(t.screenPosition); break;
                    case ETouchPhase.Canceled: pointerDown = false; break;
                }
                return;
            }

            Mouse m = Mouse.current;
            if (m == null) return;
            Vector2 mp = m.position.ReadValue();
            // press and release can land in the same frame (very quick taps): handle both
            bool pressed = m.leftButton.wasPressedThisFrame;
            if (pressed) Down(mp);
            else if (m.leftButton.isPressed) Move(mp);
            if (m.leftButton.wasReleasedThisFrame) Up(mp);

            float scroll = m.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !IsOverUI(mp))
                CameraController.I.ZoomBy(scroll > 0 ? 0.9f : 1.1f);
        }

        private void HandlePinch(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            if (!pinching)
            {
                if (IsOverUI(a) || IsOverUI(b)) return;
                pinching = true;
                dragging = false;
                pinchStartDist = Mathf.Max(1f, d);
                pinchStartZoom = CameraController.I.Zoom;
                return;
            }
            float target = pinchStartZoom * pinchStartDist / Mathf.Max(1f, d);
            CameraController.I.ZoomBy(target / CameraController.I.Zoom);
        }

        public bool IsOverUI(Vector2 screen)
        {
            if (EventSystem.current == null) return false;
            if (ped == null) ped = new PointerEventData(EventSystem.current);
            ped.position = screen;
            uiHits.Clear();
            EventSystem.current.RaycastAll(ped, uiHits);
            return uiHits.Count > 0;
        }

        private Vector2 W(Vector2 screen) => CameraController.I.ScreenToWorld(screen);

        private void Down(Vector2 pos)
        {
            pointerDown = true;
            overUI = IsOverUI(pos);
            dragging = false;
            paintKey = null;
            startPos = lastPos = pos;
            if (!overUI && PlacementController.I.Active) PlacementController.I.PointerDown(W(pos));
        }

        private void Move(Vector2 pos)
        {
            if (!pointerDown || overUI) return;
            if (!dragging)
            {
                if (Vector2.Distance(pos, startPos) < DragThreshold) return;
                dragging = true;
                if (!PlacementController.I.Active && !BuildMode)
                {
                    Interactable hit = Pick(W(startPos));
                    paintKey = hit != null ? hit.DragKey() : null;
                    if (paintKey != null)
                    {
                        hit.OnDragApply(paintKey);
                        if (!(hit is PlotView)) UIManager.I.CloseSeedBarIfNotPlanting();
                    }
                }
            }

            if (PlacementController.I.Active && PlacementController.I.Grabbed)
                PlacementController.I.Drag(W(pos));
            else if (paintKey != null)
            {
                Interactable h = PickPaint(W(pos));
                if (h != null && h.DragKey() == paintKey) h.OnDragApply(paintKey);
            }
            else
            {
                CameraController.I.Pan(W(pos) - W(lastPos));
            }
            lastPos = pos;
        }

        private void Up(Vector2 pos)
        {
            if (!pointerDown) return;
            pointerDown = false;
            if (overUI) return;
            if (PlacementController.I.Active)
            {
                if (!dragging) PlacementController.I.TapAt(W(pos));
                PlacementController.I.PointerUp();
                return;
            }
            if (!dragging) Tap(W(pos));
        }

        private void Tap(Vector2 world)
        {
            Interactable hit = Pick(world);
            if (BuildMode)
            {
                if (hit != null && hit.Movable) PlacementController.I.BeginMove(hit);
                else if (hit is PlotView) UIManager.I.Toast(L.T("Chỉ di chuyển được ô đất trống."));
                else UIManager.I.Toast(L.T("Chạm vào công trình hoặc ô đất trống để di chuyển."));
                return;
            }
            if (hit != null)
            {
                if (!(hit is PlotView)) UIManager.I.CloseSeedBarIfNotPlanting();
                hit.OnTap();
                return;
            }
            if (UIManager.I.SeedBarOpen)
            {
                UIManager.I.CloseSeedBar();
                return;
            }
            Vector2Int c = IsoGrid.ToCell(world);
            if (WorldMap.I.Walkable(c)) FarmerController.I.WalkTo(world);
        }

        public static Interactable Pick(Vector2 world)
        {
            Interactable best = null;
            int bestOrder = int.MinValue;
            foreach (Interactable i in Interactable.All)
            {
                if (i == null || !i.isActiveAndEnabled || !i.HitTest(world)) continue;
                int o = i.HitOrder;
                if (o > bestOrder) { bestOrder = o; best = i; }
            }
            return best;
        }

        private static Interactable PickPaint(Vector2 world)
        {
            PlotView p = WorldMap.I.PlotAt(IsoGrid.ToCell(world));
            if (p != null) return p;
            return Pick(world);
        }
    }
}
