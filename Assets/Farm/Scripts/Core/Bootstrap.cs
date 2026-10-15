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
                Debug.LogError(L.T("[Farm] FarmSpriteDB chưa được tạo. Trong Unity hãy chạy menu: Farm > Setup Project."));
                return;
            }

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.Init();
            double lastSave = gm.Data.lastSave; // before anything saves: used for the "while you were away" summary

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
            systems.AddComponent<PetSystem>().Init();
            systems.AddComponent<VisitorSystem>().Init();
            systems.AddComponent<CrowSystem>().Init();
            Monetization.Ensure(); // store + rewarded ads (kept across scene reloads)

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
                ui.Message(L.T("Chào mừng tới Farm Bloom!"),
                    L.T("• Dùng joystick (góc trái) để đi lại.\n") +
                    L.T("• Chạm vào ô đất, cây, vật nuôi để làm việc – nông dân sẽ tự đi tới.\n") +
                    L.T("• Kéo ngón tay qua nhiều ô để cuốc / gieo / tưới / thu hoạch hàng loạt.\n") +
                    L.T("• Bày hàng ở Chợ cho khách mua, giao Đơn hàng để kiếm xu và lên cấp!\n") +
                    L.T("• Lên cấp để mở rộng đất – chạm biển gỗ ở rìa rừng."),
                    "farmer_harvest_happy_se", L.T("Bắt đầu!"));
                if (gm.MapWasReset) ui.Toast(L.T("Bản đồ mới: nông trại đã được làm mới theo phiên bản này."));
                gm.SaveNow();
            }
            else
            {
                LocalNotifications.CancelAll();
                if (d.level >= 2) LocalNotifications.RequestPermission();
                CheckDaily(gm, ui);
                if (lastSave > 0) ui.ShowWelcomeBack(GameManager.RealNow - lastSave, lastSave + d.timeOffset);
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
            int gems = d.dailyStreak >= 7 ? Defs.DailyStreakGems : Defs.DailyGems;
            gm.AddCoins(reward);
            gm.AddGems(gems);
            ui.Message(L.T("Quà hằng ngày"), L.F("Ngày đăng nhập liên tiếp thứ <b>{0}</b>!\n\nBạn nhận được <b>+{1} xu, +{2} kim cương</b>.\nĐăng nhập mỗi ngày để nhận quà lớn hơn (ngày thứ 7: +{3} kim cương).", d.dailyStreak, reward, gems, Defs.DailyStreakGems), "hud_gift", L.T("Nhận quà"));
        }

        private double pausedAt;

        /// <summary>Back from the background on mobile: daily gift + summary of what finished meanwhile.</summary>
        private void OnApplicationPause(bool paused)
        {
            GameManager gm = GameManager.I;
            if (gm == null || UIManager.I == null || resetting) return;
            if (paused) { pausedAt = GameManager.RealNow; return; }
            if (pausedAt <= 0) return;
            double away = GameManager.RealNow - pausedAt;
            double since = pausedAt + gm.Data.timeOffset;
            pausedAt = 0;
            CheckDaily(gm, UIManager.I);
            UIManager.I.ShowWelcomeBack(away, since);
        }

        /// <summary>Change the UI language: save, then reload the scene so every label is rebuilt.</summary>
        public static void SwitchLanguage(bool english)
        {
            if (GameManager.I != null) GameManager.I.SaveNow();
            L.Set(english);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
