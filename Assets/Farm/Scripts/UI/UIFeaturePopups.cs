using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    public partial class UIManager
    {
        private static void Bar(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float pct, Color fill)
        {
            Image bg = UIFactory.Panel(parent, new Color(0.25f, 0.16f, 0.08f, 0.35f), "Bar");
            bg.raycastTarget = false;
            bg.rectTransform.Place(anchor, pivot, pos, size);
            Image f = UIFactory.Panel(bg.transform, fill, "Fill");
            f.raycastTarget = false;
            RectTransform rt = f.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp(pct, 0.06f, 1f), 1f);
            rt.offsetMin = new Vector2(3, 3);
            rt.offsetMax = new Vector2(-3, -3);
            if (pct <= 0f) f.gameObject.SetActive(false);
        }

        // ================================================================== PRODUCTION MACHINE
        public void OpenMachine(PlaceView machine)
        {
            RectTransform content = OpenWindow(machine.Def.name, new Vector2(1440, 860), out GameObject modal);
            GameManager gm = GameManager.I;
            MachineData m = Production.Get(machine.D.uid);

            RectTransform queue = UIFactory.Rect("Queue", content);
            queue.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(1380, 190));
            ScrollRect sr = UIFactory.Scroll(content, true, out RectTransform list);
            ((RectTransform)sr.transform).Stretch();
            ((RectTransform)sr.transform).offsetMax = new Vector2(0, -206);
            UIFactory.Grid(list, new Vector2(300, 470), new Vector2(18, 0), true).padding = new RectOffset(10, 10, 10, 10);

            Action rebuild = null;
            rebuild = () =>
            {
                Production.Tick(m);
                ClearChildren(queue);
                ClearChildren(list);

                // ---- queue + done tray
                Image qbg = UIFactory.Panel(queue, UIFactory.CreamDark, "QueueBg");
                qbg.rectTransform.Stretch();
                Txt(queue, "Hàng đợi", 26, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -8), new Vector2(200, 34), true);
                for (int i = 0; i < m.slots; i++)
                {
                    Image slot = UIFactory.Panel(queue, Color.white, "Slot");
                    slot.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(16 + i * 150, 14), new Vector2(136, 130));
                    if (i < m.jobs.Count)
                    {
                        ItemDef it = Defs.Item(m.jobs[i].recipe);
                        Image ic = UIFactory.Icon(slot.transform, it.icon, 84);
                        ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(84, 84));
                        string time = i == 0 ? Defs.FormatTime(Production.CurrentJobLeft(m)) : "Chờ";
                        Txt(slot.transform, time, 22, i == 0 ? UIFactory.GreenDark : UIFactory.Wood, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(130, 34), true);
                    }
                    else Txt(slot.transform, "Trống", 22, UIFactory.Gray, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 40));
                }
                if (m.slots < Defs.MachineMaxSlots)
                {
                    int cost = Defs.MachineSlotCost(m.slots);
                    Button add = UIFactory.MakeButton(queue, $"+1 chỗ\n{cost} xu", gm.CanAfford(cost) ? UIFactory.Blue : UIFactory.Gray, () =>
                    {
                        if (!gm.TrySpend(cost)) { Toast("Không đủ xu!"); return; }
                        m.slots++;
                        rebuild();
                    }, 22);
                    ((RectTransform)add.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(16 + m.slots * 150, 14), new Vector2(136, 130));
                }

                Txt(queue, $"Đã xong: {m.done.Count}", 26, UIFactory.TextDark, TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-300, -20), new Vector2(240, 40), true);
                Button collect = UIFactory.MakeButton(queue, "Thu sản phẩm", m.done.Count > 0 ? UIFactory.Green : UIFactory.Gray, () =>
                {
                    if (Production.Collect(m, machine.transform.position) == 0) Toast("Chưa có sản phẩm nào xong.");
                    rebuild();
                }, 26);
                ((RectTransform)collect.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 18), new Vector2(270, 80));

                // ---- recipes
                foreach (RecipeDef r in Defs.Recipes)
                {
                    if (r.machine != machine.Def.id) continue;
                    RecipeDef rec = r;
                    ItemDef outItem = Defs.Item(r.output);
                    bool locked = gm.Data.level < r.unlock;
                    bool ok = !locked && Production.HasInputs(r) && m.jobs.Count < m.slots;
                    Image card = UIFactory.Panel(list, Color.white, "Recipe");
                    Image oi = UIFactory.Icon(card.transform, outItem.icon, 130);
                    oi.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(130, 130));
                    Txt(card.transform, outItem.name, 28, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(290, 36), true);
                    Txt(card.transform, $"{Defs.FormatTime(r.sec)}  ·  bán {outItem.sell} xu", 22, UIFactory.Wood, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -174), new Vector2(290, 30));
                    float y = -212;
                    foreach (ItemStack inp in r.inputs)
                    {
                        ItemDef ii = Defs.Item(inp.id);
                        int have = gm.Count(inp.id);
                        Image ic = UIFactory.Icon(card.transform, ii.icon, 60);
                        ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, y), new Vector2(60, 60));
                        Txt(card.transform, $"{ii.name}  {have}/{inp.count}", 22, have >= inp.count ? UIFactory.GreenDark : UIFactory.Red, TextAnchor.MiddleLeft,
                            new Vector2(0, 1), new Vector2(0, 1), new Vector2(92, y - 12), new Vector2(200, 36), true);
                        y -= 66;
                    }
                    Button go = UIFactory.MakeButton(card.transform, locked ? $"Cấp {r.unlock}" : "Sản xuất",
                        locked ? UIFactory.Gray : ok ? UIFactory.Green : UIFactory.Orange, () =>
                        {
                            string err = Production.Start(m, rec);
                            if (err != null) Toast(err);
                            rebuild();
                        }, 28);
                    ((RectTransform)go.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(240, 70));
                }
            };
            rebuild();
            SetTicker(modal, rebuild);
        }

        // ================================================================== DELIVERY TRUCK
        public void OpenDelivery()
        {
            RectTransform content = OpenWindow("Xe tải giao hàng", new Vector2(1400, 840), out GameObject modal);
            GameManager gm = GameManager.I;
            Action rebuild = null;
            rebuild = () =>
            {
                ClearChildren(content);
                if (Delivery.Waiting)
                {
                    Image truck = UIFactory.Icon(content, "building_delivery_truck", 300);
                    truck.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(320, 320));
                    Txt(content, $"Xe tải đang đi giao hàng.\nChuyến mới sau <b>{Defs.FormatTime(gm.Data.delivery.readyAt - gm.Now)}</b>", 32, UIFactory.TextDark,
                        TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(900, 120));
                    return;
                }
                DeliveryData dd = Delivery.Current;
                RectTransform grid = UIFactory.Rect("Crates", content);
                grid.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1340, 580));
                GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(420, 270);
                g.spacing = new Vector2(20, 20);
                g.childAlignment = TextAnchor.UpperCenter;
                for (int i = 0; i < dd.crates.Count; i++)
                {
                    int idx = i;
                    CrateData c = dd.crates[i];
                    ItemDef it = Defs.Item(c.item);
                    int have = gm.Count(c.item);
                    Image card = UIFactory.Panel(grid, c.filled ? new Color(0.78f, 0.93f, 0.66f) : Color.white, "Crate");
                    Image ic = UIFactory.Icon(card.transform, it.icon, 120);
                    ic.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(120, 120));
                    Txt(card.transform, $"{it.name}\nx{c.qty}", 28, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -20), new Vector2(250, 80), true);
                    Txt(card.transform, c.filled ? "Đã đóng thùng" : $"Có {have}/{c.qty}", 24, c.filled || have >= c.qty ? UIFactory.GreenDark : UIFactory.Red,
                        TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -100), new Vector2(250, 34), true);
                    Txt(card.transform, $"+{c.coins} xu", 24, UIFactory.Wood, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -136), new Vector2(250, 34), true);
                    if (!c.filled)
                    {
                        Button fill = UIFactory.MakeButton(card.transform, "Đóng thùng", have >= c.qty ? UIFactory.Green : UIFactory.Gray, () =>
                        {
                            string err = Delivery.Fill(idx);
                            if (err != null) Toast(err); else Toast($"Đã đóng thùng {it.name}: +{c.coins} xu");
                            rebuild();
                        }, 26);
                        ((RectTransform)fill.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(260, 66));
                    }
                }

                Txt(content, $"Đóng đủ {dd.crates.Count} thùng để xuất hàng – thưởng thêm <b>+{dd.bonusCoins} xu, +{dd.xp} XP</b>", 28, UIFactory.TextDark,
                    TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 100), new Vector2(980, 50));
                bool all = Delivery.AllFilled;
                Button ship = UIFactory.MakeButton(content, "Xuất hàng!", all ? UIFactory.Green : UIFactory.Gray, () =>
                {
                    if (!Delivery.AllFilled) { Toast("Hãy đóng đủ tất cả các thùng."); return; }
                    int bonus = dd.bonusCoins;
                    Delivery.Ship();
                    Toast($"Xe tải đã xuất phát! +{bonus} xu");
                    rebuild();
                }, 32);
                ((RectTransform)ship.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 10), new Vector2(300, 84));
                if (!dd.crates.Exists(c => c.filled))
                {
                    Button reroll = UIFactory.MakeButton(content, "Đổi đơn khác", UIFactory.Orange, () => { Delivery.Reroll(); rebuild(); }, 26);
                    ((RectTransform)reroll.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-330, 10), new Vector2(260, 84));
                }
            };
            rebuild();
            SetTicker(modal, () => { if (Delivery.Waiting || gm.Data.delivery.crates.Count == 0) rebuild(); });
        }

        // ================================================================== FISHING MINI-GAME
        public void OpenFishing(Vector2Int cell)
        {
            RectTransform content = OpenWindow("Câu cá", new Vector2(1100, 640), out GameObject modal);
            var game = content.gameObject.AddComponent<FishingGame>();
            game.Build(this, content, cell, modal);
        }

        // ================================================================== DAILY TASKS + ACHIEVEMENTS
        public void OpenTasks()
        {
            RectTransform content = OpenWindow("Nhiệm vụ & Thành tựu", new Vector2(1400, 860), out GameObject modal);
            GameManager gm = GameManager.I;
            TaskSystem ts = TaskSystem.I;
            string tab = "daily";

            RectTransform tabs = UIFactory.Rect("Tabs", content);
            tabs.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(900, 76));
            UIFactory.HRow(tabs, 20);
            ScrollRect sr = UIFactory.Scroll(content, false, out RectTransform list);
            ((RectTransform)sr.transform).Stretch();
            ((RectTransform)sr.transform).offsetMax = new Vector2(0, -90);
            VerticalLayoutGroup v = list.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14;
            v.padding = new RectOffset(10, 10, 10, 10);
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandHeight = false;

            Action rebuild = null;
            Image tDaily = null, tAch = null;
            Button b1 = UIFactory.MakeButton(tabs, "Nhiệm vụ ngày", UIFactory.Wood, () => { tab = "daily"; rebuild(); }, 28);
            ((RectTransform)b1.transform).sizeDelta = new Vector2(400, 70);
            tDaily = b1.GetComponent<Image>();
            Button b2 = UIFactory.MakeButton(tabs, "Thành tựu", UIFactory.Wood, () => { tab = "ach"; rebuild(); }, 28);
            ((RectTransform)b2.transform).sizeDelta = new Vector2(400, 70);
            tAch = b2.GetComponent<Image>();

            Image Row(float h)
            {
                Image row = UIFactory.Panel(list, Color.white, "Row");
                row.rectTransform.sizeDelta = new Vector2(0, h);
                return row;
            }

            rebuild = () =>
            {
                tDaily.color = tab == "daily" ? UIFactory.GreenDark : UIFactory.Wood;
                tAch.color = tab == "ach" ? UIFactory.GreenDark : UIFactory.Wood;
                ClearChildren(list);
                if (tab == "daily")
                {
                    foreach (TaskData t in gm.Data.tasks)
                    {
                        TaskData task = t;
                        Image row = Row(150);
                        Txt(row.transform, t.text, 30, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(760, 44), true);
                        Bar(row.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(600, 40), (float)t.progress / t.target, UIFactory.Orange);
                        Txt(row.transform, $"{t.progress}/{t.target}", 26, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(640, 24), new Vector2(160, 40), true);
                        Txt(row.transform, $"+{t.coins} xu  +{t.xp} XP", 26, UIFactory.Wood, TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-300, 0), new Vector2(260, 40), true);
                        bool done = t.progress >= t.target;
                        Button c = UIFactory.MakeButton(row.transform, t.claimed ? "Đã nhận" : done ? "Nhận thưởng" : "Đang làm",
                            t.claimed ? UIFactory.Gray : done ? UIFactory.Green : UIFactory.Gray, () =>
                            {
                                if (ts.Claim(task)) Toast($"+{task.coins} xu, +{task.xp} XP");
                                rebuild();
                            }, 26);
                        ((RectTransform)c.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(250, 76));
                    }
                    Image bonus = Row(150);
                    bonus.color = new Color(1f, 0.93f, 0.75f);
                    Image gift = UIFactory.Icon(bonus.transform, "hud_gift", 110);
                    gift.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(110, 110));
                    Txt(bonus.transform, $"Hoàn thành cả 3 nhiệm vụ: rương thưởng <b>+{ts.BonusCoins} xu</b>\nNhiệm vụ mới mỗi ngày.", 28, UIFactory.TextDark, TextAnchor.MiddleLeft,
                        new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(150, 0), new Vector2(800, 110), false);
                    bool can = ts.AllTasksClaimed && !gm.Data.taskBonusClaimed;
                    Button cb = UIFactory.MakeButton(bonus.transform, gm.Data.taskBonusClaimed ? "Đã nhận" : "Mở rương", can ? UIFactory.Green : UIFactory.Gray, () =>
                    {
                        if (ts.ClaimBonus()) Toast($"Rương thưởng: +{ts.BonusCoins} xu!");
                        rebuild();
                    }, 26);
                    ((RectTransform)cb.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(250, 76));
                }
                else
                {
                    foreach (AchievementDef a in Defs.Achievements)
                    {
                        AchievementDef ach = a;
                        int tier = ts.ClaimedTier(a);
                        bool maxed = tier >= a.tiers.Length;
                        int target = a.tiers[Mathf.Min(tier, a.tiers.Length - 1)];
                        int val = gm.Data.Stat(a.stat);
                        Image row = Row(140);
                        Image trophy = UIFactory.Icon(row.transform, "hud_trophy", 96);
                        trophy.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(96, 96));
                        Txt(row.transform, $"{a.name}  <color=#B07A2A>Bậc {Mathf.Min(tier + 1, a.tiers.Length)}/{a.tiers.Length}</color>", 30, UIFactory.TextDark, TextAnchor.MiddleLeft,
                            new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -12), new Vector2(700, 42), true);
                        Txt(row.transform, string.Format(a.desc, target), 24, UIFactory.Wood, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(128, -54), new Vector2(560, 34));
                        Bar(row.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(128, 16), new Vector2(480, 32), maxed ? 1f : (float)val / target, UIFactory.Blue);
                        Txt(row.transform, maxed ? "Hoàn thành" : $"{Mathf.Min(val, target)}/{target}", 24, UIFactory.TextDark, TextAnchor.MiddleLeft,
                            new Vector2(0, 0), new Vector2(0, 0), new Vector2(620, 16), new Vector2(200, 32), true);
                        if (!maxed)
                            Txt(row.transform, $"+{a.rewards[tier]} xu", 26, UIFactory.Wood, TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-290, 0), new Vector2(200, 40), true);
                        bool can = ts.CanClaim(a);
                        Button c = UIFactory.MakeButton(row.transform, maxed ? "Xong" : can ? "Nhận thưởng" : "Chưa đạt", can ? UIFactory.Green : UIFactory.Gray, () =>
                        {
                            if (ts.ClaimAchievement(ach)) Toast($"Thành tựu: {ach.name}!");
                            rebuild();
                        }, 26);
                        ((RectTransform)c.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(250, 76));
                    }
                }
                sr.verticalNormalizedPosition = 1f;
            };
            rebuild();
        }
    }

    /// <summary>Timing mini-game: stop the moving needle inside the green zone to land the fish.</summary>
    public class FishingGame : MonoBehaviour
    {
        private UIManager ui;
        private RectTransform root, track, zone, needle;
        private Text info, result;
        private Button pull;
        private Image resultIcon;
        private Vector2Int cell;
        private FishDef fish;
        private float pos, dir = 1f;
        private bool active;
        private const float TrackW = 900f;

        public void Build(UIManager ui, RectTransform content, Vector2Int c, GameObject modal)
        {
            this.ui = ui;
            root = content;
            cell = c;
            info = UIFactory.Label(content, "", 30, UIFactory.TextDark, TextAnchor.MiddleCenter, true);
            info.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(1000, 90));

            Image tbg = UIFactory.Panel(content, new Color(0.25f, 0.45f, 0.7f), "Track");
            track = tbg.rectTransform;
            track.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(TrackW, 70));
            Image z = UIFactory.Panel(track, new Color(0.4f, 0.9f, 0.35f, 0.95f), "Zone");
            zone = z.rectTransform;
            zone.anchorMin = zone.anchorMax = new Vector2(0, 0.5f);
            zone.pivot = new Vector2(0, 0.5f);
            Image n = UIFactory.Solid(track, Color.white, "Needle");
            needle = n.rectTransform;
            needle.anchorMin = needle.anchorMax = new Vector2(0, 0.5f);
            needle.sizeDelta = new Vector2(12, 100);

            resultIcon = UIFactory.Icon(content, "icon_fish_carp", 150);
            resultIcon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, -110), new Vector2(150, 150));
            result = UIFactory.Label(content, "", 32, UIFactory.TextDark, TextAnchor.MiddleLeft, true);
            result.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80, -110), new Vector2(560, 120));

            pull = UIFactory.MakeButton(content, "KÉO CẦN!", UIFactory.Orange, OnPull, 40);
            ((RectTransform)pull.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(420, 100));
            Cast();
        }

        private void Cast()
        {
            FishSpotData spot = Fishing.Spot(cell);
            if (spot.count <= 0)
            {
                info.text = $"Cá ở đây đã hết. Quay lại sau {Defs.FormatTime(spot.regenAt - GameManager.I.Now)}.";
                pull.GetComponentInChildren<Text>().text = "Đóng";
                active = false;
                return;
            }
            fish = Fishing.Roll();
            float w = fish.zoneWidth * TrackW;
            zone.sizeDelta = new Vector2(w, 70);
            zone.anchoredPosition = new Vector2(UnityEngine.Random.Range(60f, TrackW - w - 20f), 0);
            pos = 0f;
            dir = 1f;
            active = true;
            info.text = $"Có cá cắn câu! Nhấn KÉO CẦN khi kim nằm trong vùng xanh.\n(còn {spot.count} lượt câu ở đây)";
            result.text = "";
            resultIcon.gameObject.SetActive(false);
            pull.GetComponentInChildren<Text>().text = "KÉO CẦN!";
        }

        private void Update()
        {
            if (!active) return;
            pos += dir * fish.needleSpeed * Time.unscaledDeltaTime;
            if (pos > 1f) { pos = 1f; dir = -1f; }
            if (pos < 0f) { pos = 0f; dir = 1f; }
            needle.anchoredPosition = new Vector2(pos * TrackW, 0);
        }

        private void OnPull()
        {
            if (!active)
            {
                FishSpotData spot = Fishing.Spot(cell);
                if (spot.count > 0 && result.text.Length > 0) Cast();
                else ui.CloseModal(FindModal());
                return;
            }
            active = false;
            FishSpotData s = Fishing.Spot(cell);
            s.count--;
            if (s.count == Defs.FishPerSpot - 1) s.regenAt = GameManager.I.Now + Defs.FishRegenSec;
            float x = pos * TrackW;
            bool hit = x >= zone.anchoredPosition.x && x <= zone.anchoredPosition.x + zone.sizeDelta.x;
            ItemDef item = Defs.Item(fish.id);
            if (hit && GameManager.I.AddItem(item.id, 1))
            {
                GameManager.I.AddXp(item.xp);
                GameEvents.Raise("fish", item.id);
                result.text = fish.id == "fish_golden" ? $"Tuyệt vời! Bạn câu được <b>{item.name}</b> hiếm!" : $"Bạn câu được <b>{item.name}</b>!";
                resultIcon.sprite = SpriteDB.Get(item.icon);
                resultIcon.gameObject.SetActive(true);
            }
            else if (hit) result.text = "Kho đầy! Cá đã được thả lại.";
            else result.text = "Cá sổng mất rồi!";
            pull.GetComponentInChildren<Text>().text = s.count > 0 ? $"Câu tiếp ({s.count})" : "Đóng";
        }

        private GameObject FindModal()
        {
            Transform t = transform;
            while (t != null && !t.name.StartsWith("Modal_")) t = t.parent;
            return t != null ? t.gameObject : null;
        }
    }
}
