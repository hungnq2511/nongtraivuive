using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>Hay Day-style order board: deliver goods for coins + XP.</summary>
    public class OrderSystem : MonoBehaviour
    {
        public static OrderSystem I { get; private set; }
        public const int Slots = 5;

        public List<OrderData> Orders => GameManager.I.Data.orders;

        public void Init()
        {
            I = this;
            while (Orders.Count < Slots) Orders.Add(Generate(0));
        }

        public bool IsWaiting(OrderData o) => GameManager.I.Now < o.availableAt;

        public bool CanFulfill(OrderData o)
        {
            if (IsWaiting(o)) return false;
            foreach (ItemStack r in o.req)
                if (GameManager.I.Count(r.id) < r.count) return false;
            return true;
        }

        public bool Fulfill(int index)
        {
            OrderData o = Orders[index];
            if (!CanFulfill(o)) return false;
            GameManager gm = GameManager.I;
            foreach (ItemStack r in o.req) gm.RemoveItem(r.id, r.count);
            gm.AddCoins(Defs.WithBonus(o.coins));
            gm.AddXp(Defs.WithBonus(o.xp));
            Orders[index] = Generate(20);
            GameEvents.Raise("order");
            return true;
        }

        public void Discard(int index)
        {
            Orders[index] = Generate(45);
        }

        private OrderData Generate(float delay)
        {
            GameManager gm = GameManager.I;
            int level = gm.Data.level;
            List<ItemDef> pool = ItemPool.Available(true);
            var o = new OrderData { availableAt = gm.Now + delay };
            int maxLines = Mathf.Clamp(1 + level / 2, 1, 3);
            int lines = Random.Range(1, maxLines + 1);
            int coins = 0, xp = 0;
            for (int i = 0; i < lines && pool.Count > 0; i++)
            {
                ItemDef item = pool[Random.Range(0, pool.Count)];
                pool.Remove(item);
                bool isCrop = Defs.Crop(item.id) != null;
                int qty = isCrop ? Random.Range(2, Mathf.Min(8, 3 + level / 2) + 1) : Random.Range(1, Mathf.Min(4, 1 + level / 4) + 1);
                o.req.Add(new ItemStack(item.id, qty));
                coins += item.sell * qty;
                xp += item.xp * qty;
            }
            o.coins = Mathf.CeilToInt(coins * 1.5f) + 5;
            o.xp = xp + 2;
            return o;
        }
    }
}
