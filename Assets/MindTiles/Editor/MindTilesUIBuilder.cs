using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MindTiles.Audio;
using MindTiles.Boot;
using MindTiles.UI;

namespace MindTiles.EditorTools
{
    // Builds the redesigned UI (new design sheet) from code so it can be
    // regenerated consistently: sprite import settings, TMP fonts, the shared
    // prefabs (backdrop, logo, settings, pause, confirm) and the scene wiring.
    // Run from the menu: Tools > MindTiles > ...
    public static class MindTilesUIBuilder
    {
        // ---- Paths -------------------------------------------------------------
        private const string Root = "Assets/MindTiles/UI";
        private const string SpriteDir = Root + "/Sprites";
        private const string PrefabDir = Root + "/Prefabs";
        private const string FontDir = "Assets/MindTiles/Fonts";
        private const string FontSrc = FontDir + "/Font Sources/static";
        private const string KitIcons = "Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/";
        private const string MenuScene = "Assets/Scenes/MenuSpace.unity";
        private const string GameScene = "Assets/Scenes/SampleScene.unity";
        private const string LoadingScene = "Assets/Scenes/Loading.unity";

        // ---- Palette (design sheet) ------------------------------------------
        private static readonly Color Night = Hex("1A1338");
        private static readonly Color Ink = Hex("2A1A5E");
        private static readonly Color Violet = Hex("5B3FB8");
        private static readonly Color Lavender = Hex("B29AFF");
        private static readonly Color Pink = Hex("FF94C6");
        private static readonly Color Peach = Hex("FFC79A");
        private static readonly Color TextDark = Hex("2A1752");
        private static readonly Color TextPlum = Hex("3A1640");
        private static readonly Color CardColor = Hex("231A50");
        private static readonly Color CardBorder = Hex("3B2E78");
        private static readonly Color RowColor = Hex("33296A");
        private static readonly Color Dim = new Color(0.07f, 0.05f, 0.16f, 0.8f);
        private static readonly Color Subtle = Hex("D9CCFF");
        private static readonly Color ResetFill = Hex("3A1E52");
        private static readonly Color ResetBorder = Hex("B85A8E");
        private static readonly Color ResetText = Hex("FF9ECB");
        private static readonly Color PinkLip = Hex("C24E85");
        private static readonly Color GlassFill = new Color(0.12f, 0.09f, 0.27f, 0.85f);
        private static readonly Color GlassBorder = Hex("4A3C8A");

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        // ---- Cached assets -----------------------------------------------------
        private static TMP_FontAsset fredoka, nunito;
        private static Material titleMaterial;

        private static Sprite S(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/" + file);

        private static Sprite KitIcon(string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(KitIcons + name + "-256.png").OfType<Sprite>().FirstOrDefault();
        }

        // ========================================================================
        // 1) Sprites + fonts
        // ========================================================================

        [MenuItem("Tools/MindTiles/1 Import UI Sprites")]
        public static void ImportSprites()
        {
            var borders = new Dictionary<string, Vector4>
            {
                { "ui_rounded.png", new Vector4(40, 40, 40, 40) },
                { "ui_rounded_small.png", new Vector4(16, 16, 16, 16) },
                { "ui_outline.png", new Vector4(40, 40, 40, 40) },
                { "ui_pill.png", new Vector4(32, 31, 32, 31) },
                { "btn_pink.png", new Vector4(44, 44, 44, 44) },
                { "btn_lavender.png", new Vector4(44, 44, 44, 44) },
            };

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.spritePixelsPerUnit = 100;
                string file = System.IO.Path.GetFileName(path);
                imp.spriteBorder = borders.TryGetValue(file, out Vector4 b) ? b : Vector4.zero;
                imp.SaveAndReimport();
            }
            Debug.Log("[UIBuilder] Sprites imported.");
        }

        [MenuItem("Tools/MindTiles/2 Create Fonts")]
        public static void CreateFonts()
        {
            fredoka = CreateFont("Fredoka-Bold", FontSrc + "/Fredoka-Bold.ttf");
            nunito = CreateFont("Nunito-ExtraBold", FontSrc + "/Nunito-ExtraBold.ttf");

            string matPath = FontDir + "/Fredoka-Bold SDF Title.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(fredoka.material) { name = "Fredoka-Bold SDF Title" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.CopyPropertiesFromMaterial(fredoka.material);
            mat.SetFloat("_FaceDilate", 0.15f);
            mat.SetFloat("_OutlineWidth", 0.22f);
            mat.SetColor("_OutlineColor", TextDark);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0.08f, 0.04f, 0.2f, 0.85f));
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", -0.7f);
            mat.SetFloat("_UnderlayDilate", 0.25f);
            mat.SetFloat("_UnderlaySoftness", 0.1f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[UIBuilder] Fonts ready.");
        }

