using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Farm.EditorTools
{
    /// <summary>
    /// Turns the store / ad code in Monetization.cs on only when its packages are really installed:
    /// adds FARM_IAP when Unity IAP (com.unity.purchasing) is loaded and FARM_ADS when LevelPlay
    /// (com.unity.services.levelplay) is loaded, and removes them again if a package is removed.
    /// This keeps the project compiling while Unity is still downloading the packages.
    /// </summary>
    [InitializeOnLoad]
    public static class FarmMonetizationDefines
    {
        private static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.iOS, NamedBuildTarget.Android, NamedBuildTarget.Standalone };

        static FarmMonetizationDefines()
        {
            EditorApplication.delayCall += Apply;
        }

        private static bool HasType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Any(a =>
            {
                try { return a.GetType(fullName, false) != null; }
                catch { return false; }
            });

        [MenuItem("Farm/Refresh store & ads symbols", priority = 60)]
        public static void Apply()
        {
            bool iap = HasType("UnityEngine.Purchasing.UnityIAPServices");
            bool ads = HasType("Unity.Services.LevelPlay.LevelPlay");
            bool changedAny = false;
            foreach (NamedBuildTarget t in Targets)
            {
                string current = PlayerSettings.GetScriptingDefineSymbols(t);
                var set = new List<string>(current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                bool changed = Toggle(set, "FARM_IAP", iap) | Toggle(set, "FARM_ADS", ads);
                if (changed) { PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", set)); changedAny = true; }
            }
            if (changedAny) UnityEngine.Debug.Log($"[Farm] Store package: {(iap ? "found" : "not installed")} · Ads package: {(ads ? "found" : "not installed")}");
        }

        private static bool Toggle(List<string> set, string symbol, bool on)
        {
            bool has = set.Contains(symbol);
            if (on && !has) { set.Add(symbol); return true; }
            if (!on && has) { set.Remove(symbol); return true; }
            return false;
        }
    }
}
