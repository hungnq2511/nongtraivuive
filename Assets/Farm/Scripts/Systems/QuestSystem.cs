using System;
using UnityEngine;

namespace Farm
{
    /// <summary>Linear tutorial quest chain shown on the HUD card.</summary>
    public class QuestSystem : MonoBehaviour
    {
        private class Quest
        {
            public string key, arg, text;
            public int target, reward;
            public Quest(string key, string arg, int target, int reward, string text)
            {
                this.key = key; this.arg = arg; this.target = target; this.reward = reward; this.text = text;
            }
        }

        private static readonly Quest[] Quests =
        {
            new Quest("harvest", "wheat", 2, 20, "Thu hoạch 2 ô Lúa mì đã chín (chạm vào cây)"),
            new Quest("till", null, 2, 10, "Cuốc 2 ô đất trống"),
            new Quest("plant", null, 3, 15, "Gieo hạt vào 3 ô đất (nút Hạt giống)"),
            new Quest("water", null, 3, 15, "Tưới nước cho 3 ô vừa gieo"),
            new Quest("feed", "chicken", 2, 20, "Cho 2 con gà ăn Lúa mì"),
            new Quest("collect", "egg", 2, 20, "Thu 2 quả trứng gà"),
            new Quest("order", null, 1, 30, "Giao 1 đơn ở Bảng đơn hàng"),
            new Quest("sell", null, 1, 20, "Bán nông sản ở Chợ"),
            new Quest("clear", null, 1, 25, "Dọn 1 gốc cây / tảng đá (chạm 2 lần)"),
            new Quest("buy_plot", null, 1, 30, "Mua thêm 1 ô đất ở Cửa hàng"),
            new Quest("level", null, 3, 50, "Đạt cấp 3"),
            new Quest("buy_animal", null, 1, 50, "Mua thêm 1 con vật nuôi"),
            new Quest("unlock_zone", null, 1, 100, "Mở rộng 1 vùng đất mới (biển gỗ ở rìa nông trại)"),
            new Quest("buy_place", "sprinkler", 1, 80, "Đặt 1 Vòi phun tự tưới"),
        };

        private SaveData D => GameManager.I.Data;

        public void Init()
        {
            GameEvents.OnAction += OnAction;
            // Catch up quests whose condition is already satisfied (e.g. level)
            if (D.questIndex < Quests.Length && Quests[D.questIndex].key == "level")
                OnAction("level", "", D.level);
            Refresh();
        }

        private void OnDestroy() => GameEvents.OnAction -= OnAction;

        // ------------------------------------------------------------------ tutorial pointer
        private SpriteRenderer pointer;
        private float findTimer, bob;
        private Vector3? target;

        private void Update()
        {
            if (pointer == null)
            {
                var go = new GameObject("QuestPointer");
                pointer = go.AddComponent<SpriteRenderer>();
                pointer.sprite = ProcSprites.Arrow;
                pointer.color = new Color(1f, 0.85f, 0.2f);
                pointer.sortingOrder = IsoGrid.OverlayOrder + 9000;
                go.transform.localScale = Vector3.one * 0.9f;
            }
            findTimer -= Time.deltaTime;
            if (findTimer <= 0f)
            {
                findTimer = 0.5f;
                target = FindTarget();
            }
            bool show = target.HasValue && !UIManager.I.ModalOpen && !PlacementController.I.Active;
            if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
            if (!show) return;
            bob += Time.deltaTime * 5f;
            pointer.transform.position = target.Value + new Vector3(0, 1.5f + Mathf.Abs(Mathf.Sin(bob)) * 0.35f, 0);
        }

        private Vector3? FindTarget()
        {
            if (D.questIndex >= Quests.Length || WorldMap.I == null) return null;
            Quest q = Quests[D.questIndex];
            WorldMap w = WorldMap.I;
            Vector3 farmer = FarmerController.I.transform.position;
            Vector3? Place(string id)
            {
                PlaceView p = w.Places.Find(x => x.Def.id == id);
                return p != null ? p.transform.position + Vector3.up * (p.Def.size > 1 ? 1.6f : 0.6f) : (Vector3?)null;
            }
            Vector3? Plot(System.Predicate<PlotView> f)
            {
                PlotView best = null;
                float bd = float.MaxValue;
                foreach (PlotView p in w.Plots)
                {
                    if (!f(p)) continue;
                    float d = Vector2.Distance(p.transform.position, farmer);
                    if (d < bd) { bd = d; best = p; }
                }
                return best != null ? best.transform.position : (Vector3?)null;
            }
            switch (q.key)
            {
                case "harvest": return Plot(p => p.IsReady && (q.arg == null || p.D.crop == q.arg));
                case "till": return Plot(p => p.D.state == PlotState.Untilled);
                case "plant": return Plot(p => p.D.state == PlotState.Tilled);
                case "water": return Plot(p => p.D.state == PlotState.Planted);
                case "feed":
                {
                    AnimalView a = w.Animals.Find(x => x.D.state == AnimalState.Hungry && (q.arg == null || x.Def.id == q.arg));
                    return a != null && GameManager.I.Count(a.Def.feed) > 0 ? a.transform.position : (Vector3?)null;
                }
                case "collect":
                {
                    AnimalView a = w.Animals.Find(x => x.IsReady && (q.arg == null || x.Def.product == q.arg));
                    return a != null ? a.transform.position : (Vector3?)null;
                }
                case "order": return Place("orders");
                case "sell": return Place("market");
                case "clear":
                {
                    DebrisView best = null;
                    float bd = float.MaxValue;
                    foreach (DebrisView d in w.DebrisList)
                    {
                        float dist = Vector2.Distance(d.transform.position, farmer);
                        if (dist < bd) { bd = dist; best = d; }
                    }
                    return best != null ? best.transform.position + Vector3.up * 0.4f : (Vector3?)null;
                }
                case "unlock_zone":
                {
                    ZoneSignView s = w.ReadySign;
                    return s != null ? s.transform.position + Vector3.up * 0.6f : (Vector3?)null;
                }
                case "buy_plot":
                case "buy_animal":
                case "buy_place":
                    return Place("shop");
                default: return null;
            }
        }

        private void OnAction(string key, string arg, int amount)
        {
            if (D.questIndex >= Quests.Length) return;
            Quest q = Quests[D.questIndex];
            if (q.key != key || (q.arg != null && q.arg != arg)) return;

            if (key == "level") D.questProgress = Math.Max(D.questProgress, amount);
            else D.questProgress += amount;

            if (D.questProgress >= q.target)
            {
                GameManager.I.AddCoins(q.reward);
                UIManager.I.Toast($"Hoàn thành nhiệm vụ! +{q.reward} xu");
                D.questIndex++;
                D.questProgress = 0;
                if (D.questIndex < Quests.Length && Quests[D.questIndex].key == "level")
                    D.questProgress = D.level;
                if (D.questIndex < Quests.Length && Quests[D.questIndex].key == "level" && D.level >= Quests[D.questIndex].target)
                {
                    Refresh();
                    OnAction("level", "", D.level);
                    return;
                }
            }
            Refresh();
        }

        private void Refresh()
        {
            if (D.questIndex >= Quests.Length)
            {
                UIManager.I.SetQuest("Đã xong hướng dẫn! Hoàn thành đơn hàng và mở rộng nông trại nhé.", "");
                return;
            }
            Quest q = Quests[D.questIndex];
            int p = Mathf.Min(D.questProgress, q.target);
            UIManager.I.SetQuest($"{q.text} ({p}/{q.target})", $"Thưởng: {q.reward} xu");
        }
    }
}
