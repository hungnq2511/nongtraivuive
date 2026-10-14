using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>Lifetime stats, 3 daily tasks (reset each calendar day) and tiered achievements.</summary>
    public class TaskSystem : MonoBehaviour
    {
        public static TaskSystem I { get; private set; }
        private SaveData D => GameManager.I.Data;
        private float checkTimer;

        // event key -> stat key
        private static readonly Dictionary<string, string> StatOf = new Dictionary<string, string>
        {
            { "harvest", "harvest" }, { "harvest_fruit", "harvest" }, { "collect", "collect" }, { "feed", "feed" },
            { "order", "order" }, { "produce", "produce" }, { "unlock_zone", "zone" }, { "fish", "fish" },
            { "clear", "clear" }, { "truck", "truck" }, { "sell", "sell" },
        };

        private class Template
        {
            public string stat, text;
            public int min, max;
            public Func<bool> available;
        }

        private static List<Template> Templates()
        {
            WorldMap w = WorldMap.I;
            SaveData d = GameManager.I.Data;
            return new List<Template>
            {
                new Template { stat = "harvest", text = "Thu hoạch {0} lần",          min = 8,  max = 20, available = () => true },
                new Template { stat = "sell",    text = "Bán {0} món hàng ở Chợ",       min = 10, max = 30, available = () => true },
                new Template { stat = "order",   text = "Giao {0} đơn hàng",            min = 2,  max = 4,  available = () => true },
                new Template { stat = "feed",    text = "Cho vật nuôi ăn {0} lần",      min = 4,  max = 10, available = () => d.animals.Count > 0 },
                new Template { stat = "collect", text = "Thu {0} sản phẩm vật nuôi",    min = 4,  max = 10, available = () => d.animals.Count > 0 },
                new Template { stat = "produce", text = "Chế biến {0} sản phẩm",        min = 2,  max = 5,  available = () => w.Places.Exists(p => p.Def.kind == PlaceKind.Machine) },
                new Template { stat = "clear",   text = "Dọn {0} gốc cây / tảng đá",    min = 2,  max = 3,  available = () => w.DebrisList.Count >= 3 },
                new Template { stat = "fish",    text = "Câu được {0} con cá",          min = 2,  max = 4,  available = () => w.AnyFishingUnlocked },
                new Template { stat = "truck",   text = "Hoàn thành {0} chuyến xe tải", min = 1,  max = 1,  available = () => w.Places.Exists(p => p.Def.kind == PlaceKind.Delivery) },
            };
        }

        public void Init()
        {
            I = this;
            GameEvents.OnAction += OnAction;
            CheckDay();
            RefreshBadge();
        }

        private void OnDestroy() => GameEvents.OnAction -= OnAction;

        private void Update()
        {
            checkTimer -= Time.unscaledDeltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 30f;
            CheckDay();
        }

        private void OnAction(string key, string arg, int amount)
        {
            if (!StatOf.TryGetValue(key, out string stat)) return;
            if (key == "unlock_zone") amount = 1;
            D.AddStat(stat, amount);
            foreach (TaskData t in D.tasks)
                if (t.stat == stat && !t.claimed) t.progress = Mathf.Min(t.target, t.progress + amount);
            RefreshBadge();
        }

        private void CheckDay()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (D.taskDate == today && D.tasks.Count > 0) return;
            D.taskDate = today;
            D.taskBonusClaimed = false;
            D.tasks.Clear();
            List<Template> pool = Templates().FindAll(t => t.available());
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                Template t = pool[UnityEngine.Random.Range(0, pool.Count)];
                pool.Remove(t);
                int target = UnityEngine.Random.Range(t.min, t.max + 1);
                D.tasks.Add(new TaskData
                {
                    stat = t.stat, text = string.Format(t.text, target), target = target,
                    coins = 30 + 6 * target + 4 * D.level, xp = 8 + 2 * D.level,
                });
            }
            RefreshBadge();
        }

        // ------------------------------------------------------------------ claims
        public bool Claim(TaskData t)
        {
            if (t.claimed || t.progress < t.target) return false;
            t.claimed = true;
            GameManager.I.AddCoins(t.coins);
            GameManager.I.AddXp(t.xp);
            RefreshBadge();
            return true;
        }

        public bool AllTasksClaimed => D.tasks.Count > 0 && D.tasks.TrueForAll(t => t.claimed);
        public int BonusCoins => 100 + 15 * D.level;

        public bool ClaimBonus()
        {
            if (!AllTasksClaimed || D.taskBonusClaimed) return false;
            D.taskBonusClaimed = true;
            GameManager.I.AddCoins(BonusCoins);
            GameManager.I.AddXp(20);
            RefreshBadge();
            return true;
        }

        public int ClaimedTier(AchievementDef a)
        {
            StatEntry e = D.achClaimed.Find(x => x.key == a.key);
            return e != null ? e.value : 0;
        }

        public bool CanClaim(AchievementDef a)
        {
            int tier = ClaimedTier(a);
            return tier < a.tiers.Length && D.Stat(a.stat) >= a.tiers[tier];
        }

        public bool ClaimAchievement(AchievementDef a)
        {
            if (!CanClaim(a)) return false;
            int tier = ClaimedTier(a);
            StatEntry e = D.achClaimed.Find(x => x.key == a.key);
            if (e == null) D.achClaimed.Add(new StatEntry { key = a.key, value = 1 });
            else e.value++;
            GameManager.I.AddCoins(a.rewards[tier]);
            GameManager.I.AddXp(10 * (tier + 1));
            RefreshBadge();
            return true;
        }

        public bool AnythingToClaim()
        {
            foreach (TaskData t in D.tasks) if (!t.claimed && t.progress >= t.target) return true;
            if (AllTasksClaimed && !D.taskBonusClaimed) return true;
            foreach (AchievementDef a in Defs.Achievements) if (CanClaim(a)) return true;
            return false;
        }

        public void RefreshBadge()
        {
            if (UIManager.I != null) UIManager.I.SetTasksBadge(AnythingToClaim());
        }
    }
}
