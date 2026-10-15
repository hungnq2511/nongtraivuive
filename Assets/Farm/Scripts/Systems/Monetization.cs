using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if FARM_IAP
using System.Linq;
using UnityEngine.Purchasing;
#endif
#if FARM_ADS
using Unity.Services.LevelPlay;
#endif

namespace Farm
{
    /// <summary>
    /// In-app purchases (Unity IAP 5) + rewarded ads (LevelPlay) + iOS tracking permission (ATT).
    ///
    /// The store / ad code only compiles once the packages are installed: the editor script
    /// FarmMonetizationDefines adds the FARM_IAP / FARM_ADS scripting symbols automatically when Unity has
    /// resolved com.unity.purchasing / com.unity.services.levelplay. Without them the game still runs and
    /// the shop shows "store not available".
    ///
    /// Lives across scene reloads (language switch) so the store connects and the ad SDK initializes once.
    /// </summary>
    public class Monetization : MonoBehaviour
    {
        public static Monetization I { get; private set; }

        /// <summary>Raised when prices load, a purchase finishes or an ad becomes ready (refresh shop UI).</summary>
        public static event Action Changed;

        public static void Ensure()
        {
            if (I != null) return;
            var go = new GameObject("Monetization");
            DontDestroyOnLoad(go);
            I = go.AddComponent<Monetization>();
        }

        private void Start()
        {
#if FARM_IAP
            ConnectStore();
#endif
            StartCoroutine(StartAds());
        }

        private static void Notify() => Changed?.Invoke();

        // ================================================================== IN-APP PURCHASES
        public bool StoreReady { get; private set; }
        public bool Busy { get; private set; }

        public string Price(IapProductDef p)
        {
#if FARM_IAP
            Product prod = store?.GetProductById(p.Id);
            if (prod != null && prod.metadata != null && !string.IsNullOrEmpty(prod.metadata.localizedPriceString))
                return prod.metadata.localizedPriceString;
#endif
            return p.fallbackPrice;
        }

        public static bool Owned(IapProductDef p) => p.oneTime && GameManager.I != null && GameManager.I.Data.iapOwned.Contains(p.Id);

        public void Buy(IapProductDef p)
        {
#if FARM_IAP
            if (!StoreReady || store == null) { UIManager.I.Toast(L.T("Cửa hàng đang kết nối, thử lại sau ít giây.")); return; }
            if (Owned(p)) { UIManager.I.Toast(L.T("Bạn đã mua gói này rồi.")); return; }
            Product prod = store.GetProductById(p.Id);
            if (prod == null || !prod.availableToPurchase) { UIManager.I.Toast(L.T("Gói này hiện chưa bán.")); return; }
            Busy = true;
            Notify();
            store.PurchaseProduct(prod);
#else
            UIManager.I.Toast(L.T("Cửa hàng chưa sẵn sàng trên phiên bản này."));
#endif
        }

        public void Restore()
        {
#if FARM_IAP
            if (!StoreReady || store == null) { UIManager.I.Toast(L.T("Cửa hàng đang kết nối, thử lại sau ít giây.")); return; }
            store.RestoreTransactions((ok, error) =>
            {
                // each restored purchase arrives through OnPurchasePending
                UIManager.I.Toast(ok ? L.T("Đã khôi phục giao dịch.") : L.F("Không khôi phục được: {0}", error));
                Notify();
            });
#else
            UIManager.I.Toast(L.T("Cửa hàng chưa sẵn sàng trên phiên bản này."));
#endif
        }

        /// <summary>Give the player what a product contains. Returns false if it was already granted.</summary>
        private static bool Grant(IapProductDef p, string transactionId)
        {
            SaveData d = GameManager.I.Data;
            if (!string.IsNullOrEmpty(transactionId) && d.iapOrders.Contains(transactionId)) return false;
            if (p.oneTime && d.iapOwned.Contains(p.Id))
            {
                // restore / re-delivery of something we already granted: remember the order only
                if (!string.IsNullOrEmpty(transactionId)) d.iapOrders.Add(transactionId);
                return false;
            }
            GameManager gm = GameManager.I;
            gm.AddGems(p.gems);
            if (p.coins > 0) gm.AddCoins(p.coins);
            if (p.givesDog && !PetSystem.Has("dog") && PetSystem.I != null)
            {
                d.pets.Add("dog");
                PetSystem.I.SpawnFree("dog");
            }
            if (p.oneTime) d.iapOwned.Add(p.Id);
            if (!string.IsNullOrEmpty(transactionId))
            {
                d.iapOrders.Add(transactionId);
                if (d.iapOrders.Count > 100) d.iapOrders.RemoveAt(0);
            }
            d.AddStat("iap", 1);
            return true;
        }

#if FARM_IAP
        private StoreController store;

