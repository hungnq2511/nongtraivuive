using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    public partial class UIManager
    {
        // ================================================================== FARMHOUSE (B3.1)
        public void OpenFarmhouse()
        {
            if (PlacementController.I.Active) return;
            RectTransform content = OpenWindow(L.T("Nhà chính"), new Vector2(1440, 820), out GameObject modal);
            GameManager gm = GameManager.I;
            SaveData d = gm.Data;

            Action rebuild = null;
            rebuild = () =>
            {
                ClearChildren(content);
                int lvl = Mathf.Max(1, d.homeLevel);
                int beauty = Defs.Beauty();
                int tier = Defs.BeautyTier(beauty);
                float bonus = Defs.RewardBonus();

                // ---- left: the house + farm stats
                Image left = UIFactory.Panel(content, UIFactory.CreamDark, "House");
                left.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(430, 730));
                Image fh = UIFactory.Icon(left.transform, Defs.HomeSprite(lvl), 300);
                fh.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(320, 300));
                Txt(left.transform, L.F("Nhà chính cấp {0}/{1}", lvl, Defs.HomeMaxLevel), 32, UIFactory.TextDark, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -322), new Vector2(410, 44), true);
                string stats =
                    L.F("Cấp người chơi: <b>{0}</b> ({1}/{2} XP)\n", d.level, d.xp, Defs.XpToNext(d.level)) +
                    L.F("Ô đất: <b>{0}</b>   Vật nuôi: <b>{1}</b>\n", d.plots.Count, d.animals.Count) +
                    L.F("Kho: <b>{0}/{1}</b>\n", gm.StorageUsed, gm.StorageCapacity) +
                    L.F("Chuỗi đăng nhập: <b>{0}</b> ngày", d.dailyStreak);
                Txt(left.transform, stats, 24, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -376), new Vector2(390, 150));
                Button s = UIFactory.MakeButton(left.transform, L.T("Cài đặt"), UIFactory.Blue, () => { CloseModal(modal); OpenSettings(); }, 26);
                ((RectTransform)s.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(220, 66));

                // ---- right top: beauty
                Image bp = UIFactory.Panel(content, Color.white, "Beauty");
                bp.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(920, 200));
                Txt(bp.transform, L.F("Điểm đẹp: <b>{0}</b>   ·   Hạng {1}", beauty, tier), 30, UIFactory.TextDark, TextAnchor.MiddleLeft,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -12), new Vector2(600, 44), true);
                bool maxTier = tier >= Defs.BeautyTiers.Length - 1;
                float pct = maxTier ? 1f : Mathf.InverseLerp(Defs.BeautyTiers[tier], Defs.BeautyTiers[tier + 1], beauty);
                Bar(bp.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -66), new Vector2(560, 28), pct, new Color(1f, 0.55f, 0.7f));
                Txt(bp.transform, maxTier ? L.T("Hạng cao nhất!") : L.F("Hạng {0} ở {1} điểm", tier + 1, Defs.BeautyTiers[tier + 1]), 22, UIFactory.Wood, TextAnchor.MiddleLeft,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(596, -62), new Vector2(310, 34));
                Txt(bp.transform,
                    L.T("Mỗi món trang trí cộng điểm đẹp (giá/10). Mỗi hạng: +2% xu & XP từ đơn hàng, khách, xe tải; khách ghé thăm thường xuyên hơn."),
                    22, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -104), new Vector2(880, 60));
                Txt(bp.transform, L.F("Tổng thưởng hiện tại: <b>+{0}%</b> xu & XP", Mathf.RoundToInt(bonus * 100)), 24, UIFactory.GreenDark, TextAnchor.MiddleLeft,
                    new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 8), new Vector2(600, 36), true);
                Button deco = UIFactory.MakeButton(bp.transform, L.T("Mua trang trí"), UIFactory.Green, () => { CloseModal(modal); OpenShop("decor"); }, 24);
                ((RectTransform)deco.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 10), new Vector2(230, 60));

                // ---- right bottom: upgrade
                Image up = UIFactory.Panel(content, Color.white, "Upgrade");
                up.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(920, 510));
                if (lvl >= Defs.HomeMaxLevel)
                {
                    Txt(up.transform, L.T("Nhà chính đã đạt cấp tối đa!\nCảm ơn bạn đã xây dựng nông trại tuyệt đẹp."), 30, UIFactory.GreenDark, TextAnchor.MiddleCenter,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 200), true);
                    return;
                }
                HomeLevelDef next = Defs.HomeLevel(lvl + 1);
                Txt(up.transform, L.F("Nâng lên cấp {0}", lvl + 1), 30, UIFactory.TextDark, TextAnchor.MiddleLeft,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -12), new Vector2(500, 44), true);
                Txt(up.transform,
                    L.F("• Kho +{0} chỗ\n• +{1}% xu & XP (đơn hàng, khách, xe tải)", Defs.HomeStoragePerLevel, Mathf.RoundToInt(Defs.HomeBonusPerLevel * 100)),
                    24, UIFactory.GreenDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -60), new Vector2(560, 80));

                // requirements
                bool levelOk = d.level >= next.playerLevel;
                bool coinsOk = gm.CanAfford(next.coins);
                Txt(up.transform, L.F("Cần cấp {0}", next.playerLevel), 26, levelOk ? UIFactory.GreenDark : UIFactory.Red, TextAnchor.MiddleLeft,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(600, -20), new Vector2(300, 36), true);
                Image coin = UIFactory.Icon(up.transform, "hud_coin", 44);
                coin.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(600, -66), new Vector2(44, 44));
                Txt(up.transform, $"{next.coins:N0}", 26, coinsOk ? UIFactory.GreenDark : UIFactory.Red, TextAnchor.MiddleLeft,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(652, -70), new Vector2(240, 36), true);

                int missing = 0;
                float x = 20;
                foreach (ItemStack m in next.mats)
                {
                    ItemDef it = Defs.Item(m.id);
                    int have = gm.Count(m.id);
                    if (have < m.count) missing += m.count - have;
                    Image slot = UIFactory.Panel(up.transform, UIFactory.CreamDark, "Mat");
                    slot.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -150), new Vector2(200, 200));
                    Image ic = UIFactory.Icon(slot.transform, it.icon, 96);
                    ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(96, 96));
                    Txt(slot.transform, it.name, 22, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(190, 32), true);
                    Txt(slot.transform, $"{have}/{m.count}", 30, have >= m.count ? UIFactory.GreenDark : UIFactory.Red, TextAnchor.MiddleCenter,
                        new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(190, 40), true);
                    x += 216;
                }
                Txt(up.transform, L.T("Vật liệu có được khi dọn gốc cây / tảng đá và khi giao hàng cho khách ghé thăm."), 20, UIFactory.Wood, TextAnchor.MiddleLeft,
                    new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 96), new Vector2(880, 30));

                if (missing > 0)
                {
                    int gems = missing * Defs.MaterialGemCost;
                    Button buy = UIFactory.MakeButton(up.transform, L.F("Mua {0} vật liệu thiếu", missing), UIFactory.Blue, () =>
                    {
                        if (!gm.HasSpace(missing)) { Toast(L.T("Kho đầy! Hãy bán bớt hàng.")); return; }
                        if (!gm.TrySpendGems(gems)) { Toast(L.T("Không đủ kim cương!")); return; }
                        foreach (ItemStack m in next.mats)
                        {
                            int lack = m.count - gm.Count(m.id);
                            if (lack > 0) gm.AddItem(m.id, lack);
                        }
                        rebuild();
                    }, 22);
                    ((RectTransform)buy.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 14), new Vector2(380, 70));
                    Image gi = UIFactory.Icon(buy.transform, "hud_gem", 40);
                    gi.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-56, 0), new Vector2(40, 40));
                    Txt(buy.transform, gems.ToString(), 24, Color.white, TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(48, 40), true);
                }

                bool can = levelOk && coinsOk && missing == 0;
                Button go = UIFactory.MakeButton(up.transform, L.T("Nâng cấp"), can ? UIFactory.Green : UIFactory.Gray, () =>
                {
                    if (!levelOk) { Toast(L.F("Cần đạt cấp {0}.", next.playerLevel)); return; }
                    if (missing > 0) { Toast(L.T("Chưa đủ vật liệu.")); return; }
                    if (!gm.TrySpend(next.coins)) { Toast(L.T("Không đủ xu!")); return; }
                    foreach (ItemStack m in next.mats) gm.RemoveItem(m.id, m.count);
                    d.homeLevel = lvl + 1;
                    gm.AddXp(10 * lvl);
                    PlaceView house = WorldMap.I.Places.Find(p => p.Def.id == "farmhouse");
                    if (house != null)
                    {
                        house.RefreshHomeLook();
                        house.Punch();
                        Fx.Golden(house.transform.position + Vector3.up * 0.8f);
                    }
                    Haptics.Success();
                    GameEvents.Raise("home_up", "", 1);
                    gm.NotifyChanged();
                    Toast(L.F("Nhà chính lên cấp {0}! Kho +{1}, thưởng +{2}%.", d.homeLevel, Defs.HomeStoragePerLevel, Mathf.RoundToInt(Defs.HomeBonusPerLevel * 100)));
                    rebuild();
                }, 30);
                ((RectTransform)go.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 14), new Vector2(260, 76));
            };
            rebuild();
        }

        // ================================================================== DECORATION ACTIONS
        public void OpenDecor(PlaceView v)
        {
            if (PlacementController.I.Active || v == null) return;
            RectTransform content = OpenWindow(v.Def.name, new Vector2(900, 460), out GameObject modal);
            Image ic = UIFactory.Icon(content, v.Def.sprite, 220);
            ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 20), new Vector2(240, 240));
            Txt(content, L.F("Điểm đẹp: <b>+{0}</b>\nGiá bán lại: <b>{1} xu</b>\n\nCất vào kho để đặt lại miễn phí sau (không tốn chỗ kho hàng).", Defs.BeautyOf(v.Def), Defs.DecorSellPrice(v.Def)),
                26, UIFactory.TextDark, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(280, -10), new Vector2(560, 220));

            Button move = UIFactory.MakeButton(content, L.T("Di chuyển"), UIFactory.Green, () =>
            {
                CloseModal(modal);
                if (InteractionController.I.BuildMode) ToggleBuildMode();
                PlacementController.I.BeginMove(v);
            }, 26);
            ((RectTransform)move.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(280, 10), new Vector2(180, 74));
            Button store = UIFactory.MakeButton(content, L.T("Cất kho"), UIFactory.Blue, () => { CloseModal(modal); StoreDecor(v); }, 26);
            ((RectTransform)store.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(476, 10), new Vector2(180, 74));
            Button sell = UIFactory.MakeButton(content, L.F("Bán +{0}", Defs.DecorSellPrice(v.Def)), UIFactory.Orange, () => { CloseModal(modal); SellDecor(v); }, 24);
            ((RectTransform)sell.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(672, 10), new Vector2(180, 74));
        }

        public void StoreDecor(PlaceView v)
        {
            if (v == null || !v.IsDecor) return;
            SaveData d = GameManager.I.Data;
            ItemStack st = d.decorStore.Find(x => x.id == v.Def.id);
            if (st == null) d.decorStore.Add(new ItemStack(v.Def.id, 1));
            else st.count++;
            Fx.Dust(v.transform.position, 10);
            string name = v.Def.name;
            WorldMap.I.RemovePlace(v);
            GameManager.I.NotifyChanged();
            Toast(L.F("Đã cất {0} vào kho. Đặt lại ở Cửa hàng › Trang trí.", name));
        }

        public void SellDecor(PlaceView v)
        {
            if (v == null || !v.IsDecor) return;
            int price = Defs.DecorSellPrice(v.Def);
            Vector3 at = v.transform.position + Vector3.up;
            string name = v.Def.name;
            Fx.Dust(v.transform.position, 10);
            WorldMap.I.RemovePlace(v);
            GameManager.I.AddCoins(price);
            FloatText(at, "+" + price, "hud_coin");
            Toast(L.F("Đã bán {0}: +{1} xu", name, price));
        }

        /// <summary>Place a stored decoration again for free.</summary>
        private void PlaceFromStore(PlaceDef def, GameObject shopModal)
        {
            CloseModal(shopModal);
            if (InteractionController.I.BuildMode) ToggleBuildMode();
            PlacementController.I.BeginNew(def, (gx, gy) =>
            {
                SaveData d = GameManager.I.Data;
                ItemStack st = d.decorStore.Find(x => x.id == def.id);
                if (st == null || st.count <= 0) { Toast(L.T("Món này không còn trong kho.")); return; }
                st.count--;
                if (st.count <= 0) d.decorStore.Remove(st);
                PlaceView pv = WorldMap.I.AddPlace(def.id, gx, gy);
                Fx.Dust(pv.transform.position, 8);
                GameManager.I.NotifyChanged();
            });
        }
    }
}
