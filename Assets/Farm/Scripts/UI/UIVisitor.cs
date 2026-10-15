using System;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    public partial class UIManager
    {
        // ================================================================== VISITOR (B2.1)
        public void OpenVisitor(VisitorView v)
        {
            if (PlacementController.I.Active || v == null) return;
            VisitorSystem vs = VisitorSystem.I;
            GameManager gm = GameManager.I;
            VisitorDef def = v.Def;
            VisitorData d = v.D;
            RectTransform content = OpenWindow(L.T(def.name) + " – " + L.T(def.title), new Vector2(1240, 700), out GameObject modal);

            // portrait + friendship
            Image card = UIFactory.Panel(content, UIFactory.CreamDark, "Portrait");
            card.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(330, 600));
            Image face = UIFactory.Icon(card.transform, def.Frame("wave"), 300);
            face.preserveAspect = true;
            face.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(290, 420));
            int lvl = vs.FriendLevel(def.id);
            int into = vs.Friendship(def.id) - lvl * Defs.FriendPerLevel;
            Txt(card.transform, lvl > 0 ? L.F("Thân thiết cấp {0}", lvl) : L.T("Mới quen"), 28, UIFactory.Red, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(310, 44), true);
            Bar(card.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 72), new Vector2(260, 26),
                lvl >= Defs.FriendMaxLevel ? 1f : (float)into / Defs.FriendPerLevel, new Color(1f, 0.45f, 0.55f));
            Txt(card.transform, lvl > 0 ? L.F("Thưởng +{0}%", lvl * 5) : L.F("Giao {0} lần để thân hơn", Defs.FriendPerLevel), 22, UIFactory.Wood, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(310, 40));

            // speech bubble
            Image speech = UIFactory.Panel(content, Color.white, "Speech");
            speech.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, 0), new Vector2(820, 130));
            Txt(speech.transform, "\"" + L.T(v.Line) + "\"", 28, UIFactory.TextDark, TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 120)).fontStyle = FontStyle.Italic;

            RectTransform list = UIFactory.Rect("Request", content);
            list.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -150), new Vector2(820, 230));
            RectTransform rewards = UIFactory.Rect("Rewards", content);
            rewards.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(360, 100), new Vector2(820, 80));

            Action rebuild = null;
            Button give = null;
            rebuild = () =>
            {
                if (v == null || !v.Waiting) { CloseModal(modal); return; }
                ClearChildren(list);
                ClearChildren(rewards);
                float x = 0;
                foreach (ItemStack r in d.req)
                {
                    ItemDef it = Defs.Item(r.id);
                    int have = gm.Count(r.id);
                    Image slot = UIFactory.Panel(list, Color.white, "Item");
                    slot.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, 0), new Vector2(250, 220));
                    Image ic = UIFactory.Icon(slot.transform, it.icon, 110);
                    ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(110, 110));
                    Txt(slot.transform, it.name, 24, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 54), new Vector2(240, 34), true);
                    Txt(slot.transform, $"{have}/{r.count}", 32, have >= r.count ? UIFactory.GreenDark : UIFactory.Red, TextAnchor.MiddleCenter,
                        new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(240, 40), true);
                    x += 270;
                }

                Txt(rewards, L.T("Thưởng:"), 30, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(130, 60), true);
                Image coin = UIFactory.Icon(rewards, "hud_coin", 56);
                coin.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, 0), new Vector2(56, 56));
                Txt(rewards, Defs.WithBonus(d.coins).ToString(), 32, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(204, 0), new Vector2(120, 60), true);
                Txt(rewards, $"+{Defs.WithBonus(d.xp)} XP", 30, UIFactory.Blue, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(330, 0), new Vector2(150, 60), true);
                if (d.gems > 0)
                {
                    Image gem = UIFactory.Icon(rewards, "hud_gem", 52);
                    gem.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(480, 0), new Vector2(52, 52));
                    Txt(rewards, "+" + d.gems, 30, UIFactory.TextDark, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(540, 0), new Vector2(80, 60), true);
                }
                if (give != null) give.GetComponent<Image>().color = vs.CanServe(d) ? UIFactory.Green : UIFactory.Gray;
            };

            give = UIFactory.MakeButton(content, L.T("Giao hàng"), UIFactory.Green, () =>
            {
                if (vs.Serve(v))
                {
                    Toast(L.T(def.name) + ": \"" + L.T(def.thanks[UnityEngine.Random.Range(0, def.thanks.Length)]) + "\"");
                    CloseModal(modal);
                }
                else Toast(L.T("Chưa đủ hàng. Trồng / sản xuất thêm rồi quay lại nhé – khách sẽ chờ!"));
            }, 30);
            ((RectTransform)give.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 10), new Vector2(300, 80));
            Button no = UIFactory.MakeButton(content, L.T("Từ chối"), UIFactory.Red, () =>
            {
                vs.Decline(v);
                Toast(L.F("{0} đã về. Khách khác sẽ ghé sau.", def.name));
                CloseModal(modal);
            }, 26);
            ((RectTransform)no.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-330, 10), new Vector2(200, 80));

            rebuild();
            SetTicker(modal, rebuild);
        }
    }
}
