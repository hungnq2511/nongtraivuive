using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Farm
{
    /// <summary>
    /// Light haptic feedback. iOS uses the Taptic Engine through Plugins/iOS/FarmNative.mm;
    /// Android only vibrates for big moments (level-up); the Editor does nothing.
    /// </summary>
    public static class Haptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void FarmHapticImpact(int style);
        [DllImport("__Internal")] private static extern void FarmHapticSuccess();
#endif
        private static float lastLight;

        private static bool On => GameManager.I == null || GameManager.I.Data == null || GameManager.I.Data.haptics;

        /// <summary>Tiny tick (harvest, collect). Throttled so drag-harvesting doesn't buzz constantly.</summary>
        public static void Light()
        {
            if (!On || Time.unscaledTime - lastLight < 0.07f) return;
            lastLight = Time.unscaledTime;
#if UNITY_IOS && !UNITY_EDITOR
            FarmHapticImpact(0);
#endif
        }

        public static void Medium()
        {
            if (!On) return;
#if UNITY_IOS && !UNITY_EDITOR
            FarmHapticImpact(1);
#endif
        }

        /// <summary>Level-up, golden harvest, big rewards.</summary>
        public static void Success()
        {
            if (!On) return;
#if UNITY_IOS && !UNITY_EDITOR
            FarmHapticSuccess();
#elif UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }

    /// <summary>
    /// Local notifications (iOS) scheduled when the app goes to the background and cancelled when it
    /// comes back: crops ripe, animals ready, machines done, market sold out, truck back, daily gift.
    /// </summary>
    public static class LocalNotifications
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void FarmRequestNotifications();
        [DllImport("__Internal")] private static extern void FarmScheduleNotification(string id, string title, string body, double seconds);
        [DllImport("__Internal")] private static extern void FarmCancelAllNotifications();
#endif

        public static void RequestPermission()
        {
            SaveData d = GameManager.I.Data;
            if (d.notifAsked) return;
            d.notifAsked = true;
#if UNITY_IOS && !UNITY_EDITOR
            FarmRequestNotifications();
#endif
        }

        public static void CancelAll()
        {
#if UNITY_IOS && !UNITY_EDITOR
            FarmCancelAllNotifications();
#endif
        }

        private struct Note
        {
            public string id, title, body;
            public double sec;
        }

        /// <summary>What would be scheduled right now (also handy for debugging in the Editor).</summary>
        public static List<string> Preview()
        {
            var r = new List<string>();
            foreach (Note n in Build()) r.Add($"{Defs.FormatTime(n.sec)}: {n.title} – {n.body}");
            return r;
        }

        public static void ScheduleAll()
        {
            if (GameManager.I == null || GameManager.I.Data == null || !GameManager.I.Data.initialized) return;
            CancelAll();
            if (!GameManager.I.Data.notifications) return;
#if UNITY_IOS && !UNITY_EDITOR
            foreach (Note n in Build()) FarmScheduleNotification(n.id, n.title, n.body, n.sec);
#endif
        }

        private static List<Note> Build()
        {
            var list = new List<Note>();
            GameManager gm = GameManager.I;
            SaveData d = gm.Data;
            double now = gm.Now;
            WorldMap w = WorldMap.I;
            void Add(string id, double at, string title, string body)
            {
                double sec = at - now;
                if (sec < 60) return;
                list.Add(new Note { id = id, title = title, body = body, sec = sec });
            }

            if (w != null)
            {
                // whole field ripe
                double crops = 0;
                int growing = 0;
                foreach (PlotView p in w.Plots)
                    if (p.D.state == PlotState.Growing && !p.IsReady && p.Crop != null)
                    {
                        crops = Math.Max(crops, p.D.wateredAt + p.Crop.growSec);
                        growing++;
                    }
                if (growing > 0) Add("crops", crops, L.T("Cả ruộng đã chín!"), L.F("{0} ô cây đang chờ bạn thu hoạch.", growing));

                // animals
                double animals = 0;
                foreach (AnimalView a in w.Animals)
                    if (a.D.state == AnimalState.Producing && !a.IsReady)
                        animals = Math.Max(animals, a.D.fedAt + a.Def.produceSec);
                if (animals > 0) Add("animals", animals, L.T("Vật nuôi đã có sản phẩm"), L.T("Trứng, sữa, len… đang chờ bạn ở chuồng."));

                // first machine to finish its whole queue
                double machine = double.MaxValue;
                string machineName = null;
                foreach (PlaceView p in w.Places)
                {
                    if (p.Def.kind != PlaceKind.Machine) continue;
                    MachineData m = Production.Get(p.D.uid);
                    if (m.jobs.Count == 0) continue;
                    double end = m.jobs[m.jobs.Count - 1].end;
                    if (end < machine) { machine = end; machineName = p.Def.name; }
                }
                if (machineName != null) Add("machine", machine, L.F("{0} đã làm xong", machineName), L.T("Sản phẩm chế biến đã sẵn sàng, hàng đợi đang trống."));
            }

            // market sold out
            double market = 0;
            int listed = 0;
            foreach (MarketSlotData s in d.market)
                if (!Market.IsEmpty(s) && s.soldAt > now) { market = Math.Max(market, s.soldAt); listed++; }
            if (listed > 0) Add("market", market, L.T("Chợ đã bán hết hàng!"), L.T("Khách đã mua sạch quầy, về nhận xu thôi."));

            // truck back
            if (d.delivery != null && d.delivery.crates.Count == 0 && d.delivery.readyAt > now)
                Add("truck", d.delivery.readyAt, L.T("Xe tải đã quay về"), L.T("Có chuyến hàng mới với phần thưởng lớn."));

            // tomorrow's daily gift at 8:00 local time
            DateTime tomorrow8 = DateTime.Now.Date.AddDays(1).AddHours(8);
            Add("daily", now + (tomorrow8 - DateTime.Now).TotalSeconds, L.T("Quà hằng ngày đã sẵn sàng"),
                d.dailyStreak > 0 ? L.F("Giữ chuỗi {0} ngày để nhận quà lớn hơn!", d.dailyStreak + 1) : L.T("Vào nhận xu và kim cương miễn phí."));

            // gentle reminder after a long absence
            Add("miss", now + 26 * 3600, L.T("Nông trại nhớ bạn!"), L.T("Cây đã chín, vật nuôi đang đói. Ghé thăm một chút nhé!"));
            return list;
        }
    }
}
