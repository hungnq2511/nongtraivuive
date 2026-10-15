using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    public class HomeLevelDef
    {
        public int level, playerLevel, coins;
        public List<ItemStack> mats = new List<ItemStack>();
    }

    /// <summary>B3.1 – farmhouse levels, building materials and beauty points.</summary>
    public static partial class Defs
    {
        // ---------------- building materials (dropped when clearing land, given by visitors)
        public static readonly List<ItemDef> Materials = new List<ItemDef>
        {
            new ItemDef { id = "plank",     name = "Ván gỗ",  icon = "icon_plank",     sell = 15, xp = 0, unlock = 1 },
            new ItemDef { id = "nail",      name = "Đinh",    icon = "icon_nail",      sell = 15, xp = 0, unlock = 1 },
            new ItemDef { id = "roof_tile", name = "Ngói đỏ", icon = "icon_roof_tile", sell = 20, xp = 0, unlock = 1 },
        };

        public static bool IsMaterial(string id) => Materials.Exists(m => m.id == id);

        public const float DebrisMaterialChance = 0.4f;
        public const float VisitorMaterialChance = 0.25f;
        public const int MaterialGemCost = 4;              // buy one missing material with gems

        /// <summary>Random material, rarer ones less often (plank 45%, nail 35%, roof tile 20%).</summary>
        public static string RandomMaterial()
        {
            float r = Random.value;
            return r < 0.45f ? "plank" : r < 0.8f ? "nail" : "roof_tile";
        }

        // ---------------- farmhouse
        public const int HomeMaxLevel = 5;
        public const int HomeStoragePerLevel = 20;
        public const float HomeBonusPerLevel = 0.03f;

        private static HomeLevelDef H(int lvl, int player, int coins, int plank, int nail, int tile)
        {
            var d = new HomeLevelDef { level = lvl, playerLevel = player, coins = coins };
            if (plank > 0) d.mats.Add(new ItemStack("plank", plank));
            if (nail > 0) d.mats.Add(new ItemStack("nail", nail));
            if (tile > 0) d.mats.Add(new ItemStack("roof_tile", tile));
            return d;
        }

        /// <summary>Requirements to REACH each level (index = target level).</summary>
        public static readonly List<HomeLevelDef> HomeLevels = new List<HomeLevelDef>
        {
            H(2, 4, 600, 3, 2, 0),
            H(3, 7, 1500, 6, 4, 2),
            H(4, 10, 3000, 10, 6, 5),
            H(5, 14, 6000, 15, 10, 8),
        };

        public static HomeLevelDef HomeLevel(int target) => HomeLevels.Find(h => h.level == target);

        public static string HomeSprite(int level)
        {
            for (int l = level; l >= 2; l--)
                if (SpriteDB.Exists("building_farmhouse_lv" + l)) return "building_farmhouse_lv" + l;
            return "building_farmhouse";
        }

        // ---------------- beauty points
        /// <summary>Beauty needed for each tier; every tier gives +2% coins/XP and visitors come a bit more often.</summary>
        public static readonly int[] BeautyTiers = { 0, 30, 80, 160, 300, 500, 800, 1200 };
        public const float BeautyBonusPerTier = 0.02f;

        public static int BeautyOf(PlaceDef d) => d != null && d.kind == PlaceKind.Decor ? Mathf.Max(1, d.cost / 10) : 0;

        public static int BeautyTier(int beauty)
        {
            int t = 0;
            for (int i = 1; i < BeautyTiers.Length; i++) if (beauty >= BeautyTiers[i]) t = i;
            return t;
        }

        public static int Beauty()
        {
            int sum = 0;
            if (WorldMap.I == null) return 0;
            foreach (PlaceView p in WorldMap.I.Places) sum += BeautyOf(p.Def);
            return sum;
        }

        /// <summary>Total reward bonus (farmhouse level + beauty tier), e.g. 0.11 = +11% coins and XP.</summary>
        public static float RewardBonus()
        {
            GameManager gm = GameManager.I;
            if (gm == null) return 0f;
            int home = Mathf.Max(1, gm.Data.homeLevel);
            return (home - 1) * HomeBonusPerLevel + BeautyTier(Beauty()) * BeautyBonusPerTier;
        }

        public static int WithBonus(int amount) => Mathf.CeilToInt(amount * (1f + RewardBonus()));

        // ---------------- decorations
        public const float DecorSellRate = 0.5f;
        public static int DecorSellPrice(PlaceDef d) => Mathf.Max(1, Mathf.FloorToInt(d.cost * DecorSellRate));
    }
}
