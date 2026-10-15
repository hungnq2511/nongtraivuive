using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>"Trong lúc bạn vắng nhà" – what finished while the player was away.</summary>
    public partial class UIManager
    {
        public const double WelcomeBackMinAway = 300; // seconds

        private struct AwayLine
        {
            public string icon, text;
        }

        /// <summary>Show the summary if anything is waiting. since = save time before the absence (game clock).</summary>
        public void ShowWelcomeBack(double awaySec, double since)
        {
            if (awaySec < WelcomeBackMinAway) return;
            GameManager gm = GameManager.I;
            WorldMap w = WorldMap.I;
            var lines = new List<AwayLine>();

            // crops
            var crops = new Dictionary<string, int>();
            foreach (PlotView p in w.Plots)
                if (p.IsReady && p.Crop != null)
                    crops[p.Crop.id] = crops.TryGetValue(p.Crop.id, out int n) ? n + 1 : 1;
            int cropTotal = 0;
            string cropIcon = null;
            int best = 0;
            foreach (var kv in crops)
            {
                cropTotal += kv.Value;
                if (kv.Value > best) { best = kv.Value; cropIcon = Defs.Crop(kv.Key).Icon; }
            }
            if (cropTotal > 0) lines.Add(new AwayLine { icon = cropIcon, text = L.F("<b>{0}</b> ô cây đã chín, sẵn sàng thu hoạch", cropTotal) });

            // animals
            int animals = 0;
            string animalIcon = null;
            foreach (AnimalView a in w.Animals)
                if (a.IsReady) { animals++; animalIcon = Defs.Item(a.Def.product).icon; }
            if (animals > 0) lines.Add(new AwayLine { icon = animalIcon, text = L.F("<b>{0}</b> vật nuôi đã có sản phẩm", animals) });

            // machines
            int made = 0;
            string madeIcon = null;
            foreach (PlaceView p in w.Places)
            {
                if (p.Def.kind != PlaceKind.Machine) continue;
                MachineData m = Production.Get(p.D.uid);
                Production.Tick(m);
                made += m.done.Count;
                if (m.done.Count > 0) madeIcon = Defs.Item(m.done[0]).icon;
            }
            if (made > 0) lines.Add(new AwayLine { icon = madeIcon, text = L.F("<b>{0}</b> sản phẩm chế biến đã xong", made) });

            // fruit trees & beehives
            int trees = 0;
            string treeIcon = null;
            foreach (PlaceView p in w.Places)
                if (p.Def.kind == PlaceKind.Tree && Orchard.IsReady(p)) { trees++; treeIcon = Defs.Item(Defs.Tree(p.Def.id).fruit).icon; }
            if (trees > 0) lines.Add(new AwayLine { icon = treeIcon, text = L.F("<b>{0}</b> cây ăn quả / tổ ong có thể thu", trees) });

            // market
            int sold = 0, coins = 0;
            foreach (MarketSlotData s in Market.Slots)
                if (Market.IsSold(s)) { sold++; coins += s.price; }
            if (sold > 0) lines.Add(new AwayLine { icon = "hud_coin", text = L.F("Khách đã mua <b>{0}</b> lô hàng ở chợ: <b>{1} xu</b> đang chờ nhận", sold, coins) });

            // truck & orders
            double now = gm.Now;
            if (gm.Data.delivery != null && gm.Data.delivery.readyAt > since && gm.Data.delivery.readyAt <= now)
                lines.Add(new AwayLine { icon = "hud_truck", text = L.T("Xe tải đã quay về, có chuyến hàng mới") });
            int newOrders = 0;
            foreach (OrderData o in gm.Data.orders)
                if (o.availableAt > since && o.availableAt <= now) newOrders++;
            if (newOrders > 0) lines.Add(new AwayLine { icon = "hud_quest", text = L.F("<b>{0}</b> đơn hàng mới trên bảng", newOrders) });

            if (lines.Count == 0) return;

            RectTransform content = OpenWindow(L.T("Trong lúc bạn vắng nhà"), new Vector2(1060, 200 + lines.Count * 104), out GameObject modal);
            Txt(content, L.F("Bạn đã đi vắng <b>{0}</b>. Nông trại vẫn làm việc chăm chỉ:", Defs.FormatTime(awaySec)), 28, UIFactory.TextDark,
                TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -6), new Vector2(1000, 48), true);
            float y = -64;
            foreach (AwayLine l in lines)
            {
                Image row = UIFactory.Panel(content, Color.white, "Row");
                row.raycastTarget = false;
                row.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(1000, 92));
                Image ic = UIFactory.Icon(row.transform, l.icon ?? "hud_star", 78);
                ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(78, 78));
                Txt(row.transform, l.text, 28, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(880, 84));
                y -= 104;
            }

            if (coins > 0)
            {
                Button take = UIFactory.MakeButton(content, L.F("Nhận {0} xu từ chợ", coins), UIFactory.Orange, () =>
                {
                    int got = Market.CollectAll();
                    if (got > 0) Toast(L.F("Đã nhận {0} xu từ chợ!", got));
                    CloseModal(modal);
                }, 28);
                ((RectTransform)take.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-170, 6), new Vector2(320, 80));
                Button ok = UIFactory.MakeButton(content, L.T("Đóng"), UIFactory.Green, () => CloseModal(modal), 30);
                ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(170, 6), new Vector2(260, 80));
            }
            else
            {
                Button ok = UIFactory.MakeButton(content, L.T("Tuyệt!"), UIFactory.Green, () => CloseModal(modal), 30);
                ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(280, 80));
            }
        }
    }
}
