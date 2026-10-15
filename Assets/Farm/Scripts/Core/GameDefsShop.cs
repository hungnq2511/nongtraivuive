using System.Collections.Generic;

namespace Farm
{
    public class IapProductDef
    {
        public string key, name, icon;
        public int gems, coins;
        public bool oneTime;           // NonConsumable (restorable); otherwise Consumable
        public bool givesDog;
        /// <summary>Price shown until the store returns the real localized price.</summary>
        public string fallbackPrice;
        public string Id => BundlePrefix + key;
    }

    /// <summary>Monetization config: in-app purchase catalog and rewarded-ad settings.</summary>
    public static partial class Defs
    {
        /// <summary>Product IDs in App Store Connect / Google Play must be exactly BundlePrefix + key.</summary>
        public const string BundlePrefix = "com.nutrica.nongtraivuive.";

        public static readonly List<IapProductDef> IapProducts = new List<IapProductDef>
        {
            new IapProductDef { key = "gems_80",      name = "Túi kim cương",   icon = "hud_gem",  gems = 80,   fallbackPrice = "$0.99" },
            new IapProductDef { key = "gems_450",     name = "Hộp kim cương",   icon = "hud_gem",  gems = 450,  fallbackPrice = "$4.99" },
            new IapProductDef { key = "gems_1000",    name = "Rương kim cương", icon = "hud_gem",  gems = 1000, fallbackPrice = "$9.99" },
            new IapProductDef { key = "starter_pack", name = "Gói khởi đầu",    icon = "hud_gift", gems = 250, coins = 3000, givesDog = true, oneTime = true, fallbackPrice = "$1.99" },
        };

        public static IapProductDef IapProduct(string id) => IapProducts.Find(p => p.Id == id || p.key == id);

        // ---------------- rewarded ads (LevelPlay)
        // Fill these in from the LevelPlay dashboard (Apps → App Key, Ad units → Rewarded).
        // While they are empty the game simply hides every "watch an ad" button.
        public const string LevelPlayAppKeyIOS = "";
        public const string LevelPlayAppKeyAndroid = "";
        public const string RewardedAdUnitIOS = "";
        public const string RewardedAdUnitAndroid = "";

        public const int AdGems = 2;                    // gems for watching an ad in the gem shop
        public const int AdGemsPerDay = 5;
        public const int AdSpeedUpPerDay = 8;
        public const double AdSpeedUpMaxSeconds = 30 * 60; // ads can finish waits up to 30 minutes
    }
}
