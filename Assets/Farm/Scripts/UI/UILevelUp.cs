using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>Celebratory level-up screen: confetti, big level badge, rewards and pictures of what unlocked.</summary>
    public partial class UIManager
    {
        private struct Unlock
        {
            public string icon, text;
        }

        private void OnLevelUp(int level)
        {
            var unlocks = new List<Unlock>();
            foreach (CropDef c in Defs.Crops)
                if (c.unlock == level) unlocks.Add(new Unlock { icon = c.Icon, text = c.name });
            foreach (AnimalDef a in Defs.Animals)
                if (a.unlock == level) unlocks.Add(new Unlock { icon = a.Frame("idle_se"), text = a.name });
            foreach (PlaceDef p in Defs.Places)
                if (p.shop && p.unlock == level && p.kind != PlaceKind.Plot) unlocks.Add(new Unlock { icon = p.sprite, text = p.name });
            foreach (RecipeDef r in Defs.Recipes)
                if (r.unlock == level)
                {
                    ItemDef it = Defs.Item(r.output);
                    unlocks.Add(new Unlock { icon = it.icon, text = it.name });
                }
            foreach (LandZone z in Defs.Zones)
                if (z.unlock == level && z.cost > 0) unlocks.Add(new Unlock { icon = "decor_wooden_sign", text = L.T("Đất: ") + L.T(z.name) });

            bool hasUnlocks = unlocks.Count > 0;
            RectTransform content = OpenWindow(L.T("LÊN CẤP!"), new Vector2(1100, hasUnlocks ? 760 : 560), out GameObject modal);
            modal.AddComponent<ConfettiFx>().Begin((RectTransform)modal.transform);
            Haptics.Success();
            if (level == 2) LocalNotifications.RequestPermission();

            // big badge
            Image glow = UIFactory.Icon(content, "hud_star", 230);
            glow.sprite = ProcSprites.SoftCircle;
            glow.color = new Color(1f, 0.85f, 0.3f, 0.55f);
            glow.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(260, 260));
            glow.gameObject.AddComponent<UISpin>().speed = 0f;
            Image badge = UIFactory.Icon(content, "hud_star", 180);
            badge.sprite = ProcSprites.Circle;
            badge.color = UIFactory.GreenDark;
            badge.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(180, 180));
            Image inner = UIFactory.Icon(badge.transform, "hud_star", 160);
            inner.sprite = ProcSprites.Circle;
            inner.color = UIFactory.Green;
            inner.rectTransform.Stretch(10);
            Text num = UIFactory.Label(badge.transform, level.ToString(), 84, Color.white, TextAnchor.MiddleCenter, true, true);
            num.rectTransform.Stretch();
            badge.gameObject.AddComponent<UIPop>();

            // rewards
            RectTransform rewards = UIFactory.Rect("Rewards", content);
            rewards.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -214), new Vector2(700, 80));
            UIFactory.HRow(rewards, 40);
            void Reward(string icon, string text)
            {
                RectTransform r = UIFactory.Rect("R", rewards);
                r.sizeDelta = new Vector2(260, 80);
                Image ic = UIFactory.Icon(r, icon, 72);
                ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(72, 72));
                Text t = UIFactory.Label(r, text, 40, UIFactory.TextDark, TextAnchor.MiddleLeft, true);
                t.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(82, 0), new Vector2(180, 70));
            }
            Reward("hud_coin", "+" + Defs.LevelUpCoins(level));
            Reward("hud_gem", "+" + Defs.LevelUpGems(level));

            if (hasUnlocks)
            {
                Txt(content, L.T("Mở khoá mới"), 30, UIFactory.GreenDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(0, -300), new Vector2(600, 44), true);
                RectTransform grid = UIFactory.Rect("Unlocks", content);
                grid.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(1040, 250));
                GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(190, 230);
                g.spacing = new Vector2(14, 10);
                g.childAlignment = TextAnchor.UpperCenter;
                int shown = Mathf.Min(unlocks.Count, 5);
                for (int i = 0; i < shown; i++)
                {
                    Image card = UIFactory.Panel(grid, Color.white, "Unlock");
                    card.raycastTarget = false;
                    Image ic = UIFactory.Icon(card.transform, unlocks[i].icon, 140);
                    ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(150, 150));
                    Txt(card.transform, unlocks[i].text, 22, UIFactory.TextDark, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                        new Vector2(0, 8), new Vector2(180, 62), true);
                    card.gameObject.AddComponent<UIPop>().delay = 0.25f + i * 0.08f;
                }
                if (unlocks.Count > shown)
                    Txt(content, L.F("và {0} thứ khác…", unlocks.Count - shown), 24, UIFactory.Wood, TextAnchor.MiddleRight, new Vector2(1, 0), new Vector2(1, 0),
                        new Vector2(-20, 100), new Vector2(400, 36));
            }

            Button ok = UIFactory.MakeButton(content, L.T("Tuyệt vời!"), UIFactory.Green, () => CloseModal(modal), 34);
            ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(320, 88));
        }
    }

    /// <summary>Scale-in "pop" for UI elements.</summary>
    public class UIPop : MonoBehaviour
    {
        public float delay;
        private float t;

        private void OnEnable()
        {
            t = 0f;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01((t - delay) / 0.35f);
            // ease-out-back: quick grow past full size, then settle
            float s = 0f;
            if (k > 0f)
            {
                const float c1 = 1.70158f, c3 = c1 + 1f;
                float x = k - 1f;
                s = 1f + c3 * x * x * x + c1 * x * x;
            }
            transform.localScale = Vector3.one * s;
            if (k >= 1f) { transform.localScale = Vector3.one; enabled = false; }
        }
    }

    public class UISpin : MonoBehaviour
    {
        public float speed;
        private float t;

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.06f);
            if (speed != 0f) transform.Rotate(0, 0, speed * Time.unscaledDeltaTime);
        }
    }

    /// <summary>Confetti falling over a modal for a couple of seconds.</summary>
    public class ConfettiFx : MonoBehaviour
    {
        private class Bit
        {
            public RectTransform rt;
            public Vector2 v;
            public float spin;
        }

        private readonly List<Bit> bits = new List<Bit>();
        private float age;
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.35f, 0.35f), new Color(1f, 0.8f, 0.2f), new Color(0.35f, 0.8f, 0.35f),
            new Color(0.3f, 0.6f, 1f), new Color(0.85f, 0.45f, 1f), Color.white,
        };

        public void Begin(RectTransform root)
        {
            Vector2 size = root.rect.size;
            for (int i = 0; i < 90; i++)
            {
                Image img = UIFactory.Solid(root, Colors[i % Colors.Length], "Confetti");
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Random.Range(12f, 22f), Random.Range(20f, 34f));
                // two bursts from the bottom corners plus a gentle rain from the top
                Vector2 pos, v;
                if (i < 30) { pos = new Vector2(-size.x * 0.45f, -size.y * 0.4f); v = new Vector2(Random.Range(300f, 900f), Random.Range(900f, 1600f)); }
                else if (i < 60) { pos = new Vector2(size.x * 0.45f, -size.y * 0.4f); v = new Vector2(Random.Range(-900f, -300f), Random.Range(900f, 1600f)); }
                else { pos = new Vector2(Random.Range(-size.x * 0.5f, size.x * 0.5f), size.y * 0.55f + Random.Range(0f, 300f)); v = new Vector2(Random.Range(-60f, 60f), Random.Range(-200f, -80f)); }
                rt.anchoredPosition = pos;
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                bits.Add(new Bit { rt = rt, v = v, spin = Random.Range(-500f, 500f) });
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            age += dt;
            foreach (Bit b in bits)
            {
                if (b.rt == null) continue;
                b.v.y -= 1500f * dt;
                b.v.y = Mathf.Max(b.v.y, -420f);
                b.v.x *= 1f - 1.2f * dt;
                b.rt.anchoredPosition += b.v * dt;
                b.rt.Rotate(0, 0, b.spin * dt);
            }
            if (age > 3.2f)
            {
                foreach (Bit b in bits) if (b.rt != null) Destroy(b.rt.gameObject);
                bits.Clear();
                Destroy(this);
            }
        }
    }
}