        private async void ConnectStore()
        {
            store = UnityIAPServices.StoreController();
            // every event is subscribed before Connect(): purchases left pending last session arrive right away
            store.OnPurchasePending += OnPurchasePending;
            store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            store.OnPurchaseFailed += OnPurchaseFailed;
            store.OnPurchaseDeferred += OnPurchaseDeferred;
            store.OnStoreConnected += OnStoreConnected;
            store.OnStoreDisconnected += OnStoreDisconnected;
            store.OnProductsFetched += OnProductsFetched;
            store.OnProductsFetchFailed += OnProductsFetchFailed;
            store.OnPurchasesFetched += OnPurchasesFetched;
            store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            try { await store.Connect(); }
            catch (Exception e) { Debug.LogWarning("[Farm] Store connect failed: " + e.Message); }
        }

        private void OnStoreConnected()
        {
            var defs = new List<ProductDefinition>();
            foreach (IapProductDef p in Defs.IapProducts)
                defs.Add(new ProductDefinition(p.Id, p.oneTime ? ProductType.NonConsumable : ProductType.Consumable));
            store.FetchProducts(defs);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription f)
        {
            StoreReady = false;
            Debug.LogWarning("[Farm] Store disconnected: " + f.Message);
            Notify();
        }

        private void OnProductsFetched(List<Product> products)
        {
            StoreReady = true;
            store.FetchPurchases(); // re-delivers anything that was paid but not confirmed
            Notify();
        }

        private void OnProductsFetchFailed(ProductFetchFailed f)
        {
            Debug.LogWarning("[Farm] Product fetch failed: " + f.FailureReason);
            // products that did load are still usable
            StoreReady = store.GetProducts().Count > 0;
            Notify();
        }

        private void OnPurchasesFetched(Orders orders) => Notify();
        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription f) => Debug.LogWarning("[Farm] Purchases fetch failed: " + f.Message);

        private void OnPurchasePending(PendingOrder order)
        {
            if (GameManager.I == null || !GameManager.I.Data.initialized)
            {
                // the farm is not loaded yet: leave it pending, it is delivered again next time
                return;
            }
            string tx = order.Info?.TransactionID;
            foreach (CartItem item in order.CartOrdered.Items())
            {
                IapProductDef p = Defs.IapProduct(item.Product.definition.id);
                if (p == null) continue;
                if (Grant(p, tx))
                {
                    UIManager.I.Toast(L.F("Cảm ơn bạn! Đã nhận {0}.", p.name));
                    Fx.Golden(FarmerController.I != null ? FarmerController.I.transform.position : Vector3.zero);
                    Haptics.Success();
                    GameEvents.Raise("iap", p.key);
                }
            }
            // save BEFORE confirming: an unconfirmed purchase is re-delivered, a confirmed but unsaved one is lost
            if (!GameManager.I.SaveNow())
            {
                Debug.LogWarning("[Farm] Could not save after purchase – it will be delivered again.");
                Busy = false;
                Notify();
                return;
            }
            store.ConfirmPurchase(order);
            Busy = false;
            Notify();
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed) Debug.LogWarning($"[Farm] Purchase confirm failed: {failed.FailureReason} {failed.Details}");
            Busy = false;
            Notify();
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            Busy = false;
            Debug.LogWarning($"[Farm] Purchase failed: {failed.FailureReason} {failed.Details}");
            if (failed.FailureReason != PurchaseFailureReason.UserCancelled)
                UIManager.I.Toast(L.T("Giao dịch không thành công. Bạn chưa bị trừ tiền."));
            Notify();
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            Busy = false;
            UIManager.I.Toast(L.T("Giao dịch đang chờ duyệt (ví dụ phụ huynh xác nhận). Quà sẽ đến khi được duyệt."));
            Notify();
        }
