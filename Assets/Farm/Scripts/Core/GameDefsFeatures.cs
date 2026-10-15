using System.Collections.Generic;

namespace Farm
{
    public class RecipeDef
    {
        public string output, machine;
        public ItemStack[] inputs;
        public int sec, unlock;
    }

    public class TreeDef
    {
        public string place, fruit;
        public int cycleSec, yield, harvests;
    }

    public class FishDef
    {
        public string id;
        public float chance;     // share of catches
        public float needleSpeed; // how hard the mini-game is
        public float zoneWidth;   // 0..1 width of the green zone
    }

    public class AchievementDef
    {
        public string key, stat, name, desc;
        public int[] tiers;
        public int[] rewards;
    }

    public static partial class Defs
    {
        // ------------------------------------------------------------------ new goods
        public static readonly List<ItemDef> ExtraItems = new List<ItemDef>
        {
            new ItemDef { id = "berry",            name = "Quả mọng",         icon = "icon_berry",            sell = 9,   xp = 2,  unlock = 3 },
            new ItemDef { id = "apple",            name = "Táo",              icon = "icon_apple",            sell = 14,  xp = 3,  unlock = 4 },
            new ItemDef { id = "flour",            name = "Bột mì",           icon = "icon_flour",            sell = 14,  xp = 3,  unlock = 4 },
            new ItemDef { id = "cornmeal",         name = "Bột ngô",          icon = "icon_cornmeal",         sell = 22,  xp = 4,  unlock = 4 },
            new ItemDef { id = "bread",            name = "Bánh mì",          icon = "icon_bread",            sell = 50,  xp = 8,  unlock = 6 },
            new ItemDef { id = "cornbread",        name = "Bánh ngô",         icon = "icon_cornbread",        sell = 85,  xp = 10, unlock = 6 },
            new ItemDef { id = "pumpkin_pie",      name = "Bánh bí đỏ",       icon = "icon_pumpkin_pie",      sell = 110, xp = 16, unlock = 10 },
            new ItemDef { id = "cheese",           name = "Phô mai",          icon = "icon_cheese",           sell = 75,  xp = 10, unlock = 8 },
            new ItemDef { id = "butter",           name = "Bơ",               icon = "icon_butter",           sell = 105, xp = 12, unlock = 8 },
            new ItemDef { id = "goat_cheese",      name = "Phô mai dê",       icon = "icon_goat_cheese",      sell = 130, xp = 15, unlock = 8 },
            new ItemDef { id = "tomato_juice",     name = "Nước ép cà chua",  icon = "icon_tomato_juice",     sell = 75,  xp = 9,  unlock = 9 },
            new ItemDef { id = "strawberry_juice", name = "Nước ép dâu",      icon = "icon_strawberry_juice", sell = 115, xp = 12, unlock = 9 },
            new ItemDef { id = "watermelon_juice", name = "Nước ép dưa hấu",  icon = "icon_watermelon_juice", sell = 145, xp = 16, unlock = 11 },
            new ItemDef { id = "fish_crucian",     name = "Cá diếc",          icon = "icon_fish_crucian",     sell = 18,  xp = 3,  unlock = 3 },
            new ItemDef { id = "fish_carp",        name = "Cá chép",          icon = "icon_fish_carp",        sell = 35,  xp = 5,  unlock = 3 },
            new ItemDef { id = "fish_golden",      name = "Cá vàng",          icon = "icon_fish_golden",      sell = 120, xp = 15, unlock = 3 },
            // v0.5: new fruit trees, beehive, sugar chain and 4 new machines
            new ItemDef { id = "cherry",               name = "Anh đào",          icon = "icon_cherry",               sell = 20,  xp = 4,  unlock = 5 },
            new ItemDef { id = "orange",               name = "Cam",              icon = "icon_orange",               sell = 26,  xp = 5,  unlock = 6 },
            new ItemDef { id = "lemon",                name = "Chanh",            icon = "icon_lemon",                sell = 30,  xp = 6,  unlock = 7 },
            new ItemDef { id = "honey",                name = "Mật ong",          icon = "icon_honey",                sell = 35,  xp = 5,  unlock = 4 },
            new ItemDef { id = "sugar",                name = "Đường",            icon = "icon_sugar",                sell = 65,  xp = 8,  unlock = 10 },
            new ItemDef { id = "popcorn",              name = "Bỏng ngô",         icon = "icon_popcorn",              sell = 45,  xp = 7,  unlock = 12 },
            new ItemDef { id = "caramel_popcorn",      name = "Bỏng ngô caramel", icon = "icon_caramel_popcorn",      sell = 120, xp = 13, unlock = 13 },
            new ItemDef { id = "fabric",               name = "Vải",              icon = "icon_fabric",               sell = 100, xp = 12, unlock = 14 },
            new ItemDef { id = "sweater",              name = "Áo len",           icon = "icon_sweater",              sell = 235, xp = 22, unlock = 15 },
            new ItemDef { id = "ice_cream",            name = "Kem vani",         icon = "icon_ice_cream",            sell = 115, xp = 13, unlock = 16 },
            new ItemDef { id = "strawberry_ice_cream", name = "Kem dâu",          icon = "icon_strawberry_ice_cream", sell = 180, xp = 18, unlock = 17 },
            new ItemDef { id = "orange_juice",         name = "Nước cam",         icon = "icon_orange_juice",         sell = 95,  xp = 11, unlock = 9 },
            new ItemDef { id = "lemonade",             name = "Nước chanh",       icon = "icon_lemonade",             sell = 130, xp = 14, unlock = 11 },
            new ItemDef { id = "cherry_pie",           name = "Bánh anh đào",     icon = "icon_cherry_pie",           sell = 145, xp = 16, unlock = 12 },
            new ItemDef { id = "honey_cake",           name = "Bánh mật ong",     icon = "icon_honey_cake",           sell = 150, xp = 16, unlock = 13 },
        };

