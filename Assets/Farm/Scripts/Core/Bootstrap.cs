using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;

namespace Farm
{
    /// <summary>Single entry point: the Farm scene only contains this component and a camera.</summary>
    public class Bootstrap : MonoBehaviour
    {
        private static bool resetting;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            EnhancedTouchSupport.Enable();
            GameEvents.Clear();
            Interactable.All.Clear();
            InteractionController.SelectedSeed = null;
            resetting = false;

            if (SpriteDB.Instance == null || SpriteDB.Instance.sprites.Count == 0)
            {
                Debug.LogError("[Farm] FarmSpriteDB chưa được tạo. Trong Unity hãy chạy menu: Farm > Setup Project.");
                return;
            }

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.Init();

            var world = new GameObject("World").AddComponent<WorldMap>();
            world.Build();

            var farmer = new GameObject("Farmer").AddComponent<FarmerController>();
            SaveData d = gm.Data;
            farmer.Init(new Vector3(d.farmerX, d.farmerY, 0));

            Camera cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
            }
            cam.gameObject.AddComponent<CameraController>().Init(cam, d.zoom);

            var systems = new GameObject("Systems");
            systems.AddComponent<PlacementController>().Init();
            systems.AddComponent<InteractionController>().Init();

            var ui = new GameObject("UI").AddComponent<UIManager>();
            ui.Init();

            systems.AddComponent<OrderSystem>().Init();
            systems.AddComponent<QuestSystem>().Init();
            systems.AddComponent<TaskSystem>().Init();
            systems.AddComponent<DayNight>().Init();

            gm.BeforeSave += () =>
            {
                Vector3 fp = FarmerController.I.transform.position;
                d.farmerX = fp.x;
                d.farmerY = fp.y;
                d.zoom = CameraController.I.Zoom;
            };

            if (gm.IsNewGame)
            {
                d.lastDailyDate = DateTime.Now.ToString("yyyy-MM-dd");
                d.dailyStreak = 1;
                ui.Message("Chào mừng tới Nông Trại Vui Vẻ!",
                    "• Dùng joystick (góc trái) để đi lại.\n" +
                    "• Chạm vào ô đất, cây, vật nuôi để làm việc – nông dân sẽ tự đi tới.\n" +
                    "• Kéo ngón tay qua nhiều ô để cuốc / gieo / tưới / thu hoạch hàng loạt.\n" +
                    "• Bán nông sản ở Chợ, giao Đơn hàng để kiếm xu và lên cấp!\n" +
                    "• Lên cấp để mở rộng đất – chạm biển gỗ ở rìa rừng.",
                    "farmer_harvest_happy_se", "Bắt đầu!");
                if (gm.MapWasReset) ui.Toast("Bản đồ mới: nông trại đã được làm mới theo phiên bản này.");
                gm.SaveNow();
            }
            else
            {
                CheckDaily(gm, ui);
                ReportOffline(gm, ui);
            }
        }

        private static void CheckDaily(GameManager gm, UIManager ui)
        {
            SaveData d = gm.Data;
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (d.lastDailyDate == today) return;
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            d.dailyStreak = d.lastDailyDate == yesterday ? Mathf.Min(d.dailyStreak + 1, 7) : 1;
            d.lastDailyDate = today;
            int reward = 30 * d.dailyStreak;
            gm.AddCoins(reward);
            ui.Message("Quà hằng ngày", $"Ngày đăng nhập liên tiếp thứ <b>{d.dailyStreak}</b>!\n\nBạn nhận được <b>+{reward} xu</b>.\nĐăng nhập mỗi ngày để nhận quà lớn hơn (tối đa 7 ngày).", "hud_coin", "Nhận quà");
        }

        private static void ReportOffline(GameManager gm, UIManager ui)
        {
            int ready = 0;
            foreach (PlotView p in WorldMap.I.Plots) if (p.IsReady) ready++;
            int animals = 0;
            foreach (AnimalView a in WorldMap.I.Animals) if (a.IsReady) animals++;
            if (ready + animals > 0)
                ui.Toast($"Trong lúc bạn vắng: {ready} ô cây đã chín, {animals} vật nuôi có sản phẩm!");
        }

        public static void ResetGame()
        {
            resetting = true;
            SaveSystem.Delete();
            if (GameManager.I != null) GameManager.I.Data.initialized = false; // blocks further saves
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static bool Resetting => resetting;
    }
}