#endif

        // ================================================================== REWARDED ADS
        private static string AppKey =>
            Application.platform == RuntimePlatform.Android ? Defs.LevelPlayAppKeyAndroid : Defs.LevelPlayAppKeyIOS;
        private static string RewardedUnit =>
            Application.platform == RuntimePlatform.Android ? Defs.RewardedAdUnitAndroid : Defs.RewardedAdUnitIOS;

        /// <summary>True when the build has ads and the keys are filled in (otherwise ad buttons stay hidden).</summary>
        public static bool AdsEnabled
        {
            get
            {
#if FARM_ADS
                return !string.IsNullOrEmpty(AppKey) && !string.IsNullOrEmpty(RewardedUnit);
#else
                return false;
#endif
            }
        }

        /// <summary>Last ad SDK state, shown under the ad button in the gem shop (helps diagnose "no ads").</summary>
        public static string AdStatus { get; private set; } = "";
        private static void SetAdStatus(string s) { AdStatus = s; Debug.Log("[Farm] Ads: " + s); Notify(); }
        private bool adsInitialized;

        /// <summary>Opens LevelPlay's Integration Test Suite (hidden: tap the ad status line 5 times).</summary>
        public void OpenTestSuite()
        {
#if FARM_ADS
            if (adsInitialized) LevelPlay.LaunchTestSuite();
            else UIManager.I.Toast("Ad SDK not initialized: " + AdStatus);
#endif
        }

        public bool AdReady
        {
            get
            {
#if FARM_ADS
                return rewarded != null && rewarded.IsAdReady();
#else
                return false;
#endif
            }
        }

        /// <summary>How many more ads of this kind the player may watch today.</summary>
        public static int AdsLeftToday(string kind)
        {
            SaveData d = GameManager.I.Data;
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (d.adDay != today) { d.adDay = today; d.adCounts.Clear(); }
            StatEntry e = d.adCounts.Find(x => x.key == kind);
            int limit = kind == "gems" ? Defs.AdGemsPerDay : Defs.AdSpeedUpPerDay;
            return Mathf.Max(0, limit - (e != null ? e.value : 0));
        }

        private static void CountAd(string kind)
        {
            AdsLeftToday(kind); // resets the counters on a new day
            SaveData d = GameManager.I.Data;
            StatEntry e = d.adCounts.Find(x => x.key == kind);
            if (e == null) d.adCounts.Add(new StatEntry { key = kind, value = 1 });
            else e.value++;
        }

        /// <summary>Can a "watch an ad" button for this kind be offered right now?</summary>
        public bool CanOfferAd(string kind) => AdsEnabled && AdReady && AdsLeftToday(kind) > 0;

        private Action pendingReward;
        private string pendingKind;
        private bool rewardEarned;

        /// <summary>Show a rewarded ad; onReward runs only if the player watched it to the end.</summary>
        public void ShowRewarded(string kind, Action onReward)
        {
            if (!CanOfferAd(kind))
            {
                UIManager.I.Toast(AdsLeftToday(kind) <= 0 ? L.T("Hôm nay bạn đã xem đủ quảng cáo, mai quay lại nhé!") : L.T("Quảng cáo chưa sẵn sàng, thử lại sau."));
                return;
            }
#if FARM_ADS
            pendingReward = onReward;
            pendingKind = kind;
            rewardEarned = false;
            rewarded.ShowAd();
#endif
        }

        private void GiveAdReward()
        {
            if (!rewardEarned || pendingReward == null) return;
            Action a = pendingReward;
            pendingReward = null;
            rewardEarned = false;
            CountAd(pendingKind);
            a();
            GameManager.I.SaveNow();
            Notify();
        }

        // ---------------- iOS App Tracking Transparency (Plugins/iOS/FarmNative.mm)