        // ------------------------------------------------------------------ production
        public const int MachineBaseSlots = 2;
        public const int MachineMaxSlots = 5;
        public static int MachineSlotCost(int currentSlots) => 150 * currentSlots;

        private static ItemStack[] In(params object[] a)
        {
            var r = new ItemStack[a.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = new ItemStack((string)a[i * 2], (int)a[i * 2 + 1]);
            return r;
        }

        public static readonly List<RecipeDef> Recipes = new List<RecipeDef>
        {
            new RecipeDef { output = "flour",            machine = "mill",          inputs = In("wheat", 3),                          sec = 120, unlock = 4 },
            new RecipeDef { output = "cornmeal",         machine = "mill",          inputs = In("corn", 2),                           sec = 180, unlock = 4 },
            new RecipeDef { output = "bread",            machine = "bakery",        inputs = In("flour", 2, "egg", 1),                sec = 300, unlock = 6 },
            new RecipeDef { output = "cornbread",        machine = "bakery",        inputs = In("cornmeal", 2, "egg", 2),             sec = 360, unlock = 6 },
            new RecipeDef { output = "pumpkin_pie",      machine = "bakery",        inputs = In("flour", 1, "pumpkin", 1, "egg", 1),  sec = 600, unlock = 10 },
            new RecipeDef { output = "cheese",           machine = "dairy_factory", inputs = In("milk", 2),                           sec = 360, unlock = 8 },
            new RecipeDef { output = "butter",           machine = "dairy_factory", inputs = In("milk", 3),                           sec = 480, unlock = 8 },
            new RecipeDef { output = "goat_cheese",      machine = "dairy_factory", inputs = In("goat_milk", 2),                      sec = 600, unlock = 8 },
            new RecipeDef { output = "tomato_juice",     machine = "juice_press",   inputs = In("tomato", 3),                         sec = 360, unlock = 9 },
            new RecipeDef { output = "strawberry_juice", machine = "juice_press",   inputs = In("strawberry", 3),                     sec = 480, unlock = 9 },
            new RecipeDef { output = "watermelon_juice", machine = "juice_press",   inputs = In("watermelon", 2),                     sec = 600, unlock = 11 },
            new RecipeDef { output = "orange_juice",     machine = "juice_press",   inputs = In("orange", 3),                         sec = 480, unlock = 9 },
            new RecipeDef { output = "lemonade",         machine = "juice_press",   inputs = In("lemon", 2, "sugar", 1),              sec = 540, unlock = 11 },
            new RecipeDef { output = "cherry_pie",       machine = "bakery",        inputs = In("flour", 1, "cherry", 3, "egg", 1),   sec = 720, unlock = 12 },
            new RecipeDef { output = "honey_cake",       machine = "bakery",        inputs = In("flour", 2, "honey", 1, "egg", 1),    sec = 600, unlock = 13 },
            new RecipeDef { output = "sugar",            machine = "sugar_mill",    inputs = In("sugarcane", 2),                      sec = 300, unlock = 10 },
            new RecipeDef { output = "popcorn",          machine = "popcorn_maker", inputs = In("corn", 3),                           sec = 240, unlock = 12 },
            new RecipeDef { output = "caramel_popcorn",  machine = "popcorn_maker", inputs = In("corn", 2, "sugar", 1),               sec = 420, unlock = 13 },
            new RecipeDef { output = "fabric",           machine = "loom",          inputs = In("wool", 2),                           sec = 480, unlock = 14 },
            new RecipeDef { output = "sweater",          machine = "loom",          inputs = In("fabric", 1, "wool", 2),              sec = 900, unlock = 15 },
            new RecipeDef { output = "ice_cream",        machine = "ice_cream_maker", inputs = In("milk", 1, "sugar", 1),             sec = 420, unlock = 16 },
            new RecipeDef { output = "strawberry_ice_cream", machine = "ice_cream_maker", inputs = In("milk", 1, "sugar", 1, "strawberry", 2), sec = 600, unlock = 17 },
        };

        public static RecipeDef Recipe(string output) => Recipes.Find(r => r.output == output);

        // ------------------------------------------------------------------ fruit trees
        public static readonly List<TreeDef> Trees = new List<TreeDef>
        {
            new TreeDef { place = "berry_bush", fruit = "berry", cycleSec = 360, yield = 3, harvests = 6 },
            new TreeDef { place = "apple_tree", fruit = "apple", cycleSec = 600, yield = 3, harvests = 6 },
            new TreeDef { place = "cherry_tree", fruit = "cherry", cycleSec = 900,  yield = 3, harvests = 6 },
            new TreeDef { place = "orange_tree", fruit = "orange", cycleSec = 1200, yield = 3, harvests = 6 },
            new TreeDef { place = "lemon_tree",  fruit = "lemon",  cycleSec = 1500, yield = 3, harvests = 6 },
            // harvests < 0 → never wears out
            new TreeDef { place = "beehive",     fruit = "honey",  cycleSec = 900,  yield = 2, harvests = -1 },
        };

        /// <summary>Fruit trees within this many cells of a beehive give +1 fruit per harvest.</summary>
        public const int BeehiveRadius = 3;

        public static TreeDef Tree(string placeId) => Trees.Find(t => t.place == placeId);

        /// <summary>Chance that a crop harvest is a golden harvest (double yield, album).</summary>
        public const float GoldenChance = 0.05f;

        // ------------------------------------------------------------------ gems (premium currency)
        public const int StartGems = 10;
        /// <summary>Gems to finish something right now: about 1 gem per 6 minutes left.</summary>
        public static int SpeedUpCost(double secondsLeft) => UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt((float)secondsLeft / 360f));
        public static int LevelUpGems(int level) => level % 5 == 0 ? 7 : 2;
        public const int DailyGems = 1, DailyStreakGems = 5, TaskBonusGems = 2, TruckGems = 1, GoldenFishGems = 1;
        public static int AchievementGems(int tier) => tier + 1;

