using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>
    /// In-game clock (1 day = 20 real minutes), night tint, lamp glow (via NightFactor)
    /// and afternoon rain that waters every planted plot automatically.
    /// </summary>
    public class DayNight : MonoBehaviour
    {
        public static DayNight I { get; private set; }

        public float Hour { get; private set; }
        public float NightFactor { get; private set; }
        public bool Raining { get; private set; }

        private Image tint;
        private RectTransform rainRoot;
        private readonly List<RectTransform> drops = new List<RectTransform>();
        private readonly List<Image> dropImages = new List<Image>();
        private float rainAlpha;
        private float slowTimer;
        private bool wasRaining;

        public void Init()
        {
            I = this;
            // Own overlay canvas between the world and the HUD
            var cgo = new GameObject("DayNight Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var root = (RectTransform)cgo.transform;

            tint = UIFactory.Solid(root, new Color(0, 0, 0, 0), "Tint");
            tint.raycastTarget = false;
            tint.rectTransform.Stretch();

            rainRoot = UIFactory.Rect("Rain", root).Stretch();
            for (int i = 0; i < 70; i++)
            {
                Image d = UIFactory.Solid(rainRoot, new Color(0.85f, 0.92f, 1f, 0.55f), "Drop");
                d.raycastTarget = false;
                RectTransform rt = d.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.sizeDelta = new Vector2(3f, Random.Range(28f, 46f));
                rt.localRotation = Quaternion.Euler(0, 0, 12f);
                rt.anchoredPosition = new Vector2(Random.Range(0f, 2400f), Random.Range(0f, 1400f));
                drops.Add(rt);
                dropImages.Add(d);
            }
            rainRoot.gameObject.SetActive(false);
            Evaluate();
        }

        private static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        private void Evaluate()
        {
            double now = GameManager.I.Now + GameManager.I.Data.dayOffset;
            double dayPos = now / Defs.DayLengthSec;
            long dayIndex = (long)System.Math.Floor(dayPos);
            Hour = (float)((6.0 + (dayPos - dayIndex) * 24.0) % 24.0);

            float h = Hour;
            if (h >= 17.5f) NightFactor = Smooth(17.5f, 20f, h);
            else if (h < 6.5f) NightFactor = 1f - Smooth(4.5f, 6.5f, h);
            else NightFactor = 0f;

            // deterministic "weather roll" per in-game day
            uint hash = (uint)(dayIndex * 2654435761L);
            bool rainyDay = hash % 100 < Defs.RainChancePercent;
            Raining = rainyDay && h >= Defs.RainStartHour && h < Defs.RainEndHour;
        }

        private void Update()
        {
            Evaluate();
            float dt = Time.deltaTime;

            rainAlpha = Mathf.MoveTowards(rainAlpha, Raining ? 1f : 0f, dt * 0.5f);
            Color night = new Color(0.04f, 0.08f, 0.28f, 0.42f * NightFactor);
            Color rain = new Color(0.25f, 0.3f, 0.38f, 0.18f * rainAlpha);
            tint.color = new Color(
                Mathf.Lerp(night.r, rain.r, rain.a / Mathf.Max(0.001f, night.a + rain.a)),
                Mathf.Lerp(night.g, rain.g, rain.a / Mathf.Max(0.001f, night.a + rain.a)),
                Mathf.Lerp(night.b, rain.b, rain.a / Mathf.Max(0.001f, night.a + rain.a)),
                Mathf.Clamp01(night.a + rain.a));

            bool showRain = rainAlpha > 0.01f;
            if (rainRoot.gameObject.activeSelf != showRain) rainRoot.gameObject.SetActive(showRain);
            if (showRain)
            {
                Vector2 size = rainRoot.rect.size;
                foreach (RectTransform d in drops)
                {
                    Vector2 p = d.anchoredPosition + new Vector2(-180f, -1100f) * dt;
                    if (p.y < -50f) p = new Vector2(Random.Range(0f, size.x + 300f), size.y + Random.Range(0f, 200f));
                    d.anchoredPosition = p;
                }
                foreach (Image img in dropImages)
                    img.color = new Color(0.85f, 0.92f, 1f, 0.55f * rainAlpha);
            }

            slowTimer -= dt;
            if (slowTimer > 0f) return;
            slowTimer = 1f;

            if (Raining != wasRaining)
            {
                wasRaining = Raining;
                UIManager.I.Toast(Raining ? L.T("Trời mưa! Cây vừa gieo được tưới tự động.") : L.T("Trời đã tạnh."));
            }
            if (Raining && WorldMap.I != null)
                foreach (PlotView p in WorldMap.I.Plots)
                    if (p.D.state == PlotState.Planted && !p.Pending) p.Water();

            int hh = Mathf.FloorToInt(Hour), mm = Mathf.FloorToInt((Hour - hh) * 60f) / 10 * 10;
            string icon = Raining ? "hud_rain" : NightFactor > 0.5f ? "hud_moon" : "hud_sun";
            UIManager.I.SetClock($"{hh:00}:{mm:00}", icon, Raining ? L.T("Mưa") : NightFactor > 0.5f ? L.T("Đêm") : L.T("Nắng"));
        }
    }
}
