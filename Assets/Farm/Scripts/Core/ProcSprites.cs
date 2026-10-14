using UnityEngine;

namespace Farm
{
    /// <summary>Runtime-generated white shapes for UI panels, bubbles and tile highlights.</summary>
    public static class ProcSprites
    {
        private static Sprite rounded, circle, diamond, softCircle;

        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = MakeRounded(96, 30);
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = MakeCircle(128, false);
                return circle;
            }
        }

        public static Sprite SoftCircle
        {
            get
            {
                if (softCircle == null) softCircle = MakeCircle(128, true);
                return softCircle;
            }
        }

        /// <summary>Diamond matching one iso tile top (world size 2*HalfW x 2*HalfH at 128 PPU).</summary>
        public static Sprite Diamond
        {
            get
            {
                if (diamond == null) diamond = MakeDiamond();
                return diamond;
            }
        }

        private static Sprite arrow;

        /// <summary>Downward-pointing arrow with a dark outline (tutorial pointer).</summary>
        public static Sprite Arrow
        {
            get
            {
                if (arrow == null) arrow = MakeArrow();
                return arrow;
            }
        }

        private static Sprite MakeArrow()
        {
            int w = 96, h = 120;
            Texture2D t = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = x + 0.5f - w * 0.5f, cy = y + 0.5f;
                // shaft (top part) + head (bottom triangle), y = 0 is the tip
                bool head = cy < 60f && Mathf.Abs(cx) < cy * 0.8f;
                bool shaft = cy >= 52f && cy < 116f && Mathf.Abs(cx) < 18f;
                bool headIn = cy > 8f && cy < 54f && Mathf.Abs(cx) < (cy - 8f) * 0.8f - 4f;
                bool shaftIn = cy >= 50f && cy < 110f && Mathf.Abs(cx) < 11f;
                Color32 c = new Color32(0, 0, 0, 0);
                if (head || shaft) c = new Color32(90, 50, 10, 255);
                if (headIn || shaftIn) c = new Color32(255, 255, 255, 255);
                px[y * w + x] = c;
            }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 128f);
        }

        private static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        private static Sprite MakeRounded(int size, int radius)
        {
            Texture2D t = NewTex(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite MakeCircle(int size, bool soft)
        {
            Texture2D t = NewTex(size, size);
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = soft ? Mathf.Clamp01(1f - d / r) : Mathf.Clamp01(r - d);
                if (soft) a *= a;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
        }

        private static Sprite MakeDiamond()
        {
            int w = 286, h = 180;
            Texture2D t = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f);
                float dy = Mathf.Abs(y + 0.5f - h * 0.5f) / (h * 0.5f);
                float s = dx + dy;
                byte a = s <= 1f ? (byte)(s > 0.9f ? 255 : 150) : (byte)0;
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 128f);
        }
    }
}
