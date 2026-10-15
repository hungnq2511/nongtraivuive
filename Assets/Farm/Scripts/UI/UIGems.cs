using System;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>Gems (premium currency): speed-up button on world tooltips, gem info popup.</summary>
    public partial class UIManager
    {
        private Button speedButton;
        private Text speedText;
        private Action speedAction;
        private Func<double> speedLeft;

        /// <summary>Pay gems to finish a timer now. Returns true when paid.</summary>
        public bool PaySpeedUp(double secondsLeft)
        {
            if (secondsLeft <= 0) return true;
            int cost = Defs.SpeedUpCost(secondsLeft);
            if (!GameManager.I.TrySpendGems(cost))
            {
                Toast(L.F("Cần {0} kim cương để tăng tốc. Bạn đang có {1}.", cost, GameManager.I.Data.gems));
                return false;
            }
            GameEvents.Raise("speedup", "", cost);
            return true;
        }

        /// <summary>Tooltip with a "finish now" gem button (crops, animals, trees…).</summary>
        public void ShowSpeedUp(Vector3 world, string text, double secondsLeft, Action finish)
        {
            ShowTooltip(world, text);
            if (secondsLeft <= 0 || finish == null) return;
            EnsureSpeedButton();
            double end = GameManager.I.Now + secondsLeft;
            speedLeft = () => end - GameManager.I.Now;
            speedAction = finish;
            speedButton.gameObject.SetActive(true);
            // rewarded ad alternative for short waits
            bool ad = Monetization.I != null && secondsLeft <= Defs.AdSpeedUpMaxSeconds && Monetization.I.CanOfferAd("speedup");
            EnsureAdSpeedButton();
            adSpeedButton.gameObject.SetActive(ad);
            ((RectTransform)speedButton.transform).anchoredPosition = new Vector2(ad ? -160 : 0, 12);
            tooltip.sizeDelta = new Vector2(ad ? 660 : 460, 200);
            tooltipText.rectTransform.offsetMin = new Vector2(10, 88);
            tooltipTimer = 6f;
            UpdateSpeedLabel();
        }

        private void EnsureSpeedButton()
        {
            if (speedButton != null) return;
            speedButton = UIFactory.MakeButton(tooltip, " ", UIFactory.Blue, () =>
            {
                double left = speedLeft != null ? speedLeft() : 0;
                if (!PaySpeedUp(left)) return;
                Action a = speedAction;
                tooltipTimer = 0f;
                tooltip.gameObject.SetActive(false);
                HideSpeedButton();
                a?.Invoke();
                Toast(L.T("Đã tăng tốc!"));
            }, 26);
            ((RectTransform)speedButton.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(300, 66));
            speedText = speedButton.GetComponentInChildren<Text>();
            speedText.alignment = TextAnchor.MiddleLeft;
            speedText.rectTransform.offsetMin = new Vector2(24, 6);
            Image gem = UIFactory.Icon(speedButton.transform, "hud_gem", 56);
            gem.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(56, 56));
            speedButton.gameObject.SetActive(false);
        }

        private Button adSpeedButton;

        private void EnsureAdSpeedButton()
        {
            if (adSpeedButton != null) return;
            adSpeedButton = UIFactory.MakeButton(tooltip, L.T("Xem QC: xong ngay"), UIFactory.Orange, () =>
            {
                Action a = speedAction;
                tooltipTimer = 0f;
                tooltip.gameObject.SetActive(false);
                HideSpeedButton();
                Monetization.I.ShowRewarded("speedup", () =>
                {
                    a?.Invoke();
                    Toast(L.T("Đã tăng tốc!"));
                });
            }, 24);
            ((RectTransform)adSpeedButton.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(160, 12), new Vector2(300, 66));
            adSpeedButton.gameObject.SetActive(false);
        }

        private void UpdateSpeedLabel()
        {
            if (speedButton == null || !speedButton.gameObject.activeSelf || speedLeft == null) return;
            double left = speedLeft();
            if (left <= 0) { HideSpeedButton(); return; }
            speedText.text = L.F("Xong ngay   {0}", Defs.SpeedUpCost(left));
        }

        private void HideSpeedButton()
        {
            if (speedButton == null) return;
            speedButton.gameObject.SetActive(false);
            if (adSpeedButton != null) adSpeedButton.gameObject.SetActive(false);
            speedAction = null;
            speedLeft = null;
            tooltip.sizeDelta = new Vector2(380, 110);
            tooltipText.rectTransform.offsetMin = new Vector2(10, 10);
        }

        private float speedLabelTimer;

        /// <summary>Called from Update: keeps the gem price in step with the remaining time.</summary>
        private void TickSpeedButton(float dt)
        {
            speedLabelTimer -= dt;
            if (speedLabelTimer > 0f) return;
            speedLabelTimer = 0.5f;
            UpdateSpeedLabel();
        }

        /// <summary>Small inline gem button used inside popups (machines, market, truck, orders).</summary>
        public Button GemButton(Transform parent, string label, Func<double> left, Action finish, int fontSize = 24)
        {
            Button b = UIFactory.MakeButton(parent, " ", UIFactory.Blue, null, fontSize);
            Text t = b.GetComponentInChildren<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.offsetMin = new Vector2(16, 6);
            Image gem = UIFactory.Icon(b.transform, "hud_gem", 48);
            gem.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(48, 48));
            t.text = $"{label} {Defs.SpeedUpCost(left())}";
            b.onClick.AddListener(() =>
            {
                if (!PaySpeedUp(left())) return;
                finish();
            });
            return b;
        }

        // ================================================================== GEM SHOP
        public void OpenGems()
        {
            RectTransform content = OpenWindow(L.T("Kim cương"), new Vector2(1440, 820), out GameObject modal);
            GameManager gm = GameManager.I;
            Monetization m = Monetization.I;

            RectTransform top = UIFactory.Rect("Top", content);
            top.Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1380, 80));
            RectTransform row = UIFactory.Rect("Packs", content);
            row.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(1380, 430));
            UIFactory.HRow(row, 18);
            RectTransform bottom = UIFactory.Rect("Bottom", content);
            bottom.Place(new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(1380, 210));

            Action rebuild = null;
            rebuild = () =>
            {
                if (modal == null) return;
                ClearChildren(top);
                ClearChildren(row);
                ClearChildren(bottom);
                Image gi = UIFactory.Icon(top, "hud_gem", 70);
                gi.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(70, 70));
                Txt(top, L.F("Bạn đang có <b>{0}</b> kim cương.", gm.Data.gems), 32, UIFactory.TextDark, TextAnchor.MiddleLeft,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(96, 0), new Vector2(700, 60), true);
                Button info = UIFactory.MakeButton(top, L.T("Cách nhận miễn phí"), UIFactory.Blue, () => OpenGemInfo(), 24);
                ((RectTransform)info.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(280, 66));

                // ---- packs
                foreach (IapProductDef p in Defs.IapProducts)
                {
                    IapProductDef pack = p;
                    bool owned = Monetization.Owned(pack);
                    Image card = UIFactory.Panel(row, pack.oneTime ? new Color(1f, 0.93f, 0.75f) : Color.white, "Pack");
                    card.rectTransform.sizeDelta = new Vector2(320, 430);
                    if (pack.oneTime)
                        Txt(card.transform, L.T("Chỉ mua 1 lần!"), 22, UIFactory.Red, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(300, 32), true);
                    Image ic = UIFactory.Icon(card.transform, pack.icon, 130);
                    ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(130, 130));
                    Txt(card.transform, pack.name, 28, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(300, 40), true);
                    string what = L.F("+{0} kim cương", pack.gems);
                    if (pack.coins > 0) what += "\n" + L.F("+{0:N0} xu", pack.coins);
                    if (pack.givesDog) what += "\n" + L.T("+ Chó Vàng (đuổi quạ)");
                    Txt(card.transform, what, 24, UIFactory.Wood, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -224), new Vector2(300, 100), true);
                    string label = owned ? L.T("Đã mua") : (m != null && m.Busy ? "…" : (m != null ? m.Price(pack) : pack.fallbackPrice));
                    Button buy = UIFactory.MakeButton(card.transform, label, owned ? UIFactory.Gray : UIFactory.Green, () =>
                    {
                        if (owned || m == null || m.Busy) return;
                        m.Buy(pack);
                        rebuild();
                    }, 30);
                    ((RectTransform)buy.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(260, 76));
                }

                // ---- free gems by ad + restore
                if (Monetization.AdsEnabled)
                {
                    int left = Monetization.AdsLeftToday("gems");
                    bool can = m != null && m.CanOfferAd("gems");
                    Image adCard = UIFactory.Panel(bottom, UIFactory.CreamDark, "Ad");
                    adCard.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 100), new Vector2(900, 100));
                    Txt(adCard.transform, L.F("Xem quảng cáo nhận +{0} kim cương  (còn {1} lần hôm nay)", Defs.AdGems, left), 26, UIFactory.TextDark, TextAnchor.MiddleLeft,
                        new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(600, 80), true);
                    Button watch = UIFactory.MakeButton(adCard.transform, can ? L.T("Xem ngay") : left > 0 ? L.T("Đang tải…") : L.T("Hết lượt"), can ? UIFactory.Orange : UIFactory.Gray, () =>
                    {
                        if (m == null) return;
                        m.ShowRewarded("gems", () =>
                        {
                            gm.AddGems(Defs.AdGems);
                            Toast(L.F("+{0} kim cương!", Defs.AdGems));
                        });
                    }, 26);
                    ((RectTransform)watch.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(240, 72));
                }
                Button restore = UIFactory.MakeButton(bottom, L.T("Khôi phục giao dịch"), UIFactory.Blue, () => { if (m != null) m.Restore(); }, 24);
                ((RectTransform)restore.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 112), new Vector2(330, 76));
                Txt(bottom, L.T("Giao dịch được xử lý bởi App Store / Google Play. Kim cương mua thêm giúp hoàn thành công việc nhanh hơn – trò chơi vẫn chơi được đầy đủ mà không cần mua."),
                    20, UIFactory.Wood, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 4), new Vector2(1360, 80));
            };
            rebuild();
            Action onChanged = () => rebuild();
            Monetization.Changed += onChanged;
            modal.AddComponent<OnDestroyHook>().Action = () => Monetization.Changed -= onChanged;
        }

        /// <summary>How to earn gems for free (was the old gem popup).</summary>
        public void OpenGemInfo()
        {
            RectTransform content = OpenWindow(L.T("Kim cương"), new Vector2(980, 640), out GameObject modal);
            Image ic = UIFactory.Icon(content, "hud_gem", 200);
            ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(200, 200));
            Txt(content, L.F("Bạn đang có <b>{0}</b> kim cương.", GameManager.I.Data.gems), 32, UIFactory.TextDark, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, -20), new Vector2(680, 60), true);
            Txt(content,
                L.T("<b>Cách nhận:</b>\n") +
                L.F("• Lên cấp: +{0} (mỗi 5 cấp: +{1})\n", Defs.LevelUpGems(2), Defs.LevelUpGems(5)) +
                L.T("• Nhận thưởng thành tựu: +1 / +2 / +3\n") +
                L.F("• Rương nhiệm vụ ngày: +{0} · Xuất xe tải: +{1}\n", Defs.TaskBonusGems, Defs.TruckGems) +
                L.F("• Quà đăng nhập: +{0} mỗi ngày, ngày thứ 7: +{1}\n", Defs.DailyGems, Defs.DailyStreakGems) +
                L.F("• Câu được cá vàng: +{0}\n\n", Defs.GoldenFishGems) +
                L.T("<b>Dùng để:</b> chạm cây, vật nuôi, cây ăn quả đang chờ → \"Xong ngay\"; tăng tốc máy chế biến, bán ngay ở chợ, gọi xe tải và đơn hàng mới.\n") +
                L.T("Giá: khoảng 1 kim cương cho mỗi 6 phút còn lại."),
                26, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, -90), new Vector2(680, 440));
            Button ok = UIFactory.MakeButton(content, L.T("Đóng"), UIFactory.Green, () => CloseModal(modal), 30);
            ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(260, 76));
        }
    }

    /// <summary>Runs an action when its GameObject is destroyed (unsubscribe helpers for popups).</summary>
    public class OnDestroyHook : MonoBehaviour
    {
        public Action Action;
        private void OnDestroy() => Action?.Invoke();
    }
}