#if UNITY_IOS && !UNITY_EDITOR
        private delegate void AttCallback(int status);
        [DllImport("__Internal")] private static extern int FarmATTStatus();
        [DllImport("__Internal")] private static extern void FarmATTRequest(AttCallback callback);
        private static bool attDone;

        [AOT.MonoPInvokeCallback(typeof(AttCallback))]
        private static void OnAtt(int status) { attDone = true; }
#endif

        /// <summary>Ask for tracking permission once (iOS 14.5+). Only used when ads are enabled.</summary>
        private IEnumerator RequestTracking()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (FarmATTStatus() != 0) yield break; // already answered (0 = not determined)
            // wait until the farm is on screen and no welcome popup is open, so the system dialog is not lost
            float t = 0f;
            while (t < 8f && (UIManager.I == null || UIManager.I.ModalOpen)) { t += Time.unscaledDeltaTime; yield return null; }
            attDone = false;
            FarmATTRequest(OnAtt);
            float wait = 0f;
            while (!attDone && wait < 120f) { wait += Time.unscaledDeltaTime; yield return null; }
#else
            yield break;
#endif
        }

        private IEnumerator StartAds()
        {
            if (!AdsEnabled) { SetAdStatus("off (no keys / FARM_ADS)"); yield break; }
            SetAdStatus("waiting");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return RequestTracking(); // must finish before the ad SDK starts
#if FARM_ADS
            SetAdStatus("initializing");
            LevelPlay.SetMetaData("is_test_suite", "enable");
            LevelPlay.OnInitSuccess += OnAdsInit;
            LevelPlay.OnInitFailed += OnAdsInitFailed;
            LevelPlay.Init(AppKey);
#endif
        }

#if FARM_ADS
        private LevelPlayRewardedAd rewarded;

        private void OnAdsInit(LevelPlayConfiguration config)
        {
            adsInitialized = true;
            SetAdStatus("loading");
            rewarded = new LevelPlayRewardedAd(RewardedUnit);
            rewarded.OnAdLoaded += OnAdLoaded;
            rewarded.OnAdLoadFailed += OnAdLoadFailed;
            rewarded.OnAdDisplayed += OnAdDisplayed;
            rewarded.OnAdDisplayFailed += OnAdDisplayFailed;
            rewarded.OnAdRewarded += OnAdRewarded;
            rewarded.OnAdClosed += OnAdClosed;
            rewarded.LoadAd();
        }

        private void OnAdsInitFailed(LevelPlayInitError error)
        {
            SetAdStatus("init failed: " + error.ErrorCode + " " + error.ErrorMessage);
        }

        private void LoadAd()
        {
            if (rewarded != null) rewarded.LoadAd();
        }

        private void OnAdLoaded(LevelPlayAdInfo info) => SetAdStatus("ready (" + info.AdNetwork + ")");

        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            SetAdStatus("no fill: " + error.ErrorCode + " " + error.ErrorMessage + " (retry 30s)");
            CancelInvoke(nameof(LoadAd));
            Invoke(nameof(LoadAd), 30f);
            Notify();
        }

        private void OnAdDisplayed(LevelPlayAdInfo info) { }

        private void OnAdDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning("[Farm] Rewarded ad failed to show: " + error.ErrorMessage);
            pendingReward = null;
            UIManager.I.Toast(L.T("Quảng cáo chưa sẵn sàng, thử lại sau."));
            LoadAd();
            Notify();
        }

        private void OnAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            rewardEarned = true;
            // some networks report the reward after the ad closes, others before: give it in either case
            GiveAdReward();
        }

        private void OnAdClosed(LevelPlayAdInfo info)
        {
            GiveAdReward();
            pendingReward = null;
            LoadAd(); // keep one ready for the next button
            Notify();
        }

        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= OnAdsInit;
            LevelPlay.OnInitFailed -= OnAdsInitFailed;
            if (rewarded != null)
            {
                rewarded.OnAdLoaded -= OnAdLoaded;
                rewarded.OnAdLoadFailed -= OnAdLoadFailed;
                rewarded.OnAdDisplayed -= OnAdDisplayed;
                rewarded.OnAdDisplayFailed -= OnAdDisplayFailed;
                rewarded.OnAdRewarded -= OnAdRewarded;
                rewarded.OnAdClosed -= OnAdClosed;
            }
        }
#endif
    }
}
