using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    public partial class UIManager
    {
        private GameObject tickModal;
        private Action tickAction;
        private float tickTimer;

        private void LateUpdate()
        {
            if (tickModal == null) { tickAction = null; return; }
            tickTimer -= Time.unscaledDeltaTime;
            if (tickTimer <= 0f)
            {
                tickTimer = 1f;
                tickAction?.Invoke();
            }
        }

        private void SetTicker(GameObject modal, Action a)
        {
            tickModal = modal;
            tickAction = a;
            tickTimer = 1f;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        private static Text Txt(Transform parent, string s, int size, Color c, TextAnchor a, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sz, bool bold = false)
        {
            Text t = UIFactory.Label(parent, s, size, c, a, bold);
            t.rectTransform.Place(anchor, pivot, pos, sz);
            return t;
        }

        // ================================================================== STORAGE / MARKET
        public void OpenStorage(bool market)
        {
            RectTransform content = OpenWindow(market ? "Chợ nông sản" : "Kho hàng", new Vector2(1300, 820), out GameObject modal);
            GameManager gm = GameManager.I;
            string selected = null;

            Text cap = Txt(content, "", 30, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(560, 50), true);
            Button up = UIFactory.MakeButton(content, " ", UIFactory.Blue, null, 26);
            ((RectTransform)up.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -2), new Vector2(460, 64));
            Text upText = up.GetComponentInChildren<Text>();

            ScrollRect sr = UIFactory.Scroll(content, false, out RectTransform grid);
            ((RectTransform)sr.transform).Stretch();
            ((RectTransform)sr.transform).offsetMax = new Vector2(0, -76);
            ((RectTransform)sr.transform).offsetMin = new Vector2(0, 130);
            UIFactory.Grid(grid, new Vector2(170, 200), new Vector2(16, 16), false);

            Image bottom = UIFactory.Panel(content, UIFactory.CreamDark, "SellBar");
            bottom.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(1240, 118));
            Image selIcon = UIFactory.Icon(bottom.transform, "hud_inventory", 96);
            selIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(96, 96));
            Text selText = Txt(bottom.transform, "", 28, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(124, 0), new Vector2(520, 100), true);
            Button sell1 = UIFactory.MakeButton(bottom.transform, " ", UIFactory.Green, null, 28);
            ((RectTransform)sell1.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-290, 0), new Vector2(250, 84));
            Button sellAll = UIFactory.MakeButton(bottom.transform, " ", UIFactory.Orange, null, 28);
            ((RectTransform)sellAll.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(260, 84));

            Action rebuild = null;

            void Sell(int n)
            {
                if (selected == null) return;
                ItemDef def = Defs.Item(selected);
                int have = gm.Count(selected);
                n = Mathf.Min(n, have);
                if (n <= 0 || !gm.RemoveItem(selected, n)) return;
                gm.AddCoins(def.sell * n);
                GameEvents.Raise("sell", selected, n);
                Toast($"Đã bán {n} {def.name}: +{def.sell * n} xu");
                if (gm.Count(selected) <= 0) selected = null;
                rebuild();
            }

            sell1.onClick.AddListener(() => Sell(1));
            sellAll.onClick.AddListener(() => Sell(int.MaxValue));
            up.onClick.AddListener(() =>
            {
                if (gm.TryUpgradeStorage()) Toast($"Kho đã nâng cấp! Sức chứa {gm.StorageCapacity}");
                else Toast("Không đủ xu để nâng cấp kho.");
                rebuild();
            });

            rebuild = () =>
            {
                cap.text = $"Sức chứa: {gm.StorageUsed}/{gm.StorageCapacity}";
                upText.text = $"Nâng cấp (+{Defs.StoragePerUpgrade}) – {gm.StorageUpgradeCost} xu";
                ClearChildren(grid);
                var stacks = new List<ItemStack>(gm.Data.items);
                stacks.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));
                if (stacks.Count == 0)
                {
                    Text empty = UIFactory.Label(grid, "Kho trống – hãy thu hoạch nông sản!", 30, UIFactory.Wood);
                    empty.rectTransform.sizeDelta = new Vector2(600, 100);
                }
                foreach (ItemStack s in stacks)
                {
                    ItemDef def = Defs.Item(s.id);
                    if (def == null) continue;
                    string id = s.id;
                    Image card = UIFactory.Panel(grid, id == selected ? new Color(0.7f, 0.92f, 0.55f) : Color.white, "Item");
                    card.gameObject.AddComponent<Button>().onClick.AddListener(() => { selected = id; rebuild(); });
                    Image ic = UIFactory.Icon(card.transform, def.icon, 110);
                    ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(110, 110));
                    Txt(card.transform, "x" + s.count, 30, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(160, 36), true);
                    Txt(card.transform, def.sell + " xu", 22, UIFactory.Wood, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(160, 30));
                }

                if (selected != null)
                {
                    ItemDef def = Defs.Item(selected);
                    int have = gm.Count(selected);
                    selIcon.sprite = SpriteDB.Get(def.icon);
                    selText.text = $"{def.name}  (có {have})\nGiá bán: {def.sell} xu / cái";
                    sell1.GetComponentInChildren<Text>().text = $"Bán 1 (+{def.sell})";
                    sellAll.GetComponentInChildren<Text>().text = $"Bán hết (+{def.sell * have})";
                    sell1.interactable = sellAll.interactable = true;
                }
                else
                {
                    selIcon.sprite = SpriteDB.Get("hud_inventory");
                    selText.text = market ? "Chọn nông sản để bán lấy xu" : "Chọn một món để xem / bán";
                    sell1.GetComponentInChildren<Text>().text = "Bán 1";
                    sellAll.GetComponentInChildren<Text>().text = "Bán hết";
                    sell1.interactable = sellAll.interactable = false;
                }
            };
            rebuild();
        }

        // ================================================================== SHOP
        private static readonly string[] ShopTabs = { "land", "animal", "house", "machine", "decor" };
        private static readonly string[] ShopTabNames = { "Đất & Cây", "Vật nuôi", "Chuồng trại", "Chế biến", "Trang trí" };

        public void OpenShop(string tab)
        {
            if (PlacementController.I.Active) return;
            RectTransform content = OpenWindow("Cửa hàng", new Vector2(1440, 860), out GameObject modal);
            GameManager gm = GameManager.I;
            string current = tab ?? "land";

            RectTransform tabs = UIFactory.Rect("Tabs", content);
            tabs.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(1380, 80));
            UIFactory.HRow(tabs, 16);
            var tabButtons = new List<Image>();

            ScrollRect sr = UIFactory.Scroll(content, false, out RectTransform grid);
            ((RectTransform)sr.transform).Stretch();
            ((RectTransform)sr.transform).offsetMax = new Vector2(0, -92);
            UIFactory.Grid(grid, new Vector2(250, 300), new Vector2(18, 18), false);

            Action rebuild = null;
            for (int i = 0; i < ShopTabs.Length; i++)
            {
                string key = ShopTabs[i];
                Button b = UIFactory.MakeButton(tabs, ShopTabNames[i], UIFactory.Wood, () => { current = key; rebuild(); }, 28);
                ((RectTransform)b.transform).sizeDelta = new Vector2(262, 72);
                tabButtons.Add(b.GetComponent<Image>());
            }

            void Card(string sprite, string name, string sub, int price, int unlock, Action buy, float iconScale = 1f)
            {
                bool locked = gm.Data.level < unlock;
                Image card = UIFactory.Panel(grid, Color.white, "Card");
                Image ic = UIFactory.Icon(card.transform, sprite, 160 * iconScale);
                ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -88), new Vector2(170, 150));
                Txt(card.transform, name, 26, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(240, 60), true);
                if (!string.IsNullOrEmpty(sub))
                    Txt(card.transform, sub, 20, UIFactory.Wood, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(240, 28));
                Button b = UIFactory.MakeButton(card.transform, unlock >= 999 ? "Đã có" : locked ? $"Cấp {unlock}" : $"{price} xu",
                    locked ? UIFactory.Gray : (gm.CanAfford(price) ? UIFactory.Green : UIFactory.Red), null, 28);
                ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(210, 60));
                b.onClick.AddListener(() =>
                {
                    if (unlock >= 999) { Toast("Bạn đã có công trình này."); return; }
                    if (locked) { Toast($"Mở khoá ở cấp {unlock}."); return; }
                    if (!gm.CanAfford(price)) { Toast("Không đủ xu!"); return; }
                    buy();
                });
            }

            rebuild = () =>
            {
                for (int i = 0; i < tabButtons.Count; i++)
                    tabButtons[i].color = ShopTabs[i] == current ? UIFactory.GreenDark : UIFactory.Wood;
                ClearChildren(grid);
                switch (current)
                {
                    case "land":
                        Card("carrot_dry", "Ô đất mới", $"Đã mua thêm: {gm.Data.plotsBought}", gm.NextPlotCost, 1, () => BuyPlace(Defs.Place("plot"), modal), 0.9f);
                        Card("prop_sprinkler", "Vòi phun", "Tự tưới 3x3 ô", Defs.Place("sprinkler").cost, Defs.Place("sprinkler").unlock, () => BuyPlace(Defs.Place("sprinkler"), modal));
                        Card("building_barn", "Nâng cấp kho", $"Sức chứa {gm.StorageCapacity} → {gm.StorageCapacity + Defs.StoragePerUpgrade}", gm.StorageUpgradeCost, 1, () =>
                        {
                            if (gm.TryUpgradeStorage()) Toast($"Kho đã nâng cấp! Sức chứa {gm.StorageCapacity}");
                            rebuild();
                        });
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.kind == PlaceKind.Tree)
                            {
                                PlaceDef def = p;
                                TreeDef td = Defs.Tree(def.id);
                                Card(def.sprite, def.name, $"{td.yield} {Defs.Item(td.fruit).name} / {Defs.FormatTime(td.cycleSec)} · {td.harvests} lần", def.cost, def.unlock, () => BuyPlace(def, modal));
                            }
                        break;
                    case "machine":
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.category == "machine")
                            {
                                PlaceDef def = p;
                                bool owned = WorldMap.I.CountPlaces(def.id) > 0;
                                string sub;
                                if (def.kind == PlaceKind.Delivery) sub = "Đơn lớn 6 thùng hàng";
                                else
                                {
                                    var outs = new List<string>();
                                    foreach (RecipeDef r in Defs.Recipes) if (r.machine == def.id) outs.Add(Defs.Item(r.output).name);
                                    sub = string.Join(", ", outs);
                                }
                                if (owned)
                                    Card(def.sprite, def.name, "Đã xây · " + sub, 0, 999, () => { });
                                else
                                    Card(def.sprite, def.name, sub, def.cost, def.unlock, () => BuyPlace(def, modal));
                            }
                        break;
                    case "animal":
                        foreach (AnimalDef a in Defs.Animals)
                        {
                            AnimalDef def = a;
                            int owned = gm.Data.animals.FindAll(x => x.type == def.id).Count;
                            string houseName = Defs.Place(def.house).name;
                            Card(def.Frame("idle_se"), def.name, $"Có {owned} · ăn {Defs.Crop(def.feed).name}", def.cost, def.unlock, () =>
                            {
                                PlaceView house = WorldMap.I.FindHouseWithSpace(def.house);
                                if (house == null)
                                {
                                    Toast($"Cần {houseName} còn chỗ trống. Xây thêm chuồng hoặc nâng cấp chuồng.");
                                    return;
                                }
                                if (!gm.TrySpend(def.cost)) return;
                                AnimalView av = WorldMap.I.AddAnimal(def.id, house);
                                gm.AddXp(3);
                                GameEvents.Raise("buy_animal", def.id);
                                Toast($"Đã mua {def.name}! Nó đang ở {houseName}.");
                                rebuild();
                            });
                        }
                        break;
                    case "house":
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.category == "house")
                            {
                                PlaceDef def = p;
                                Card(def.sprite, def.name, $"Đang có {WorldMap.I.CountPlaces(def.id)} · chứa {def.capacity}+ con", def.cost, def.unlock, () => BuyPlace(def, modal));
                            }
                        break;
                    default:
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.category == "decor")
                            {
                                PlaceDef def = p;
                                Card(def.sprite, def.name, $"+{def.Xp} XP", def.cost, def.unlock, () => BuyPlace(def, modal));
                            }
                        break;
                }
                sr.verticalNormalizedPosition = 1f;
            };
            rebuild();
        }

        private void BuyPlace(PlaceDef def, GameObject shopModal)
        {
            CloseModal(shopModal);
            GameManager gm = GameManager.I;
            if (InteractionController.I.BuildMode) ToggleBuildMode();
            PlacementController.I.BeginNew(def, (gx, gy) =>
            {
                int price = def.kind == PlaceKind.Plot ? gm.NextPlotCost : def.cost;
                if (!gm.TrySpend(price))
                {
                    Toast("Không đủ xu!");
                    return;
                }
                Vector3 at;
                if (def.kind == PlaceKind.Plot)
                {
                    PlotView pv = WorldMap.I.AddPlot(gx, gy);
                    gm.Data.plotsBought++;
                    gm.AddXp(2);
                    GameEvents.Raise("buy_plot");
                    at = pv.transform.position;
                }
                else
                {
                    PlaceView pv = WorldMap.I.AddPlace(def.id, gx, gy);
                    gm.AddXp(def.Xp);
                    GameEvents.Raise("buy_place", def.id);
                    at = pv.transform.position;
                    if (def.kind == PlaceKind.House) Toast($"Đã xây {def.name}! Vào Cửa hàng › Vật nuôi để mua thú.");
                }
                FloatText(at + Vector3.up, "-" + price, "hud_coin");
            });
        }

        // ================================================================== ORDERS
        public void OpenOrders()
        {
            RectTransform content = OpenWindow("Bảng đơn hàng", new Vector2(1500, 760), out GameObject modal);
            OrderSystem os = OrderSystem.I;
            GameManager gm = GameManager.I;
            RectTransform row = UIFactory.Rect("Row", content).Stretch();
            UIFactory.HRow(row, 18);

            Action rebuild = null;
            rebuild = () =>
            {
                ClearChildren(row);
                for (int i = 0; i < os.Orders.Count; i++)
                {
                    int idx = i;
                    OrderData o = os.Orders[i];
                    Image card = UIFactory.Panel(row, Color.white, "Order");
                    card.rectTransform.sizeDelta = new Vector2(270, 640);
                    if (os.IsWaiting(o))
                    {
                        Image ic = UIFactory.Icon(card.transform, "prop_harvest_crate", 150);
                        ic.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(150, 150));
                        Txt(card.transform, "Đơn mới sau\n" + Defs.FormatTime(o.availableAt - gm.Now), 28, UIFactory.Wood, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(250, 90), true);
                        continue;
                    }
                    float y = -20;
                    foreach (ItemStack r in o.req)
                    {
                        ItemDef def = Defs.Item(r.id);
                        int have = gm.Count(r.id);
                        Image ic = UIFactory.Icon(card.transform, def.icon, 100);
                        ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, y), new Vector2(100, 100));
                        Txt(card.transform, $"{have}/{r.count}", 32, have >= r.count ? UIFactory.GreenDark : UIFactory.Red, TextAnchor.MiddleLeft,
                            new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, y - 18), new Vector2(130, 40), true);
                        Txt(card.transform, def.name, 22, UIFactory.TextDark, TextAnchor.MiddleLeft,
                            new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, y - 56), new Vector2(140, 30));
                        y -= 115;
                    }
                    Image coin = UIFactory.Icon(card.transform, "hud_coin", 60);
                    coin.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 170), new Vector2(60, 60));
                    Txt(card.transform, o.coins.ToString(), 32, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(84, 180), new Vector2(90, 40), true);
                    Txt(card.transform, "+" + o.xp + " XP", 24, UIFactory.Blue, TextAnchor.MiddleRight, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 180), new Vector2(100, 40), true);

                    bool can = os.CanFulfill(o);
                    Button give = UIFactory.MakeButton(card.transform, "Giao hàng", can ? UIFactory.Green : UIFactory.Gray, () =>
                    {
                        if (os.Fulfill(idx)) Toast($"Giao hàng thành công! +{o.coins} xu, +{o.xp} XP");
                        else Toast("Chưa đủ hàng cho đơn này.");
                        rebuild();
                    }, 28);
                    ((RectTransform)give.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 86), new Vector2(240, 70));
                    Button drop = UIFactory.MakeButton(card.transform, "Bỏ đơn", UIFactory.Red, () => { os.Discard(idx); rebuild(); }, 22);
                    ((RectTransform)drop.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(160, 54));
                }
            };
            rebuild();
            SetTicker(modal, rebuild);
        }

        // ================================================================== FARMHOUSE / HOUSE
        public void OpenFarmhouse()
        {
            RectTransform content = OpenWindow("Nông trại của bạn", new Vector2(980, 640), out GameObject modal);
            GameManager gm = GameManager.I;
            SaveData d = gm.Data;
            Image fh = UIFactory.Icon(content, "building_farmhouse", 300);
            fh.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 30), new Vector2(320, 320));
            string stats =
                $"Cấp độ: <b>{d.level}</b>   ({d.xp}/{Defs.XpToNext(d.level)} XP)\n" +
                $"Xu: <b>{d.coins:N0}</b>\n" +
                $"Ô đất: <b>{d.plots.Count}</b>    Vật nuôi: <b>{d.animals.Count}</b>\n" +
                $"Kho: <b>{gm.StorageUsed}/{gm.StorageCapacity}</b>\n" +
                $"Chuỗi đăng nhập: <b>{d.dailyStreak}</b> ngày";
            Txt(content, stats, 32, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -20), new Vector2(560, 300));

            string next = "";
            foreach (CropDef c in Defs.Crops) if (c.unlock > d.level) { next = $"Cấp {c.unlock}: mở {c.name}"; break; }
            foreach (LandZone z in Defs.Zones)
                if (!d.zones.Contains(z.id)) { next += (next.Length > 0 ? "\n" : "") + $"Đất mới: {z.name} (cấp {z.unlock})"; break; }
            Txt(content, next, 26, UIFactory.GreenDark, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(360, 100), new Vector2(560, 80), true);

            Button s = UIFactory.MakeButton(content, "Cài đặt", UIFactory.Blue, () => { CloseModal(modal); OpenSettings(); }, 28);
            ((RectTransform)s.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 10), new Vector2(220, 70));
        }

        public void OpenHouse(PlaceView house)
        {
            RectTransform content = OpenWindow(house.Def.name, new Vector2(960, 600), out GameObject modal);
            GameManager gm = GameManager.I;
            int n = WorldMap.I.AnimalsInHouse(house.D.uid);
            int cap = WorldMap.I.HouseCapacity(house);
            PlaceState st = gm.Data.State(house.D.uid);
            Image ic = UIFactory.Icon(content, house.Def.sprite, 260);
            ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 30), new Vector2(280, 280));
            var lines = new List<string>();
            foreach (AnimalDef a in Defs.Animals)
                if (a.house == house.Def.id)
                    lines.Add($"• {a.name}: ăn {Defs.Crop(a.feed).name} → {Defs.Item(a.product).name} ({Defs.FormatTime(a.produceSec)})");
            Txt(content, $"Đang nuôi: <b>{n}/{cap}</b> con   ·   Cấp chuồng: <b>{st.level + 1}/{Defs.HouseMaxLevel + 1}</b>\n\n" + string.Join("\n", lines), 28, UIFactory.TextDark, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(310, -20), new Vector2(600, 320));
            Button buy = UIFactory.MakeButton(content, "Mua vật nuôi", UIFactory.Green, () => { CloseModal(modal); OpenShop("animal"); }, 28);
            ((RectTransform)buy.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 10), new Vector2(280, 74));
            if (st.level < Defs.HouseMaxLevel)
            {
                int cost = Defs.HouseUpgradeCost(house.Def, st.level);
                Button up = UIFactory.MakeButton(content, $"Nâng cấp +{Defs.HouseSlotsPerLevel} chỗ – {cost} xu", gm.CanAfford(cost) ? UIFactory.Blue : UIFactory.Gray, () =>
                {
                    if (!gm.TrySpend(cost)) { Toast("Không đủ xu!"); return; }
                    st.level++;
                    gm.AddXp(5);
                    Toast($"Đã nâng cấp {house.Def.name}! Sức chứa {WorldMap.I.HouseCapacity(house)} con.");
                    CloseModal(modal);
                    OpenHouse(house);
                }, 26);
                ((RectTransform)up.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-310, 10), new Vector2(380, 74));
            }
        }

        // ================================================================== LAND EXPANSION
        public void OpenZone(LandZone z)
        {
            if (PlacementController.I.Active) return;
            RectTransform content = OpenWindow("Mở rộng đất", new Vector2(940, 560), out GameObject modal);
            GameManager gm = GameManager.I;
            bool levelOk = gm.Data.level >= z.unlock;
            bool coinsOk = gm.CanAfford(z.cost);
            int debris = 0;
            foreach (DebrisData d in gm.Data.debris) if (z.Contains(d.gx, d.gy)) debris++;

            Image ic = UIFactory.Icon(content, "decor_wooden_sign", 230);
            ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 40), new Vector2(240, 240));
            string msg =
                $"<b>{z.name}</b>\n" +
                $"Diện tích: {z.Cells} ô đất\n" +
                $"Có {debris} gốc cây / tảng đá cần dọn\n\n" +
                $"Yêu cầu cấp: <color={(levelOk ? "#3B8A28" : "#D04A3A")}><b>{z.unlock}</b></color>   (bạn đang cấp {gm.Data.level})\n" +
                $"Chi phí: <color={(coinsOk ? "#3B8A28" : "#D04A3A")}><b>{z.cost:N0} xu</b></color>";
            Txt(content, msg, 30, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(270, -10), new Vector2(600, 320));

            string label = !levelOk ? $"Cần cấp {z.unlock}" : $"Mở rộng – {z.cost:N0} xu";
            Color col = !levelOk ? UIFactory.Gray : coinsOk ? UIFactory.Green : UIFactory.Red;
            Button b = UIFactory.MakeButton(content, label, col, () =>
            {
                if (!levelOk) { Toast($"Đạt cấp {z.unlock} để mở rộng vùng này."); return; }
                if (!gm.TrySpend(z.cost)) { Toast("Không đủ xu!"); return; }
                CloseModal(modal);
                WorldMap.I.UnlockZone(z);
                gm.AddXp(Mathf.Max(5, z.cost / 20));
                GameEvents.Raise("unlock_zone", z.id.ToString());
                Vector3 center = IsoGrid.ToWorld((z.x0 + z.x1) * 0.5f, (z.y0 + z.y1) * 0.5f);
                CameraController.I.FocusOn(center);
                Toast($"Đã mở rộng: {z.name}!");
            }, 32);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(420, 86));
        }

        // ================================================================== SETTINGS
        public void OpenSettings()
        {
            RectTransform content = OpenWindow("Cài đặt", new Vector2(860, 560), out GameObject modal);
            GameManager gm = GameManager.I;
            Txt(content, "Tốc độ thời gian (dùng để thử nghiệm):", 28, UIFactory.TextDark, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(760, 50), true);
            Button speed = null;
            speed = UIFactory.MakeButton(content, gm.Speed > 1 ? "x10 (đang bật)" : "x1 (bình thường)", gm.Speed > 1 ? UIFactory.Orange : UIFactory.Green, () =>
            {
                gm.Speed = gm.Speed > 1 ? 1f : 10f;
                speed.GetComponentInChildren<Text>().text = gm.Speed > 1 ? "x10 (đang bật)" : "x1 (bình thường)";
                speed.GetComponent<Image>().color = gm.Speed > 1 ? UIFactory.Orange : UIFactory.Green;
            }, 28);
            ((RectTransform)speed.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -80), new Vector2(360, 76));

            Button save = UIFactory.MakeButton(content, "Lưu ngay", UIFactory.Blue, () => { gm.SaveNow(); Toast("Đã lưu!"); }, 28);
            ((RectTransform)save.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(400, -80), new Vector2(300, 76));

            Button reset = UIFactory.MakeButton(content, "Xoá dữ liệu & chơi lại", UIFactory.Red, () =>
                Confirm("Xoá toàn bộ tiến trình?", "Nông trại sẽ trở về ban đầu. Không thể hoàn tác.", () => Bootstrap.ResetGame()), 28);
            ((RectTransform)reset.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -200), new Vector2(460, 76));

            Button cheat = UIFactory.MakeButton(content, "Thử nghiệm: +1000 xu, +1 cấp", UIFactory.Orange, () =>
            {
                gm.AddCoins(1000);
                if (gm.Data.level < Defs.MaxLevel) gm.AddXp(Defs.XpToNext(gm.Data.level) - gm.Data.xp);
            }, 24);
            ((RectTransform)cheat.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(500, -200), new Vector2(300, 76));

            Txt(content, "Nông Trại Vui Vẻ v" + Application.version + "\nĐiều khiển: joystick / WASD để đi, chạm để làm việc,\nkéo qua nhiều ô để thao tác hàng loạt, 2 ngón để zoom.",
                22, UIFactory.Wood, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 10), new Vector2(780, 110));
        }

        public void Confirm(string title, string message, Action onYes)
        {
            RectTransform content = OpenWindow(title, new Vector2(820, 420), out GameObject modal);
            Txt(content, message, 30, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(740, 160));
            Button yes = UIFactory.MakeButton(content, "Đồng ý", UIFactory.Red, () => { CloseModal(modal); onYes(); }, 30);
            ((RectTransform)yes.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-160, 16), new Vector2(260, 80));
            Button no = UIFactory.MakeButton(content, "Huỷ", UIFactory.Gray, () => CloseModal(modal), 30);
            ((RectTransform)no.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(160, 16), new Vector2(260, 80));
        }

        public void Message(string title, string message, string icon, string button = "Tuyệt!")
        {
            RectTransform content = OpenWindow(title, new Vector2(900, 560), out GameObject modal);
            float x = 0;
            if (!string.IsNullOrEmpty(icon))
            {
                Image ic = UIFactory.Icon(content, icon, 220);
                ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 40), new Vector2(240, 240));
                x = 260;
            }
            Txt(content, message, 30, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x + 10, -10), new Vector2(820 - x, 330));
            Button ok = UIFactory.MakeButton(content, button, UIFactory.Green, () => CloseModal(modal), 32);
            ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(300, 84));
        }

        // ================================================================== LEVEL UP
        private void OnLevelUp(int level)
        {
            var unlocks = new List<string>();
            foreach (CropDef c in Defs.Crops) if (c.unlock == level) unlocks.Add("Cây: " + c.name);
            foreach (AnimalDef a in Defs.Animals) if (a.unlock == level) unlocks.Add("Vật nuôi: " + a.name);
            foreach (PlaceDef p in Defs.Places) if (p.shop && p.unlock == level && p.kind != PlaceKind.Plot) unlocks.Add(p.name);
            foreach (LandZone z in Defs.Zones) if (z.unlock == level && z.cost > 0) unlocks.Add($"Vùng đất mới: {z.name} (chạm biển gỗ để mở rộng)");
            string msg = $"Chúc mừng! Bạn đã đạt cấp <b>{level}</b>.\nThưởng: <b>+{Defs.LevelUpCoins(level)} xu</b>";
            if (unlocks.Count > 0) msg += "\n\nMở khoá mới:\n• " + string.Join("\n• ", unlocks);
            Message($"LÊN CẤP {level}!", msg, "hud_confirm");
        }
    }
}
