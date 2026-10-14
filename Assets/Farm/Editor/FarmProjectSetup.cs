using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farm.EditorTools
{
    /// <summary>
    /// One-click project setup:
    ///  1. Slices the re-packed FarmGameArt atlases using Data/sprite_layout.json (exact rects + pivots).
    ///  2. Builds Resources/FarmSpriteDB.asset (name → sprite lookup used at runtime).
    ///  3. Configures landscape-only mobile player settings.
    ///  4. Creates Assets/Farm/Scenes/Farm.unity and puts it first in Build Settings.
    /// Runs automatically the first time the project is opened (when the DB is missing),
    /// and can be re-run any time from the menu Farm > Setup Project.
    /// </summary>
    [InitializeOnLoad]
    public static class FarmProjectSetup
    {
        private const string LayoutPath = "Assets/FarmGameArt/Data/sprite_layout.json";
        private const string DbPath = "Assets/Farm/Resources/FarmSpriteDB.asset";
        private const string ScenePath = "Assets/Farm/Scenes/Farm.unity";
        private const string AutoKey = "Farm.AutoSetupDone.v1";

        [Serializable] private class Layout { public int pixelsPerUnit; public AtlasEntry[] atlases; }
        [Serializable] private class AtlasEntry { public string path; public string category; public int width, height; public SpriteEntry[] sprites; }
        [Serializable] private class SpriteEntry { public string name; public int x, y, w, h; public float pivotX, pivotY; }

        static FarmProjectSetup()
        {
            // Scripts were recompiled while in Play mode: runtime state is lost, so stop cleanly.
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[Farm] Scripts changed during Play mode – stopping play. Press Play again.");
                EditorApplication.isPlaying = false;
            }
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.delayCall += AutoRun;
        }

        private static void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += AutoRun;
        }

        private static void AutoRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoRun; // try again once the editor is idle
                return;
            }
            var existing = AssetDatabase.LoadAssetAtPath<SpriteDB>(DbPath);
            bool layoutChanged = existing != null && existing.layoutHash != LayoutHash();
            if (existing != null && !layoutChanged && File.Exists(ScenePath)) return;
            if (SessionState.GetBool(AutoKey + LayoutHash(), false)) return;
            SessionState.SetBool(AutoKey + LayoutHash(), true);
            if (layoutChanged && File.Exists(ScenePath))
            {
                Debug.Log("[Farm] Sprite layout changed – re-slicing atlases...");
                ImportSprites();
                BuildSpriteDB();
                return;
            }
            Debug.Log("[Farm] First-time setup: importing sprites and creating the Farm scene...");
            try
            {
                RunAll(false);
            }
            catch (Exception e)
            {
                Debug.LogError("[Farm] Auto setup failed: " + e + "\nRun menu Farm > Setup Project after fixing the error.");
            }
        }

        private static string LayoutHash()
        {
            if (!File.Exists(LayoutPath)) return "";
            using (var md5 = System.Security.Cryptography.MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(LayoutPath))).Replace("-", "");
        }

        [MenuItem("Farm/Setup Project (import sprites + scene)", priority = 0)]
        public static void RunAllMenu() => RunAll(true);

        [MenuItem("Farm/Open Farm Scene", priority = 1)]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Farm/Delete Save Data", priority = 20)]
        public static void DeleteSave()
        {
            SaveSystem.Delete();
            Debug.Log("[Farm] Save deleted: " + SaveSystem.FilePath);
        }

        public static void RunAll(bool interactive)
        {
            ImportSprites();
            BuildSpriteDB();
            ConfigurePlayer();
            CreateScene(interactive);
            Debug.Log("[Farm] Setup complete. Open Assets/Farm/Scenes/Farm.unity and press Play (Game view: landscape, e.g. 1920x1080).");
        }

        // ------------------------------------------------------------------ 1. sprites
        public static void ImportSprites()
        {
            if (!File.Exists(LayoutPath)) throw new FileNotFoundException("Missing " + LayoutPath);
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            int total = 0;
            {
                foreach (AtlasEntry atlas in layout.atlases)
                {
                    var importer = AssetImporter.GetAtPath(atlas.path) as TextureImporter;
                    if (importer == null)
                    {
                        Debug.LogError("[Farm] Texture not found: " + atlas.path);
                        continue;
                    }
                    bool ui = atlas.category == "icon";
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePixelsPerUnit = layout.pixelsPerUnit;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = !ui;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();

                    var factories = new SpriteDataProviderFactories();
                    factories.Init();
                    ISpriteEditorDataProvider dp = factories.GetSpriteEditorDataProviderFromObject(importer);
                    dp.InitSpriteEditorDataProvider();

                    Dictionary<string, GUID> existing = dp.GetSpriteRects()
                        .Where(r => !string.IsNullOrEmpty(r.name))
                        .GroupBy(r => r.name)
                        .ToDictionary(g => g.Key, g => g.First().spriteID);

                    var rects = new List<SpriteRect>();
                    foreach (SpriteEntry s in atlas.sprites)
                    {
                        rects.Add(new SpriteRect
                        {
                            name = s.name,
                            spriteID = existing.TryGetValue(s.name, out GUID id) ? id : GUID.Generate(),
                            rect = new Rect(s.x, s.y, s.w, s.h),
                            alignment = SpriteAlignment.Custom,
                            pivot = new Vector2(s.pivotX, s.pivotY),
                            border = Vector4.zero,
                        });
                    }
                    dp.SetSpriteRects(rects.ToArray());
                    var nameIds = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
                    if (nameIds != null)
                        nameIds.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                    dp.Apply();
                    importer.SaveAndReimport();
                    total += rects.Count;
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Farm] Sliced {layout.atlases.Length} atlases / {total} sprites.");
        }

        // ------------------------------------------------------------------ 2. sprite DB
        public static void BuildSpriteDB()
        {
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            var db = AssetDatabase.LoadAssetAtPath<SpriteDB>(DbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<SpriteDB>();
                AssetDatabase.CreateAsset(db, DbPath);
            }
            db.sprites.Clear();
            foreach (AtlasEntry atlas in layout.atlases)
            {
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(atlas.path).OfType<Sprite>().ToArray();
                if (sprites.Length != atlas.sprites.Length)
                    Debug.LogWarning($"[Farm] {atlas.path}: expected {atlas.sprites.Length} sprites, found {sprites.Length}.");
                db.sprites.AddRange(sprites);
            }
            db.layoutHash = LayoutHash();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Farm] FarmSpriteDB contains {db.sprites.Count} sprites.");
        }

        // ------------------------------------------------------------------ 3. player settings
        public static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Nông Trại Vui Vẻ";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.useAnimatedAutorotation = true;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.nutrica.nongtraivuive");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.nutrica.nongtraivuive");
        }

        // ------------------------------------------------------------------ 4. scene
        public static void CreateScene(bool interactive)
        {
            if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.62f, 0.78f);
            camGo.transform.position = new Vector3(0, -12f, -10f);

            new GameObject("Bootstrap").AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