        private static TMP_FontAsset CreateFont(string name, string ttfPath)
        {
            string path = FontDir + "/" + name + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            fa.name = name + " SDF";
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = name + " SDF Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = name + " SDF Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·…", out _);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static void LoadFonts()
        {
            if (fredoka == null) fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "/Fredoka-Bold SDF.asset");
            if (nunito == null) nunito = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "/Nunito-ExtraBold SDF.asset");
            if (titleMaterial == null) titleMaterial = AssetDatabase.LoadAssetAtPath<Material>(FontDir + "/Fredoka-Bold SDF Title.mat");
        }

        // ========================================================================
        // 2) Shared prefabs
        // ========================================================================

        [MenuItem("Tools/MindTiles/3 Build Panel Prefabs")]
        public static void BuildPanelPrefabs()
        {
            LoadFonts();
            if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder(Root, "Prefabs");

            BuildLogoPrefab();
            var confirm = BuildConfirmPrefab();
            BuildSettingsPrefab(confirm);
            BuildPausePrefab(confirm);
            BuildInfoPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[UIBuilder] Panel prefabs built.");
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildLogoPrefab()
        {
            var root = NewRect("Logo", null);
            root.sizeDelta = new Vector2(860f, 400f);
            var img = AddImage(root, S("logo_mindtiles.png"), Color.white, false);
            img.preserveAspect = true;
            SavePrefab(root.gameObject, "Logo");
        }

        // Full-screen dim + rounded card with the three little tiles on top.
        private static RectTransform Overlay(string name, out RectTransform card, Vector2 cardSize)
        {
            var root = NewRect(name, null);
            Stretch(root);
            var dim = AddImage(root, null, Dim, false);
            dim.raycastTarget = true;   // swallows taps behind the panel

            card = NewRect("Card", root);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = cardSize;
            AddImage(card, S("ui_rounded.png"), CardColor, true).pixelsPerUnitMultiplier = 0.6f;
            var border = NewRect("Border", card);
            Stretch(border);
            AddImage(border, S("ui_outline.png"), CardBorder, true).pixelsPerUnitMultiplier = 0.6f;

            var tiles = NewRect("MiniTiles", card);
            TopCenter(tiles, 108f, new Vector2(200f, 60f));
            Color[] colors = { Pink, Peach, Lavender };
            float[] rot = { -10f, 0f, 9f };
            float[] dy = { -4f, 4f, -2f };
            for (int i = 0; i < 3; i++)
            {
                var t = NewRect("Tile" + i, tiles);
                t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
                t.anchoredPosition = new Vector2((i - 1) * 64f, dy[i]);
                t.sizeDelta = new Vector2(40f, 40f);
                t.localRotation = Quaternion.Euler(0f, 0f, rot[i]);
                AddImage(t, S("ui_rounded_small.png"), colors[i], true).pixelsPerUnitMultiplier = 1.6f;
            }
            return root;
        }

        private static GameObject BuildConfirmPrefab()
        {
            var root = Overlay("ConfirmDialog", out RectTransform card, new Vector2(890f, 720f));
            var title = AddTitle(card, "Title", "Are you sure?", 228f, 100f);
            var msg = AddText(NewTop(card, "Message", 380f, new Vector2(780f, 170f)), "Reset your score and progress?\nThis can't be undone.",
                nunito, 46f, Subtle, TextAlignmentOptions.Center);
            msg.textWrappingMode = TextWrappingModes.Normal;

            var yes = BigButton(card, "YesButton", 580f, new Vector2(370f, 150f), S("btn_pink.png"), PinkLip, "Yes", TextPlum, null, 64f);
            ((RectTransform)yes.transform.parent).anchoredPosition += new Vector2(-200f, 0f);
            var no = BigButton(card, "NoButton", 580f, new Vector2(370f, 150f), S("btn_lavender.png"), Violet, "No", TextDark, null, 64f);
            ((RectTransform)no.transform.parent).anchoredPosition += new Vector2(200f, 0f);

            var dialog = root.gameObject.AddComponent<ConfirmDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("messageText").objectReferenceValue = msg;
            so.FindProperty("yesButton").objectReferenceValue = yes;
            so.FindProperty("noButton").objectReferenceValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return SavePrefab(root.gameObject, "ConfirmDialog");
        }

        private static void BuildSettingsPrefab(GameObject confirmPrefab)
        {
            var root = Overlay("SettingsPanel", out RectTransform card, new Vector2(890f, 1330f));
            AddTitle(card, "Title", "Settings", 226f, 120f);
            var close = SquareIconButton(card, "CloseButton", KitIcon("cross"), new Vector2(-44f, -44f), 108f, 46f);

            var sound = ToggleRow(card, "SoundRow", 416f, KitIcon("sound"), Hex("FFC8E2"), "Sound");
            var music = ToggleRow(card, "MusicRow", 598f, KitIcon("music"), Hex("FFD9B9"), "Music");
            var vibration = ToggleRow(card, "VibrationRow", 780f, S("icon_vibration.png"), Hex("DCD0FF"), "Vibration");

            var reset = OutlineButton(card, "ResetProgress", 972f, new Vector2(767f, 133f), KitIcon("try-again"), "Reset Progress");
            var done = BigButton(card, "Done", 1171f, new Vector2(767f, 153f), S("btn_lavender.png"), Violet, "Done", TextDark, null, 72f);

            var confirm = (GameObject)PrefabUtility.InstantiatePrefab(confirmPrefab, root);
            Stretch((RectTransform)confirm.transform);

            var panel = root.gameObject.AddComponent<SettingsPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("soundToggle").objectReferenceValue = sound;
            so.FindProperty("musicToggle").objectReferenceValue = music;
            so.FindProperty("vibrationToggle").objectReferenceValue = vibration;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("doneButton").objectReferenceValue = done;
            so.FindProperty("resetButton").objectReferenceValue = reset;
            so.FindProperty("confirmDialog").objectReferenceValue = confirm.GetComponent<ConfirmDialog>();
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            SavePrefab(root.gameObject, "SettingsPanel");
        }

        private static void BuildPausePrefab(GameObject confirmPrefab)
        {
            var root = Overlay("PausePanel", out RectTransform card, new Vector2(890f, 1075f));
            AddTitle(card, "Title", "Paused", 228f, 120f);
            var subtitle = AddText(NewTop(card, "Subtitle", 334f, new Vector2(760f, 70f)), "Level 1 · Take a breather",
                nunito, 44f, Subtle, TextAlignmentOptions.Center);
            var gear = SquareIconButton(card, "SettingsButton", KitIcon("settings"), new Vector2(-44f, -44f), 108f, 56f);

            BigButton(card, "Resume", 512f, new Vector2(757f, 160f), S("btn_pink.png"), PinkLip, "Resume", TextPlum, KitIcon("play"), 72f);
            BigButton(card, "GoToMenu", 720f, new Vector2(757f, 160f), S("btn_lavender.png"), Violet, "Go to Menu", TextDark, KitIcon("home"), 72f);
            OutlineButton(card, "ResetProgress", 925f, new Vector2(757f, 130f), KitIcon("try-again"), "Reset Progress");

            var confirm = (GameObject)PrefabUtility.InstantiatePrefab(confirmPrefab, root);
            Stretch((RectTransform)confirm.transform);

            var view = root.gameObject.AddComponent<PausePanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("subtitle").objectReferenceValue = subtitle;
            so.FindProperty("settingsButton").objectReferenceValue = gear;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            SavePrefab(root.gameObject, "PausePanel");
        }

        // ========================================================================
        // 3) Loading scene
        // ========================================================================

        [MenuItem("Tools/MindTiles/4 Build Loading Scene")]
        public static void BuildLoadingScene()
        {
            LoadFonts();
            var backdrop = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Backdrop.prefab");
            var logo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Logo.prefab");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Night;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var canvasRt = MakeCanvas("Canvas", 0);
            if (backdrop != null) PrefabUtility.InstantiatePrefab(backdrop, canvasRt);

            var safe = NewRect("SafeArea", canvasRt);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            if (logo != null)
            {
                var l = (RectTransform)((GameObject)PrefabUtility.InstantiatePrefab(logo, safe)).transform;
                l.anchorMin = l.anchorMax = new Vector2(0.5f, 0.58f);
                l.anchoredPosition = Vector2.zero;
                l.sizeDelta = new Vector2(860f, 400f);
            }

            // Tile progress bar
            var bar = NewRect("ProgressBar", safe);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.anchoredPosition = new Vector2(0f, 265f);
            bar.sizeDelta = new Vector2(890f, 100f);
            AddImage(bar, S("ui_pill.png"), GlassFill, true);
            var barBorder = NewRect("Border", bar);
            Stretch(barBorder);
            AddImage(barBorder, S("ui_outline.png"), GlassBorder, true).pixelsPerUnitMultiplier = 1.4f;

            var slots = new Image[10];
            const float slot = 62f, pad = 22f;
            float gap = (890f - 2f * pad - 10f * slot) / 9f;
            for (int i = 0; i < 10; i++)
            {
                var s = NewRect("Slot" + i, bar);
                s.anchorMin = s.anchorMax = new Vector2(0f, 0.5f);
                s.pivot = new Vector2(0f, 0.5f);
                s.anchoredPosition = new Vector2(pad + i * (slot + gap), 0f);
                s.sizeDelta = new Vector2(slot, slot);
                slots[i] = AddImage(s, S("tile_slot_empty.png"), Color.white, false);
            }

            var label = NewRect("Label", safe);
            label.anchorMin = label.anchorMax = new Vector2(0f, 0f);
            label.pivot = new Vector2(0f, 0.5f);
            label.anchoredPosition = new Vector2(62f, 192f);
            label.sizeDelta = new Vector2(600f, 60f);
            AddText(label, "Loading puzzles…", nunito, 38f, Subtle, TextAlignmentOptions.Left);

            var pct = NewRect("Percent", safe);
            pct.anchorMin = pct.anchorMax = new Vector2(1f, 0f);
            pct.pivot = new Vector2(1f, 0.5f);
            pct.anchoredPosition = new Vector2(-62f, 192f);
            pct.sizeDelta = new Vector2(240f, 60f);
            var pctText = AddText(pct, "0%", fredoka, 44f, Color.white, TextAlignmentOptions.Right);

            var loaderGo = new GameObject("BootLoader");
            var loader = loaderGo.AddComponent<BootLoader>();
            var so = new SerializedObject(loader);
            var slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            var filled = so.FindProperty("filledSprites");
            Sprite[] fills = { S("tile_pink.png"), S("tile_peach.png"), S("tile_lavender.png") };
            filled.arraySize = fills.Length;
            for (int i = 0; i < fills.Length; i++) filled.GetArrayElementAtIndex(i).objectReferenceValue = fills[i];
            so.FindProperty("emptySprite").objectReferenceValue = S("tile_slot_empty.png");
            so.FindProperty("nextSprite").objectReferenceValue = S("tile_slot_dashed.png");
            so.FindProperty("percentText").objectReferenceValue = pctText;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, LoadingScene);

            // Loading first, then menu, then game; engine splash off.
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(LoadingScene, true),
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(GameScene, true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            Debug.Log("[UIBuilder] Loading scene built; build order: Loading, MenuSpace, SampleScene.");
        }

        private static RectTransform MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            ApplyScaler(go.GetComponent<CanvasScaler>());
            return (RectTransform)go.transform;
        }

        private static void ApplyScaler(CanvasScaler s)
        {
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080f, 1920f);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0.357f;
        }

        // ========================================================================
        // 4) Menu scene
        // ========================================================================

        [MenuItem("Tools/MindTiles/5 Rebuild Menu")]
        public static void RebuildMenu()
        {
            LoadFonts();
            var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().First(g => g.GetComponent<Canvas>() != null);
            var canvasRt = (RectTransform)canvas.transform;

            // a) Same canvas scaling as the game, so the shared backdrop looks identical.
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || scaler.referenceResolution != new Vector2(1080f, 1920f))
            {
                Rect old = canvasRt.rect;
                float aspect = old.width / old.height;
                float c = Mathf.Exp(0.643f * Mathf.Log(1080f) + 0.357f * Mathf.Log(1920f));
                float newWidth = Mathf.Pow(aspect, 0.357f) * c;
                float k = newWidth / old.width;
                foreach (Transform child in canvasRt) ScaleTree(child, k);
                ApplyScaler(scaler);
                Debug.Log("[UIBuilder] Menu re-scaled by " + k);
            }

            Transform Find(string n) => canvasRt.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
            var safe = (RectTransform)Find("SafeArea");

            // b) Shared backdrop prefab, made from the menu's moving background.
            var bg = Find("Background");
            string backdropPath = PrefabDir + "/Backdrop.prefab";
            if (bg != null && !PrefabUtility.IsPartOfPrefabInstance(bg.gameObject))
            {
                bg.name = "Backdrop";
                PrefabUtility.SaveAsPrefabAssetAndConnect(bg.gameObject, backdropPath, InteractionMode.AutomatedAction);
            }

            // c) Logo instead of the old HEAD text
            var head = Find("HEAD");
            if (head != null) head.gameObject.SetActive(false);
            var logoRt = (RectTransform)Find("Logo");
            if (logoRt == null)
            {
                var logoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Logo.prefab");
                logoRt = (RectTransform)((GameObject)PrefabUtility.InstantiatePrefab(logoPrefab, safe)).transform;
            }
            logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 1f);
            logoRt.pivot = new Vector2(0.5f, 0.5f);
            logoRt.anchoredPosition = new Vector2(0f, -640f);
            logoRt.sizeDelta = new Vector2(860f, 400f);

            // d) Top-right info / settings buttons
            var settingsBtn = (RectTransform)Find("ayarlar");
            var infoBtn = (RectTransform)Find("info");
            StyleTopButton(settingsBtn, safe, KitIcon("settings"), new Vector2(-65f, -100f));
            StyleTopButton(infoBtn, safe, KitIcon("info"), new Vector2(-65f - 117f - 29f, -100f));

            // e) Mode buttons at the bottom
            var daily = (RectTransform)Find("HızlıOyun");
            var endless = (RectTransform)Find("devamet");
            StyleModeButton(daily, safe, 505f, S("btn_pink.png"), PinkLip, KitIcon("flame"), Hex("FF6B7A"),
                "Daily Streak", "Keep the flame alive");
            StyleModeButton(endless, safe, 230f, S("btn_lavender.png"), Violet, S("icon_infinity.png"), Violet,
                "Endless Mode", "Play at your own pace");

            // f) Score pill above the mode buttons
            var scoreText = (RectTransform)Find("LEVEL 999");
            var pill = BuildScorePill(safe, scoreText, 730f);

            // g) One shared settings panel (prefab) instead of the old one
            var oldSettings = Find("ayarlarpanel");
            if (oldSettings != null) Object.DestroyImmediate(oldSettings.gameObject);
            var oldConfirm = Find("ConfirmDialog");
            if (oldConfirm != null && !PrefabUtility.IsPartOfPrefabInstance(oldConfirm.gameObject)) Object.DestroyImmediate(oldConfirm.gameObject);

            var settings = Find("SettingsPanel");
            if (settings == null)
            {
                var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/SettingsPanel.prefab");
                settings = ((GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, canvasRt)).transform;
                Stretch((RectTransform)settings);
                settings.gameObject.SetActive(false);
            }

            // h) Menu manager + exit transition wiring
            var menu = Object.FindFirstObjectByType<MenuManager>();
            var mso = new SerializedObject(menu);
            mso.FindProperty("settingsPanel").objectReferenceValue = settings.gameObject;
            mso.FindProperty("scoreText").objectReferenceValue = scoreText.GetComponent<TMP_Text>();
            mso.ApplyModifiedPropertiesWithoutUndo();

            var exit = Object.FindFirstObjectByType<MenuExitTransition>(FindObjectsInactive.Include);
            if (exit != null)
            {
                var eso = new SerializedObject(exit);
                SetSlides(eso, "slideUp", logoRt, infoBtn, settingsBtn);
                SetSlides(eso, "slideDown", pill);
                SetSlides(eso, "slideLeft", daily);
                SetSlides(eso, "slideRight", endless);
                eso.ApplyModifiedPropertiesWithoutUndo();

                var loading = Find("Loading");
                if (loading != null)
                {
                    var t = loading.GetComponent<TMP_Text>();
                    t.font = nunito;
                    t.fontSize = 48f;
                    t.color = Subtle;
                }
            }

            EnsureStreakBar(safe, exit);
            EnsureInfoPanel(canvasRt, menu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIBuilder] Menu rebuilt.");
        }

        // Weekly Daily Streak bar between the logo and the score pill (menu only).
        [MenuItem("Tools/MindTiles/8 Add Streak Bar To Menu")]
        public static void AddStreakBar()
        {
            LoadFonts();
            var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().First(g => g.GetComponent<Canvas>() != null);
            var safe = (RectTransform)canvas.GetComponentsInChildren<Transform>(true).First(t => t.name == "SafeArea");
            var exit = Object.FindFirstObjectByType<MenuExitTransition>(FindObjectsInactive.Include);

            EnsureStreakBar(safe, exit);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIBuilder] Streak bar added to the menu.");
        }

        private static void EnsureStreakBar(RectTransform safe, MenuExitTransition exit)
        {
            if (safe.Find("StreakCard") != null) return;

            const int days = 7;
            const float width = 890f, height = 210f;

            var card = NewRect("StreakCard", safe);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0f, 930f);
            card.sizeDelta = new Vector2(width, height);
            AddImage(card, S("ui_rounded.png"), GlassFill, true).pixelsPerUnitMultiplier = 0.55f;
            var border = NewRect("Border", card);
            Stretch(border);
            AddImage(border, S("ui_outline.png"), GlassBorder, true).pixelsPerUnitMultiplier = 0.7f;

            var title = NewRect("Title", card);
            title.anchorMin = title.anchorMax = new Vector2(0f, 1f);
            title.pivot = new Vector2(0f, 0.5f);
            title.anchoredPosition = new Vector2(40f, -44f);
            title.sizeDelta = new Vector2(560f, 50f);
            var titleText = AddText(title, "DAILY STREAK", nunito, 32f, Subtle, TextAlignmentOptions.Left);
            titleText.characterSpacing = 12f;

            var count = NewRect("Count", card);
            count.anchorMin = count.anchorMax = new Vector2(1f, 1f);
            count.pivot = new Vector2(1f, 0.5f);
            count.anchoredPosition = new Vector2(-40f, -44f);
            count.sizeDelta = new Vector2(240f, 56f);
            var countText = AddText(count, "0 / 7", fredoka, 44f, Peach, TextAlignmentOptions.Right);

            var slots = new Image[days];
            const float slot = 96f, pad = 34f;
            float gap = (width - 2f * pad - days * slot) / (days - 1);
            for (int i = 0; i < days; i++)
            {
                var s = NewRect("Day" + (i + 1), card);
                s.anchorMin = s.anchorMax = new Vector2(0f, 0f);
                s.pivot = new Vector2(0f, 0.5f);
                s.anchoredPosition = new Vector2(pad + i * (slot + gap), 66f);
                s.sizeDelta = new Vector2(slot, slot);
                slots[i] = AddImage(s, S("tile_slot_empty.png"), Color.white, false);
            }

            var bar = card.gameObject.AddComponent<StreakBar>();
            var so = new SerializedObject(bar);
            var slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = days;
            for (int i = 0; i < days; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            Sprite[] fills = { S("tile_pink.png"), S("tile_peach.png"), S("tile_lavender.png") };
            var filled = so.FindProperty("filledSprites");
            filled.arraySize = fills.Length;
            for (int i = 0; i < fills.Length; i++) filled.GetArrayElementAtIndex(i).objectReferenceValue = fills[i];
            so.FindProperty("emptySprite").objectReferenceValue = S("tile_slot_empty.png");
            so.FindProperty("nextSprite").objectReferenceValue = S("tile_slot_dashed.png");
            so.FindProperty("countText").objectReferenceValue = countText;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Leaves the screen with the score pill when a mode is picked.
            if (exit != null)
            {
                var eso = new SerializedObject(exit);
                var list = eso.FindProperty("slideDown");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("target").objectReferenceValue = card;
                eso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ScaleTree(Transform t, float k)
        {
            if (t is RectTransform rt)
            {
                rt.anchoredPosition *= k;
                rt.sizeDelta *= k;
            }
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize *= k;
                tmp.fontSizeMin *= k;
                tmp.fontSizeMax *= k;
            }
            foreach (Transform c in t) ScaleTree(c, k);
        }

        private static void SetSlides(SerializedObject so, string list, params RectTransform[] targets)
        {
            var prop = so.FindProperty(list);
            prop.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                prop.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue = targets[i];
        }

        private static void ClearChildren(Transform t, params string[] keep)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                if (keep.Contains(c.name)) continue;
                Object.DestroyImmediate(c.gameObject);
            }
        }

        private static void StyleTopButton(RectTransform btn, RectTransform parent, Sprite icon, Vector2 posFromTopRight)
        {
            btn.SetParent(parent, false);
            btn.anchorMin = btn.anchorMax = new Vector2(1f, 1f);
            btn.pivot = new Vector2(1f, 1f);
            btn.anchoredPosition = posFromTopRight;
            btn.sizeDelta = new Vector2(117f, 117f);
            btn.localRotation = Quaternion.identity;
            btn.localScale = Vector3.one;
            ClearChildren(btn);

            var img = btn.GetComponent<Image>();
            img.sprite = S("ui_rounded.png");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.2f;
            img.color = GlassFill;
            var border = NewRect("Border", btn);
            Stretch(border);
            AddImage(border, S("ui_outline.png"), GlassBorder, true).pixelsPerUnitMultiplier = 1.2f;
            var ic = NewRect("Icon", btn);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(58f, 58f);
            AddImage(ic, icon, Color.white, false).preserveAspect = true;
        }

        private static void StyleModeButton(RectTransform btn, RectTransform parent, float centerFromBottom,
            Sprite face, Color lip, Sprite icon, Color iconColor, string title, string subtitle)
        {
            btn.SetParent(parent, false);
            btn.anchorMin = btn.anchorMax = new Vector2(0.5f, 0f);
            btn.pivot = new Vector2(0.5f, 0.5f);
            btn.anchoredPosition = new Vector2(0f, centerFromBottom);
            btn.sizeDelta = new Vector2(890f, 228f);
            btn.localScale = Vector3.one;

            // Keep the old label object (it may be referenced) and rebuild the rest.
            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.transform.SetParent(null, false);
            ClearChildren(btn);

            var rootImg = btn.GetComponent<Image>();
            rootImg.sprite = null;
            rootImg.color = new Color(1f, 1f, 1f, 0f);   // invisible, still catches taps

            var lipRt = NewRect("Lip", btn);
            Stretch(lipRt);
            lipRt.offsetMin = new Vector2(0f, -18f);
            lipRt.offsetMax = new Vector2(0f, -18f);
            AddImage(lipRt, S("ui_rounded.png"), lip, true).pixelsPerUnitMultiplier = 0.9f;

            var faceRt = NewRect("Face", btn);
            Stretch(faceRt);
            var faceImg = AddImage(faceRt, face, Color.white, true);
            faceImg.pixelsPerUnitMultiplier = 0.9f;
            var button = btn.GetComponent<Button>();
            button.targetGraphic = faceImg;
            button.transition = Selectable.Transition.ColorTint;

            var box = NewRect("IconBox", faceRt);
            box.anchorMin = box.anchorMax = new Vector2(0f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(39f, 0f);
            box.sizeDelta = new Vector2(152f, 152f);
            AddImage(box, S("ui_rounded.png"), new Color(1f, 0.97f, 0.99f, 0.95f), true).pixelsPerUnitMultiplier = 1.1f;
            var ic = NewRect("Icon", box);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(72f, 72f);
            AddImage(ic, icon, iconColor, false).preserveAspect = true;

            if (label == null) label = NewRect("Title", null).gameObject.AddComponent<TextMeshProUGUI>();
            label.name = "Title";
            var lt = (RectTransform)label.transform;
            lt.SetParent(faceRt, false);
            lt.anchorMin = lt.anchorMax = new Vector2(0f, 0.5f);
            lt.pivot = new Vector2(0f, 0.5f);
            lt.anchoredPosition = new Vector2(230f, 26f);
            lt.sizeDelta = new Vector2(500f, 90f);
            StyleText(label, title, fredoka, 70f, TextDark, TextAlignmentOptions.Left);

            var sub = NewRect("Subtitle", faceRt);
            sub.anchorMin = sub.anchorMax = new Vector2(0f, 0.5f);
            sub.pivot = new Vector2(0f, 0.5f);
            sub.anchoredPosition = new Vector2(232f, -40f);
            sub.sizeDelta = new Vector2(500f, 60f);
            AddText(sub, subtitle, nunito, 38f, new Color(TextDark.r, TextDark.g, TextDark.b, 0.85f), TextAlignmentOptions.Left);

            var play = NewRect("Play", faceRt);
            play.anchorMin = play.anchorMax = new Vector2(1f, 0.5f);
            play.pivot = new Vector2(1f, 0.5f);
            play.anchoredPosition = new Vector2(-47f, 0f);
            play.sizeDelta = new Vector2(104f, 104f);
            AddImage(play, S("ui_circle.png"), TextDark, false);
            var pi = NewRect("Icon", play);
            pi.anchorMin = pi.anchorMax = new Vector2(0.5f, 0.5f);
            pi.anchoredPosition = new Vector2(4f, 0f);
            pi.sizeDelta = new Vector2(40f, 40f);
            AddImage(pi, KitIcon("play"), Color.white, false).preserveAspect = true;
        }

        private static RectTransform BuildScorePill(RectTransform parent, RectTransform scoreText, float centerFromBottom)
        {
            Transform existing = parent.Find("ScorePill");
            RectTransform pill = existing != null ? (RectTransform)existing : NewRect("ScorePill", parent);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0f);
            pill.pivot = new Vector2(0.5f, 0.5f);
            pill.anchoredPosition = new Vector2(0f, centerFromBottom);
            pill.sizeDelta = new Vector2(330f, 117f);
            if (pill.GetComponent<Image>() == null)
            {
                AddImage(pill, S("ui_pill.png"), GlassFill, true).pixelsPerUnitMultiplier = 0.55f;
                var border = NewRect("Border", pill);
                Stretch(border);
                AddImage(border, S("ui_outline.png"), GlassBorder, true).pixelsPerUnitMultiplier = 0.7f;

                var circle = NewRect("Trophy", pill);
                circle.anchorMin = circle.anchorMax = new Vector2(0f, 0.5f);
                circle.pivot = new Vector2(0f, 0.5f);
                circle.anchoredPosition = new Vector2(14f, 0f);
                circle.sizeDelta = new Vector2(89f, 89f);
                AddImage(circle, S("ui_circle.png"), Peach, false);
                var ic = NewRect("Icon", circle);
                ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
                ic.sizeDelta = new Vector2(50f, 50f);
                AddImage(ic, KitIcon("trophy"), Hex("5A3418"), false).preserveAspect = true;

                var label = NewRect("Label", pill);
                label.anchorMin = label.anchorMax = new Vector2(0f, 0.5f);
                label.pivot = new Vector2(0f, 0.5f);
                label.anchoredPosition = new Vector2(122f, 24f);
                label.sizeDelta = new Vector2(190f, 40f);
                var lt = AddText(label, "SCORE", nunito, 30f, Subtle, TextAlignmentOptions.Left);
                lt.characterSpacing = 12f;
            }

            // The existing score text (MenuManager writes into it) becomes the value.
            scoreText.SetParent(pill, false);
            scoreText.anchorMin = scoreText.anchorMax = new Vector2(0f, 0.5f);
            scoreText.pivot = new Vector2(0f, 0.5f);
            scoreText.anchoredPosition = new Vector2(122f, -18f);
            scoreText.sizeDelta = new Vector2(200f, 64f);
            scoreText.localScale = Vector3.one;
            foreach (var mb in scoreText.GetComponents<MonoBehaviour>())
                if (mb is ScorePulseEffect) mb.enabled = false;   // static readout in the new design
            StyleText(scoreText.GetComponent<TMP_Text>(), "999", fredoka, 62f, Color.white, TextAlignmentOptions.Left);
            EnsureScorePillFitter(pill, scoreText.GetComponent<TMP_Text>());
            return pill;
        }

        // The pill widens with the score instead of the text overflowing it.
        private static void EnsureScorePillFitter(RectTransform pill, TMP_Text value)
        {
            var fitter = pill.GetComponent<ScorePillFitter>();
            if (fitter == null) fitter = pill.gameObject.AddComponent<ScorePillFitter>();
            var so = new SerializedObject(fitter);
            so.FindProperty("valueText").objectReferenceValue = value;
            var label = pill.Find("Label");
            so.FindProperty("labelText").objectReferenceValue = label != null ? label.GetComponent<TMP_Text>() : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ========================================================================
        // 5) Game scene
        // ========================================================================

        [MenuItem("Tools/MindTiles/6 Rebuild Game Panels")]
        public static void RebuildGame()
        {
            LoadFonts();
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            var canvases = scene.GetRootGameObjects().Where(g => g.GetComponent<Canvas>() != null).ToList();
            var boardCanvas = canvases.First(c => c.GetComponent<Canvas>().sortingOrder == 0).transform;
            var topCanvas = canvases.First(c => c.GetComponent<Canvas>().sortingOrder != 0).transform;

            // a) Shared moving backdrop behind the board
            var oldBg = boardCanvas.Find("Image");
            if (oldBg != null) oldBg.gameObject.SetActive(false);
            if (boardCanvas.Find("Backdrop") == null)
            {
                var backdrop = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Backdrop.prefab");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(backdrop, boardCanvas);
                inst.transform.SetAsFirstSibling();
            }

            // b) Replace the old pause panel and confirm dialog with the shared prefabs
            foreach (var t in topCanvas.GetComponentsInChildren<Transform>(true).ToArray())
            {
                if (t == null) continue;
                bool old = (t.name == "PausePanel" || t.name == "ConfirmDialog") && !PrefabUtility.IsPartOfPrefabInstance(t.gameObject);
                if (old) Object.DestroyImmediate(t.gameObject);
            }

            var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/PausePanel.prefab");
            var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/SettingsPanel.prefab");
            var pause = topCanvas.Find("PausePanel") ?? ((GameObject)PrefabUtility.InstantiatePrefab(pausePrefab, topCanvas)).transform;
            var settings = topCanvas.Find("SettingsPanel") ?? ((GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, topCanvas)).transform;
            Stretch((RectTransform)pause);
            Stretch((RectTransform)settings);
            pause.SetAsLastSibling();
            settings.SetAsLastSibling();
            pause.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);

            var view = pause.GetComponent<PausePanelView>();
            var vso = new SerializedObject(view);
            vso.FindProperty("settingsPanel").objectReferenceValue = settings.GetComponent<SettingsPanel>();
            vso.ApplyModifiedPropertiesWithoutUndo();

            // c) ButtonManager wiring to the new pause panel
            Button B(string n) => pause.GetComponentsInChildren<Button>(true).First(b => b.name == n);
            var bm = Object.FindFirstObjectByType<ButtonManager>();
            var bso = new SerializedObject(bm);
            bso.FindProperty("pausePanel").objectReferenceValue = pause.gameObject;
            bso.FindProperty("resumeButton").objectReferenceValue = B("Resume");
            bso.FindProperty("pauseMenuButton").objectReferenceValue = B("GoToMenu");
            bso.FindProperty("pauseResetButton").objectReferenceValue = B("ResetProgress");
            bso.FindProperty("confirmDialog").objectReferenceValue = pause.GetComponentInChildren<ConfirmDialog>(true);
            bso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIBuilder] Game panels rebuilt.");
        }

        // ========================================================================
        // 6) In-game HUD: header panel + texts, corner buttons, end panel
        // ========================================================================

        [MenuItem("Tools/MindTiles/7 Restyle Game HUD")]
        public static void RestyleGameHud()
        {
            LoadFonts();
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            Transform F(string n) => Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.gameObject.scene == scene && t.name == n);

            var levelText = (RectTransform)F("LEVEL_text");
            var holder = (RectTransform)levelText.parent;

            // ---- Header panel (top sheet, mirrors the end panel) ----
            var header = holder.Find("HeaderPanel") as RectTransform;
            if (header == null)
            {
                header = NewRect("HeaderPanel", holder);
                AddImage(header, S("ui_rounded.png"), CardColor, true).pixelsPerUnitMultiplier = 0.6f;
                var border = NewRect("Border", header);
                Stretch(border);
                AddImage(border, S("ui_outline.png"), CardBorder, true).pixelsPerUnitMultiplier = 0.6f;
            }
            header.SetAsFirstSibling();
            header.anchorMin = header.anchorMax = new Vector2(0.5f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(1000f, 230f);
            var fill = header.GetComponent<FillToScreenEdges>();
            if (fill == null) fill = header.gameObject.AddComponent<FillToScreenEdges>();
            var fso = new SerializedObject(fill);
            fso.FindProperty("extendTop").boolValue = true;
            fso.FindProperty("extendBottom").boolValue = false;
            fso.ApplyModifiedPropertiesWithoutUndo();

            // ---- Corner buttons ----
            // The buttons have an icon child with the same name; pick the object that holds the Button.
            RectTransform Btn(string n) => (RectTransform)Resources.FindObjectsOfTypeAll<Button>()
                .First(x => x.gameObject.scene == scene && x.name == n).transform;
            CornerButton(Btn("back"), new Vector2(0f, 1f), new Vector2(36f, -48f), KitIcon("back"));
            CornerButton(Btn("pause"), new Vector2(1f, 1f), new Vector2(-36f, -48f), KitIcon("pause"));

            // ---- Daily header text ----
            PlaceTop(levelText, -62f, new Vector2(560f, 110f));
            TitleStyle(levelText.GetComponent<TMP_Text>(), 84f);

            // ---- Endless header ----
            var total = (RectTransform)F("TotalScore_text");
            PlaceTop(total, -40f, new Vector2(560f, 100f));
            TitleStyle(total.GetComponent<TMP_Text>(), 76f);

            var endlessLevel = (RectTransform)F("EndlessLevel_text");
            PlaceTop(endlessLevel, -143f, new Vector2(560f, 56f));
            StyleText(endlessLevel.GetComponent<TMP_Text>(), "LEVEL 1", nunito, 40f, Subtle, TextAlignmentOptions.Center);
            endlessLevel.GetComponent<TMP_Text>().characterSpacing = 8f;

            var timer = (RectTransform)F("Timer_text");
            var endlessHeader = (RectTransform)total.parent;
            var pill = endlessHeader.Find("TimerPill") as RectTransform;
            if (pill == null)
            {
                pill = NewRect("TimerPill", endlessHeader);
                AddImage(pill, S("ui_pill.png"), GlassFill, true).pixelsPerUnitMultiplier = 0.9f;
                var border = NewRect("Border", pill);
                Stretch(border);
                AddImage(border, S("ui_outline.png"), GlassBorder, true).pixelsPerUnitMultiplier = 1.1f;
                var ic = NewRect("Icon", pill);
                ic.anchorMin = ic.anchorMax = new Vector2(0f, 0.5f);
                ic.pivot = new Vector2(0f, 0.5f);
                ic.anchoredPosition = new Vector2(24f, 0f);
                ic.sizeDelta = new Vector2(38f, 38f);
                AddImage(ic, KitIcon("hourglass"), Peach, false).preserveAspect = true;
            }
            PlaceTop(pill, -210f, new Vector2(220f, 74f));
            timer.SetParent(pill, false);
            timer.anchorMin = Vector2.zero;
            timer.anchorMax = Vector2.one;
            timer.pivot = new Vector2(0.5f, 0.5f);
            timer.offsetMin = new Vector2(56f, 0f);
            timer.offsetMax = new Vector2(-14f, 0f);
            StyleText(timer.GetComponent<TMP_Text>(), "0:30", fredoka, 46f, Color.white, TextAlignmentOptions.Center);

            // ---- Board stays below the header panel ----
            var fitter = Object.FindFirstObjectByType<BoardFitter>(FindObjectsInactive.Include);
            var bf = new SerializedObject(fitter);
            var refs = bf.FindProperty("topReferences");
            bool has = false;
            for (int i = 0; i < refs.arraySize; i++) if (refs.GetArrayElementAtIndex(i).objectReferenceValue == header) has = true;
            if (!has)
            {
                refs.arraySize++;
                refs.GetArrayElementAtIndex(refs.arraySize - 1).objectReferenceValue = header;
            }
            bf.ApplyModifiedPropertiesWithoutUndo();

            var ui = Object.FindFirstObjectByType<UIManager>();
            var uso = new SerializedObject(ui);
            uso.FindProperty("headerPanel").objectReferenceValue = fill;
            uso.ApplyModifiedPropertiesWithoutUndo();

            // ---- End panel: same card style as pause / settings ----
            var end = (RectTransform)F("EndPanel");
            var endImg = end.GetComponent<Image>();
            endImg.sprite = S("ui_rounded.png");
            endImg.type = Image.Type.Sliced;
            endImg.pixelsPerUnitMultiplier = 0.6f;
            endImg.color = CardColor;
            if (end.Find("Border") == null)
            {
                var border = NewRect("Border", end);
                Stretch(border);
                AddImage(border, S("ui_outline.png"), CardBorder, true).pixelsPerUnitMultiplier = 0.6f;
                border.SetAsFirstSibling();
            }

            var state = (RectTransform)end.Find("State");
            state.anchorMin = new Vector2(0f, 1f); state.anchorMax = new Vector2(1f, 1f);
            state.pivot = new Vector2(0.5f, 1f);
            state.anchoredPosition = new Vector2(0f, -36f);
            state.sizeDelta = new Vector2(-120f, 110f);
            TitleStyle(state.GetComponent<TMP_Text>(), 84f);

            foreach (var n in new[] { "CurrentScore", "HighestScore" })
            {
                var rt = (RectTransform)end.Find(n);
                bool left = n == "CurrentScore";
                rt.anchorMin = new Vector2(left ? 0f : 0.5f, 1f);
                rt.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -150f);
                rt.sizeDelta = new Vector2(0f, 150f);
                var t = rt.GetComponent<TMP_Text>();
                string keep = t.text;
                StyleText(t, keep, fredoka, 72f, Color.white, TextAlignmentOptions.Center);
                t.richText = true;
            }

            var next = (RectTransform)end.Find("NextLevel");
            next.anchorMin = new Vector2(0f, 1f); next.anchorMax = new Vector2(1f, 1f);
            next.pivot = new Vector2(0.5f, 1f);
            next.anchoredPosition = new Vector2(0f, -316f);
            next.sizeDelta = new Vector2(-180f, 140f);
            var nextImg = next.GetComponent<Image>();
            nextImg.sprite = S("btn_pink.png");
            nextImg.type = Image.Type.Sliced;
            nextImg.pixelsPerUnitMultiplier = 0.9f;
            nextImg.color = Color.white;
            foreach (Transform c in next) if (c.name != "Content") c.gameObject.SetActive(false);
            if (next.Find("Content") == null) IconLabel(next, KitIcon("play"), "Next", fredoka, 66f, TextPlum, 0.6f);

            var lip = end.Find("NextLevelLip") as RectTransform;
            if (lip == null)
            {
                lip = NewRect("NextLevelLip", end);
                AddImage(lip, S("ui_rounded.png"), PinkLip, true).pixelsPerUnitMultiplier = 0.9f;
            }
            lip.anchorMin = next.anchorMin; lip.anchorMax = next.anchorMax; lip.pivot = next.pivot;
            lip.sizeDelta = next.sizeDelta;
            lip.anchoredPosition = next.anchoredPosition + new Vector2(0f, -16f);
            // The lip must sit right before (behind) the button.
            if (lip.GetSiblingIndex() > next.GetSiblingIndex()) lip.SetSiblingIndex(next.GetSiblingIndex());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIBuilder] Game HUD restyled.");
        }

        private static void PlaceTop(RectTransform rt, float y, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        private static void TitleStyle(TMP_Text t, float size)
        {
            StyleText(t, t.text, fredoka, size, Color.white, TextAlignmentOptions.Center);
            if (titleMaterial != null) t.fontSharedMaterial = titleMaterial;
            t.enableVertexGradient = true;
            t.colorGradient = new VertexGradient(Color.white, Color.white, Hex("E6DAFF"), Hex("E6DAFF"));
            t.richText = true;
        }

        private static void CornerButton(RectTransform btn, Vector2 corner, Vector2 pos, Sprite icon)
        {
            btn.anchorMin = btn.anchorMax = corner;
            btn.pivot = corner;
            btn.anchoredPosition = pos;
            btn.sizeDelta = new Vector2(117f, 117f);
            btn.localScale = Vector3.one;
            ClearChildren(btn);

            var img = btn.GetComponent<Image>();
            img.sprite = S("ui_rounded.png");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.2f;
            img.color = GlassFill;
            var border = NewRect("Border", btn);
            Stretch(border);
            AddImage(border, S("ui_outline.png"), GlassBorder, true).pixelsPerUnitMultiplier = 1.2f;
            var ic = NewRect("Icon", btn);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(54f, 54f);
            AddImage(ic, icon, Color.white, false).preserveAspect = true;
        }

        // ========================================================================
        // Widget helpers
        // ========================================================================

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Child placed on the parent's top edge, centered horizontally, `fromTop` to its center.
        private static void TopCenter(RectTransform rt, float fromTop, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -fromTop);
            rt.sizeDelta = size;
        }

        private static RectTransform NewTop(RectTransform parent, string name, float fromTop, Vector2 size)
        {
            var rt = NewRect(name, parent);
            TopCenter(rt, fromTop, size);
            return rt;
        }

        private static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool sliced)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            StyleText(t, text, font, size, color, align);
            return t;
        }

        private static void StyleText(TMP_Text t, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            t.text = text;
            t.font = font;
            t.fontSharedMaterial = font.material;
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.fontStyle = FontStyles.Normal;
            t.color = color;
            t.enableVertexGradient = false;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.characterSpacing = 0f;
        }

        // Big white-to-lavender title with a dark outline (the design's headline style).
        private static TextMeshProUGUI AddTitle(RectTransform card, string name, string text, float fromTop, float size)
        {
            var t = AddText(NewTop(card, name, fromTop, new Vector2(760f, 160f)), text, fredoka, size, Color.white, TextAlignmentOptions.Center);
            if (titleMaterial != null) t.fontSharedMaterial = titleMaterial;
            t.enableVertexGradient = true;
            t.colorGradient = new VertexGradient(Color.white, Color.white, Hex("E6DAFF"), Hex("E6DAFF"));
            return t;
        }

        // Rounded button with a darker "lip" under it, a gradient face and an optional icon.
        private static Button BigButton(RectTransform card, string name, float fromTop, Vector2 size, Sprite face, Color lip,
            string label, Color labelColor, Sprite icon, float fontSize)
        {
            var holder = NewTop(card, name + "Holder", fromTop, size);

            var lipRt = NewRect("Lip", holder);
            Stretch(lipRt);
            lipRt.offsetMin = new Vector2(0f, -16f);
            lipRt.offsetMax = new Vector2(0f, -16f);
            AddImage(lipRt, S("ui_rounded.png"), lip, true).pixelsPerUnitMultiplier = 0.9f;

            var faceRt = NewRect(name, holder);
            Stretch(faceRt);
            var faceImg = AddImage(faceRt, face, Color.white, true);
            faceImg.pixelsPerUnitMultiplier = 0.9f;
            faceImg.raycastTarget = true;
            var button = faceRt.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImg;

            IconLabel(faceRt, icon, label, fredoka, fontSize, labelColor, 0.62f);
            return button;
        }

        // Dark outlined button (Reset Progress).
        private static Button OutlineButton(RectTransform card, string name, float fromTop, Vector2 size, Sprite icon, string label)
        {
            var rt = NewTop(card, name, fromTop, size);
            var bg = AddImage(rt, S("ui_rounded.png"), ResetFill, true);
            bg.pixelsPerUnitMultiplier = 0.9f;
            bg.raycastTarget = true;
            var border = NewRect("Border", rt);
            Stretch(border);
            AddImage(border, S("ui_outline.png"), ResetBorder, true).pixelsPerUnitMultiplier = 0.9f;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            IconLabel(rt, icon, label, fredoka, 54f, ResetText, 0.62f);
            return button;
        }

        // Icon + label centered together in a button.
        private static void IconLabel(RectTransform parent, Sprite icon, string label, TMP_FontAsset font, float fontSize, Color color, float iconScale)
        {
            var row = NewRect("Content", parent);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0f, 2f);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 22f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (icon != null)
            {
                var ic = NewRect("Icon", row);
                var img = AddImage(ic, icon, color, false);
                img.preserveAspect = true;
                var le = ic.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = fontSize * iconScale;
            }

            var txt = NewRect("Label", row);
            AddText(txt, label, font, fontSize, color, TextAlignmentOptions.Center);
        }

        private static Button SquareIconButton(RectTransform card, string name, Sprite icon, Vector2 posFromTopRight, float size, float iconSize)
        {
            var rt = NewRect(name, card);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = posFromTopRight;
            rt.sizeDelta = new Vector2(size, size);
            var bg = AddImage(rt, S("ui_rounded.png"), Hex("2E2466"), true);
            bg.pixelsPerUnitMultiplier = 1.3f;
            bg.raycastTarget = true;
            var border = NewRect("Border", rt);
            Stretch(border);
            AddImage(border, S("ui_outline.png"), Hex("4C3F8F"), true).pixelsPerUnitMultiplier = 1.3f;
            var ic = NewRect("Icon", rt);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(iconSize, iconSize);
            AddImage(ic, icon, Color.white, false).preserveAspect = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            return button;
        }

        private static ToggleSwitch ToggleRow(RectTransform card, string name, float fromTop, Sprite icon, Color boxColor, string label)
        {
            var row = NewTop(card, name, fromTop, new Vector2(767f, 156f));
            AddImage(row, S("ui_rounded.png"), RowColor, true).pixelsPerUnitMultiplier = 1.1f;

            var box = NewRect("IconBox", row);
            box.anchorMin = box.anchorMax = new Vector2(0f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(31f, 0f);
            box.sizeDelta = new Vector2(99f, 99f);
            AddImage(box, S("ui_rounded.png"), boxColor, true).pixelsPerUnitMultiplier = 1.6f;
            var ic = NewRect("Icon", box);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(56f, 56f);
            AddImage(ic, icon, TextPlum, false).preserveAspect = true;

            var lbl = NewRect("Label", row);
            lbl.anchorMin = lbl.anchorMax = new Vector2(0f, 0.5f);
            lbl.pivot = new Vector2(0f, 0.5f);
            lbl.anchoredPosition = new Vector2(162f, 0f);
            lbl.sizeDelta = new Vector2(400f, 80f);
            AddText(lbl, label, nunito, 50f, Color.white, TextAlignmentOptions.Left);

            var sw = NewRect("Toggle", row);
            sw.anchorMin = sw.anchorMax = new Vector2(1f, 0.5f);
            sw.pivot = new Vector2(1f, 0.5f);
            sw.anchoredPosition = new Vector2(-31f, 0f);
            sw.sizeDelta = new Vector2(140f, 83f);
            var track = AddImage(sw, S("ui_pill.png"), Pink, true);
            track.pixelsPerUnitMultiplier = 0.77f;
            track.raycastTarget = true;
            var button = sw.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            var knob = NewRect("Knob", sw);
            knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(66f, 66f);
            knob.anchoredPosition = new Vector2(28f, 0f);
            AddImage(knob, S("ui_circle.png"), Color.white, false);

            var toggle = sw.gameObject.AddComponent<ToggleSwitch>();
            var so = new SerializedObject(toggle);
            so.FindProperty("track").objectReferenceValue = track;
            so.FindProperty("knob").objectReferenceValue = knob;
            so.FindProperty("onColor").colorValue = Pink;
            so.FindProperty("offColor").colorValue = Hex("4B4280");
            so.FindProperty("knobTravel").floatValue = 28f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toggle;
        }

        // ========================================================================
        // Info panel (menu "i"): folder tabs "Info" / "How to Play"
        // ========================================================================

        private static readonly Color PageColor = Hex("33296A");
        private static readonly Color TabInactive = Hex("1C1442");
        private static readonly Color TabInactiveText = Hex("9A8CC7");
        private static readonly Color LinkFill = Hex("231A50");

        private static readonly string[][] HowToPlay =
        {
            new[] { "Memorize", "Look closely! The tiles show where they belong while the countdown runs." },
            new[] { "Rebuild", "Then they get scrambled. Drag a tile onto a cell, or flick it to move it one cell. Drop it on another tile to swap them." },
            new[] { "Check", "Tap the check button when you're done. Green means right, red means wrong, and an arrow points to the right cell." },
            new[] { "Pass the level", "Get at least two thirds of the tiles right to pass and score. Miss it and you play the level again." },
            new[] { "Use hints", "The first hint marks a misplaced tile and where it belongs. The second lights up every correct tile for a few seconds." },
            new[] { "Daily Streak", "Finish all 10 levels in a day to fill a streak box. Seven days in a row fills the whole week!" },
            new[] { "Endless Mode", "Levels adapt to how well you play. Each one has a timer, and checking early earns bonus points." },
        };

        [MenuItem("Tools/MindTiles/9 Build Info Panel")]
        public static void BuildInfoPanelMenu()
        {
            LoadFonts();
            if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder(Root, "Prefabs");
            BuildInfoPrefab();

            var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().First(g => g.GetComponent<Canvas>() != null);
            var menu = Object.FindFirstObjectByType<MenuManager>();
            EnsureInfoPanel((RectTransform)canvas.transform, menu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIBuilder] Info panel built and placed in the menu.");
        }

        // Applies the tab/page joint, the scrollbar and the score pill fitter to the
        // existing prefab and menu, keeping anything edited by hand (texts, links).
        [MenuItem("Tools/MindTiles/10 Update Info Panel And Score Pill")]
        public static void UpdateInfoPanelAndScorePill()
        {
            const string path = PrefabDir + "/InfoPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var card = root.transform.Find("Card");
            const float pageW = 800f;

            var infoTab = (RectTransform)card.Find("InfoTab");
            var howTab = (RectTransform)card.Find("HowToPlayTab");
            infoTab.sizeDelta = howTab.sizeDelta = new Vector2(pageW / 2f, infoTab.sizeDelta.y);
            infoTab.anchoredPosition = new Vector2(-pageW / 4f, infoTab.anchoredPosition.y);
            howTab.anchoredPosition = new Vector2(pageW / 4f, howTab.anchoredPosition.y);

            var page = (RectTransform)card.Find("Page");
            if (page.Find("TopFlat") == null) AddPageTopFlat(page);

            var scroll = page.GetComponentInChildren<ScrollRect>(true);
            if (scroll.verticalScrollbar == null)
            {
                scroll.viewport.offsetMax = new Vector2(-34f, 0f);
                AddScrollbar((RectTransform)scroll.transform, scroll);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var canvas = scene.GetRootGameObjects().First(g => g.GetComponent<Canvas>() != null);
            var pill = (RectTransform)canvas.GetComponentsInChildren<Transform>(true).First(t => t.name == "ScorePill");
            var menu = Object.FindFirstObjectByType<MenuManager>();
            var value = (TMP_Text)new SerializedObject(menu).FindProperty("scoreText").objectReferenceValue;
            EnsureScorePillFitter(pill, value);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIBuilder] Info panel and score pill updated.");
        }

        private static void BuildInfoPrefab()
        {
            var root = Overlay("InfoPanel", out RectTransform card, new Vector2(890f, 1450f));
            AddTitle(card, "Title", "MindTiles", 226f, 120f);
            var close = SquareIconButton(card, "CloseButton", KitIcon("cross"), new Vector2(-44f, -44f), 108f, 46f);

            const float pageW = 800f, tabH = 130f, tabTop = 330f, overlap = 4f;
            float pageTop = tabTop + tabH - overlap;
            const float pageH = 754f;

            // Tabs first, then the page on top of their bottom edge: the open tab and
            // the page are the same color, so they read as one folder.
            var infoTab = BuildTab(card, "InfoTab", tabTop + tabH / 2f, -pageW / 4f, KitIcon("info"), "Info");
            var howTab = BuildTab(card, "HowToPlayTab", tabTop + tabH / 2f, pageW / 4f, KitIcon("question"), "How to Play");

            var page = NewTop(card, "Page", pageTop + pageH / 2f, new Vector2(pageW, pageH));
            AddImage(page, S("ui_rounded.png"), PageColor, true).pixelsPerUnitMultiplier = 0.8f;
            AddPageTopFlat(page);

            var infoPage = NewRect("InfoPage", page);
            Stretch(infoPage);
            var info = BuildInfoPage(infoPage);

            var howPage = NewRect("HowToPlayPage", page);
            Stretch(howPage);
            var scroll = BuildHowToPage(howPage);

            var done = BigButton(card, "Done", 1325f, new Vector2(767f, 150f), S("btn_lavender.png"), Violet, "Done", TextDark, null, 72f);

            // Saved with the Info tab open (InfoPanel does the same at runtime).
            howPage.gameObject.SetActive(false);
            foreach (var g in infoTab.GetComponentsInChildren<Graphic>(true))
                g.color = g.name == "Bottom" || g.name == "Top" ? PageColor : Color.white;

            AddUISound(close.gameObject, Sfx.PanelClose);
            AddUISound(done.gameObject, Sfx.PanelClose);

            var panel = root.gameObject.AddComponent<InfoPanel>();
            var so = new SerializedObject(panel);
            var tabsProp = so.FindProperty("tabs");
            tabsProp.arraySize = 2;
            FillTab(tabsProp.GetArrayElementAtIndex(0), infoTab, infoPage.gameObject);
            FillTab(tabsProp.GetArrayElementAtIndex(1), howTab, howPage.gameObject);
            so.FindProperty("activeFill").colorValue = PageColor;
            so.FindProperty("inactiveFill").colorValue = TabInactive;
            so.FindProperty("activeContent").colorValue = Color.white;
            so.FindProperty("inactiveContent").colorValue = TabInactiveText;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("doneButton").objectReferenceValue = done;
            so.FindProperty("howToPlayScroll").objectReferenceValue = scroll;
            so.FindProperty("versionText").objectReferenceValue = info.version;
            so.FindProperty("emailText").objectReferenceValue = info.email;
            so.FindProperty("emailButton").objectReferenceValue = info.emailButton;
            so.FindProperty("instagramButton").objectReferenceValue = info.instagram;
            so.FindProperty("xButton").objectReferenceValue = info.x;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            SavePrefab(root.gameObject, "InfoPanel");
        }

        private static void AddUISound(GameObject target, Sfx sound)
        {
            var ui = target.AddComponent<UISound>();
            var so = new SerializedObject(ui);
            so.FindProperty("sound").enumValueIndex = (int)sound;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FillTab(SerializedProperty tab, RectTransform holder, GameObject page)
        {
            tab.FindPropertyRelative("button").objectReferenceValue = holder.GetComponent<Button>();
            tab.FindPropertyRelative("page").objectReferenceValue = page;

            Image[] fills = holder.Find("Top").GetComponents<Image>().Concat(holder.Find("Bottom").GetComponents<Image>()).ToArray();
            var fillsProp = tab.FindPropertyRelative("fills");
            fillsProp.arraySize = fills.Length;
            for (int i = 0; i < fills.Length; i++) fillsProp.GetArrayElementAtIndex(i).objectReferenceValue = fills[i];

            Graphic[] contents = holder.Find("Content").GetComponentsInChildren<Graphic>(true);
            var contentsProp = tab.FindPropertyRelative("contents");
            contentsProp.arraySize = contents.Length;
            for (int i = 0; i < contents.Length; i++) contentsProp.GetArrayElementAtIndex(i).objectReferenceValue = contents[i];
        }

        // One folder tab: rounded top, flat bottom (a plain rect hides the bottom corners).
        private static RectTransform BuildTab(RectTransform card, string name, float centerFromTop, float x, Sprite icon, string label)
        {
            var tab = NewTop(card, name, centerFromTop, new Vector2(400f, 130f));
            tab.anchoredPosition += new Vector2(x, 0f);

            var top = NewRect("Top", tab);
            Stretch(top);
            var topImg = AddImage(top, S("ui_rounded.png"), TabInactive, true);
            topImg.pixelsPerUnitMultiplier = 1.1f;
            topImg.raycastTarget = true;

            var bottom = NewRect("Bottom", tab);
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = new Vector2(0f, 60f);
            AddImage(bottom, null, TabInactive, false);

            IconLabel(tab, icon, label, fredoka, 46f, TabInactiveText, 0.85f);
            ((RectTransform)tab.Find("Content")).anchoredPosition = new Vector2(0f, 12f);

            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = topImg;
            button.transition = Selectable.Transition.None;
            return tab;
        }

        // Square top corners for the page: the tabs sit flush on it, so the open
        // tab and the page read as one piece.
        private static void AddPageTopFlat(RectTransform page)
        {
            var flat = NewRect("TopFlat", page);
            flat.anchorMin = new Vector2(0f, 1f);
            flat.anchorMax = new Vector2(1f, 1f);
            flat.pivot = new Vector2(0.5f, 1f);
            flat.anchoredPosition = Vector2.zero;
            flat.sizeDelta = new Vector2(0f, 70f);
            AddImage(flat, null, PageColor, false);
            flat.SetAsFirstSibling();
        }

        // Slim always-visible scrollbar on the right of the How to Play list.
        private static Scrollbar AddScrollbar(RectTransform scrollRt, ScrollRect scroll)
        {
            var bar = NewRect("Scrollbar", scrollRt);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.anchoredPosition = new Vector2(-6f, 0f);
            bar.sizeDelta = new Vector2(16f, -16f);
            var track = AddImage(bar, S("ui_pill.png"), TabInactive, true);
            track.pixelsPerUnitMultiplier = 4f;
            track.raycastTarget = true;

            var area = NewRect("SlidingArea", bar);
            Stretch(area);
            var handle = NewRect("Handle", area);
            Stretch(handle);
            var handleImg = AddImage(handle, S("ui_pill.png"), Lavender, true);
            handleImg.pixelsPerUnitMultiplier = 4f;
            handleImg.raycastTarget = true;

            var sb = bar.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle;
            sb.targetGraphic = handleImg;
            sb.direction = Scrollbar.Direction.BottomToTop;

            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return sb;
        }

        private struct InfoPageRefs
        {
            public TMP_Text version, email;
            public Button emailButton, instagram, x;
        }

        private static InfoPageRefs BuildInfoPage(RectTransform page)
        {
            var refs = new InfoPageRefs();

            AddText(NewTop(page, "DevelopedBy", 62f, new Vector2(700f, 50f)), "Developed by", nunito, 36f, Subtle, TextAlignmentOptions.Center);
            AddText(NewTop(page, "Studio", 138f, new Vector2(760f, 110f)), "PAKUE GAMES", fredoka, 84f, Peach, TextAlignmentOptions.Center);

            var pill = NewTop(page, "VersionPill", 232f, new Vector2(300f, 76f));
            AddImage(pill, S("ui_pill.png"), LinkFill, true).pixelsPerUnitMultiplier = 0.9f;
            var pillBorder = NewRect("Border", pill);
            Stretch(pillBorder);
            AddImage(pillBorder, S("ui_outline.png"), CardBorder, true).pixelsPerUnitMultiplier = 1.2f;
            refs.version = AddText(NewRect("Text", pill), "Version 1.0", nunito, 38f, Color.white, TextAlignmentOptions.Center);
            Stretch(refs.version.rectTransform);

            var divider = NewTop(page, "Divider", 306f, new Vector2(680f, 4f));
            AddImage(divider, null, CardBorder, false);

            var contact = AddText(NewTop(page, "ContactLabel", 358f, new Vector2(700f, 44f)), "CONTACT", nunito, 30f, Subtle, TextAlignmentOptions.Center);
            contact.characterSpacing = 12f;
            var mail = LinkButton(page, "EmailButton", 440f, 0f, new Vector2(700f, 116f), KitIcon("mail"), Pink,
                "pakuegames@gmail.com", 480f, out refs.email);
            refs.emailButton = mail;

            var follow = AddText(NewTop(page, "FollowLabel", 546f, new Vector2(700f, 44f)), "FOLLOW US", nunito, 30f, Subtle, TextAlignmentOptions.Center);
            follow.characterSpacing = 12f;
            refs.instagram = LinkButton(page, "InstagramButton", 628f, -182f, new Vector2(336f, 116f), S("icon_instagram.png"), Lavender,
                "Instagram", 200f, out _);
            refs.x = LinkButton(page, "XButton", 628f, 182f, new Vector2(336f, 116f), S("icon_x.png"), Peach,
                "X", 200f, out _);

            AddText(NewTop(page, "Copyright", 712f, new Vector2(700f, 40f)), "© 2026 Pakue Games", nunito, 28f, TabInactiveText, TextAlignmentOptions.Center);
            return refs;
        }

        // Dark rounded button with a pastel icon box on the left and a label.
        private static Button LinkButton(RectTransform parent, string name, float fromTop, float x, Vector2 size,
            Sprite icon, Color boxColor, string label, float labelWidth, out TMP_Text labelText)
        {
            var rt = NewTop(parent, name, fromTop, size);
            rt.anchoredPosition += new Vector2(x, 0f);
            var bg = AddImage(rt, S("ui_rounded.png"), LinkFill, true);
            bg.pixelsPerUnitMultiplier = 1.1f;
            bg.raycastTarget = true;
            var border = NewRect("Border", rt);
            Stretch(border);
            AddImage(border, S("ui_outline.png"), CardBorder, true).pixelsPerUnitMultiplier = 1.1f;

            float box = size.y - 36f;
            var boxRt = NewRect("IconBox", rt);
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0f, 0.5f);
            boxRt.pivot = new Vector2(0f, 0.5f);
            boxRt.anchoredPosition = new Vector2(18f, 0f);
            boxRt.sizeDelta = new Vector2(box, box);
            AddImage(boxRt, S("ui_rounded.png"), boxColor, true).pixelsPerUnitMultiplier = 1.6f;
            var ic = NewRect("Icon", boxRt);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(box * 0.58f, box * 0.58f);
            AddImage(ic, icon, TextPlum, false).preserveAspect = true;

            var lbl = NewRect("Label", rt);
            lbl.anchorMin = lbl.anchorMax = new Vector2(0f, 0.5f);
            lbl.pivot = new Vector2(0f, 0.5f);
            lbl.anchoredPosition = new Vector2(18f + box + 22f, 0f);
            lbl.sizeDelta = new Vector2(labelWidth, 70f);
            labelText = AddText(lbl, label, nunito, 40f, Color.white, TextAlignmentOptions.Left);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            return button;
        }

        private static ScrollRect BuildHowToPage(RectTransform page)
        {
            var scrollRt = NewRect("Scroll", page);
            Stretch(scrollRt);
            scrollRt.offsetMin = new Vector2(16f, 20f);
            scrollRt.offsetMax = new Vector2(-16f, -20f);

            var viewport = NewRect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.offsetMax = new Vector2(-34f, 0f);   // room for the scrollbar
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = AddImage(viewport, null, new Color(0f, 0f, 0f, 0f), false);   // lets drags start anywhere
            hit.raycastTarget = true;

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 34f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Color[] colors = { Pink, Peach, Lavender };
            for (int i = 0; i < HowToPlay.Length; i++)
                HowToRow(content, i + 1, colors[i % colors.Length], HowToPlay[i][0], HowToPlay[i][1]);

            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 50f;
            AddScrollbar(scrollRt, scroll);
            return scroll;
        }

        private static void HowToRow(RectTransform content, int number, Color color, string title, string body)
        {
            var row = NewRect("Step" + number, content);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 26f;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var badge = NewRect("Number", row);
            AddImage(badge, S("ui_circle.png"), color, false);
            var le = badge.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 84f;
            le.minHeight = le.preferredHeight = 84f;
            var num = NewRect("Text", badge);
            Stretch(num);
            AddText(num, number.ToString(), fredoka, 46f, TextPlum, TextAlignmentOptions.Center);

            var col = NewRect("Text", row);
            var cle = col.gameObject.AddComponent<LayoutElement>();
            cle.flexibleWidth = 1f;
            cle.minWidth = 0f;
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var t = AddText(NewRect("Title", col), title, fredoka, 44f, Color.white, TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.Normal;
            var b = AddText(NewRect("Body", col), body, nunito, 34f, Subtle, TextAlignmentOptions.Left);
            b.textWrappingMode = TextWrappingModes.Normal;
        }

        // Replaces the old empty info panel with the prefab and points MenuManager at it.
        private static void EnsureInfoPanel(RectTransform canvasRt, MenuManager menu)
        {
            foreach (var t in canvasRt.GetComponentsInChildren<Transform>(true).Where(t => t.name == "infopanel").ToList())
                Object.DestroyImmediate(t.gameObject);

            Transform instance = canvasRt.Find("InfoPanel");
            if (instance == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/InfoPanel.prefab");
                instance = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasRt)).transform;
                Stretch((RectTransform)instance);
                instance.gameObject.SetActive(false);
            }

            var mso = new SerializedObject(menu);
            mso.FindProperty("infoPanel").objectReferenceValue = instance.gameObject;
            mso.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
