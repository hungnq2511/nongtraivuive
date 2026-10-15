using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Farm.EditorTools
{
    /// <summary>
    /// Menu Farm > Build iOS (Xcode project): configures iOS player settings for TestFlight and exports
    /// an Xcode project to Builds/iOS. Then open Builds/iOS/Unity-iPhone.xcodeproj in Xcode → Archive → Upload.
    /// </summary>
    public static class FarmBuildIOS
    {
        public const string BundleId = "com.nutrica.nongtraivuive";
        private const string OutputDir = "Builds/iOS";

        [MenuItem("Farm/Build iOS (Xcode project for TestFlight)", priority = 40)]
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                EditorUtility.DisplayDialog("Thiếu iOS Build Support",
                    "Unity chưa cài module iOS Build Support.\nMở Unity Hub > Installs > 6000.6.5f1 > Add modules > iOS Build Support, rồi chạy lại.", "OK");
                return;
            }

            Configure();

            string scene = "Assets/Farm/Scenes/Farm.unity";
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = OutputDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Farm] iOS build {PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber}) exported to {Path.GetFullPath(OutputDir)}");
                EditorUtility.RevealInFinder(Path.Combine(OutputDir, "Unity-iPhone.xcodeproj"));
            }
            else Debug.LogError("[Farm] iOS build failed: " + report.summary.result);
        }

        private static void Configure()
        {
            var ios = NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(ios, BundleId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "0.1.0";

            // Every upload to TestFlight needs a higher build number.
            int.TryParse(PlayerSettings.iOS.buildNumber, out int build);
            PlayerSettings.iOS.buildNumber = (build + 1).ToString();

            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(ios, 1); // ARM64

            // Landscape only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
        }

#if UNITY_IOS
        /// <summary>Answers App Store Connect's export-compliance question automatically (no custom encryption).</summary>
        [PostProcessBuild(100)]
        public static void OnPostProcess(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            // Shown in the iOS tracking-permission dialog (Monetization.cs asks before starting rewarded ads)
            plist.root.SetString("NSUserTrackingUsageDescription",
                "Cho phép để quảng cáo phù hợp với bạn hơn. Bạn vẫn chơi đầy đủ nếu từ chối. / Allow this so ads are more relevant to you. The game works fully if you decline.");
            plist.WriteToFile(plistPath);

            // Plugins/iOS/FarmNative.mm uses local notifications and App Tracking Transparency
            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "UserNotifications.framework", false);
            proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "AppTrackingTransparency.framework", true);
            proj.WriteToFile(projPath);
        }
#endif
    }
}