        // ------------------------------------------------------------------ house upgrades
        public const int HouseMaxLevel = 3;
        public const int HouseSlotsPerLevel = 2;
        public static int HouseUpgradeCost(PlaceDef house, int level) => house.cost / 2 * (level + 1);

        // ------------------------------------------------------------------ fishing
        public const int FishPerSpot = 5;
        public const int FishRegenSec = 240;
        public static readonly List<FishDef> Fish = new List<FishDef>
        {
            new FishDef { id = "fish_crucian", chance = 0.65f, needleSpeed = 0.9f, zoneWidth = 0.22f },
            new FishDef { id = "fish_carp",    chance = 0.30f, needleSpeed = 1.3f, zoneWidth = 0.16f },
            new FishDef { id = "fish_golden",  chance = 0.05f, needleSpeed = 1.9f, zoneWidth = 0.11f },
        };
        public static readonly UnityEngine.Vector2Int[] FishingSpots =
        {
            new UnityEngine.Vector2Int(19, 11),
            new UnityEngine.Vector2Int(24, 13),
        };

        // ------------------------------------------------------------------ delivery truck
        public const int DeliveryCrates = 6;
        public const int DeliveryCooldownSec = 1200;

        // ------------------------------------------------------------------ farmers' market (stall listings)
        public const int MarketBaseSlots = 3;
        public const int MarketMaxSlots = 8;
        public static int MarketSlotCost(int currentSlots) { int k = currentSlots - MarketBaseSlots + 1; return 120 * k * k + 80; }
        public const int MarketMaxQty = 10;
        public const float MarketMinPrice = 0.6f, MarketMaxPrice = 2.0f;   // multiples of the fair price
        public const float MarketBaseSec = 30f;     // a fair-priced single item sells in ~30s
        public const int MarketHotCount = 2;        // "hot price" goods per in-game day
        public const float MarketHotBonus = 1.5f;

        // ------------------------------------------------------------------ day / night / weather
        public const float DayLengthSec = 1200f;      // one in-game day = 20 real minutes
        public const int RainChancePercent = 35;
        public const float RainStartHour = 12f, RainEndHour = 15.5f;

