using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>
    /// Tiny pooled sprite-particle system for "juice": dust when tilling, droplets when watering,
    /// leaves when harvesting, sparkles for products and golden harvests. No assets needed.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        private class P
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector3 v;
            public float age, life, spin, gravity, size, drag;
            public Color c;
            public bool grow;
        }

        private static Fx inst;
        private readonly List<P> live = new List<P>();
        private readonly Stack<P> pool = new Stack<P>();
        private const int SortBand = IsoGrid.OverlayOrder - 100;

        private static Fx I
        {
            get
            {
                if (inst == null) inst = new GameObject("Fx").AddComponent<Fx>();
                return inst;
            }
        }

        private P Get(Sprite s)
        {
            P p = pool.Count > 0 ? pool.Pop() : null;
            if (p == null)
            {
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                p = new P { t = go.transform, sr = go.AddComponent<SpriteRenderer>() };
            }
            p.t.gameObject.SetActive(true);
            p.sr.sprite = s;
            p.sr.sortingOrder = SortBand;
            return p;
        }

        /// <summary>Generic burst. speed = launch speed, up = extra upward bias, gravity in world units/s².</summary>
        public static void Burst(Vector3 pos, Color color, int count, Sprite sprite, float speed, float up, float gravity,
                                 float size, float life, float spin = 0f, float drag = 1.5f, bool grow = false, float colorJitter = 0.1f)
        {
            if (count <= 0) return;
            Fx fx = I;
            for (int i = 0; i < count; i++)
            {
                P p = fx.Get(sprite);
                Vector2 dir = Random.insideUnitCircle.normalized;
                float sp = speed * Random.Range(0.45f, 1f);
                p.v = new Vector3(dir.x * sp, dir.y * sp * 0.6f + up * Random.Range(0.6f, 1.2f), 0);
                p.t.position = pos + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.05f, 0.1f), 0);
                p.t.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                p.age = 0f;
                p.life = life * Random.Range(0.75f, 1.2f);
                p.spin = spin * Random.Range(-1f, 1f);
                p.gravity = gravity;
                p.drag = drag;
                p.size = size * Random.Range(0.7f, 1.3f);
                p.grow = grow;
                float j = colorJitter;
                p.c = new Color(Mathf.Clamp01(color.r + Random.Range(-j, j)), Mathf.Clamp01(color.g + Random.Range(-j, j)),
                                Mathf.Clamp01(color.b + Random.Range(-j, j)), color.a);
                p.sr.color = p.c;
                p.t.localScale = Vector3.one * (grow ? p.size * 0.3f : p.size);
                fx.live.Add(p);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                P p = live[i];
                p.age += dt;
                if (p.age >= p.life)
                {
                    p.t.gameObject.SetActive(false);
                    live.RemoveAt(i);
                    pool.Push(p);
                    continue;
                }
                p.v.y += p.gravity * dt;
                p.v *= Mathf.Clamp01(1f - p.drag * dt);
                p.t.position += p.v * dt;
                if (p.spin != 0f) p.t.Rotate(0, 0, p.spin * dt);
                float k = p.age / p.life;
                float s = p.grow ? p.size * Mathf.Lerp(0.3f, 1f, Mathf.Min(1f, k * 3f)) : p.size * Mathf.Lerp(1f, 0.6f, k);
                p.t.localScale = Vector3.one * s;
                Color c = p.c;
                c.a = p.c.a * (k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
                p.sr.color = c;
            }
        }

        // ------------------------------------------------------------------ presets
        private static readonly Color DustC = new Color(0.62f, 0.45f, 0.27f, 0.9f);
        private static readonly Color WaterC = new Color(0.45f, 0.75f, 1f, 0.95f);
        private static readonly Color LeafC = new Color(0.45f, 0.78f, 0.25f, 1f);
        public static readonly Color Gold = new Color(1f, 0.85f, 0.25f, 1f);

        public static void Dust(Vector3 pos, int count = 10) =>
            Burst(pos, DustC, count, ProcSprites.SoftCircle, 1.8f, 0.9f, -2.5f, 0.35f, 0.55f, 0f, 3f, true, 0.06f);

        public static void Water(Vector3 pos) =>
            Burst(pos + Vector3.up * 0.25f, WaterC, 12, ProcSprites.Circle, 1.6f, 2.2f, -9f, 0.09f, 0.6f, 0f, 0.5f);

        public static void Leaves(Vector3 pos, Color tint) =>
            Burst(pos + Vector3.up * 0.35f, Color.Lerp(LeafC, tint, 0.35f), 9, ProcSprites.Diamond, 2.2f, 2.4f, -6f, 0.16f, 0.8f, 540f, 1f);

        public static void Sparkle(Vector3 pos, Color color, int count = 10) =>
            Burst(pos + Vector3.up * 0.5f, color, count, ProcSprites.Diamond, 1.5f, 0.9f, 0.6f, 0.13f, 0.9f, 360f, 2f, false, 0.05f);

        /// <summary>Golden harvest: gold sparkles + a soft glow puff.</summary>
        public static void Golden(Vector3 pos)
        {
            Burst(pos + Vector3.up * 0.4f, new Color(1f, 0.95f, 0.6f, 0.55f), 4, ProcSprites.SoftCircle, 0.6f, 0.4f, 0f, 1.1f, 0.7f, 0f, 4f, true, 0f);
            Sparkle(pos, Gold, 22);
        }
    }
}
