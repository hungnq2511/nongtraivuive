using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Farm
{
    public partial class UIManager
    {
        // ------------------------------------------------------------------ seed picker (Hay Day style)
        // Tap a tilled plot → a small seed picker pops up NEXT TO that plot (never on top of it).
        //   • drag a seed out of the picker over the field: every tilled plot under the finger is planted;
        //   • or tap a seed: the plot is planted and that seed stays selected (chip at the top) so the
        //     next taps / drags on tilled plots plant it too. ✕ on the chip or a tap on empty ground stops.
        private const int SeedPageSize = 8;
        private const float SeedCell = 124f;

        private RectTransform seedPicker, seedGrid;
        private CanvasGroup seedPickerGroup;
        private Text seedHint;
        private Button seedPrev, seedNext;
        private int seedPage;
        private PlotView seedAnchor;          // plot the picker was opened from (null = docked above the HUD buttons)
        private bool seedPickerOpen;
        private string seedPickerState;

        private GameObject seedChip;
        private Image seedChipIcon;
        private Text seedChipText;

        private RectTransform seedGhost;      // icon that follows the finger while dragging a seed
        private string draggingSeed;

        /// <summary>Picker shown, or a seed is selected for planting.</summary>
        public bool SeedBarOpen => seedPickerOpen || InteractionController.SelectedSeed != null;
        public bool SeedDragActive => draggingSeed != null;

        private void BuildSeedBar()
        {
            Image p = UIFactory.Panel(canvasRt, new Color(1f, 0.97f, 0.89f, 0.97f), "SeedPicker");
            seedPicker = p.rectTransform;
            seedPicker.anchorMin = seedPicker.anchorMax = Vector2.zero;
            seedPicker.sizeDelta = new Vector2(SeedCell * 4 + 120, SeedCell * 2 + 78);
            seedPickerGroup = p.gameObject.AddComponent<CanvasGroup>();
            seedHint = UIFactory.Label(seedPicker, L.T("Kéo hạt vào ô đất · hoặc chạm để chọn"), 22, UIFactory.Wood, TextAnchor.MiddleCenter, true);
            seedHint.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(SeedCell * 4 + 40, 36));

            seedGrid = UIFactory.Rect("Seeds", seedPicker);
            seedGrid.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(SeedCell * 4, SeedCell * 2));
            GridLayoutGroup g = seedGrid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(SeedCell - 8, SeedCell - 8);
            g.spacing = new Vector2(8, 8);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;
            g.childAlignment = TextAnchor.MiddleCenter;

            seedPrev = UIFactory.MakeButton(seedPicker, "<", UIFactory.Wood, () => { seedPage--; seedPickerState = null; }, 44);
            ((RectTransform)seedPrev.transform).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -14), new Vector2(44, 110));
            seedNext = UIFactory.MakeButton(seedPicker, ">", UIFactory.Wood, () => { seedPage++; seedPickerState = null; }, 44);
            ((RectTransform)seedNext.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, -14), new Vector2(44, 110));
            seedPicker.gameObject.SetActive(false);

            // "currently planting" chip
            Image chip = UIFactory.Panel(hudRoot, new Color(0.2f, 0.45f, 0.15f, 0.92f), "SeedChip");
            chip.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(520, 76));
            chip.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSeedPicker(null));
            seedChipIcon = UIFactory.Icon(chip.transform, "hud_seed", 64);
            seedChipIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(64, 64));
            seedChipText = UIFactory.Label(chip.transform, "", 26, Color.white, TextAnchor.MiddleLeft, true, true);
            seedChipText.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(84, 0), new Vector2(360, 70));
            Button stop = UIFactory.IconButton(chip.transform, "hud_close", null, 64, CloseSeedBar);
            ((RectTransform)stop.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(64, 64));
            seedChip = chip.gameObject;
            seedChip.SetActive(false);

            Image ghost = UIFactory.Icon(canvasRt, "hud_seed", 120);
            ghost.raycastTarget = false;
            seedGhost = ghost.rectTransform;
            seedGhost.anchorMin = seedGhost.anchorMax = Vector2.zero;
            seedGhost.gameObject.SetActive(false);
        }

        /// <summary>HUD "Hạt giống" button: picker docked above the bottom buttons.</summary>
        public void OpenSeedBar() => OpenSeedPicker(null);

        /// <summary>Open the picker next to a tilled plot (or docked when plot == null).</summary>
        public void OpenSeedPicker(PlotView plot)
        {
            if (PlacementController.I.Active) return;
            if (InteractionController.I.BuildMode) ToggleBuildMode();
            seedAnchor = plot;
            seedPickerOpen = true;
            seedPickerState = null;
            // start on the page of the last used seed
            List<CropDef> list = PickerSeeds();
            int idx = list.FindIndex(c => c.id == (InteractionController.SelectedSeed ?? lastSeed));
            seedPage = idx >= 0 ? idx / SeedPageSize : 0;
            seedPicker.gameObject.SetActive(true);
            seedPickerGroup.alpha = 1f;
            seedPicker.SetAsLastSibling();
            TickSeedPicker();
        }

        /// <summary>Close the picker and stop planting.</summary>
        public void CloseSeedBar()
        {
            CloseSeedPickerOnly();
            InteractionController.SelectedSeed = null;
            if (seedChip != null) seedChip.SetActive(false);
        }

        private void CloseSeedPickerOnly()
        {
            seedPickerOpen = false;
            seedAnchor = null;
            if (seedPicker != null) seedPicker.gameObject.SetActive(false);
        }

        public void CloseSeedBarIfNotPlanting()
        {
            if (SeedBarOpen) CloseSeedBar();
        }

        private string lastSeed = "wheat";

        /// <summary>Unlocked seeds, plus the next locked one as a teaser.</summary>
        private static List<CropDef> PickerSeeds()
        {
            int lvl = GameManager.I.Data.level;
            var list = new List<CropDef>();
            CropDef nextLocked = null;
            foreach (CropDef c in Defs.Crops)
            {
                if (c.unlock <= lvl) list.Add(c);
                else if (nextLocked == null || c.unlock < nextLocked.unlock) nextLocked = c;
            }
            if (nextLocked != null) list.Add(nextLocked);
            return list;
        }

        private void BuildSeedCards()
        {
            ClearChildren(seedGrid);
            List<CropDef> list = PickerSeeds();
            int pages = Mathf.Max(1, (list.Count + SeedPageSize - 1) / SeedPageSize);
            seedPage = Mathf.Clamp(seedPage, 0, pages - 1);
            seedPrev.gameObject.SetActive(seedPage > 0);
            seedNext.gameObject.SetActive(seedPage < pages - 1);
            int lvl = GameManager.I.Data.level;
            for (int i = seedPage * SeedPageSize; i < Mathf.Min(list.Count, (seedPage + 1) * SeedPageSize); i++)
            {
                CropDef c = list[i];
                bool locked = c.unlock > lvl;
                bool sel = InteractionController.SelectedSeed == c.id;
                Image bg = UIFactory.Panel(seedGrid, sel ? new Color(0.7f, 0.92f, 0.55f) : Color.white, "Seed_" + c.id);
                Image icon = UIFactory.Icon(bg.transform, c.Icon, 78);
                icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(78, 78));
                Text cost = UIFactory.Label(bg.transform, locked ? L.F("Cấp {0}", c.unlock) : L.F("{0} xu", c.seedCost), 20,
                    locked ? UIFactory.Red : UIFactory.Wood, TextAnchor.MiddleCenter, true);
                cost.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(SeedCell - 8, 28));
                if (locked)
                {
                    icon.color = new Color(0.35f, 0.35f, 0.35f, 0.8f);
                    bg.gameObject.AddComponent<Button>().onClick.AddListener(() => Toast(L.F("{0} mở khoá ở cấp {1}.", c.name, c.unlock)));
                    continue;
                }
                var item = bg.gameObject.AddComponent<SeedDragItem>();
                item.ui = this;
                item.seed = c;
            }
        }

        /// <summary>Called every frame from Update.</summary>
        private void TickSeedPicker()
        {
            // chip
            string sel = InteractionController.SelectedSeed;
            bool showChip = sel != null && !seedPickerOpen && draggingSeed == null;
            if (seedChip.activeSelf != showChip) seedChip.SetActive(showChip);
            if (showChip)
            {
                CropDef c = Defs.Crop(sel);
                seedChipIcon.sprite = SpriteDB.Get(c.Icon);
                seedChipText.text = L.F("Đang gieo: {0} ({1} xu)", c.name, c.seedCost);
            }

            if (!seedPickerOpen) return;
            string state = GameManager.I.Data.level + "|" + sel + "|" + seedPage;
            if (state != seedPickerState)
            {
                seedPickerState = state;
                BuildSeedCards();
            }
            PositionSeedPicker();
        }

        /// <summary>Put the picker beside its plot: above it when the plot is low on screen, below otherwise.</summary>
        private void PositionSeedPicker()
        {
            float scale = canvasRt.lossyScale.x <= 0 ? 1f : canvasRt.lossyScale.x;
            Vector2 canvas = canvasRt.rect.size;
            Vector2 size = seedPicker.sizeDelta;
            Vector2 pos;
            if (seedAnchor != null && seedAnchor)
            {
                Vector2 sp = CameraController.I.WorldToScreen(seedAnchor.transform.position) / scale;
                // plot diamond on screen, in canvas units
                float plotH = CameraController.I.WorldToScreen(seedAnchor.transform.position + Vector3.up * IsoGrid.HalfH).y / scale - sp.y;
                bool above = sp.y < canvas.y * 0.55f;
                float gap = Mathf.Max(plotH, 30f) + 40f;
                pos = above ? new Vector2(sp.x, sp.y + gap + size.y * 0.5f) : new Vector2(sp.x, sp.y - gap - size.y * 0.5f);
            }
            else pos = new Vector2(canvas.x * 0.5f, 230f + size.y * 0.5f);
            pos.x = Mathf.Clamp(pos.x, size.x * 0.5f + 20f, canvas.x - size.x * 0.5f - 20f);
            pos.y = Mathf.Clamp(pos.y, size.y * 0.5f + 170f, canvas.y - size.y * 0.5f - 110f);
            seedPicker.pivot = new Vector2(0.5f, 0.5f);
            seedPicker.anchoredPosition = pos;
        }

        // ---- picker callbacks (from SeedDragItem)
        internal void SeedTapped(CropDef c)
        {
            InteractionController.SelectedSeed = c.id;
            lastSeed = c.id;
            PlotView plot = seedAnchor;
            CloseSeedPickerOnly();
            if (plot != null && plot && plot.D.state == PlotState.Tilled) plot.OnDragApply("plant");
            else Toast(L.F("Đã chọn {0}: chạm hoặc kéo qua các ô đất đã cuốc để gieo.", c.name));
        }

        internal void SeedDragBegin(CropDef c, Vector2 screen)
        {
            draggingSeed = c.id;
            lastSeed = c.id;
            InteractionController.SelectedSeed = c.id;
            seedPickerGroup.alpha = 0.2f;
            seedGhost.GetComponent<Image>().sprite = SpriteDB.Get(c.Icon);
            seedGhost.gameObject.SetActive(true);
            seedGhost.SetAsLastSibling();
            SeedDragMove(screen);
        }

        internal void SeedDragMove(Vector2 screen)
        {
            if (draggingSeed == null) return;
            float scale = canvasRt.lossyScale.x <= 0 ? 1f : canvasRt.lossyScale.x;
            // keep the seed visible above the finger
            seedGhost.anchoredPosition = screen / scale + new Vector2(0, 90f);
            Vector2 world = CameraController.I.ScreenToWorld(screen);
            PlotView p = WorldMap.I.PlotAt(IsoGrid.ToCell(world));
            if (p != null && p.D.state == PlotState.Tilled && !p.Pending) p.OnDragApply("plant");
        }

        internal void SeedDragEnd()
        {
            draggingSeed = null;
            seedGhost.gameObject.SetActive(false);
            // after a drag the seed stays selected (chip) so the player can keep planting by tapping
            CloseSeedPickerOnly();
        }
    }

    /// <summary>One seed in the picker: tap to select, drag out over the field to plant.</summary>
    public class SeedDragItem : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UIManager ui;
        public CropDef seed;
        private bool dragged;

        public void OnPointerClick(PointerEventData e)
        {
            if (dragged) { dragged = false; return; }
            ui.SeedTapped(seed);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            dragged = true;
            ui.SeedDragBegin(seed, e.position);
        }

        public void OnDrag(PointerEventData e) => ui.SeedDragMove(e.position);

        public void OnEndDrag(PointerEventData e) => ui.SeedDragEnd();
    }

    public partial class UIManager
    {
        // ------------------------------------------------------------------ placement bar
        private GameObject placementBar;
        private Text placementTitle;
        private Image confirmImg;
        private GameObject decorButtons;

        private void BuildPlacementBar()
        {
            Image p = UIFactory.Panel(hudRoot, new Color(1f, 0.97f, 0.89f, 0.97f), "PlacementBar");
            p.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(820, 140));
            placementTitle = UIFactory.Label(p.transform, "", 30, UIFactory.TextDark, TextAnchor.MiddleLeft, true);
            placementTitle.rectTransform.Stretch(20);
            placementTitle.rectTransform.offsetMax = new Vector2(-260, -10);
            Text hint = UIFactory.Label(p.transform, L.T("Kéo vật thể để di chuyển rồi nhấn nút xanh để đặt"), 22, UIFactory.Wood, TextAnchor.LowerLeft);
            hint.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(22, 10), new Vector2(560, 30));

            Button ok = UIFactory.IconButton(p.transform, "hud_confirm", null, 110, () => PlacementController.I.Confirm());
            ((RectTransform)ok.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-140, 0), new Vector2(110, 110));
            confirmImg = ok.GetComponent<Image>();
            Button no = UIFactory.IconButton(p.transform, "hud_close", null, 110, () => PlacementController.I.Cancel());
            ((RectTransform)no.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(110, 110));
            // moving a decoration: put it away or sell it
            RectTransform db = UIFactory.Rect("DecorButtons", p.transform);
            db.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-270, 0), new Vector2(330, 110));
            Button store = UIFactory.MakeButton(db, L.T("Cất kho"), UIFactory.Blue, () =>
            {
                PlaceView v = PlacementController.I.MovingDecor;
                if (v == null) return;
                PlacementController.I.Cancel();
                StoreDecor(v);
            }, 24);
            ((RectTransform)store.transform).Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(155, 90));
            Button sell = UIFactory.MakeButton(db, L.T("Bán"), UIFactory.Orange, () =>
            {
                PlaceView v = PlacementController.I.MovingDecor;
                if (v == null) return;
                PlacementController.I.Cancel();
                SellDecor(v);
            }, 24);
            ((RectTransform)sell.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(155, 90));
            decorButtons = db.gameObject;
            decorButtons.SetActive(false);

            placementBar = p.gameObject;
            placementBar.SetActive(false);
        }

        public void ShowPlacementBar(bool on, string title)
        {
            placementBar.SetActive(on);
            bottomButtons.SetActive(!on);
            bool decor = on && PlacementController.I != null && PlacementController.I.MovingDecor != null;
            decorButtons.SetActive(decor);
            ((RectTransform)placementBar.transform).sizeDelta = new Vector2(decor ? 1160 : 820, 140);
            placementTitle.rectTransform.offsetMax = new Vector2(decor ? -610 : -260, -10);
            if (on)
            {
                placementTitle.text = title;
                CloseSeedBar();
            }
        }

        public void SetPlacementValid(bool ok)
        {
            if (confirmImg != null) confirmImg.color = ok ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
        }
    }
}
