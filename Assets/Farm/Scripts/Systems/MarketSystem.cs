using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>
    /// Farmers' market (roadside stall, Hay Day style). The player puts goods on a stall slot with a quantity and
    /// a price; NPC customers buy them after a while (cheaper → faster, dearer → slower). Coins are collected
    /// from the sold slot. Two "hot" goods per in-game day pay +50%. Selling only happens here – the barn
    /// (Kho hàng) is for storing, upgrading and discarding.
    /// </summary>
    public static class Market
    {
        private static SaveData D => GameManager.I.Data;

        // ------------------------------------------------------------------ slots
        public static List<MarketSlotData> Slots
        {
            get
            {
                if (D.marketSlots < Defs.MarketBaseSlots) D.marketSlots = Defs.MarketBaseSlots;
                while (D.market.Count < D.marketSlots) D.market.Add(new MarketSlotData());
                return D.market;
            }
        }

        // JsonUtility saves null strings as "", so an empty slot comes back with item == "".
        public static bool IsEmpty(MarketSlotData s) => string.IsNullOrEmpty(s.item) || Defs.Item(s.item) == null;
        public static bool IsSold(MarketSlotData s) => !IsEmpty(s) && GameManager.I.Now >= s.soldAt;
        public static bool AnySold => Slots.Exists(IsSold);
        public static int ListedCount => Slots.FindAll(s => !IsEmpty(s)).Count;

        public static bool CanBuySlot => D.marketSlots < Defs.MarketMaxSlots;
        public static int NextSlotCost => Defs.MarketSlotCost(D.marketSlots);

        public static string BuySlot()
        {
            if (!CanBuySlot) return L.T("Quầy đã đủ chỗ tối đa.");
            if (!GameManager.I.TrySpend(NextSlotCost)) return L.T("Không đủ xu.");
            D.marketSlots++;
            D.market.Add(new MarketSlotData());
            return null;
        }

        // ------------------------------------------------------------------ hot goods of the day
        public static long DayIndex => (long)Math.Floor((GameManager.I.Now + D.dayOffset) / Defs.DayLengthSec);

        public static List<string> Hot
        {
            get
            {
                long day = DayIndex;
                if (D.marketHotDay != day || D.marketHot == null)
                {
                    D.marketHotDay = day;
                    D.marketHot = new List<string>();
                    List<ItemDef> pool = ItemPool.Available(true);
                    pool.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
                    var rng = new System.Random((int)(day * 7919 % int.MaxValue));
                    for (int i = 0; i < Defs.MarketHotCount && pool.Count > 0; i++)
                    {
                        int k = rng.Next(pool.Count);
                        D.marketHot.Add(pool[k].id);
                        pool.RemoveAt(k);
                    }
                }
                return D.marketHot;
            }
        }

        public static bool IsHot(string id) => Hot.Contains(id);

        /// <summary>Seconds until the hot list changes (next in-game day).</summary>
        public static double HotLeft => (DayIndex + 1) * (double)Defs.DayLengthSec - (GameManager.I.Now + D.dayOffset);

        // ------------------------------------------------------------------ pricing
        public static int FairPrice(string id, int qty)
        {
            ItemDef def = Defs.Item(id);
            float unit = def.sell * (IsHot(id) ? Defs.MarketHotBonus : 1f);
            return Mathf.Max(1, Mathf.RoundToInt(unit * qty));
        }

        public static int MinPrice(string id, int qty) => Mathf.Max(1, Mathf.FloorToInt(FairPrice(id, qty) * Defs.MarketMinPrice));
        public static int MaxPrice(string id, int qty) => Mathf.Max(1, Mathf.CeilToInt(FairPrice(id, qty) * Defs.MarketMaxPrice));

        /// <summary>Expected time for a customer to buy this listing (the actual time varies ±20%).</summary>
        public static float ExpectedSec(string id, int qty, int price)
        {
            float ratio = price / (float)FairPrice(id, qty);
            return Defs.MarketBaseSec * (0.6f + 0.4f * Mathf.Sqrt(qty)) * ratio * ratio * ratio;
        }

        public static string Speed(string id, int qty, int price)
        {
            float r = price / (float)FairPrice(id, qty);
            if (r <= 0.85f) return L.T("Bán rất nhanh");
            if (r <= 1.1f) return L.T("Bán nhanh");
            if (r <= 1.5f) return L.T("Bán hơi chậm");
            return L.T("Bán chậm");
        }

        // ------------------------------------------------------------------ actions
        public static string Put(int slot, string id, int qty, int price)
        {
            MarketSlotData s = Slots[slot];
            if (!IsEmpty(s)) return L.T("Chỗ này đang bày hàng.");
            if (qty <= 0 || GameManager.I.Count(id) < qty) return L.T("Không đủ hàng trong kho.");
            if (!GameManager.I.RemoveItem(id, qty)) return L.T("Không đủ hàng trong kho.");
            price = Mathf.Clamp(price, MinPrice(id, qty), MaxPrice(id, qty));
            s.item = id;
            s.qty = qty;
            s.price = price;
            float sec = ExpectedSec(id, qty, price) * UnityEngine.Random.Range(0.8f, 1.2f);
            s.soldAt = GameManager.I.Now + Mathf.Max(5f, sec);
            GameManager.I.NotifyChanged();
            return null;
        }

        /// <summary>Take an unsold listing back into the barn.</summary>
        public static string Cancel(int slot)
        {
            MarketSlotData s = Slots[slot];
            if (IsEmpty(s) || IsSold(s)) return null;
            if (!GameManager.I.AddItem(s.item, s.qty)) return L.T("Kho không đủ chỗ để lấy hàng về.");
            Clear(s);
            return null;
        }

        /// <summary>Collect coins of one sold slot. Returns coins collected.</summary>
        public static int Collect(int slot)
        {
            MarketSlotData s = Slots[slot];
            if (!IsSold(s)) return 0;
            int coins = s.price;
            string item = s.item;
            int qty = s.qty;
            Clear(s);
            GameManager.I.AddCoins(coins);
            GameManager.I.AddXp(1 + qty / 3);
            GameEvents.Raise("sell", item, qty);
            return coins;
        }

        public static int CollectAll()
        {
            int total = 0;
            for (int i = 0; i < Slots.Count; i++) total += Collect(i);
            return total;
        }

        private static void Clear(MarketSlotData s)
        {
            s.item = null;
            s.qty = 0;
            s.price = 0;
            s.soldAt = 0;
            GameManager.I.NotifyChanged();
        }

        // ------------------------------------------------------------------ item categories (barn tabs)
        public static string Category(string id)
        {
            if (Defs.Crop(id) != null || Defs.Trees.Exists(t => t.fruit == id)) return "crop";
            if (Defs.Animals.Exists(a => a.product == id) || Defs.Fish.Exists(f => f.id == id)) return "animal";
            if (Defs.Recipe(id) != null) return "processed";
            return "other";
        }
    }
}
