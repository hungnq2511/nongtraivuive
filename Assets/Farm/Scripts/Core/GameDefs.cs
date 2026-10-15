using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    public class CropDef
    {
        public string id, name;
        public int unlock, seedCost, growSec, sell, xp;
        public int yield = 2;
        public string Icon => "icon_" + id;
        public string Stage(string stage) => id + "_" + stage;
    }

    public class AnimalDef
    {
        public string id, name, feed, product, house;
        /// <summary>Atlas column that shows the animal from behind (walking up-left).</summary>
        public string back = "nw";
        public int unlock, cost, produceSec;
        public float scale;
        public string Frame(string f) => id + "_" + f;
    }

    public class ItemDef
    {
        public string id, name, icon;
        public int sell, xp, unlock;
    }

    /// <summary>A rectangle of land that unlocks at a player level for a coin price.</summary>
    public class LandZone
    {
        public int id, x0, y0, x1, y1, unlock, cost;
        public string name;
        public int Cells => (x1 - x0 + 1) * (y1 - y0 + 1);
        public bool Contains(int x, int y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
    }

    public enum PlaceKind { Plot, Decor, House, Sprinkler, Special, Machine, Tree, Delivery }

    public class PlaceDef
    {
        public string id, name, sprite, action, category;
        public int cost, unlock, size = 1, capacity;
        public float scale = 1f;
        public PlaceKind kind;
        public bool shop = true;
        /// <summary>Animal enclosures: walkable yard quad (left, front, right, back), see Defs.Yard.</summary>
        public Vector2[] yard;
        public int Xp => Mathf.Max(1, cost / 10);
    }

    public static partial class Defs
    {
        public const int MaxLevel = 30;
        public const int StartCoins = 120;
        public const int BaseStorage = 50;
        public const int StoragePerUpgrade = 25;

        public static readonly List<CropDef> Crops = new List<CropDef>
        {
            new CropDef { id = "wheat",      name = "Lúa mì",   unlock = 1,  seedCost = 2,  growSec = 45,   sell = 3,  xp = 1 },
            new CropDef { id = "carrot",     name = "Cà rốt",   unlock = 1,  seedCost = 4,  growSec = 120,  sell = 5,  xp = 2 },
            new CropDef { id = "corn",       name = "Ngô",      unlock = 2,  seedCost = 6,  growSec = 300,  sell = 8,  xp = 3 },
            new CropDef { id = "cabbage",    name = "Bắp cải",  unlock = 3,  seedCost = 8,  growSec = 480,  sell = 11, xp = 4 },
            new CropDef { id = "potato",     name = "Khoai tây",unlock = 4,  seedCost = 10, growSec = 600,  sell = 13, xp = 5 },
            new CropDef { id = "tomato",     name = "Cà chua",  unlock = 5,  seedCost = 14, growSec = 900,  sell = 18, xp = 6 },
            new CropDef { id = "rice",       name = "Lúa nước", unlock = 6,  seedCost = 16, growSec = 1200, sell = 21, xp = 7 },
            new CropDef { id = "strawberry", name = "Dâu tây",  unlock = 7,  seedCost = 22, growSec = 1800, sell = 28, xp = 9 },
            new CropDef { id = "sugarcane",  name = "Mía",      unlock = 8,  seedCost = 18, growSec = 1500, sell = 24, xp = 8 },
            new CropDef { id = "pumpkin",    name = "Bí đỏ",    unlock = 9,  seedCost = 30, growSec = 2700, sell = 38, xp = 12 },
            new CropDef { id = "watermelon", name = "Dưa hấu",  unlock = 11, seedCost = 40, growSec = 3600, sell = 52, xp = 15 },
        };

        public static readonly List<AnimalDef> Animals = new List<AnimalDef>
        {
            new AnimalDef { id = "chicken", name = "Gà",  unlock = 1, cost = 40,  feed = "wheat",   product = "egg",       produceSec = 90,  house = "coop",   scale = 0.42f },
            new AnimalDef { id = "cow",     name = "Bò",  unlock = 3, cost = 150, feed = "corn",    product = "milk",      produceSec = 240, house = "dairy",  scale = 0.80f, back = "sw" },
            new AnimalDef { id = "pig",     name = "Lợn", unlock = 4, cost = 200, feed = "carrot",  product = "truffle",   produceSec = 300, house = "stable", scale = 0.60f },
            new AnimalDef { id = "sheep",   name = "Cừu", unlock = 5, cost = 260, feed = "cabbage", product = "wool",      produceSec = 420, house = "sheep_pen", scale = 0.62f },
            new AnimalDef { id = "goat",    name = "Dê",  unlock = 6, cost = 320, feed = "potato",  product = "goat_milk", produceSec = 480, house = "goat_pen", scale = 0.62f },
            new AnimalDef { id = "duck",    name = "Vịt", unlock = 7, cost = 380, feed = "rice",    product = "duck_egg",  produceSec = 360, house = "duck_pen", scale = 0.42f },
        };

        public static readonly List<ItemDef> Products = new List<ItemDef>
        {
            new ItemDef { id = "egg",       name = "Trứng gà",  icon = "icon_chicken_egg", sell = 10, xp = 2, unlock = 1 },
            new ItemDef { id = "milk",      name = "Sữa bò",    icon = "icon_cow_milk",    sell = 25, xp = 4, unlock = 3 },
            new ItemDef { id = "truffle",   name = "Nấm truffle", icon = "icon_truffle",   sell = 32, xp = 5, unlock = 4 },
            new ItemDef { id = "wool",      name = "Len cừu",   icon = "icon_wool",        sell = 40, xp = 6, unlock = 5 },
            new ItemDef { id = "goat_milk", name = "Sữa dê",    icon = "icon_goat_milk",   sell = 46, xp = 7, unlock = 6 },
            new ItemDef { id = "duck_egg",  name = "Trứng vịt", icon = "icon_duck_egg",    sell = 44, xp = 6, unlock = 7 },
        };

        public static readonly List<PlaceDef> Places = new List<PlaceDef>
        {
            // Fixed farm buildings (not sold)
            new PlaceDef { id = "farmhouse", name = "Nhà chính",   sprite = "building_farmhouse",  size = 2, scale = 1.9f, kind = PlaceKind.Special, action = "farmhouse", shop = false },
            new PlaceDef { id = "barn",      name = "Kho hàng",    sprite = "building_barn",       size = 2, scale = 1.9f, kind = PlaceKind.Special, action = "storage",   shop = false },
            new PlaceDef { id = "market",    name = "Chợ",         sprite = "building_market_stall", size = 2, scale = 1.7f, kind = PlaceKind.Special, action = "market", shop = false },
            new PlaceDef { id = "shop",      name = "Cửa hàng",    sprite = "building_farm_shop",  size = 2, scale = 1.8f, kind = PlaceKind.Special, action = "shop",      shop = false },
            new PlaceDef { id = "orders",    name = "Bảng đơn hàng", sprite = "prop_order_board",  size = 1, scale = 0.85f, kind = PlaceKind.Special, action = "orders",  shop = false },

            // Land & equipment
            new PlaceDef { id = "plot",      name = "Ô đất",       sprite = "ground_soil_bare",    size = 1, scale = 1f,   kind = PlaceKind.Plot,      cost = 20,  unlock = 1, category = "land" },
            new PlaceDef { id = "sprinkler", name = "Vòi phun (tự tưới 3x3)", sprite = "prop_sprinkler", size = 1, scale = 0.6f, kind = PlaceKind.Sprinkler, cost = 250, unlock = 3, category = "land" },

            // Production machines
            new PlaceDef { id = "mill",          name = "Cối xay gió",   sprite = "building_windmill",       size = 2, scale = 1.8f, kind = PlaceKind.Machine, cost = 400,  unlock = 4, category = "machine" },
            new PlaceDef { id = "bakery",        name = "Lò bánh",       sprite = "building_bakery",         size = 2, scale = 1.8f, kind = PlaceKind.Machine, cost = 800,  unlock = 6, category = "machine" },
            new PlaceDef { id = "dairy_factory", name = "Xưởng bơ sữa",  sprite = "building_dairy_factory",  size = 2, scale = 1.8f, kind = PlaceKind.Machine, cost = 1200, unlock = 8, category = "machine" },
            new PlaceDef { id = "juice_press",   name = "Máy ép nước",   sprite = "building_juice_press",    size = 2, scale = 1.8f, kind = PlaceKind.Machine, cost = 1500, unlock = 9, category = "machine" },
            new PlaceDef { id = "sugar_mill",      name = "Xưởng đường",  sprite = "building_sugar_mill",      size = 2, scale = 1.8f, kind = PlaceKind.Machine, cost = 1800, unlock = 10, category = "machine" },
            new PlaceDef { id = "popcorn_maker",   name = "Máy bỏng ngô", sprite = "building_popcorn_maker",   size = 2, scale = 1.7f, kind = PlaceKind.Machine, cost = 2200, unlock = 12, category = "machine" },
            new PlaceDef { id = "loom",            name = "Khung dệt",    sprite = "building_loom",            size = 2, scale = 1.7f, kind = PlaceKind.Machine, cost = 2800, unlock = 14, category = "machine" },
            new PlaceDef { id = "ice_cream_maker", name = "Máy làm kem",  sprite = "building_ice_cream_maker", size = 2, scale = 1.7f, kind = PlaceKind.Machine, cost = 3200, unlock = 16, category = "machine" },

            // Fruit trees (harvest many times)
            new PlaceDef { id = "berry_bush", name = "Bụi quả mọng", sprite = "nature_berry_bush", scale = 0.8f, kind = PlaceKind.Tree, cost = 80,  unlock = 3, category = "land" },
            new PlaceDef { id = "apple_tree", name = "Cây táo",      sprite = "nature_apple_tree", scale = 1.2f, kind = PlaceKind.Tree, cost = 150, unlock = 4, category = "land" },
            new PlaceDef { id = "beehive",     name = "Tổ ong",      sprite = "building_beehive",   scale = 1.0f, kind = PlaceKind.Tree, cost = 350, unlock = 4, category = "land" },
            new PlaceDef { id = "cherry_tree", name = "Cây anh đào", sprite = "nature_cherry_tree", scale = 1.2f, kind = PlaceKind.Tree, cost = 220, unlock = 5, category = "land" },
            new PlaceDef { id = "orange_tree", name = "Cây cam",     sprite = "nature_orange_tree", scale = 1.2f, kind = PlaceKind.Tree, cost = 280, unlock = 6, category = "land" },
            new PlaceDef { id = "lemon_tree",  name = "Cây chanh",   sprite = "nature_lemon_tree",  scale = 1.2f, kind = PlaceKind.Tree, cost = 340, unlock = 7, category = "land" },

            // Delivery truck (big multi-crate orders)
            new PlaceDef { id = "delivery", name = "Trạm xe tải", sprite = "building_delivery_truck", size = 2, scale = 1.7f, kind = PlaceKind.Delivery, cost = 300, unlock = 5, category = "machine" },

            // Animal houses
            // Animal enclosures (house + fenced yard, one per animal type). yard = walkable floor quad
            // (left, front, right, back corners) in units of the footprint's half width, y pointing down.
            new PlaceDef { id = "coop",      name = "Chuồng gà",  sprite = "enclosure_chicken", size = 3, scale = 1.1085f, kind = PlaceKind.House, cost = 300,  unlock = 1, capacity = 6, category = "house",
                           yard = Yard(-0.4572f, -0.1051f, -0.0037f, 0.0694f, 0.4359f, -0.119f, -0.0107f, -0.3004f) },
            new PlaceDef { id = "dairy",     name = "Chuồng bò",  sprite = "enclosure_cow",     size = 4, scale = 1.2737f, kind = PlaceKind.House, cost = 600,  unlock = 3, capacity = 6, category = "house",
                           yard = Yard(-0.4678f, 0.0152f, -0.0169f, 0.1414f, 0.5603f, -0.0028f, 0.0913f, -0.1832f) },
            new PlaceDef { id = "stable",    name = "Chuồng lợn", sprite = "enclosure_pig",     size = 4, scale = 1.2865f, kind = PlaceKind.House, cost = 800,  unlock = 4, capacity = 6, category = "house",
                           yard = Yard(-0.3904f, -0.0429f, -0.0806f, 0.0761f, 0.3566f, -0.0611f, -0.0989f, -0.2154f) },
            new PlaceDef { id = "sheep_pen", name = "Chuồng cừu", sprite = "enclosure_sheep",   size = 4, scale = 1.3006f, kind = PlaceKind.House, cost = 1000, unlock = 5, capacity = 6, category = "house",
                           yard = Yard(-0.5846f, -0.0443f, -0.0321f, 0.1399f, 0.502f, -0.0075f, -0.0873f, -0.2285f) },
            new PlaceDef { id = "goat_pen",  name = "Chuồng dê",  sprite = "enclosure_goat",    size = 4, scale = 1.3747f, kind = PlaceKind.House, cost = 1200, unlock = 6, capacity = 6, category = "house",
                           yard = Yard(-0.5371f, -0.077f, -0.0505f, 0.1372f, 0.4362f, 0.0009f, -0.1283f, -0.1938f) },
            new PlaceDef { id = "duck_pen",  name = "Ao vịt",     sprite = "enclosure_duck",    size = 3, scale = 1.1828f, kind = PlaceKind.House, cost = 1400, unlock = 7, capacity = 6, category = "house",
                           yard = Yard(-0.5639f, -0.0233f, -0.0949f, 0.1331f, 0.5081f, -0.0456f, 0.1954f, -0.2391f) },

            // Decorations
            new PlaceDef { id = "flower_patch", name = "Khóm hoa",    sprite = "nature_flower_patch", scale = 0.6f,  kind = PlaceKind.Decor, cost = 30,  unlock = 1, category = "decor" },
            new PlaceDef { id = "sign",         name = "Biển gỗ",     sprite = "decor_wooden_sign",   scale = 0.6f,  kind = PlaceKind.Decor, cost = 40,  unlock = 1, category = "decor" },
            new PlaceDef { id = "fence_a",      name = "Hàng rào gỗ (chéo phải)", sprite = "fence_wood_straight_ne_sw", scale = 0.9f, kind = PlaceKind.Decor, cost = 10, unlock = 1, category = "decor" },
            new PlaceDef { id = "fence_b",      name = "Hàng rào gỗ (chéo trái)", sprite = "fence_wood_straight_nw_se", scale = 0.9f, kind = PlaceKind.Decor, cost = 10, unlock = 1, category = "decor" },
            new PlaceDef { id = "scarecrow",    name = "Bù nhìn",     sprite = "prop_scarecrow",      scale = 0.75f, kind = PlaceKind.Decor, cost = 80,  unlock = 2, category = "decor" },
            new PlaceDef { id = "bench",        name = "Ghế gỗ",      sprite = "decor_bench",         scale = 0.7f,  kind = PlaceKind.Decor, cost = 60,  unlock = 2, category = "decor" },
            new PlaceDef { id = "lamp",         name = "Đèn đường",   sprite = "decor_lamp_post",     scale = 0.75f, kind = PlaceKind.Decor, cost = 80,  unlock = 2, category = "decor" },
            new PlaceDef { id = "hay",          name = "Kiện rơm",    sprite = "nature_hay_bale",     scale = 0.6f,  kind = PlaceKind.Decor, cost = 40,  unlock = 2, category = "decor" },
            new PlaceDef { id = "hedge",        name = "Hàng rào cây",sprite = "fence_hedge_straight",scale = 0.9f,  kind = PlaceKind.Decor, cost = 15,  unlock = 2, category = "decor" },
            new PlaceDef { id = "mushrooms",    name = "Nấm rừng",    sprite = "nature_mushrooms",    scale = 0.55f, kind = PlaceKind.Decor, cost = 50,  unlock = 3, category = "decor" },
            new PlaceDef { id = "compost",      name = "Thùng ủ phân",sprite = "prop_compost_bin",    scale = 0.6f,  kind = PlaceKind.Decor, cost = 70,  unlock = 3, category = "decor" },
            new PlaceDef { id = "stone_wall",   name = "Tường đá",    sprite = "wall_stone_straight_ne_sw", scale = 0.9f, kind = PlaceKind.Decor, cost = 20, unlock = 4, category = "decor" },
            new PlaceDef { id = "oak",          name = "Cây sồi",     sprite = "nature_oak_tree",     scale = 1.2f,  kind = PlaceKind.Decor, cost = 120, unlock = 4, category = "decor" },
            new PlaceDef { id = "pine",         name = "Cây thông",   sprite = "nature_pine_tree",    scale = 1.2f,  kind = PlaceKind.Decor, cost = 120, unlock = 5, category = "decor" },
            new PlaceDef { id = "arch",         name = "Cổng hoa",    sprite = "decor_flower_arch",   scale = 0.9f,  kind = PlaceKind.Decor, cost = 200, unlock = 5, category = "decor" },
            new PlaceDef { id = "gate",         name = "Cổng nông trại", sprite = "building_farm_gate", size = 2, scale = 1.7f, kind = PlaceKind.Decor, cost = 250, unlock = 5, category = "decor" },
            new PlaceDef { id = "well",         name = "Giếng đá",    sprite = "building_stone_well", scale = 1.0f,  kind = PlaceKind.Decor, cost = 300, unlock = 6, category = "decor" },
            new PlaceDef { id = "tool_shed",    name = "Kho dụng cụ", sprite = "building_tool_shed",  size = 2, scale = 1.7f, kind = PlaceKind.Decor, cost = 500, unlock = 6, category = "decor" },
            new PlaceDef { id = "fountain",       name = "Đài phun nước", sprite = "decor_fountain",          scale = 1.1f, kind = PlaceKind.Decor, cost = 450,  unlock = 8,  category = "decor" },
            new PlaceDef { id = "roadside_stand", name = "Sạp rau ven đường", sprite = "building_roadside_stand", size = 2, scale = 1.7f, kind = PlaceKind.Decor, cost = 600, unlock = 9, category = "decor" },
            new PlaceDef { id = "silo",         name = "Tháp chứa",   sprite = "building_silo",       size = 2, scale = 1.6f, kind = PlaceKind.Decor, cost = 600, unlock = 7, category = "decor" },
        };

        public class DebrisDef { public string sprite, name; public int cost; public float scale; public string pose; }

        public static readonly List<DebrisDef> Debris = new List<DebrisDef>
        {
            new DebrisDef { sprite = "nature_tree_stump", name = "Gốc cây",  cost = 15, scale = 0.75f, pose = "axe" },
            new DebrisDef { sprite = "nature_rock_small", name = "Hòn đá",   cost = 20, scale = 0.7f,  pose = "axe" },
            new DebrisDef { sprite = "nature_rock_large", name = "Tảng đá",  cost = 40, scale = 0.9f,  pose = "axe" },
            new DebrisDef { sprite = "nature_bush",       name = "Bụi rậm",  cost = 10, scale = 0.75f, pose = "axe" },
            new DebrisDef { sprite = "nature_fallen_log", name = "Khúc gỗ",  cost = 25, scale = 0.8f,  pose = "axe" },
        };

        // Land expansion (grid build area is 2..27). Zone 0 is the starter farm.
        public static readonly List<LandZone> Zones = new List<LandZone>
        {
            new LandZone { id = 0, name = "Nông trại khởi đầu", x0 = 2,  y0 = 2,  x1 = 15, y1 = 15, unlock = 1,  cost = 0 },
            new LandZone { id = 1, name = "Bờ ao phía Đông",    x0 = 16, y0 = 2,  x1 = 21, y1 = 15, unlock = 3,  cost = 250 },
            new LandZone { id = 2, name = "Đồng cỏ phía Nam",   x0 = 2,  y0 = 16, x1 = 15, y1 = 21, unlock = 5,  cost = 500 },
            new LandZone { id = 3, name = "Rừng thông",         x0 = 16, y0 = 16, x1 = 21, y1 = 21, unlock = 7,  cost = 900 },
            new LandZone { id = 4, name = "Hồ lớn",             x0 = 22, y0 = 2,  x1 = 27, y1 = 15, unlock = 9,  cost = 1400 },
            new LandZone { id = 5, name = "Thung lũng",         x0 = 2,  y0 = 22, x1 = 21, y1 = 27, unlock = 11, cost = 2000 },
            new LandZone { id = 6, name = "Đồi xa",             x0 = 22, y0 = 16, x1 = 27, y1 = 27, unlock = 13, cost = 2800 },
        };

        public static LandZone ZoneAt(int x, int y) => Zones.Find(z => z.Contains(x, y));

        private static Dictionary<string, ItemDef> items;

        public static Dictionary<string, ItemDef> Items
        {
            get
            {
                if (items == null)
                {
                    items = new Dictionary<string, ItemDef>();
                    foreach (CropDef c in Crops)
                        items[c.id] = new ItemDef { id = c.id, name = c.name, icon = c.Icon, sell = c.sell, xp = c.xp, unlock = c.unlock };
                    foreach (ItemDef p in Products) items[p.id] = p;
                    foreach (ItemDef p in ExtraItems) items[p.id] = p;
                    foreach (ItemDef p in Materials) items[p.id] = p;
                }
                return items;
            }
        }

        public static CropDef Crop(string id) => Crops.Find(c => c.id == id);
        public static AnimalDef Animal(string id) => Animals.Find(a => a.id == id);
        public static PlaceDef Place(string id) => Places.Find(p => p.id == id);
        public static DebrisDef DebrisBySprite(string s) => Debris.Find(d => d.sprite == s);
        private static Vector2[] Yard(float lx, float ly, float fx, float fy, float rx, float ry, float bx, float by) =>
            new[] { new Vector2(lx, ly), new Vector2(fx, fy), new Vector2(rx, ry), new Vector2(bx, by) };

        public static ItemDef Item(string id) => id != null && Items.TryGetValue(id, out ItemDef d) ? d : null;

        public static int XpToNext(int level)
        {
            int l = level - 1;
            return 10 + 12 * l + 4 * l * l;
        }

        public static int LevelUpCoins(int newLevel) => 20 * newLevel;

        public static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            int s = Mathf.CeilToInt((float)seconds);
            if (s >= 3600) return L.En ? $"{s / 3600}h {(s % 3600) / 60:00}m" : $"{s / 3600}g {(s % 3600) / 60:00}p";
            if (s >= 60) return $"{s / 60}:{s % 60:00}";
            return s + "s";
        }
    }
}
