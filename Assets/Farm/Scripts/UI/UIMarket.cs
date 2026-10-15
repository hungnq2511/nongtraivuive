using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>Farmers' market popup: stall slots, listing editor, hot goods of the day.</summary>
    public partial class UIManager
    {
        // ================================================================== MARKET (stall)
        public void OpenMarket(string preselect = null)
        {
            RectTransform content = OpenWindow(L.T("Chợ nông sản"), new Vector2(1380, 860), out GameObject modal);
            GameManager gm = GameManager.I;
            bool editing = false;
            var timers = new List<KeyValuePair<Text, MarketSlotData>>();
            Text hotTimer = null;
            int soldShown = 0;
            Action showStall = null;
            Action<int, string> showEditor = null;

            // ------------------------------------------------------------- stall view
            showStall = () =>
            {
                editing = false;
                ClearChildren(content);
                timers.Clear();
                List<MarketSlotData> slots = Market.Slots;
                soldShown = slots.FindAll(Market.IsSold).Count;

                // hot goods banner
                Image banner = UIFactory.Panel(content, new Color(1f, 0.89f, 0.72f), "Hot");
                banner.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1320, 96));
                Txt(banner.transform, L.F("Giá hot hôm nay\n<color=#E05A47>+{0}% giá</color>", Mathf.RoundToInt((Defs.MarketHotBonus - 1f) * 100)), 26, UIFactory.TextDark,
                    TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(260, 86), true);
                float x = 290;
                foreach (string id in Market.Hot)
                {
                    ItemDef it = Defs.Item(id);
                    if (it == null) continue;
                    Image ic = UIFactory.Icon(banner.transform, it.icon, 78);
                    ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(78, 78));
                    Txt(banner.transform, L.F("{0}\n{1} xu/cái", it.name, Mathf.RoundToInt(it.sell * Defs.MarketHotBonus)), 24, UIFactory.TextDark, TextAnchor.MiddleLeft,
                        new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x + 86, 0), new Vector2(230, 80), true);
                    x += 340;
                }
                if (Market.Hot.Count == 0)
                    Txt(banner.transform, L.T("Hãy trồng trọt, chăn nuôi để mở thêm mặt hàng."), 24, UIFactory.Wood, TextAnchor.MiddleLeft,
                        new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(700, 80));
                hotTimer = Txt(banner.transform, "", 22, UIFactory.Wood, TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(260, 80));

                // stall slots
                ScrollRect sr = UIFactory.Scroll(content, true, out RectTransform row);
                ((RectTransform)sr.transform).Stretch();
                ((RectTransform)sr.transform).offsetMax = new Vector2(0, -104);
                ((RectTransform)sr.transform).offsetMin = new Vector2(0, 96);
                UIFactory.Grid(row, new Vector2(250, 540), new Vector2(18, 18), true);

                for (int i = 0; i < slots.Count; i++)
                {
                    int idx = i;
                    MarketSlotData s = slots[i];
                    if (Market.IsEmpty(s))
                    {
                        Image card = UIFactory.Panel(row, new Color(1f, 1f, 1f, 0.75f), "Empty");
                        card.gameObject.AddComponent<Button>().onClick.AddListener(() => showEditor(idx, null));
                        Image add = UIFactory.Icon(card.transform, "hud_add", 120);
                        add.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(120, 120));
                        Txt(card.transform, L.T("Chỗ trống\nChạm để bày hàng"), 26, UIFactory.Wood, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(230, 90), true);
                        continue;
                    }

                    ItemDef it = Defs.Item(s.item);
                    bool sold = Market.IsSold(s);
                    Image c = UIFactory.Panel(row, sold ? new Color(0.78f, 0.93f, 0.66f) : Color.white, "Listing");
                    Image icon = UIFactory.Icon(c.transform, it.icon, 140);
                    icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(140, 140));
                    Txt(c.transform, $"{L.T(it.name)}\nx{s.qty}", 28, UIFactory.TextDark, TextAnchor.MiddleCenter,
                        new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(240, 80), true);
                    Image coin = UIFactory.Icon(c.transform, "hud_coin", 40);
                    coin.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(1, 0.5f), new Vector2(-30, -282), new Vector2(40, 40));
                    Txt(c.transform, s.price.ToString(), 32, UIFactory.Wood, TextAnchor.MiddleLeft,
                        new Vector2(0.5f, 1), new Vector2(0, 0.5f), new Vector2(-24, -282), new Vector2(140, 44), true);

                    if (sold)
                    {
                        Txt(c.transform, L.T("Đã có khách mua!"), 26, UIFactory.GreenDark, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(240, 60), true);
                        Button get = UIFactory.MakeButton(c.transform, L.F("Nhận {0} xu", s.price), UIFactory.Orange, () =>
                        {
                            int coins = Market.Collect(idx);
                            if (coins > 0) Toast(L.F("Đã nhận {0} xu từ chợ!", coins));
                            showStall();
                        }, 26);
                        ((RectTransform)get.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(220, 74));
                    }
                    else
                    {
                        Text t = Txt(c.transform, "", 24, UIFactory.TextDark, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -318), new Vector2(240, 70));
                        timers.Add(new KeyValuePair<Text, MarketSlotData>(t, s));
                        Button back = UIFactory.MakeButton(c.transform, L.T("Thu về kho"), UIFactory.Gray, () =>
                        {
                            string err = Market.Cancel(idx);
                            Toast(err ?? L.T("Đã mang hàng về kho."));
                            showStall();
                        }, 24);
                        ((RectTransform)back.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(220, 66));
                        MarketSlotData listing = s;
                        Button now = GemButton(c.transform, L.T("Bán ngay"), () => listing.soldAt - gm.Now, () =>
                        {
                            listing.soldAt = gm.Now;
                            showStall();
                        }, 22);
                        ((RectTransform)now.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(220, 60));
                    }
                }

                // buy one more slot
                if (Market.CanBuySlot)
                {
                    int cost = Market.NextSlotCost;
                    Image more = UIFactory.Panel(row, new Color(0.85f, 0.78f, 0.66f), "MoreSlot");
                    Image lockIc = UIFactory.Icon(more.transform, "hud_upgrade", 110);
                    lockIc.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(110, 110));
                    Txt(more.transform, L.T("Thêm chỗ\nbày hàng"), 28, UIFactory.TextDark, TextAnchor.MiddleCenter,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(230, 90), true);
                    Button buy = UIFactory.MakeButton(more.transform, L.F("{0} xu", cost), gm.CanAfford(cost) ? UIFactory.Green : UIFactory.Red, () =>
                    {
                        string err = Market.BuySlot();
                        Toast(err ?? L.T("Đã mở thêm một chỗ bày hàng!"));
                        showStall();
                    }, 28);
                    ((RectTransform)buy.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(200, 70));
                }

                // footer
                Txt(content, L.T("Khách ghé chợ mua hàng theo giá bạn đặt: <b>giá rẻ bán nhanh, giá cao phải chờ lâu</b>.\nHàng đang bày không chiếm chỗ trong kho."), 24, UIFactory.TextDark,
                    TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 4), new Vector2(1000, 84));
                if (soldShown > 1)
                {
                    Button all = UIFactory.MakeButton(content, L.T("Nhận tất cả"), UIFactory.Orange, () =>
                    {
                        int coins = Market.CollectAll();
                        if (coins > 0) Toast(L.F("Đã nhận {0} xu từ chợ!", coins));
                        showStall();
                    }, 28);
                    ((RectTransform)all.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 8), new Vector2(300, 80));
                }
                UpdateTimers();
            };

            void UpdateTimers()
            {
                if (hotTimer != null) hotTimer.text = L.T("Đổi mặt hàng sau\n") + Defs.FormatTime(Market.HotLeft);
                foreach (var kv in timers)
                    kv.Key.text = L.T("Đang chờ khách…\n") + Defs.FormatTime(kv.Value.soldAt - gm.Now);
            }

            // ------------------------------------------------------------- listing editor
            showEditor = (slot, pre) =>
            {
                editing = true;
                ClearChildren(content);
                string selected = pre != null && gm.Count(pre) > 0 ? pre : null;
                int qty = 1, price = 0;

                Txt(content, L.T("Chọn hàng trong kho để bày bán"), 28, UIFactory.TextDark, TextAnchor.MiddleLeft,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, 0), new Vector2(700, 56), true);
                ScrollRect sr = UIFactory.Scroll(content, false, out RectTransform grid);
                RectTransform srt = (RectTransform)sr.transform;
                srt.anchorMin = new Vector2(0, 0);
                srt.anchorMax = new Vector2(0, 1);
                srt.pivot = new Vector2(0, 0.5f);
                srt.offsetMin = new Vector2(0, 0);
                srt.offsetMax = new Vector2(720, -60);
                UIFactory.Grid(grid, new Vector2(132, 164), new Vector2(12, 12), false);

                Image side = UIFactory.Panel(content, UIFactory.CreamDark, "Editor");
                side.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(580, 780));
                Transform st = side.transform;

                Image selIcon = UIFactory.Icon(st, "hud_inventory", 150);
                selIcon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(150, 150));
                Text nameText = Txt(st, "", 30, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -172), new Vector2(560, 80), true);

                Txt(st, L.T("Số lượng"), 26, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -270), new Vector2(160, 70), true);
                Button qMinus = UIFactory.MakeButton(st, "–", UIFactory.Wood, null, 40);
                ((RectTransform)qMinus.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -270), new Vector2(80, 70));
                Text qtyText = Txt(st, "", 34, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(276, -270), new Vector2(150, 70), true);
                Button qPlus = UIFactory.MakeButton(st, "+", UIFactory.Wood, null, 40);
                ((RectTransform)qPlus.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(432, -270), new Vector2(80, 70));

                Txt(st, L.T("Giá bán"), 26, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -356), new Vector2(160, 70), true);
                Button pMinus = UIFactory.MakeButton(st, "–", UIFactory.Wood, null, 40);
                ((RectTransform)pMinus.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -356), new Vector2(80, 70));
                Text priceText = Txt(st, "", 34, UIFactory.Wood, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(276, -356), new Vector2(150, 70), true);
                Button pPlus = UIFactory.MakeButton(st, "+", UIFactory.Wood, null, 40);
                ((RectTransform)pPlus.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(432, -356), new Vector2(80, 70));

                RectTransform presets = UIFactory.Rect("Presets", st);
                presets.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -446), new Vector2(560, 64));
                UIFactory.HRow(presets, 12);
                var presetDefs = new[] { (L.T("Rẻ"), 0.8f, UIFactory.Blue), (L.T("Chuẩn"), 1f, UIFactory.Green), ("Cao", 1.5f, UIFactory.Orange) };
                var presetBtns = new List<Button>();

                Text info = Txt(st, "", 24, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -526), new Vector2(560, 110));

                Button list = UIFactory.MakeButton(st, L.T("Bày bán"), UIFactory.Green, null, 32);
                ((RectTransform)list.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(260, 84));
                Button cancel = UIFactory.MakeButton(st, L.T("Quay lại"), UIFactory.Gray, () => showStall(), 30);
                ((RectTransform)cancel.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(240, 84));

                Action refresh = null;

                void Select(string id)
                {
                    selected = id;
                    qty = Mathf.Max(1, Mathf.Min(gm.Count(id), Defs.MarketMaxQty));
                    price = Market.FairPrice(id, qty);
                    refresh();
                }

                void SetQty(int q)
                {
                    if (selected == null) return;
                    q = Mathf.Clamp(q, 1, Mathf.Min(gm.Count(selected), Defs.MarketMaxQty));
                    float ratio = price / (float)Market.FairPrice(selected, qty);
                    qty = q;
                    price = Mathf.Clamp(Mathf.RoundToInt(Market.FairPrice(selected, qty) * ratio), Market.MinPrice(selected, qty), Market.MaxPrice(selected, qty));
                    refresh();
                }

                void SetPrice(int p)
                {
                    if (selected == null) return;
                    price = Mathf.Clamp(p, Market.MinPrice(selected, qty), Market.MaxPrice(selected, qty));
                    refresh();
                }

                int Step() => selected == null ? 1 : Mathf.Max(1, Mathf.RoundToInt(Market.FairPrice(selected, qty) * 0.1f));

                qMinus.onClick.AddListener(() => SetQty(qty - 1));
                qPlus.onClick.AddListener(() => SetQty(qty + 1));
                pMinus.onClick.AddListener(() => SetPrice(price - Step()));
                pPlus.onClick.AddListener(() => SetPrice(price + Step()));
                foreach (var p in presetDefs)
                {
                    float mul = p.Item2;
                    Button b = UIFactory.MakeButton(presets, p.Item1, p.Item3, () =>
                    {
                        if (selected != null) SetPrice(Mathf.RoundToInt(Market.FairPrice(selected, qty) * mul));
                    }, 26);
                    ((RectTransform)b.transform).sizeDelta = new Vector2(170, 60);
                    presetBtns.Add(b);
                }

                list.onClick.AddListener(() =>
                {
                    if (selected == null) { Toast(L.T("Hãy chọn một món hàng.")); return; }
                    string err = Market.Put(slot, selected, qty, price);
                    if (err != null) { Toast(err); return; }
                    Toast(L.F("Đã bày {0} {1} lên quầy.", qty, Defs.Item(selected).name));
                    showStall();
                });

                refresh = () =>
                {
                    ClearChildren(grid);
                    var stacks = gm.Data.items.FindAll(s => Defs.Item(s.id) != null);
                    stacks.Sort((a, b) =>
                    {
                        int h = Market.IsHot(b.id).CompareTo(Market.IsHot(a.id));
                        return h != 0 ? h : string.CompareOrdinal(a.id, b.id);
                    });
                    if (stacks.Count == 0)
                    {
                        Text empty = UIFactory.Label(grid, L.T("Kho trống – hãy thu hoạch nông sản!"), 28, UIFactory.Wood);
                        empty.rectTransform.sizeDelta = new Vector2(600, 100);
                    }
                    foreach (ItemStack s in stacks)
                    {
                        ItemDef def = Defs.Item(s.id);
                        string id = s.id;
                        Image card = UIFactory.Panel(grid, id == selected ? new Color(0.7f, 0.92f, 0.55f) : Color.white, "Item");
                        card.gameObject.AddComponent<Button>().onClick.AddListener(() => Select(id));
                        Image ic = UIFactory.Icon(card.transform, def.icon, 96);
                        ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(96, 96));
                        Txt(card.transform, "x" + s.count, 28, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(124, 34), true);
                        Txt(card.transform, def.name, 18, UIFactory.Wood, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(128, 26));
                        if (Market.IsHot(id))
                        {
                            Image tag = UIFactory.Panel(card.transform, UIFactory.Red, "Hot", false);
                            tag.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-4, -4), new Vector2(58, 30));
                            Text tt = UIFactory.Label(tag.transform, "HOT", 18, Color.white, TextAnchor.MiddleCenter, true);
                            tt.rectTransform.Stretch();
                        }
                    }

                    bool has = selected != null;
                    foreach (Button b in new[] { qMinus, qPlus, pMinus, pPlus, list }) b.interactable = has;
                    foreach (Button b in presetBtns) b.interactable = has;
                    if (!has)
                    {
                        selIcon.sprite = SpriteDB.Get("hud_inventory");
                        nameText.text = L.T("Chưa chọn hàng");
                        qtyText.text = "–";
                        priceText.text = "–";
                        info.text = L.T("Chọn một món bên trái.");
                        return;
                    }
                    ItemDef sel = Defs.Item(selected);
                    selIcon.sprite = SpriteDB.Get(sel.icon);
                    nameText.text = L.F("{0}  <size=24>(trong kho: {1})</size>", sel.name, gm.Count(selected)) + (Market.IsHot(selected) ? L.T("\n<color=#E05A47><size=24>Đang giá hot!</size></color>") : "");
                    qtyText.text = qty.ToString();
                    priceText.text = price + L.T(" xu");
                    int fair = Market.FairPrice(selected, qty);
                    float sec = Market.ExpectedSec(selected, qty, price);
                    string speed = Market.Speed(selected, qty, price);
                    Color sc = price <= fair * 1.1f ? UIFactory.GreenDark : price <= fair * 1.5f ? UIFactory.Orange : UIFactory.Red;
                    info.text = L.F("Giá chuẩn: {0} xu\n<color=#{1}><b>{2}</b></color> – khách mua sau khoảng {3}", fair, ColorUtility.ToHtmlStringRGB(sc), speed, Defs.FormatTime(Mathf.Max(5f, sec)));
                };
                if (selected != null) Select(selected);
                else refresh();
            };

            // ------------------------------------------------------------- open
            if (preselect != null)
            {
                int free = Market.Slots.FindIndex(Market.IsEmpty);
                if (free >= 0) showEditor(free, preselect);
                else
                {
                    showStall();
                    Toast(L.T("Quầy đã kín chỗ – hãy chờ khách mua hoặc mở thêm chỗ."));
                }
            }
            else showStall();

            SetTicker(modal, () =>
            {
                if (editing) return;
                int sold = Market.Slots.FindAll(Market.IsSold).Count;
                if (sold != soldShown) showStall();
                else UpdateTimers();
            });
        }
    }
}