        // ------------------------------------------------------------------ achievements
        public static readonly List<AchievementDef> Achievements = new List<AchievementDef>
        {
            new AchievementDef { key = "farmer",   stat = "harvest", name = "Nông dân chăm chỉ", desc = "Thu hoạch {0} ô cây",        tiers = new[] { 50, 250, 1000 }, rewards = new[] { 60, 180, 500 } },
            new AchievementDef { key = "rancher",  stat = "collect", name = "Người chăn nuôi",   desc = "Thu {0} sản phẩm vật nuôi",  tiers = new[] { 20, 100, 500 },  rewards = new[] { 60, 180, 500 } },
            new AchievementDef { key = "trader",   stat = "order",   name = "Thương nhân",       desc = "Giao {0} đơn hàng",          tiers = new[] { 10, 50, 200 },   rewards = new[] { 80, 250, 700 } },
            new AchievementDef { key = "maker",    stat = "produce", name = "Nhà sản xuất",      desc = "Chế biến {0} sản phẩm",      tiers = new[] { 10, 60, 250 },   rewards = new[] { 80, 250, 700 } },
            new AchievementDef { key = "explorer", stat = "zone",    name = "Nhà thám hiểm",     desc = "Mở rộng {0} vùng đất",       tiers = new[] { 1, 3, 6 },       rewards = new[] { 100, 300, 800 } },
            new AchievementDef { key = "fisher",   stat = "fish",    name = "Ngư dân",           desc = "Câu được {0} con cá",        tiers = new[] { 5, 25, 100 },    rewards = new[] { 60, 180, 500 } },
            new AchievementDef { key = "clearer",  stat = "clear",   name = "Người mở đất",      desc = "Dọn {0} gốc cây / tảng đá",  tiers = new[] { 10, 30, 80 },    rewards = new[] { 60, 180, 500 } },
            new AchievementDef { key = "golden",   stat = "golden",  name = "Bàn tay vàng",      desc = "Có {0} vụ mùa vàng",         tiers = new[] { 1, 10, 50 },     rewards = new[] { 80, 250, 700 } },
            new AchievementDef { key = "trucker",  stat = "truck",   name = "Tài xế giỏi",       desc = "Hoàn thành {0} chuyến xe tải", tiers = new[] { 1, 10, 40 },   rewards = new[] { 80, 250, 700 } },
        };

        // ------------------------------------------------------------------ placeholder art
        /// <summary>
        /// Sprites that still need to be generated. Until they exist in the atlases, the game shows the
        /// stand-in on the right so everything stays playable.
        /// </summary>
        public static readonly Dictionary<string, string> SpriteFallbacks = new Dictionary<string, string>
        {
            { "icon_berry", "nature_berry_bush" },
            { "icon_apple", "nature_apple_tree" },
            { "icon_flour", "prop_seed_sack" },
            { "icon_cornmeal", "prop_fertilizer_sack" },
            { "icon_bread", "prop_harvest_basket" },
            { "icon_cornbread", "prop_harvest_crate" },
            { "icon_pumpkin_pie", "icon_pumpkin" },
            { "icon_cheese", "icon_cow_milk" },
            { "icon_butter", "prop_milk_pail" },
            { "icon_goat_cheese", "icon_goat_milk" },
            { "icon_tomato_juice", "icon_tomato" },
            { "icon_strawberry_juice", "icon_strawberry" },
            { "icon_watermelon_juice", "icon_watermelon" },
            { "icon_fish_crucian", "water_fishing_spot" },
            { "icon_fish_carp", "water_fishing_spot" },
            { "icon_fish_golden", "water_fishing_spot" },
            { "building_bakery", "building_workshop" },
            { "building_dairy_factory", "building_feed_shed" },
            { "building_juice_press", "building_greenhouse" },
            { "building_delivery_truck", "building_shipping_bin" },
            { "nature_dead_tree", "nature_tree_stump" },
            { "hud_tasks", "hud_quest" },
            { "hud_trophy", "hud_confirm" },
            { "hud_sun", "hud_clock" },
            { "hud_rain", "hud_clock" },
            { "hud_moon", "hud_clock" },
            { "hud_production", "hud_tools" },
            { "hud_truck", "hud_map" },
            { "hud_fishing", "hud_animals" },
            { "hud_upgrade", "hud_add" },
            { "hud_gift", "hud_coin" },
            { "sugarcane_dry", "rice_dry" },
            // building materials (B3.1) until dedicated icons are added
            { "icon_plank", "nature_fallen_log" },
            { "icon_nail", "hud_tools" },
            { "icon_roof_tile", "wall_stone_corner" },
        };
    }
}
