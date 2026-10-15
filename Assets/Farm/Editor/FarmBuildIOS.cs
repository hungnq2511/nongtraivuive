using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor.iOS.Xcode;

namespace Farm.EditorTools
{
    /// <summary>
    /// Menu Farm > Build iOS (Xcode project): configures iOS player settings for TestFlight and exports
    /// an Xcode project to Builds/iOS. Then open Builds/iOS/Unity-iPhone.xcworkspace in Xcode → Archive → Upload.
    /// </summary>
    public static class FarmBuildIOS
    {
        public const string BundleId = "com.nutrica.nongtraivuive";
        private const string OutputDir = "Builds/iOS";

        // App icons live in Assets/Farm/AppIcon and are copied into the Xcode asset catalog after every iOS build.
        // App Store Connect rejects uploads without the 1024x1024 "ios-marketing" icon (error 91111).
        private static readonly string[,] AppIconFiles =
        {
            { "AppIcon-120.png", "Icon-iPhone-120.png" },
            { "AppIcon-180.png", "Icon-iPhone-180.png" },
            { "AppIcon-76.png", "Icon-iPad-76.png" },
            { "AppIcon-152.png", "Icon-iPad-152.png" },
            { "AppIcon-167.png", "Icon-iPad-167.png" },
            { "AppIcon-1024.png", "Icon-AppStore-1024.png" },
        };

        private const string AppIconContentsJson = @"{
  ""images"" : [
    { ""filename"" : ""Icon-iPhone-120.png"", ""idiom"" : ""iphone"", ""scale"" : ""2x"", ""size"" : ""60x60"" },
    { ""filename"" : ""Icon-iPhone-180.png"", ""idiom"" : ""iphone"", ""scale"" : ""3x"", ""size"" : ""60x60"" },
    { ""filename"" : ""Icon-iPad-76.png"", ""idiom"" : ""ipad"", ""scale"" : ""1x"", ""size"" : ""76x76"" },
    { ""filename"" : ""Icon-iPad-152.png"", ""idiom"" : ""ipad"", ""scale"" : ""2x"", ""size"" : ""76x76"" },
    { ""filename"" : ""Icon-iPad-167.png"", ""idiom"" : ""ipad"", ""scale"" : ""2x"", ""size"" : ""83.5x83.5"" },
    { ""filename"" : ""Icon-AppStore-1024.png"", ""idiom"" : ""ios-marketing"", ""scale"" : ""1x"", ""size"" : ""1024x1024"" }
  ],
  ""info"" : { ""author"" : ""xcode"", ""version"" : 1 },
  ""properties"" : { ""pre-rendered"" : false }
}";

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
                EditorUtility.RevealInFinder(Path.Combine(OutputDir, "Unity-iPhone.xcworkspace"));
            }
            else Debug.LogError("[Farm] iOS build failed: " + report.summary.result);
        }

        private static void Configure()
        {
            var ios = NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(ios, BundleId);
            PlayerSettings.productName = "Farm Bloom"; // home-screen name, matches the App Store name
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "0.1.0";

            // Every upload to TestFlight needs a higher build number.
            int.TryParse(PlayerSettings.iOS.buildNumber, out int build);
            PlayerSettings.iOS.buildNumber = (build + 1).ToString();

            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly; // iPad not supported
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

        /// <summary>Answers App Store Connect's export-compliance question automatically (no custom encryption).</summary>
        [PostProcessBuild(100)]
        public static void OnPostProcess(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            InstallAppIcons(path);

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

        private static void InstallAppIcons(string buildPath)
        {
            string sourceDir = Path.Combine(Application.dataPath, "Farm/AppIcon");
            string iconSetDir = Path.Combine(buildPath, "Unity-iPhone/Images.xcassets/AppIcon.appiconset");
            Directory.CreateDirectory(iconSetDir);

            for (int i = 0; i < AppIconFiles.GetLength(0); i++)
            {
                string source = Path.Combine(sourceDir, AppIconFiles[i, 0]);
                if (!File.Exists(source))
                    throw new BuildFailedException("Missing iOS app icon: " + source);
                File.Copy(source, Path.Combine(iconSetDir, AppIconFiles[i, 1]), true);
            }

            File.WriteAllText(Path.Combine(iconSetDir, "Contents.json"), AppIconContentsJson);
            Debug.Log("[Farm] Installed iOS app icons, including the 1024x1024 App Store icon.");
        }
    }
}
