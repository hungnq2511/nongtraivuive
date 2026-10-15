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

        // ================================================================== STORAGE (barn): view, upgrade, discard – no selling
        private static readonly string[] StorageTabs = { "all", "crop", "animal", "processed" };
        private static readonly string[] StorageTabNames = { "Tất cả", "Nông sản", "Vật nuôi & cá", "Chế biến" };

        public void OpenStorage()
        {
            RectTransform content = OpenWindow(L.T("Kho hàng"), new Vector2(1300, 840), out GameObject modal);
            GameManager gm = GameManager.I;
            string selected = null;
            string tab = "all";

            // capacity bar + upgrade
            Text cap = Txt(content, "", 28, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -4), new Vector2(330, 56), true);
            Image barBg = UIFactory.Panel(content, new Color(0.55f, 0.4f, 0.25f, 0.35f), "CapBar", false);
            barBg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(340, -16), new Vector2(420, 32));
            Image barFill = UIFactory.Panel(barBg.transform, UIFactory.Green, "Fill", false);
            barFill.rectTransform.anchorMin = Vector2.zero;
            barFill.rectTransform.pivot = new Vector2(0, 0.5f);
            barFill.rectTransform.offsetMin = barFill.rectTransform.offsetMax = Vector2.zero;
            Button up = UIFactory.MakeButton(content, " ", UIFactory.Blue, null, 26);
            ((RectTransform)up.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -2), new Vector2(440, 62));
            Text upText = up.GetComponentInChildren<Text>();

            // category tabs
            RectTransform tabs = UIFactory.Rect("Tabs", content);
            tabs.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -74), new Vector2(1000, 64));
            HorizontalLayoutGroup hr = UIFactory.HRow(tabs, 12);
            hr.childAlignment = TextAnchor.MiddleLeft;
            var tabImgs = new List<Image>();

            ScrollRect sr = UIFactory.Scroll(content, false, out RectTransform grid);
            ((RectTransform)sr.transform).Stretch();
            ((RectTransform)sr.transform).offsetMax = new Vector2(0, -144);
            ((RectTransform)sr.transform).offsetMin = new Vector2(0, 130);
            UIFactory.Grid(grid, new Vector2(150, 180), new Vector2(14, 14), false);

            // bottom info bar
            Image bottom = UIFactory.Panel(content, UIFactory.CreamDark, "InfoBar");
            bottom.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(1240, 118));
            Image selIcon = UIFactory.Icon(bottom.transform, "hud_inventory", 96);
            selIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(96, 96));
            Text selText = Txt(bottom.transform, "", 24, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(124, 0), new Vector2(560, 108));
            Button drop1 = UIFactory.MakeButton(bottom.transform, L.T("Bỏ 1"), UIFactory.Gray, null, 26);
            ((RectTransform)drop1.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-498, 0), new Vector2(150, 80));
            Button dropAll = UIFactory.MakeButton(bottom.transform, L.T("Bỏ hết"), UIFactory.Red, null, 26);
            ((RectTransform)dropAll.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-336, 0), new Vector2(150, 80));
            Button toMarket = UIFactory.MakeButton(bottom.transform, L.T("Ra chợ bán"), UIFactory.Green, null, 26);
            ((RectTransform)toMarket.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(310, 80));

            Action rebuild = null;

            void Discard(int n)
            {
                if (selected == null) return;
                ItemDef def = Defs.Item(selected);
                n = Mathf.Min(n, gm.Count(selected));
                if (n <= 0 || !gm.RemoveItem(selected, n)) return;
                Toast(L.F("Đã bỏ {0} {1} khỏi kho.", n, def.name));
                if (gm.Count(selected) <= 0) selected = null;
                rebuild();
            }

            drop1.onClick.AddListener(() => Discard(1));
            dropAll.onClick.AddListener(() =>
            {
                if (selected == null) return;
                string id = selected;
                ItemDef def = Defs.Item(id);
                Confirm(L.T("Bỏ hàng"), L.F("Bỏ toàn bộ {0} {1}?\nHàng bỏ đi sẽ mất, không nhận xu.", gm.Count(id), def.name), () => { selected = id; Discard(int.MaxValue); });
            });
            toMarket.onClick.AddListener(() =>
            {
                if (selected == null) return;
                if (!gm.Data.places.Exists(p => Defs.Place(p.def)?.action == "market"))
                {
                    Toast(L.T("Bạn chưa có Chợ nông sản."));
                    return;
                }
                string id = selected;
                CloseModal(modal);
                OpenMarket(id);
            });
            up.onClick.AddListener(() =>
            {
                if (gm.TryUpgradeStorage()) Toast(L.F("Kho đã nâng cấp! Sức chứa {0}", gm.StorageCapacity));
                else Toast(L.T("Không đủ xu để nâng cấp kho."));
                rebuild();
            });

            for (int i = 0; i < StorageTabs.Length; i++)
            {
                string key = StorageTabs[i];
                Button b = UIFactory.MakeButton(tabs, StorageTabNames[i], UIFactory.Wood, () => { tab = key; selected = null; rebuild(); }, 24);
                ((RectTransform)b.transform).sizeDelta = new Vector2(key == "animal" ? 230 : 180, 58);
                tabImgs.Add(b.GetComponent<Image>());
            }

            rebuild = () =>
            {
                int used = gm.StorageUsed, capN = gm.StorageCapacity;
                float frac = capN > 0 ? Mathf.Clamp01(used / (float)capN) : 0f;
                cap.text = L.F("Sức chứa: {0}/{1}", used, capN);
                barFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, frac), 1);
                barFill.color = frac >= 1f ? UIFactory.Red : frac >= 0.8f ? UIFactory.Orange : UIFactory.Green;
                upText.text = L.F("Nâng cấp (+{0}) – {1} xu", Defs.StoragePerUpgrade, gm.StorageUpgradeCost);
                for (int i = 0; i < tabImgs.Count; i++) tabImgs[i].color = StorageTabs[i] == tab ? UIFactory.Orange : UIFactory.Wood;

                ClearChildren(grid);
                var stacks = gm.Data.items.FindAll(s => Defs.Item(s.id) != null && (tab == "all" || Market.Category(s.id) == tab));
                stacks.Sort((a, b) =>
                {
                    int c = string.CompareOrdinal(Market.Category(a.id), Market.Category(b.id));
                    return c != 0 ? c : string.CompareOrdinal(a.id, b.id);
                });
                if (stacks.Count == 0)
                {
                    Text empty = UIFactory.Label(grid, tab == "all" ? L.T("Kho trống – hãy thu hoạch nông sản!") : L.T("Chưa có món nào thuộc loại này."), 28, UIFactory.Wood);
                    empty.rectTransform.sizeDelta = new Vector2(600, 100);
                }
                foreach (ItemStack s in stacks)
                {
                    ItemDef def = Defs.Item(s.id);
                    string id = s.id;
                    Image card = UIFactory.Panel(grid, id == selected ? new Color(0.7f, 0.92f, 0.55f) : Color.white, "Item");
                    card.gameObject.AddComponent<Button>().onClick.AddListener(() => { selected = id; rebuild(); });
                    Image ic = UIFactory.Icon(card.transform, def.icon, 104);
                    ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(104, 104));
                    Txt(card.transform, "x" + s.count, 30, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(140, 36), true);
                    Txt(card.transform, def.name, 18, UIFactory.Wood, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(146, 26));
                }

                if (selected != null)
                {
                    ItemDef def = Defs.Item(selected);
                    int have = gm.Count(selected);
                    selIcon.sprite = SpriteDB.Get(def.icon);
                    var uses = new List<string>();
                    foreach (RecipeDef r in Defs.Recipes)
                        if (Array.Exists(r.inputs, x => x.id == selected)) uses.Add(L.T(Defs.Item(r.output).name));
                    foreach (AnimalDef a in Defs.Animals)
                        if (a.feed == selected) uses.Add(L.F("thức ăn cho {0}", L.T(a.name).ToLower()));
                    bool ordered = gm.Data.orders.Exists(o => o.req.Exists(x => x.id == selected));
                    string hot = Market.IsHot(selected) ? L.T("  <color=#E05A47><b>Đang giá hot!</b></color>") : "";
                    string line2 = uses.Count > 0 ? L.T("Dùng làm: ") + string.Join(", ", uses.GetRange(0, Mathf.Min(3, uses.Count))) : "";
                    if (ordered) line2 = (line2.Length > 0 ? line2 + "\n" : "") + L.T("<color=#388028>Đang có đơn hàng cần món này</color>");
                    selText.text = L.F("<b>{0}</b>  (có {1})  ·  giá chợ ~{2} xu{3}\n{4}", def.name, have, def.sell, hot, line2);
                    drop1.interactable = dropAll.interactable = toMarket.interactable = true;
                }
                else
                {
                    selIcon.sprite = SpriteDB.Get("hud_inventory");
                    selText.text = L.T("Chọn một món để xem công dụng.\nMuốn bán lấy xu? Mang ra <b>Chợ nông sản</b>.");
                    drop1.interactable = dropAll.interactable = toMarket.interactable = false;
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
            RectTransform content = OpenWindow(L.T("Cửa hàng"), new Vector2(1440, 860), out GameObject modal);
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

            void Card(string sprite, string name, string sub, int price, int unlock, Action buy, float iconScale = 1f, string label = null)
            {
                bool locked = gm.Data.level < unlock;
                Image card = UIFactory.Panel(grid, Color.white, "Card");
                Image ic = UIFactory.Icon(card.transform, sprite, 160 * iconScale);
                ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -88), new Vector2(170, 150));
                Txt(card.transform, name, 26, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(240, 60), true);
                if (!string.IsNullOrEmpty(sub))
                    Txt(card.transform, sub, 20, UIFactory.Wood, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(240, 28));
                Button b = UIFactory.MakeButton(card.transform, label ?? (unlock >= 999 ? L.T("Đã có") : locked ? L.F("Cấp {0}", unlock) : L.F("{0} xu", price)),
                    label != null ? UIFactory.Blue : locked ? UIFactory.Gray : (gm.CanAfford(price) ? UIFactory.Green : UIFactory.Red), null, 28);
                ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(210, 60));
                b.onClick.AddListener(() =>
                {
                    if (label != null) { buy(); return; }
                    if (unlock >= 999) { Toast(L.T("Bạn đã có công trình này.")); return; }
                    if (locked) { Toast(L.F("Mở khoá ở cấp {0}.", unlock)); return; }
                    if (!gm.CanAfford(price)) { Toast(L.T("Không đủ xu!")); return; }
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
                        Card("carrot_dry", L.T("Ô đất mới"), L.F("Đã mua thêm: {0}", gm.Data.plotsBought), gm.NextPlotCost, 1, () => BuyPlace(Defs.Place("plot"), modal), 0.9f);
                        Card("prop_sprinkler", L.T("Vòi phun"), L.T("Tự tưới 3x3 ô"), Defs.Place("sprinkler").cost, Defs.Place("sprinkler").unlock, () => BuyPlace(Defs.Place("sprinkler"), modal));
                        Card("building_barn", L.T("Nâng cấp kho"), L.F("Sức chứa {0} → {1}", gm.StorageCapacity, gm.StorageCapacity + Defs.StoragePerUpgrade), gm.StorageUpgradeCost, 1, () =>
                        {
                            if (gm.TryUpgradeStorage()) Toast(L.F("Kho đã nâng cấp! Sức chứa {0}", gm.StorageCapacity));
                            rebuild();
                        });
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.kind == PlaceKind.Tree)
                            {
                                PlaceDef def = p;
                                TreeDef td = Defs.Tree(def.id);
                                Card(def.sprite, def.name, $"{td.yield} {Defs.Item(td.fruit).name} / {Defs.FormatTime(td.cycleSec)} · " + (td.harvests < 0 ? L.T("mãi mãi") : L.F("{0} lần", td.harvests)), def.cost, def.unlock, () => BuyPlace(def, modal));
                            }
                        break;
                    case "machine":
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.category == "machine")
                            {
                                PlaceDef def = p;
                                bool owned = WorldMap.I.CountPlaces(def.id) > 0;
                                string sub;
                                if (def.kind == PlaceKind.Delivery) sub = L.T("Đơn lớn 6 thùng hàng");
                                else
                                {
                                    var outs = new List<string>();
                                    foreach (RecipeDef r in Defs.Recipes) if (r.machine == def.id) outs.Add(L.T(Defs.Item(r.output).name));
                                    sub = string.Join(", ", outs);
                                }
                                if (owned)
                                    Card(def.sprite, def.name, L.T("Đã xây · ") + L.T(sub), 0, 999, () => { });
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
                            Card(def.Frame("idle_se"), def.name, L.F("Có {0} · ăn {1}", owned, Defs.Crop(def.feed).name), def.cost, def.unlock, () =>
                            {
                                PlaceView house = WorldMap.I.FindHouseWithSpace(def.house);
                                if (house == null)
                                {
                                    Toast(L.F("Cần {0} còn chỗ trống. Xây thêm chuồng hoặc nâng cấp chuồng.", houseName));
                                    return;
                                }
                                if (!gm.TrySpend(def.cost)) return;
                                AnimalView av = WorldMap.I.AddAnimal(def.id, house);
                                gm.AddXp(3);
                                GameEvents.Raise("buy_animal", def.id);
                                Toast(L.F("Đã mua {0}! Nó đang ở {1}.", def.name, houseName));
                                rebuild();
                            });
                        }
                        // pets (B2.2): one of each, they follow the farmer
                        foreach (PetDef pd in Defs.Pets)
                        {
                            PetDef pet = pd;
                            if (PetSystem.Has(pet.id))
                                Card(pet.Frame("sit_se"), pet.name, L.T("Đã nuôi · ") + L.T(pet.perk), 0, 999, () => { });
                            else
                                Card(pet.Frame("sit_se"), pet.name, pet.perk, pet.cost, pet.unlock, () =>
                                {
                                    if (PetSystem.I.Buy(pet)) Toast(L.F("{0} đã về nhà! {1}.", pet.name, pet.perk));
                                    rebuild();
                                }, 0.85f);
                        }
                        break;
                    case "house":
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.category == "house")
                            {
                                PlaceDef def = p;
                                Card(def.sprite, def.name, L.F("Đang có {0} · chứa {1}+ con", WorldMap.I.CountPlaces(def.id), def.capacity), def.cost, def.unlock, () => BuyPlace(def, modal));
                            }
                        break;
                    default:
                        // decorations put away earlier: placing them again is free
                        foreach (ItemStack st in gm.Data.decorStore)
                        {
                            PlaceDef def = Defs.Place(st.id);
                            if (def == null || st.count <= 0) continue;
                            Card(def.sprite, def.name, L.F("Trong kho: {0}", st.count), 0, 0, () => PlaceFromStore(def, modal), 1f, L.T("Đặt ra"));
                        }
                        foreach (PlaceDef p in Defs.Places)
                            if (p.shop && p.category == "decor")
                            {
                                PlaceDef def = p;
                                Card(def.sprite, def.name, def.id == "scarecrow" ? L.F("Đuổi quạ {0}x{1} ô · +{2} XP", Defs.ScarecrowRadius * 2 + 1, Defs.ScarecrowRadius * 2 + 1, def.Xp) : $"+{def.Xp} XP", def.cost, def.unlock, () => BuyPlace(def, modal));
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
                    Toast(L.T("Không đủ xu!"));
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
                    if (def.kind == PlaceKind.House) Toast(L.F("Đã xây {0}! Vào Cửa hàng › Vật nuôi để mua thú.", def.name));
                }
                FloatText(at + Vector3.up, "-" + price, "hud_coin");
            });
        }

        // ================================================================== ORDERS
        public void OpenOrders()
        {
            RectTransform content = OpenWindow(L.T("Bảng đơn hàng"), new Vector2(1500, 760), out GameObject modal);
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
                        Txt(card.transform, L.T("Đơn mới sau\n") + Defs.FormatTime(o.availableAt - gm.Now), 28, UIFactory.Wood, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(250, 90), true);
                        OrderData od = o;
                        Button now = GemButton(card.transform, L.T("Gọi ngay"), () => od.availableAt - gm.Now, () => { od.availableAt = gm.Now; rebuild(); });
                        ((RectTransform)now.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(230, 70));
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
                    Txt(card.transform, Defs.WithBonus(o.coins).ToString(), 32, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(84, 180), new Vector2(90, 40), true);
                    Txt(card.transform, "+" + Defs.WithBonus(o.xp) + " XP", 24, UIFactory.Blue, TextAnchor.MiddleRight, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 180), new Vector2(100, 40), true);

                    bool can = os.CanFulfill(o);
                    Button give = UIFactory.MakeButton(card.transform, L.T("Giao hàng"), can ? UIFactory.Green : UIFactory.Gray, () =>
                    {
                        if (os.Fulfill(idx)) Toast(L.F("Giao hàng thành công! +{0} xu, +{1} XP", Defs.WithBonus(o.coins), Defs.WithBonus(o.xp)));
                        else Toast(L.T("Chưa đủ hàng cho đơn này."));
                        rebuild();
                    }, 28);
                    ((RectTransform)give.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 86), new Vector2(240, 70));
                    Button drop = UIFactory.MakeButton(card.transform, L.T("Bỏ đơn"), UIFactory.Red, () => { os.Discard(idx); rebuild(); }, 22);
                    ((RectTransform)drop.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(160, 54));
                }
            };
            rebuild();
            SetTicker(modal, rebuild);
        }

        // ================================================================== FARMHOUSE / HOUSE
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
                    lines.Add(L.F("• {0}: ăn {1} → {2} ({3})", a.name, Defs.Crop(a.feed).name, Defs.Item(a.product).name, Defs.FormatTime(a.produceSec)));
            Txt(content, L.F("Đang nuôi: <b>{0}/{1}</b> con   ·   Cấp chuồng: <b>{2}/{3}</b>\n\n", n, cap, st.level + 1, Defs.HouseMaxLevel + 1) + string.Join("\n", lines), 28, UIFactory.TextDark, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(310, -20), new Vector2(600, 320));
            Button buy = UIFactory.MakeButton(content, L.T("Mua vật nuôi"), UIFactory.Green, () => { CloseModal(modal); OpenShop("animal"); }, 28);
            ((RectTransform)buy.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 10), new Vector2(280, 74));
            if (st.level < Defs.HouseMaxLevel)
            {
                int cost = Defs.HouseUpgradeCost(house.Def, st.level);
                Button up = UIFactory.MakeButton(content, L.F("Nâng cấp +{0} chỗ – {1} xu", Defs.HouseSlotsPerLevel, cost), gm.CanAfford(cost) ? UIFactory.Blue : UIFactory.Gray, () =>
                {
                    if (!gm.TrySpend(cost)) { Toast(L.T("Không đủ xu!")); return; }
                    st.level++;
                    gm.AddXp(5);
                    Toast(L.F("Đã nâng cấp {0}! Sức chứa {1} con.", house.Def.name, WorldMap.I.HouseCapacity(house)));
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
            RectTransform content = OpenWindow(L.T("Mở rộng đất"), new Vector2(940, 560), out GameObject modal);
            GameManager gm = GameManager.I;
            bool levelOk = gm.Data.level >= z.unlock;
            bool coinsOk = gm.CanAfford(z.cost);
            int debris = 0;
            foreach (DebrisData d in gm.Data.debris) if (z.Contains(d.gx, d.gy)) debris++;

            Image ic = UIFactory.Icon(content, "decor_wooden_sign", 230);
            ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 40), new Vector2(240, 240));
            string msg =
                "<b>" + L.T(z.name) + "</b>\n" +
                L.F("Diện tích: {0} ô đất\n", z.Cells) +
                L.F("Có {0} gốc cây / tảng đá cần dọn\n\n", debris) +
                L.F("Yêu cầu cấp: <color={0}><b>{1}</b></color>   (bạn đang cấp {2})\n", (levelOk ? "#3B8A28" : "#D04A3A"), z.unlock, gm.Data.level) +
                L.F("Chi phí: <color={0}><b>{1:N0} xu</b></color>", (coinsOk ? "#3B8A28" : "#D04A3A"), z.cost);
            Txt(content, msg, 30, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(270, -10), new Vector2(600, 320));

            string label = !levelOk ? L.F("Cần cấp {0}", z.unlock) : L.F("Mở rộng – {0:N0} xu", z.cost);
            Color col = !levelOk ? UIFactory.Gray : coinsOk ? UIFactory.Green : UIFactory.Red;
            Button b = UIFactory.MakeButton(content, label, col, () =>
            {
                if (!levelOk) { Toast(L.F("Đạt cấp {0} để mở rộng vùng này.", z.unlock)); return; }
                if (!gm.TrySpend(z.cost)) { Toast(L.T("Không đủ xu!")); return; }
                CloseModal(modal);
                WorldMap.I.UnlockZone(z);
                gm.AddXp(Mathf.Max(5, z.cost / 20));
                GameEvents.Raise("unlock_zone", z.id.ToString());
                Vector3 center = IsoGrid.ToWorld((z.x0 + z.x1) * 0.5f, (z.y0 + z.y1) * 0.5f);
                CameraController.I.FocusOn(center);
                Toast(L.F("Đã mở rộng: {0}!", z.name));
            }, 32);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(420, 86));
        }

        // ================================================================== SETTINGS
        public void OpenSettings()
        {
            RectTransform content = OpenWindow(L.T("Cài đặt"), new Vector2(860, 640), out GameObject modal);
            GameManager gm = GameManager.I;
            Button save = UIFactory.MakeButton(content, L.T("Lưu ngay"), UIFactory.Blue, () => { gm.SaveNow(); Toast(L.T("Đã lưu!")); }, 28);
            ((RectTransform)save.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(360, 76));

            Button reset = UIFactory.MakeButton(content, L.T("Xoá dữ liệu & chơi lại"), UIFactory.Red, () =>
                Confirm(L.T("Xoá toàn bộ tiến trình?"), L.T("Nông trại sẽ trở về ban đầu. Không thể hoàn tác."), () => Bootstrap.ResetGame()), 26);
            ((RectTransform)reset.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(400, -20), new Vector2(360, 76));

            Button haptic = null;
            haptic = UIFactory.MakeButton(content, gm.Data.haptics ? L.T("Rung: Bật") : L.T("Rung: Tắt"), gm.Data.haptics ? UIFactory.Green : UIFactory.Gray, () =>
            {
                gm.Data.haptics = !gm.Data.haptics;
                haptic.GetComponentInChildren<Text>().text = gm.Data.haptics ? L.T("Rung: Bật") : L.T("Rung: Tắt");
                haptic.GetComponent<Image>().color = gm.Data.haptics ? UIFactory.Green : UIFactory.Gray;
                Haptics.Medium();
            }, 28);
            ((RectTransform)haptic.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -130), new Vector2(360, 76));
            Button notif = null;
            notif = UIFactory.MakeButton(content, gm.Data.notifications ? L.T("Thông báo: Bật") : L.T("Thông báo: Tắt"), gm.Data.notifications ? UIFactory.Green : UIFactory.Gray, () =>
            {
                gm.Data.notifications = !gm.Data.notifications;
                if (gm.Data.notifications) { gm.Data.notifAsked = false; LocalNotifications.RequestPermission(); }
                notif.GetComponentInChildren<Text>().text = gm.Data.notifications ? L.T("Thông báo: Bật") : L.T("Thông báo: Tắt");
                notif.GetComponent<Image>().color = gm.Data.notifications ? UIFactory.Green : UIFactory.Gray;
            }, 28);
            ((RectTransform)notif.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(400, -130), new Vector2(360, 76));

            // language (reloads the farm so every text is rebuilt)
            Txt(content, "Ngôn ngữ / Language:", 28, UIFactory.TextDark, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -236), new Vector2(760, 50), true);
            Button vi = UIFactory.MakeButton(content, "Tiếng Việt", !L.En ? UIFactory.GreenDark : UIFactory.Gray, () => { if (L.En) Bootstrap.SwitchLanguage(false); }, 28);
            ((RectTransform)vi.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -296), new Vector2(360, 76));
            Button en = UIFactory.MakeButton(content, "English", L.En ? UIFactory.GreenDark : UIFactory.Gray, () => { if (!L.En) Bootstrap.SwitchLanguage(true); }, 28);
            ((RectTransform)en.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(400, -296), new Vector2(360, 76));

            Txt(content, "Farm Bloom v" + Application.version + L.T("\nĐiều khiển: joystick / WASD để đi, chạm để làm việc,\nkéo qua nhiều ô để thao tác hàng loạt, 2 ngón để zoom."),
                22, UIFactory.Wood, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 10), new Vector2(780, 110));
        }

        public void Confirm(string title, string message, Action onYes)
        {
            RectTransform content = OpenWindow(title, new Vector2(820, 420), out GameObject modal);
            Txt(content, message, 30, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(740, 160));
            Button yes = UIFactory.MakeButton(content, L.T("Đồng ý"), UIFactory.Red, () => { CloseModal(modal); onYes(); }, 30);
            ((RectTransform)yes.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-160, 16), new Vector2(260, 80));
            Button no = UIFactory.MakeButton(content, L.T("Huỷ"), UIFactory.Gray, () => CloseModal(modal), 30);
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

    }
}
