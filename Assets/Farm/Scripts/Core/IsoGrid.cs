using UnityEngine;

namespace Farm
{
    /// <summary>
    /// Isometric grid math. Grid (gx, gy): gx grows toward screen bottom-right, gy toward bottom-left.
    /// Values measured from the art: tile top diamond is ~286 x 180 px at 128 PPU.
    /// </summary>
    public static class IsoGrid
    {
        public const float HalfW = 143f / 128f;
        public const float HalfH = 90f / 128f;
        public const int Size = 30;

        // Sorting bands (sortingOrder is a short: -32768..32767)
        public const int GroundBase = -20000;
        public const int OverlayOrder = 20000;

        public static Vector3 ToWorld(float gx, float gy)
        {
            return new Vector3((gx - gy) * HalfW, -(gx + gy) * HalfH, 0f);
        }

        public static Vector3 CellCenter(Vector2Int c) => ToWorld(c.x, c.y);

        public static Vector2 ToGrid(Vector2 world)
        {
            float a = world.x / HalfW;   // gx - gy
            float b = -world.y / HalfH;  // gx + gy
            return new Vector2((a + b) * 0.5f, (b - a) * 0.5f);
        }

        public static Vector2Int ToCell(Vector2 world)
        {
            Vector2 g = ToGrid(world);
            return new Vector2Int(Mathf.RoundToInt(g.x), Mathf.RoundToInt(g.y));
        }

        public static bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Size && c.y < Size;

        public static int SortOrder(float worldY, int bias = 0)
        {
            return Mathf.Clamp(Mathf.RoundToInt(-worldY * 100f) + bias, -15000, 15000);
        }

        public static int GroundOrder(int gx, int gy, int bias = 0) => GroundBase + (gx + gy) * 2 + bias;

        /// <summary>Point inside the top diamond of a cell.</summary>
        public static bool InDiamond(Vector2 p, Vector2 center, float scale = 1f)
        {
            float dx = Mathf.Abs(p.x - center.x) / (HalfW * scale);
            float dy = Mathf.Abs(p.y - center.y) / (HalfH * scale);
            return dx + dy <= 1f;
        }
    }
}
