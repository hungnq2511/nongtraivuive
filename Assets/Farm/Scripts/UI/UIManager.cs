using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>Keeps a RectTransform inside Screen.safeArea (notches, rounded corners).</summary>
    public class SafeArea : MonoBehaviour
    {
        private Rect last;
        private RectTransform rt;

        private void Awake() => rt = (RectTransform)transform;

        private void Update()
        {
            Rect sa = Screen.safeArea;
            if (sa == last || Screen.width <= 0 || Screen.height <= 0) return;
            last = sa;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>HUD, toasts, tooltips, floating texts, seed bar, placement bar and modal popups.</summary>
    public partial class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }

        private RectTransform canvasRt, safeRoot, hudRoot, modalRoot;
        private readonly List<GameObject> modals = new List<GameObject>();
        public bool ModalOpen => modals.Count > 0;

        // HUD
        private Text levelText, xpText, coinText, storageText;
        private RectTransform xpFill;
        private Text questText, questReward;
        private GameObject questCard;
        private GameObject bottomButtons;
        private GameObject buildBanner;

        // Toast / tooltip / floating text
        private Text toastText;
        private CanvasGroup toastGroup;
        private float toastTimer;
        private readonly Queue<string> toastQueue = new Queue<string>();
        private RectTransform tooltip;
        private Text tooltipText;
        private Vector3 tooltipWorld;
        private float tooltipTimer;

        private class Floater { public RectTransform rt; public CanvasGroup cg; public Vector3 world; public float t; }
        private readonly List<Floater> floaters = new List<Floater>();

        public void Init()
        {
            I = this;
            BuildCanvas();
            BuildHud();
            BuildSeedBar();
            BuildPlacementBar();
            BuildOverlays();
            GameManager.I.Changed += RefreshHud;
            GameManager.I.LeveledUp += OnLevelUp;
            RefreshHud();
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.Changed -= RefreshHud;
                GameManager.I.LeveledUp -= OnLevelUp;
            }
        }

        // ------------------------------------------------------------------ canvas
        private void BuildCanvas()
        {
            var cgo = new GameObject("UI Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            cgo.AddComponent<GraphicRaycaster>();
            canvasRt = (RectTransform)cgo.transform;

            safeRoot = UIFactory.Rect("SafeArea", canvasRt).Stretch();
            safeRoot.gameObject.AddComponent<SafeArea>();
            hudRoot = UIFactory.Rect("HUD", safeRoot).Stretch();
            modalRoot = UIFactory.Rect("Modals", canvasRt).Stretch();

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.transform.SetParent(transform, false);
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
        }

        // ------------------------------------------------------------------ HUD
        private void BuildHud()
        {
            // Level badge + XP bar (top-left)
            Image badge = UIFactory.Panel(hudRoot, UIFactory.GreenDark, "LevelBadge");
            badge.sprite = ProcSprites.Circle;
            badge.type = Image.Type.Simple;
            badge.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(116, 116));
            Image inner = UIFactory.Panel(badge.transform, UIFactory.Green, "Inner");
            inner.sprite = ProcSprites.Circle;
            inner.type = Image.Type.Simple;
            inner.raycastTarget = false;
            inner.rectTransform.Stretch(8);
            levelText = UIFactory.Label(badge.transform, "1", 54, Color.white, TextAnchor.MiddleCenter, true, true);
            levelText.rectTransform.Stretch();
            badge.gameObject.AddComponent<Button>().onClick.AddListener(OpenFarmhouse);

            Image xpBg = UIFactory.Panel(hudRoot, new Color(0.2f, 0.12f, 0.05f, 0.75f), "XpBar");
            xpBg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(130, -50), new Vector2(330, 52));
            Image fill = UIFactory.Panel(xpBg.transform, UIFactory.Orange, "Fill");
            fill.raycastTarget = false;
            xpFill = fill.rectTransform;
            xpFill.anchorMin = new Vector2(0, 0);
            xpFill.anchorMax = new Vector2(0.5f, 1);
            xpFill.offsetMin = new Vector2(5, 5);
            xpFill.offsetMax = new Vector2(-5, -5);
            xpText = UIFactory.Label(xpBg.transform, "0/10", 26, Color.white, TextAnchor.MiddleCenter, true, true);
            xpText.rectTransform.Stretch();

            // Coins & storage (top-center)
            coinText = Pill(new Vector2(-180, -22), "hud_coin", () => OpenShop(null));
            storageText = Pill(new Vector2(180, -22), "hud_inventory", () => OpenStorage(false));

            // Settings + daily (top-right)
            Button set = UIFactory.IconButton(hudRoot, "hud_settings", null, 100, OpenSettings);
            ((RectTransform)set.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -16), new Vector2(100, 100));
            BuildTopRightExtras();

            // Quest card (left)
            Image q = UIFactory.Panel(hudRoot, new Color(1f, 0.97f, 0.89f, 0.95f), "Quest");
            q.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -150), new Vector2(460, 150));
            questCard = q.gameObject;
            Image qi = UIFactory.Icon(q.transform, "hud_map", 96);
            qi.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(96, 96));
            Text qt = UIFactory.Label(q.transform, "NHIỆM VỤ", 24, UIFactory.GreenDark, TextAnchor.UpperLeft, true);
            qt.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(116, -10), new Vector2(330, 30));
            questText = UIFactory.Label(q.transform, "", 26, UIFactory.TextDark, TextAnchor.UpperLeft);
            questText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(116, -40), new Vector2(334, 70));
            questReward = UIFactory.Label(q.transform, "", 22, UIFactory.Wood, TextAnchor.LowerLeft, true);
            questReward.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(116, 8), new Vector2(330, 28));

            // Bottom-right action buttons
            RectTransform row = UIFactory.Rect("Actions", hudRoot);
            row.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 52), new Vector2(760, 130));
            UIFactory.HRow(row, 22f).childAlignment = TextAnchor.LowerRight;
            UIFactory.IconButton(row, "hud_build", "Xây dựng", 126, ToggleBuildMode);
            UIFactory.IconButton(row, "hud_seed", "Hạt giống", 126, OpenSeedBar);
            UIFactory.IconButton(row, "hud_inventory", "Kho", 126, () => OpenStorage(false));
            UIFactory.IconButton(row, "hud_quest", "Đơn hàng", 126, OpenOrders);
            UIFactory.IconButton(row, "hud_shop", "Cửa hàng", 126, () => OpenShop(null));
            bottomButtons = row.gameObject;

            Joystick.Create(hudRoot);

            // Build mode banner
            Image bb = UIFactory.Panel(hudRoot, new Color(0.15f, 0.35f, 0.6f, 0.92f), "BuildBanner");
            bb.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(900, 80));
            Text bt = UIFactory.Label(bb.transform, "CHẾ ĐỘ XÂY DỰNG – chạm công trình / ô đất trống để di chuyển", 26, Color.white, TextAnchor.MiddleLeft, true);
            bt.rectTransform.Stretch(20);
            bt.rectTransform.offsetMax = new Vector2(-170, -6);
            Button done = UIFactory.MakeButton(bb.transform, "Xong", UIFactory.Green, ToggleBuildMode, 28);
            ((RectTransform)done.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(140, 60));
            buildBanner = bb.gameObject;
            buildBanner.SetActive(false);
        }

        private Text Pill(Vector2 pos, string icon, UnityEngine.Events.UnityAction onClick)
        {
            Image p = UIFactory.Panel(hudRoot, new Color(0.2f, 0.12f, 0.05f, 0.75f), "Pill");
            p.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, new Vector2(300, 72));
            p.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
            Image i = UIFactory.Icon(p.transform, icon, 92);
            i.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(92, 92));
            Text t = UIFactory.Label(p.transform, "0", 36, Color.white, TextAnchor.MiddleCenter, true, true);
            t.rectTransform.Stretch();
            t.rectTransform.offsetMin = new Vector2(60, 0);
            return t;
        }

        public void RefreshHud()
        {
            GameManager gm = GameManager.I;
            if (gm == null) return;
            SaveData d = gm.Data;
            levelText.text = d.level.ToString();
            int need = Defs.XpToNext(d.level);
            bool max = d.level >= Defs.MaxLevel;
            xpText.text = max ? "MAX" : $"{d.xp}/{need} XP";
            float pct = max ? 1f : Mathf.Clamp01((float)d.xp / need);
            xpFill.anchorMax = new Vector2(Mathf.Max(0.04f, pct), 1);
            coinText.text = d.coins.ToString("N0");
            int used = gm.StorageUsed, cap = gm.StorageCapacity;
            storageText.text = $"{used}/{cap}";
            storageText.color = used >= cap ? new Color(1f, 0.55f, 0.5f) : Color.white;
        }

        public void SetQuest(string text, string reward)
        {
            questCard.SetActive(text != null);
            if (text == null) return;
            questText.text = text;
            questReward.text = reward;
        }

        // ------------------------------------------------------------------ build mode
        public void ToggleBuildMode()
        {
            bool on = !InteractionController.I.BuildMode;
            if (PlacementController.I.Active) PlacementController.I.Cancel();
            CloseSeedBar();
            InteractionController.I.SetBuildMode(on);
            buildBanner.SetActive(on);
        }

        // ------------------------------------------------------------------ toast / tooltip / floaters
        private void BuildOverlays()
        {
            Image tb = UIFactory.Panel(canvasRt, new Color(0.15f, 0.1f, 0.05f, 0.88f), "Toast");
            tb.raycastTarget = false;
            tb.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(1000, 84));
            toastText = UIFactory.Label(tb.transform, "", 30, Color.white, TextAnchor.MiddleCenter, true);
            toastText.rectTransform.Stretch(12);
            toastGroup = tb.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0;
            toastGroup.blocksRaycasts = false;

            Image tt = UIFactory.Panel(canvasRt, new Color(1f, 0.97f, 0.89f, 0.96f), "Tooltip");
            tt.raycastTarget = false;
            tooltip = tt.rectTransform;
            tooltip.anchorMin = tooltip.anchorMax = Vector2.zero;
            tooltip.pivot = new Vector2(0.5f, 0f);
            tooltip.sizeDelta = new Vector2(380, 110);
            tooltipText = UIFactory.Label(tt.transform, "", 27, UIFactory.TextDark, TextAnchor.MiddleCenter, true);
            tooltipText.rectTransform.Stretch(10);
            tooltip.gameObject.SetActive(false);
            tooltip.SetSiblingIndex(1);
        }

        public void Toast(string msg)
        {
            if (toastQueue.Count > 2) return;
            if (toastTimer > 0 && toastText.text == msg) { toastTimer = 2.2f; return; }
            toastQueue.Enqueue(msg);
        }

        public void ShowTooltip(Vector3 world, string text)
        {
            tooltipWorld = world;
            tooltipText.text = text;
            tooltipTimer = 2.6f;
            tooltip.gameObject.SetActive(true);
            PositionAtWorld(tooltip, world);
        }

        public void FloatText(Vector3 world, string text, string icon)
        {
            RectTransform rt = UIFactory.Rect("Float", canvasRt);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(220, 80);
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Text t = UIFactory.Label(rt, text, 40, Color.white, TextAnchor.MiddleRight, true, true);
            t.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(110, 70));
            if (!string.IsNullOrEmpty(icon))
            {
                Image i = UIFactory.Icon(rt, icon, 70);
                i.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(116, 0), new Vector2(70, 70));
            }
            floaters.Add(new Floater { rt = rt, cg = cg, world = world, t = 0 });
            PositionAtWorld(rt, world);
            if (!string.IsNullOrEmpty(icon) && text.StartsWith("+")) FlyIcon(world, icon);
        }

        private void PositionAtWorld(RectTransform rt, Vector3 world)
        {
            Vector2 sp = CameraController.I.WorldToScreen(world);
            float scale = canvasRt.lossyScale.x <= 0 ? 1f : canvasRt.lossyScale.x;
            rt.anchoredPosition = sp / scale;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.3f);
            }
            else if (toastQueue.Count > 0)
            {
                toastText.text = toastQueue.Dequeue();
                toastTimer = 2.2f;
                toastGroup.alpha = 1f;
                toastGroup.transform.SetAsLastSibling();
            }

            if (tooltipTimer > 0f)
            {
                tooltipTimer -= dt;
                PositionAtWorld(tooltip, tooltipWorld);
                if (tooltipTimer <= 0f) tooltip.gameObject.SetActive(false);
            }

            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                Floater f = floaters[i];
                f.t += dt;
                PositionAtWorld(f.rt, f.world);
                f.rt.anchoredPosition += new Vector2(0, f.t * 120f);
                f.cg.alpha = Mathf.Clamp01(1.4f - f.t);
                if (f.t > 1.4f)
                {
                    Destroy(f.rt.gameObject);
                    floaters.RemoveAt(i);
                }
            }

            if (seedBarOpen) RefreshSeedCards(false);
        }

        // ------------------------------------------------------------------ modal windows
        /// <summary>Creates a modal window and returns its content area.</summary>
        public RectTransform OpenWindow(string title, Vector2 size, out GameObject modal)
        {
            RectTransform m = UIFactory.Rect("Modal_" + title, modalRoot).Stretch();
            Image dim = m.gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.55f);
            modal = m.gameObject;
            GameObject captured = modal;

            Image frame = UIFactory.Panel(m, UIFactory.Wood, "Frame");
            frame.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), size + new Vector2(16, 16));
            Image win = UIFactory.Panel(frame.transform, UIFactory.Cream, "Window");
            win.rectTransform.Stretch(8);

            Image bar = UIFactory.Panel(frame.transform, UIFactory.WoodDark, "TitleBar");
            bar.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(Mathf.Min(900, size.x - 200), 84));
            Text t = UIFactory.Label(bar.transform, title, 40, Color.white, TextAnchor.MiddleCenter, true, true);
            t.rectTransform.Stretch(6);
            t.rectTransform.offsetMin = new Vector2(24, 6);
            t.rectTransform.offsetMax = new Vector2(-24, -6);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 22;
            t.resizeTextMaxSize = 40;

            Button close = UIFactory.IconButton(frame.transform, "hud_close", null, 96, () => CloseModal(captured));
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-14, -14), new Vector2(96, 96));

            RectTransform content = UIFactory.Rect("Content", win.transform).Stretch();
            content.offsetMax = new Vector2(-20, -54);
            content.offsetMin = new Vector2(20, 20);

            modals.Add(modal);
            if (seedBarOpen) CloseSeedBar();
            return content;
        }

        public void CloseModal(GameObject modal)
        {
            if (modal == null) return;
            modals.Remove(modal);
            Destroy(modal);
        }

        public void CloseAllModals()
        {
            foreach (GameObject m in modals.ToArray()) CloseModal(m);
        }
    }
}
