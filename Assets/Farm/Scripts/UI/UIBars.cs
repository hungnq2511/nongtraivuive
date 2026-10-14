using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    public partial class UIManager
    {
        // ------------------------------------------------------------------ seed bar
        private GameObject seedBar;
        private bool seedBarOpen;
        public bool SeedBarOpen => seedBarOpen;

        private class SeedCard { public CropDef def; public Image bg; public GameObject locked; }
        private readonly List<SeedCard> seedCards = new List<SeedCard>();
        private string seedCardsState;

        private void BuildSeedBar()
        {
            Image p = UIFactory.Panel(hudRoot, new Color(1f, 0.97f, 0.89f, 0.97f), "SeedBar");
            p.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-120, 200), new Vector2(1180, 270));
            Text h = UIFactory.Label(p.transform, "Chọn hạt giống rồi chạm / kéo qua các ô đất đã cuốc", 26, UIFactory.TextDark, TextAnchor.MiddleLeft, true);
            h.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -6), new Vector2(1000, 44));
            Button close = UIFactory.IconButton(p.transform, "hud_close", null, 80, CloseSeedBar);
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-10, -10), new Vector2(80, 80));

            ScrollRect sr = UIFactory.Scroll(p.transform, true, out RectTransform content);
            ((RectTransform)sr.transform).Stretch();
            ((RectTransform)sr.transform).offsetMax = new Vector2(-10, -48);
            UIFactory.Grid(content, new Vector2(170, 196), new Vector2(14, 0), true).padding = new RectOffset(12, 12, 6, 6);

            foreach (CropDef c in Defs.Crops)
            {
                CropDef def = c;
                Image bg = UIFactory.Panel(content, Color.white, "Seed_" + c.id);
                bg.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectSeed(def));
                Image icon = UIFactory.Icon(bg.transform, c.Icon, 96);
                icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(96, 96));
                Text n = UIFactory.Label(bg.transform, c.name, 24, UIFactory.TextDark, TextAnchor.MiddleCenter, true);
                n.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(166, 30));
                Text info = UIFactory.Label(bg.transform, $"{c.seedCost} xu · {Defs.FormatTime(c.growSec)}", 20, UIFactory.Wood, TextAnchor.MiddleCenter);
                info.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(166, 28));

                Image lk = UIFactory.Panel(bg.transform, new Color(0.15f, 0.1f, 0.05f, 0.72f), "Lock");
                lk.raycastTarget = false;
                lk.rectTransform.Stretch();
                Text lt = UIFactory.Label(lk.transform, $"Cấp {c.unlock}", 30, Color.white, TextAnchor.MiddleCenter, true, true);
                lt.rectTransform.Stretch();

                seedCards.Add(new SeedCard { def = c, bg = bg, locked = lk.gameObject });
            }
            seedBar = p.gameObject;
            seedBar.SetActive(false);
        }

        public void OpenSeedBar()
        {
            if (PlacementController.I.Active) return;
            if (InteractionController.I.BuildMode) ToggleBuildMode();
            seedBar.SetActive(true);
            seedBarOpen = true;
            if (InteractionController.SelectedSeed == null) InteractionController.SelectedSeed = "wheat";
            RefreshSeedCards(true);
        }

        public void CloseSeedBar()
        {
            seedBarOpen = false;
            if (seedBar != null) seedBar.SetActive(false);
            InteractionController.SelectedSeed = null;
        }

        public void CloseSeedBarIfNotPlanting()
        {
            if (seedBarOpen) CloseSeedBar();
        }

        private void SelectSeed(CropDef c)
        {
            if (GameManager.I.Data.level < c.unlock)
            {
                Toast($"{c.name} mở khoá ở cấp {c.unlock}.");
                return;
            }
            InteractionController.SelectedSeed = c.id;
            RefreshSeedCards(true);
        }

        private void RefreshSeedCards(bool force)
        {
            string state = GameManager.I.Data.level + "|" + InteractionController.SelectedSeed;
            if (!force && state == seedCardsState) return;
            seedCardsState = state;
            int lvl = GameManager.I.Data.level;
            foreach (SeedCard sc in seedCards)
            {
                bool locked = lvl < sc.def.unlock;
                sc.locked.SetActive(locked);
                bool sel = InteractionController.SelectedSeed == sc.def.id;
                sc.bg.color = sel ? new Color(0.7f, 0.92f, 0.55f) : Color.white;
            }
        }

        // ------------------------------------------------------------------ placement bar
        private GameObject placementBar;
        private Text placementTitle;
        private Image confirmImg;

        private void BuildPlacementBar()
        {
            Image p = UIFactory.Panel(hudRoot, new Color(1f, 0.97f, 0.89f, 0.97f), "PlacementBar");
            p.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(820, 140));
            placementTitle = UIFactory.Label(p.transform, "", 30, UIFactory.TextDark, TextAnchor.MiddleLeft, true);
            placementTitle.rectTransform.Stretch(20);
            placementTitle.rectTransform.offsetMax = new Vector2(-260, -10);
            Text hint = UIFactory.Label(p.transform, "Kéo vật thể để di chuyển rồi nhấn nút xanh để đặt", 22, UIFactory.Wood, TextAnchor.LowerLeft);
            hint.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(22, 10), new Vector2(560, 30));

            Button ok = UIFactory.IconButton(p.transform, "hud_confirm", null, 110, () => PlacementController.I.Confirm());
            ((RectTransform)ok.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-140, 0), new Vector2(110, 110));
            confirmImg = ok.GetComponent<Image>();
            Button no = UIFactory.IconButton(p.transform, "hud_close", null, 110, () => PlacementController.I.Cancel());
            ((RectTransform)no.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(110, 110));
            placementBar = p.gameObject;
            placementBar.SetActive(false);
        }

        public void ShowPlacementBar(bool on, string title)
        {
            placementBar.SetActive(on);
            bottomButtons.SetActive(!on);
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
