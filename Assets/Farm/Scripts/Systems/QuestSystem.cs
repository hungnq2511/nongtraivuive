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
            new Quest("plant", null, 3, 15, "Chạm ô đất đã cuốc, kéo hạt giống vào 3 ô"),
            new Quest("water", null, 3, 15, "Tưới nước cho 3 ô vừa gieo"),
            new Quest("feed", "chicken", 2, 20, "Cho 2 con gà ăn Lúa mì"),
            new Quest("collect", "egg", 2, 20, "Thu 2 quả trứng gà"),
            new Quest("order", null, 1, 30, "Giao 1 đơn ở Bảng đơn hàng"),
            new Quest("sell", null, 1, 20, "Bày hàng ở Chợ và nhận xu khi khách mua"),
            new Quest("clear", null, 1, 25, "Dọn 1 gốc cây / tảng đá (chạm 2 lần)"),
            new Quest("buy_plot", null, 1, 30, "Mua thêm 1 ô đất ở Cửa hàng"),
            new Quest("level", null, 3, 50, "Đạt cấp 3"),
            new Quest("buy_animal", null, 1, 50, "Mua thêm 1 con vật nuôi"),
            new Quest("unlock_zone", null, 1, 100, "Mở rộng 1 vùng đất mới (biển gỗ ở rìa nông trại)"),
            new Quest("buy_place", "sprinkler", 1, 80, "Đặt 1 Vòi phun tự tưới"),
            new Quest("buy_place", "beehive", 1, 80, "Đặt 1 Tổ ong (Cửa hàng › Đất & Cây)"),
            new Quest("harvest_fruit", "honey", 2, 40, "Thu 2 hũ mật ong từ tổ ong"),
            new Quest("speedup", null, 1, 30, "Dùng kim cương \"Xong ngay\" cho 1 việc đang chờ"),
            new Quest("visitor", null, 1, 40, "Giao hàng cho 1 vị khách ghé thăm (khách đứng chờ trên đường)"),
            new Quest("crow", null, 2, 20, "Đuổi 2 con quạ khỏi ruộng (chạm vào quạ)"),
            new Quest("buy_pet", "dog", 1, 60, "Nuôi Chó Vàng (Cửa hàng › Vật nuôi) – nó tự đuổi quạ giúp bạn"),
            new Quest("home_up", null, 1, 120, "Nâng cấp Nhà chính lên cấp 2 (chạm vào Nhà chính, cần cấp 4 và vật liệu)"),
        };

        private SaveData D => GameManager.I.Data;

        public void Init()
        {
            GameEvents.OnAction += OnAction;
            // Catch up quests whose condition is already satisfied (e.g. level)
            if (D.questIndex < Quests.Length && Quests[D.questIndex].key == "level")
                OnAction("level", "", D.level);
            if (D.questIndex < Quests.Length && Quests[D.questIndex].key == "buy_pet" && D.pets.Contains(Quests[D.questIndex].arg))
                OnAction("buy_pet", Quests[D.questIndex].arg, 1);
            if (D.questIndex < Quests.Length && Quests[D.questIndex].key == "home_up" && D.homeLevel >= 2)
                OnAction("home_up", "", 1);
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
                case "visitor":
                {
                    VisitorView v = VisitorSystem.I != null ? VisitorSystem.I.Views.Find(x => x != null && x.Waiting) : null;
                    return v != null ? v.transform.position + Vector3.up * 1.4f : (Vector3?)null;
                }
                case "crow":
                {
                    CrowView c = CrowSystem.I != null ? CrowSystem.I.Crows.Find(x => x != null && x.Perched) : null;
                    return c != null ? c.transform.position + Vector3.up * 0.2f : (Vector3?)null;
                }
                case "buy_pet": return Place("shop");
                case "home_up": return D.level >= 4 ? Place("farmhouse") : (Vector3?)null;
                case "sell": return Place("market");
                case "harvest_fruit": return q.arg == "honey" ? Place("beehive") : null;
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
                UIManager.I.Toast(L.F("Hoàn thành nhiệm vụ! +{0} xu", q.reward));
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
                UIManager.I.SetQuest(L.T("Đã xong hướng dẫn! Hoàn thành đơn hàng và mở rộng nông trại nhé."), "");
                return;
            }
            Quest q = Quests[D.questIndex];
            int p = Mathf.Min(D.questProgress, q.target);
            UIManager.I.SetQuest(L.T(q.text) + $" ({p}/{q.target})", L.F("Thưởng: {0} xu", q.reward));
        }
    }
}
