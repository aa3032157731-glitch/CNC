using System;
using System.Collections.Generic;
using System.IO;
using CNC.DigitalTwin;
using ECM.Components;
using ECM.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

namespace CNC.EditorTools
{
    public static class Step3_BuildFrontendScenes
    {
        private const string ConfigsDir = "Assets/_Project/Configs";
        private const string StyleADir = "Assets/_Project/UI/Sprites/StyleA";
        private const string LegacySpritesDir = "Assets/_Project/UI/Sprites";
        private const string FontsDir = "Assets/_Project/UI/Fonts";

        private static TMP_FontAsset _fontReg;
        private static TMP_FontAsset _fontBold;

        // Style A VisionOS Spatial Frosted Glass Sprites
        private static Sprite _vaCardGlass;
        private static Sprite _vaPillGlass;
        private static Sprite _vaSubcard;
        private static Sprite _vaPillActiveWhite;
        private static Sprite _vaPillActiveSky;
        private static Sprite _vaPillIdle;
        private static Sprite _vaPillGhost;
        private static Sprite _vaPillGreen;
        private static Sprite _vaPillDanger;
        private static Sprite _vaCircleBtn;
        private static Sprite _vaWorldPill;
        private static Sprite _vaBarPill;
        private static Sprite _vaSlotTile;
        private static Sprite _spDot;
        private static Sprite _spCrosshair;

        // Style A VisionOS Palette
        private static readonly Color ColWhite = new Color(0.97f, 0.98f, 1.0f, 1f);
        private static readonly Color ColDarkText = new Color(0.06f, 0.09f, 0.16f, 1f);
        private static readonly Color ColSubText = new Color(0.80f, 0.85f, 0.92f, 0.78f);
        private static readonly Color ColSky = new Color(0.22f, 0.74f, 0.97f, 1f);
        private static readonly Color ColSkySoft = new Color(0.49f, 0.83f, 0.99f, 1f);
        private static readonly Color ColMint = new Color(0.20f, 0.83f, 0.60f, 1f);
        private static readonly Color ColAmber = new Color(0.98f, 0.74f, 0.14f, 1f);
        private static readonly Color ColTrack = new Color(1.0f, 1.0f, 1.0f, 0.14f);
        private static readonly Color ColSep = new Color(1.0f, 1.0f, 1.0f, 0.16f);

        [MenuItem("CNC Optimize/Apply Style A (VisionOS) & Fix Smoke + Cameras + Walk")]
        public static void Run()
        {
            Debug.Log("[CNC StyleA] Applying Style A (VisionOS Frosted Glass) + Scene Fixes (Smoke, Cameras, Default Walk)...");
            Directory.CreateDirectory(ConfigsDir);

            ConfigureStyleASprites();
            LoadUiAssets();

            var catalog = CreateOrUpdateWorkshopCatalog();
            BuildPlayStartSceneStyleA();
            BuildSampleSceneStyleAAndFixes(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CNC StyleA] Style A Frontend & Scene Fixes completed cleanly!");
        }

        private static void ConfigureStyleASprites()
        {
            if (!Directory.Exists(StyleADir)) return;
            foreach (var f in Directory.GetFiles(StyleADir, "*.png"))
            {
                string assetPath = f.Replace("\\", "/");
                var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti == null) continue;

                string name = Path.GetFileNameWithoutExtension(assetPath);
                if (name == "va_holo_ring")
                {
                    ti.textureType = TextureImporterType.Default;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.textureCompression = TextureImporterCompression.CompressedHQ;
                    ti.SaveAndReimport();
                    continue;
                }

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;

                if (name == "va_card_glass") ti.spriteBorder = new Vector4(28, 28, 28, 28);
                else if (name == "va_subcard") ti.spriteBorder = new Vector4(18, 18, 18, 18);
                else if (name == "va_slot_tile") ti.spriteBorder = new Vector4(10, 10, 10, 10);
                // Horizontal-only 9-slicing on exact-height capsules => mathematically perfect circular pill ends!
                else if (name == "va_pill_glass") ti.spriteBorder = new Vector4(28, 0, 28, 0);
                else if (name.StartsWith("va_pill_")) ti.spriteBorder = new Vector4(21, 0, 21, 0);
                else if (name.StartsWith("va_cta_")) ti.spriteBorder = new Vector4(32, 0, 32, 0);
                else if (name == "va_world_pill") ti.spriteBorder = new Vector4(38, 0, 38, 0);
                else if (name == "va_bar_pill") ti.spriteBorder = new Vector4(6, 0, 6, 0);

                ti.SaveAndReimport();
            }
        }

        private static void LoadUiAssets()
        {
            _fontReg = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/CNC_Sans_Regular_SDF.asset");
            _fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/CNC_Sans_Bold_SDF.asset") ?? _fontReg;

            _vaCardGlass       = LoadSp($"{StyleADir}/va_card_glass.png");
            _vaPillGlass       = LoadSp($"{StyleADir}/va_pill_glass.png");
            _vaSubcard         = LoadSp($"{StyleADir}/va_subcard.png");
            _vaPillActiveWhite = LoadSp($"{StyleADir}/va_pill_active_white.png");
            _vaPillActiveSky   = LoadSp($"{StyleADir}/va_pill_active_sky.png");
            _vaPillIdle        = LoadSp($"{StyleADir}/va_pill_idle.png");
            _vaPillGhost       = LoadSp($"{StyleADir}/va_pill_ghost.png");
            _vaPillGreen       = LoadSp($"{StyleADir}/va_pill_green.png");
            _vaPillDanger      = LoadSp($"{StyleADir}/va_pill_danger.png");
            _vaCircleBtn       = LoadSp($"{StyleADir}/va_circle_btn.png");
            _vaWorldPill       = LoadSp($"{StyleADir}/va_world_pill.png");
            _vaBarPill         = LoadSp($"{StyleADir}/va_bar_pill.png");
            _vaSlotTile        = LoadSp($"{StyleADir}/va_slot_tile.png");
            _spDot             = LoadSp($"{LegacySpritesDir}/ui_circle_dot.png");
            _spCrosshair       = LoadSp($"{LegacySpritesDir}/ui_crosshair.png");
        }

        private static Sprite LoadSp(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static WorkshopCatalog CreateOrUpdateWorkshopCatalog()
        {
            string path = $"{ConfigsDir}/WorkshopCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<WorkshopCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WorkshopCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            catalog.stations = WorkshopCatalog.CreateDefaultStations();
            foreach (var st in catalog.stations)
            {
                if (!string.IsNullOrEmpty(st.previewSpritePath))
                    st.previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(st.previewSpritePath);
            }
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ==================== A. STYLE A START SCREEN (PLAY.UNITY) ====================

        private static void BuildPlayStartSceneStyleA()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);

            var cam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.08f, 0.13f, 1f);
                var uac = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                uac.renderPostProcessing = false;
            }

            foreach (var oldCanvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                UnityEngine.Object.DestroyImmediate(oldCanvas.gameObject);

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = CreateResponsiveCanvas("StartScreen_Canvas", 0);
            var ctrl = canvasGo.AddComponent<StartScreenController>();

            // 1. Clean Full-Bleed Hero Backdrop
            var bgGo = CreateUiRect("HeroBackground", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bgGo.AddComponent<UIImage>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Scenes/Play/摄图网_600865141_工业棉纱纺织工厂(仅交流学习使用).jpg");
            bgImg.color = new Color(0.45f, 0.56f, 0.70f, 1f);
            var arf = bgGo.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = 1.5f;
            ctrl.heroBackground = bgGo.GetComponent<RectTransform>();

            // Soft deep slate veil
            var scrim = CreateUiRect("SlateVeil", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrim.AddComponent<UIImage>().color = new Color(0.04f, 0.07f, 0.12f, 0.62f);

            // 2. Minimal Top Floating Brand Pill
            var topPill = CreateUiRect("TopBrandPill", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-240, -76), new Vector2(240, -28));
            AddSliced(topPill, _vaPillGlass, Color.white);
            CreateTmp("TopLabel", topPill.transform, "<color=#34D399>●</color>   SMART TEXTILE FACTORY  ·  DIGITAL TWIN v2.0", 13, true, ColSubText,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0));

            // 3. Center VisionOS Frosted Glass Hero Card (860 x 490)
            var hero = CreateUiRect("HeroGlassCard", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-430, -255), new Vector2(430, 235));
            AddSliced(hero, _vaCardGlass, Color.white);

            // Subtle sky eyebrow pill
            var tagPill = CreateUiRect("EyebrowPill", hero.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-130, -68), new Vector2(130, -38));
            AddSliced(tagPill, _vaPillActiveSky, Color.white);
            CreateTmp("EyebrowTxt", tagPill.transform, "URP 高精度三维仿真平台", 13, true, ColSkySoft,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Airy clean headline & subtitle
            CreateTmp("MainTitle", hero.transform, "棉纺智能工厂三维数字孪生与实训平台", 38, true, ColWhite,
                TextAlignmentOptions.Center, new Vector2(0, 1), new Vector2(1, 1), new Vector2(44, -142), new Vector2(-44, -82));

            CreateTmp("SubTitle", hero.transform, "清花抓棉 · 除杂梳棉 · 精梳并条 · 粗细纱络筒 · 视觉分拣与立体仓储", 16, false, ColSubText,
                TextAlignmentOptions.Center, new Vector2(0, 1), new Vector2(1, 1), new Vector2(44, -182), new Vector2(-44, -146));

            // 3 Minimal Translucent Subcards
            string[] ft = { "自由漫游与监控", "交互式工艺实训", "URP 现代工业渲染" };
            string[] fd = { "第一人称行走 / 鸟瞰屋顶透视", "9 大工序原理解析与随堂考核", "高帧率 LOD 合批 / PBR 光影" };
            for (int i = 0; i < 3; i++)
            {
                float x0 = 44 + i * 262;
                var sc = CreateUiRect($"Feature_{i}", hero.transform, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(x0, -290), new Vector2(x0 + 248, -206));
                AddSliced(sc, _vaSubcard, Color.white);
                CreateTmp("T", sc.transform, ft[i], 15, true, ColWhite, TextAlignmentOptions.Center,
                    new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(8, 2), new Vector2(-8, -10));
                CreateTmp("D", sc.transform, fd[i], 12, false, ColSubText, TextAlignmentOptions.Center,
                    new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(8, 10), new Vector2(-8, 2));
            }

            // 2 Clean VisionOS Pill CTAs (using exact 60px height capsule sprites)
            var ctaWhite = LoadSp($"{StyleADir}/va_cta_white.png") ?? _vaPillActiveWhite;
            var ctaSky   = LoadSp($"{StyleADir}/va_cta_sky.png")   ?? _vaPillActiveSky;
            ctrl.btnEnterDashboard = CreateStyleAButton("BtnEnterDashboard", hero.transform, "进入工厂漫游与监控", 18, true,
                ctaWhite, ColDarkText, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-386, 108), new Vector2(-14, 168));

            ctrl.btnEnterTeaching = CreateStyleAButton("BtnEnterTeaching", hero.transform, "进入工艺实训导览", 18, true,
                ctaSky, ColWhite, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(14, 108), new Vector2(386, 168));

            // 2 Minimal Secondary Pills
            ctrl.btnOpenSettings = CreateStyleAButton("BtnSettings", hero.transform, "画质与操控设置", 14, false,
                _vaPillIdle, ColSubText, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-190, 38), new Vector2(-10, 82));

            ctrl.btnQuit = CreateStyleAButton("BtnQuit", hero.transform, "退出系统", 14, false,
                _vaPillDanger, new Color(0.99f, 0.72f, 0.72f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(10, 38), new Vector2(190, 82));

            // Subtle Footer Caption
            CreateTmp("FooterHint", canvasGo.transform, "WASD 自由漫游  ·  鼠标右键旋转视角  ·  F1 鸟瞰全景  ·  F4 屋顶透视", 13, false,
                new Color(0.85f, 0.90f, 0.96f, 0.65f), TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(0, 48));

            // Settings Modal
            ctrl.spPillActiveWhite = _vaPillActiveWhite;
            ctrl.spPillIdle = _vaPillIdle;
            ctrl.settingsModal = BuildStyleASettingsModal(canvasGo.transform, out ctrl.btnQualityMed, out ctrl.btnQualityHigh,
                out ctrl.btnQualityUltra, out ctrl.sensitivitySlider, out ctrl.sensitivityValueText, out ctrl.btnCloseSettings);

            // Async Loading Overlay
            var loadOverlay = CreateUiRect("LoadingOverlay", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            loadOverlay.AddComponent<UIImage>().color = new Color(0.03f, 0.06f, 0.11f, 0.88f);
            var loadCard = CreateUiRect("LoadCard", loadOverlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-320, -90), new Vector2(320, 90));
            AddSliced(loadCard, _vaCardGlass, Color.white);
            CreateTmp("LoadTitle", loadCard.transform, "正在进入棉纺智能工厂...", 20, true, ColWhite, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -56), new Vector2(-24, -20));

            var track = CreateUiRect("Track", loadCard.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(40, -6), new Vector2(-96, 6));
            AddSliced(track, _vaBarPill, ColTrack);
            ctrl.loadingBarFill = CreateBarFill(track.transform, ColSky, 0f);
            ctrl.loadingPercentText = CreateTmp("Pct", loadCard.transform, "0%", 16, true, ColSkySoft, TextAlignmentOptions.MidlineRight,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-92, -16), new Vector2(-36, 16));
            ctrl.loadingTipText = CreateTmp("Tip", loadCard.transform, "准备场景中...", 13, false, ColSubText, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(28, 22), new Vector2(-28, 56));
            loadOverlay.SetActive(false);
            ctrl.loadingOverlay = loadOverlay;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[CNC StyleA] Built Style A VisionOS Start Screen (Play.unity).");
        }

        // ==================== B. STYLE A MAIN SCENE + ALL 3 SCENE BUGFIXES ====================

        private static void BuildSampleSceneStyleAAndFixes(WorkshopCatalog catalog)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            // FIX 1: Eliminate black smoke particle clumps in Image 2!
            FixBlackSmokeParticles();

            // FIX 2: Fix floor holographic ring material (clean URP Unlit Transparent ring, zero square glare!)
            FixFloorHoloRingMaterial();

            // FIX 3: Solidify zero-thickness floors (7.8e-7m -> 2.0m slabs), add safety sub-floors,
            // fix ALL negative-scale / negative-size BoxColliders (0 console warnings!), & hide stray below-floor cylinder!
            FixFloorsNegativeCollidersAndStrayObjects();

            // FIX 4: Place ECM_FirstPerson cleanly grounded on Workshop 1 floor (Y=1.18m) with preserved 4.8m eye height!
            var fpsRoot = GameObject.Find("ECM_FirstPerson");
            if (fpsRoot != null)
            {
                fpsRoot.transform.position = new Vector3(198f, 1.18f, 162f);
                fpsRoot.transform.rotation = Quaternion.Euler(0f, -122f, 0f);
                var pivot = fpsRoot.transform.Find("Camera_Pivot");
                if (pivot != null)
                {
                    pivot.localPosition = Vector3.zero;
                    pivot.localRotation = Quaternion.identity;
                    pivot.localScale = Vector3.one;
                }
            }

            // Disable old legacy canvases
            foreach (var cname in new[] { "Canvas (1)", "UI", "Canvas" })
            {
                var go = GameObject.Find(cname);
                if (go != null) go.SetActive(false);
            }

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            foreach (var oldName in new[] { "__DigitalTwinSystem", "HUD_Canvas", "WorldMarkers_Root", "__StationClickVolumes", "__StationBeacons" })
            {
                var old = GameObject.Find(oldName);
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            }

            // Rebuild core system managers
            var sysRoot = new GameObject("__DigitalTwinSystem");
            var simState = sysRoot.AddComponent<SimulationState>();
            simState.catalog = catalog;

            var camRig = sysRoot.AddComponent<CameraRigManager>();
            var mainCam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (mainCam != null) mainCam.tag = "MainCamera";
            camRig.mainCamera = mainCam;
            camRig.fpsPlayerRoot = fpsRoot;
            camRig.fpsCameraLocalOffset = new Vector3(0f, 4.80f, 0f);
            camRig.hall1FloorY = 1.10f;
            camRig.hall2FloorY = 3.80f;
            if (fpsRoot != null)
            {
                camRig.fpsController = fpsRoot.GetComponent<BaseFirstPersonController>();
                camRig.fpsMouseLook = fpsRoot.GetComponent<MouseLook>();
                camRig.fpsCameraAnchor = fpsRoot.transform.Find("Camera_Pivot") ?? (mainCam != null ? mainCam.transform.parent : null);
                if (mainCam != null && camRig.fpsCameraAnchor != null)
                {
                    mainCam.transform.SetParent(camRig.fpsCameraAnchor, false);
                    mainCam.transform.localPosition = camRig.fpsCameraLocalOffset;
                    mainCam.transform.localRotation = Quaternion.identity;
                }
            }
            CollectRoofCutawayObjects(camRig);

            var raycaster = sysRoot.AddComponent<MachineRaycaster>();
            raycaster.targetCamera = mainCam;
            raycaster.hoverHighlightMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_HighlightHover.mat");
            raycaster.selectedHighlightMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_HighlightSelected.mat");

            var animator = sysRoot.AddComponent<WorkshopAnimator>();
            WireWorkshopAnimator(animator);

            // Rebuild station beacons, click volumes & sleek Style A 3D floating pills at calibrated positions!
            RebuildStationBeacons(catalog);
            SetupStationInteractables(catalog);
            BuildStyleAWorldMarkers(catalog);

            // Build exact Style A VisionOS Frosted Glass HUD!
            BuildStyleAHud(catalog);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[CNC StyleA] Built Style A VisionOS HUD + Fixed Smoke Particles + Calibrated All 9 Cameras!");
        }

        private static void FixBlackSmokeParticles()
        {
            Shader urpPartUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpPartUnlit == null) urpPartUnlit = Shader.Find("Universal Render Pipeline/Unlit");

            var smokeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Other/Smoke/Smoke0391_L.jpg");
            string smokeMatPath = "Assets/Other/Smoke/Smoke.mat";
            var smokeMat = AssetDatabase.LoadAssetAtPath<Material>(smokeMatPath);
            if (smokeMat == null)
            {
                smokeMat = new Material(urpPartUnlit);
                AssetDatabase.CreateAsset(smokeMat, smokeMatPath);
            }

            smokeMat.shader = urpPartUnlit;
            smokeMat.SetFloat("_Surface", 1f);        // Transparent
            smokeMat.SetFloat("_Blend", 2f);          // Additive -> black JPG background becomes 100% invisible!
            smokeMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            smokeMat.SetInt("_DstBlend", (int)BlendMode.One);
            smokeMat.SetInt("_ZWrite", 0);
            smokeMat.renderQueue = (int)RenderQueue.Transparent;
            smokeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            smokeMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            if (smokeTex != null) smokeMat.SetTexture("_BaseMap", smokeTex);
            smokeMat.SetColor("_BaseColor", new Color(0.92f, 0.95f, 1.0f, 0.24f)); // Soft airy cotton mist
            smokeMat.enableInstancing = false;
            EditorUtility.SetDirty(smokeMat);

            // Reassign repaired additive mist material onto ALL ParticleSystemRenderers in SampleScene!
            int fixedCount = 0;
            foreach (var psr in UnityEngine.Object.FindObjectsOfType<ParticleSystemRenderer>(true))
            {
                psr.sharedMaterial = smokeMat;
                psr.shadowCastingMode = ShadowCastingMode.Off;
                psr.receiveShadows = false;
                fixedCount++;
            }
            Debug.Log($"[CNC StyleA] Fixed {fixedCount} ParticleSystemRenderers -> URP/Particles/Unlit Additive (eliminated black smoke clumps!).");
        }

        private static void FixFloorHoloRingMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_HoloFloorPad.mat");
            var ringTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{StyleADir}/va_holo_ring.png");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (mat != null && unlit != null)
            {
                mat.shader = unlit;
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetTexture("_BaseMap", ringTex);
                mat.SetColor("_BaseColor", new Color(0.45f, 0.85f, 1.0f, 0.72f));
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);

                // Ensure ALL 7 floor UI trigger pads (including 除杂、梳棉、卷棉UI & 抓棉机UI) use the transparent holo ring!
                foreach (var pname in new[] { "抓棉机UI", "除杂、梳棉、卷棉UI", "末并", "精梳", "粗纱", "细纱", "络筒" })
                {
                    var go = GameObject.Find(pname);
                    if (go == null) continue;
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        mr.sharedMaterial = mat;
                        mr.shadowCastingMode = ShadowCastingMode.Off;
                        mr.receiveShadows = false;
                    }
                }
            }

            // Fix Workshop 2 hanging LED signboards so "智能视觉分拣区" renders on one crisp balanced line (no orphan wrap!)
            foreach (var tmp in UnityEngine.Object.FindObjectsOfType<TextMeshPro>(true))
            {
                if (tmp.text != null && tmp.text.Contains("智能视觉检测分拣区"))
                {
                    tmp.text = "智能视觉分拣区";
                    tmp.enableWordWrapping = false;
                    EditorUtility.SetDirty(tmp);
                }
            }
        }

        private static void FixFloorsNegativeCollidersAndStrayObjects()
        {
            // 1. Thicken both Bldg_Floor_001 BoxColliders from 0.00000078m to solid 2.0m downward slabs (flush at top visual floor!)
            int thickenedFloors = 0;
            foreach (var bname in new[] { "Building", "Building (1)" })
            {
                var b = GameObject.Find(bname);
                if (b == null) continue;
                var floorTr = b.transform.Find("Bldg_Floor_001");
                if (floorTr == null) continue;
                var bc = floorTr.GetComponent<BoxCollider>();
                if (bc == null) bc = floorTr.gameObject.AddComponent<BoxCollider>();
                float sy = Mathf.Max(0.01f, Mathf.Abs(floorTr.lossyScale.y));
                float localThick = 2.0f / sy; // Exact 2.0m solid slab in world space
                var sz = bc.size;
                sz.x = Mathf.Abs(sz.x);
                sz.y = localThick;
                sz.z = Mathf.Abs(sz.z);
                bc.size = sz;
                var c = bc.center;
                c.y = -localThick * 0.5f; // Top surface sits mathematically flush at local Y = 0 (world 1.10 / 3.80)
                bc.center = c;
                bc.isTrigger = false;
                bc.enabled = true;
                thickenedFloors++;
            }

            // 2. Build continuous rock-solid safety sub-floors for Workshop 1 & Workshop 2 (impossible to fall into skybox!)
            var oldSafety = GameObject.Find("__SafetyColliders");
            if (oldSafety != null) UnityEngine.Object.DestroyImmediate(oldSafety);
            var safetyRoot = new GameObject("__SafetyColliders");

            void AddSafetySlab(string slabName, Vector3 center, Vector3 size)
            {
                var slab = new GameObject(slabName);
                slab.layer = 0; // Default layer (matches GroundDetection _groundMask = 1)
                slab.transform.SetParent(safetyRoot.transform, false);
                slab.transform.position = center;
                var box = slab.AddComponent<BoxCollider>();
                box.size = size;
                box.isTrigger = false;
            }

            // Workshop 1 visual floor: X=[-67.14, 215.26], Y=1.10, Z=[-40.88, 315.75] -> top at Y=1.08
            AddSafetySlab("SafetyFloor_Workshop1", new Vector3(74.0f, -0.92f, 137.4f), new Vector3(340f, 4.0f, 420f));
            // Workshop 2 visual floor: X=[0.60, 76.87], Y=3.80, Z=[345.94, 450.38] -> top at Y=3.78
            AddSafetySlab("SafetyFloor_Workshop2", new Vector3(38.7f, 1.78f, 398.2f), new Vector3(120f, 4.0f, 150f));

            // Preserve truck solid collision on a positive-scale proxy before removing negative-scale BoxColliders
            var truck = GameObject.Find("货车");
            if (truck != null)
            {
                var tmr = truck.GetComponent<MeshRenderer>();
                if (tmr != null)
                    AddSafetySlab("SafetyProxy_Truck", tmr.bounds.center, tmr.bounds.size);
            }

            // 3. Fix ALL negative-size & negative-scale BoxColliders across entire scene (ZERO PhysX warnings!)
            int fixedNegSize = 0;
            int removedNegScale = 0;
            foreach (var bc in UnityEngine.Object.FindObjectsOfType<BoxCollider>(true))
            {
                if (bc == null) continue;
                Vector3 ls = bc.transform.lossyScale;
                if (ls.x < 0f || ls.y < 0f || ls.z < 0f)
                {
                    UnityEngine.Object.DestroyImmediate(bc);
                    removedNegScale++;
                    continue;
                }
                Vector3 sz = bc.size;
                if (sz.x < 0f || sz.y < 0f || sz.z < 0f)
                {
                    bc.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
                    fixedNegSize++;
                }
            }

            // 4. Hide stray below-floor cotton roll 'chun' (pos Y = -10.15)
            var chun = GameObject.Find("chun");
            if (chun != null) chun.SetActive(false);

            Debug.Log($"[CNC StyleA] Physics & Floor hardening: thickenedFloors={thickenedFloors}, fixedNegSize={fixedNegSize}, removedNegScale={removedNegScale}, hid stray below-floor 'chun'.");
        }

        private static void RebuildStationBeacons(WorkshopCatalog catalog)
        {
            var matMetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_BeaconMetal.mat");
            var matLens = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_BeaconLens.mat");
            var root = new GameObject("__StationBeacons");

            foreach (var st in catalog.stations)
            {
                var beaconGo = new GameObject($"Beacon_{st.code}_{st.shortName}");
                beaconGo.transform.SetParent(root.transform, false);
                // Place ST-09 beacon cleanly beside the left inspection line instead of behind center LED sign
                Vector3 bPos = st.index == 8
                    ? new Vector3(26.5f, 8.2f, 404.0f)
                    : new Vector3(st.worldPosition.x, st.worldPosition.y + 4.4f, st.worldPosition.z);
                beaconGo.transform.position = bPos;

                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(beaconGo.transform, false);
                pole.transform.localPosition = new Vector3(0f, -1.8f, 0f);
                pole.transform.localScale = new Vector3(0.15f, 1.8f, 0.15f);
                if (matMetal != null) pole.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
                UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());

                var green = CreateBeaconRing(beaconGo.transform, "GreenLens", 0.30f, matLens);
                var amber = CreateBeaconRing(beaconGo.transform, "AmberLens", 0.72f, matLens);
                var red   = CreateBeaconRing(beaconGo.transform, "RedLens",   1.14f, matLens);

                var comp = beaconGo.AddComponent<MachineStatusBeacon>();
                comp.stationIndex = st.index;
                comp.greenLens = green;
                comp.amberLens = amber;
                comp.redLens = red;
            }
        }

        private static MeshRenderer CreateBeaconRing(Transform parent, string name, float localY, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, localY, 0f);
            go.transform.localScale = new Vector3(0.45f, 0.18f, 0.45f);
            var mr = go.GetComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return mr;
        }

        private static void CollectRoofCutawayObjects(CameraRigManager camRig)
        {
            camRig.workshop1RoofObjects.Clear();
            camRig.workshop2RoofObjects.Clear();
            var b1 = GameObject.Find("Building");
            if (b1 != null)
            {
                foreach (var mr in b1.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string nl = mr.gameObject.name.ToLowerInvariant();
                    if (mr.bounds.center.y > 24.0f || nl.Contains("ceiling") || nl.Contains("roof") || nl.Contains("truss") || nl.Contains("skylight"))
                        camRig.workshop1RoofObjects.Add(mr.gameObject);
                }
            }
            var b2 = GameObject.Find("Building (1)");
            if (b2 != null)
            {
                foreach (var mr in b2.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string nl = mr.gameObject.name.ToLowerInvariant();
                    if (mr.bounds.center.y > 16.5f || nl.Contains("ceiling") || nl.Contains("roof") || nl.Contains("truss"))
                        camRig.workshop2RoofObjects.Add(mr.gameObject);
                }
            }
        }

        private static void WireWorkshopAnimator(WorkshopAnimator anim)
        {
            int idx = 0;
            foreach (var tr in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            {
                if (tr.parent == null && tr.name.StartsWith("agv"))
                {
                    anim.agvItems.Add(new WorkshopAnimator.AgvTrackItem
                    {
                        agvTransform = tr,
                        homePos = tr.position,
                        homeRot = tr.rotation,
                        phaseOffset = idx * 0.95f,
                        patrolLength = 22f
                    });
                    idx++;
                }
            }
            var armRoot = GameObject.Find("__InspectionArms");
            if (armRoot != null)
                foreach (Transform child in armRoot.transform) anim.inspectionArms.Add(child);
            foreach (var pname in new[] { "末并", "精梳", "粗纱", "细纱", "络筒" })
            {
                var go = GameObject.Find(pname);
                if (go == null) continue;
                // Rotate the actual floor ring mesh child around its own center — NOT the offset parent!
                var childMr = go.GetComponentInChildren<MeshRenderer>(true);
                anim.floorRingPads.Add(childMr != null ? childMr.transform : go.transform);
            }
        }

        private static void SetupStationInteractables(WorkshopCatalog catalog)
        {
            var volRoot = new GameObject("__StationClickVolumes");
            foreach (var st in catalog.stations)
            {
                GameObject targetGo = null;
                if (st.sceneTargetNames != null)
                {
                    foreach (var n in st.sceneTargetNames)
                    {
                        var found = GameObject.Find(n);
                        if (found != null && found.GetComponentsInChildren<MeshRenderer>(true).Length > 0)
                        {
                            targetGo = found;
                            break;
                        }
                    }
                }
                var clickVol = new GameObject($"Interact_{st.code}_{st.shortName}");
                clickVol.transform.SetParent(volRoot.transform, false);
                clickVol.transform.position = st.worldPosition;
                var bc = clickVol.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                // Tight machine-hugging click volume so it never spills into the entrance walking aisle
                bc.size = st.index == 8 ? new Vector3(36f, 13f, 42f) : new Vector3(14f, 11f, 18f);

                var inter = clickVol.AddComponent<MachineInteractable>();
                inter.stationIndex = st.index;
                inter.displayName = st.name;
                inter.highlightRoot = targetGo != null ? targetGo.transform : clickVol.transform;
            }
        }

        private static void BuildStyleAWorldMarkers(WorkshopCatalog catalog)
        {
            var root = new GameObject("WorldMarkers_Root");
            foreach (var st in catalog.stations)
            {
                var go = new GameObject($"Marker_{st.code}_{st.shortName}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = st.worldPosition + new Vector3(0f, 8.6f, 0f);
                Vector3 toFromCam = go.transform.position - st.focusCameraPos;
                toFromCam.y = 0f;
                if (toFromCam.sqrMagnitude > 0.01f)
                    go.transform.rotation = Quaternion.LookRotation(toFromCam.normalized, Vector3.up);
                go.transform.localScale = Vector3.one * 0.036f;

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 70);

                var cg = go.AddComponent<CanvasGroup>();
                cg.interactable = false;
                cg.blocksRaycasts = false;

                // Sleek VisionOS Frosted Glass Pill instead of sharp neon cyan box!
                AddSliced(go, _vaWorldPill, Color.white);

                var dotGo = CreateUiRect("StatusDot", go.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(22, -6), new Vector2(34, 6));
                var dotImg = dotGo.AddComponent<UIImage>();
                dotImg.sprite = _spDot;
                dotImg.color = ColMint;

                var codeTxt = CreateTmp("Code", go.transform, st.code, 15, true, ColSkySoft,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(46, 0), new Vector2(-16, -8));
                var nameTxt = CreateTmp("Name", go.transform, st.shortName, 20, true, ColWhite,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(1, 0.56f), new Vector2(46, 8), new Vector2(-16, 0));

                var marker = go.AddComponent<WorldStationMarker>();
                marker.stationIndex = st.index;
                marker.codeText = codeTxt;
                marker.nameText = nameTxt;
                marker.statusDot = dotImg;
                marker.canvasGroup = cg;
            }
        }

        // ==================== STYLE A VISIONOS HUD BUILDER ====================

        private static void BuildStyleAHud(WorkshopCatalog catalog)
        {
            var canvasGo = CreateResponsiveCanvas("HUD_Canvas", 10);
            var hud = canvasGo.AddComponent<DigitalTwinHUD>();

            // Wire Style A pill sprites
            hud.spPillActiveWhite = _vaPillActiveWhite;
            hud.spPillActiveSky = _vaPillActiveSky;
            hud.spPillIdle = _vaPillIdle;
            hud.spPillGhost = _vaPillGhost;
            hud.spPillGreen = _vaPillGreen;
            hud.spPillDanger = _vaPillDanger;

            var main = CreateUiRect("HUD_MainContainer", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hud.hudMainContainer = main;

            // ==================== 1. TOP 3 FLOATING GLASS PILLS (NO HEAVY BAR!) ====================

            // 1A. Top-Left Brand Pill
            var topLeft = CreateUiRect("TopLeftPill", main.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -72), new Vector2(364, -22));
            AddSliced(topLeft, _vaPillGlass, Color.white);
            var bdot = CreateUiRect("Dot", topLeft.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -5), new Vector2(28, 5));
            var bdotImg = bdot.AddComponent<UIImage>();
            bdotImg.sprite = _spDot;
            bdotImg.color = ColMint;
            CreateTmp("BrandTitle", topLeft.transform, "棉纺智能工厂数字孪生", 15, true, ColWhite,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(0, 1), new Vector2(38, 0), new Vector2(214, 0));
            CreateDividerV(topLeft.transform, 218, 18);
            hud.statusSummaryText = CreateTmp("StatusSub", topLeft.transform, "9/9 工段在线", 12, false, ColSubText,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(1, 1), new Vector2(228, 0), new Vector2(-14, 0));

            // 1B. Top-Center Navigation Pill (Mode | Camera | Hall + Roof)
            var topMid = CreateUiRect("TopCenterPill", main.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-386, -72), new Vector2(386, -22));
            AddSliced(topMid, _vaPillGlass, Color.white);

            float mx = 8f;
            hud.btnModeDashboard = CreateSegBtn("BtnModeDash", topMid.transform, "产线监控", ref mx, 86, _vaPillActiveWhite, ColDarkText);
            hud.btnModeTeaching  = CreateSegBtn("BtnModeTeach", topMid.transform, "工艺实训", ref mx, 86, _vaPillGhost, ColSubText);
            mx += 6f; CreateDividerV(topMid.transform, mx, 20); mx += 8f;

            hud.btnCamFps   = CreateSegBtn("BtnCamFps",   topMid.transform, "自由漫游", ref mx, 86, _vaPillActiveSky, ColWhite);
            hud.btnCamOrbit = CreateSegBtn("BtnCamOrbit", topMid.transform, "鸟瞰全景", ref mx, 86, _vaPillGhost, ColSubText);
            hud.btnCamTour  = CreateSegBtn("BtnCamTour",  topMid.transform, "自动巡检", ref mx, 86, _vaPillGhost, ColSubText);
            mx += 6f; CreateDividerV(topMid.transform, mx, 20); mx += 8f;

            hud.btnHall1       = CreateSegBtn("BtnHall1",   topMid.transform, "主车间", ref mx, 74, _vaPillActiveSky, ColWhite);
            hud.btnHall2       = CreateSegBtn("BtnHall2",   topMid.transform, "立体库", ref mx, 74, _vaPillGhost, ColSubText);
            hud.btnRoofCutaway = CreateSegBtn("BtnRoofCut", topMid.transform, "屋顶透视", ref mx, 84, _vaPillGhost, ColSubText);

            // 1C. Top-Right Utility Pill (Clock + Circular Minimal Icon Buttons)
            var topRight = CreateUiRect("TopRightPill", main.transform, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-340, -72), new Vector2(-24, -22));
            AddSliced(topRight, _vaPillGlass, Color.white);
            hud.clockText = CreateTmp("Clock", topRight.transform, "21:35:08", 13, true, ColWhite,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(0, 1), new Vector2(18, 0), new Vector2(92, 0));

            float ux = 96f;
            hud.btnToggleMarkers = CreateCircleIconBtn("BtnTag",  topRight.transform, "标", ref ux);
            hud.btnToggleHud     = CreateCircleIconBtn("BtnHide", topRight.transform, "隐", ref ux);
            hud.btnOpenSettings  = CreateCircleIconBtn("BtnSet",  topRight.transform, "设", ref ux);
            hud.btnOpenHelp      = CreateCircleIconBtn("BtnHelp", topRight.transform, "?",  ref ux);
            hud.btnReturnHome    = CreateCircleIconBtn("BtnHome", topRight.transform, "主", ref ux);
            hud.btnRequestQuit   = CreateCircleIconBtn("BtnQuit", topRight.transform, "×",  ref ux);

            // ==================== 2. LEFT FLOATING COMPACT GLASS ISLAND (418px tall!) ====================
            var leftCard = CreateUiRect("LeftGlassIsland", main.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -504), new Vector2(320, -86));
            // Use top-left pivot so height smoothly expands for Teaching Mode
            var lrt = leftCard.GetComponent<RectTransform>();
            lrt.pivot = new Vector2(0, 1);
            lrt.anchoredPosition = new Vector2(24, -86);
            lrt.sizeDelta = new Vector2(296, 418);
            AddSliced(leftCard, _vaCardGlass, Color.white);
            hud.leftPanelRoot = leftCard;
            hud.leftPanelRect = lrt;

            // --- 2A. Compact Dashboard Island (leaves bottom-left 55% open!) ---
            var dashRoot = CreateUiRect("DashboardContent", leftCard.transform, Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -18));
            hud.dashboardLeftContent = dashRoot;

            CreateTmp("HdrL", dashRoot.transform, "实时生产概览", 13, true, ColSubText, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.65f, 1), new Vector2(2, -22), new Vector2(0, 0));
            CreateTmp("HdrR", dashRoot.transform, "<color=#34D399>● 正常</color>", 12, true, ColMint, TextAlignmentOptions.MidlineRight,
                new Vector2(0.6f, 1), new Vector2(1, 1), Vector2.zero, new Vector2(-2, 0));

            // 2x2 Airy KPI Subcards
            hud.kpiOutputText         = CreateKpiSubcard(dashRoot.transform, "今日累计产量", "1,842 kg", ColWhite, 0, -34);
            hud.kpiPassRateText       = CreateKpiSubcard(dashRoot.transform, "视觉分拣良率", "97.8%",    ColMint,  136, -34);
            hud.kpiOeeText            = CreateKpiSubcard(dashRoot.transform, "综合效率 OEE", "91.4%",    ColSky,   0, -118);
            hud.kpiWarehouseShortText = CreateKpiSubcard(dashRoot.transform, "立体库货位",   "11 / 16",  ColWhite, 136, -118);

            // Compact 8x2 Warehouse Occupancy Matrix
            CreateTmp("WhLbl", dashRoot.transform, "立体库货位矩阵", 12, false, ColSubText, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.7f, 1), new Vector2(2, -226), new Vector2(0, -204));
            hud.warehouseOccupancyText = CreateTmp("WhPct", dashRoot.transform, "68.8%", 12, true, ColSkySoft, TextAlignmentOptions.MidlineRight,
                new Vector2(0.65f, 1), new Vector2(1, 1), Vector2.zero, new Vector2(-2, -204));

            var whGridGo = CreateUiRect("WhMatrix", dashRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -278), new Vector2(0, -234));
            var glg = whGridGo.AddComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 8;
            glg.cellSize = new Vector2(28, 18);
            glg.spacing = new Vector2(5, 6);
            glg.childAlignment = TextAnchor.MiddleCenter;
            for (int s = 0; s < 16; s++)
            {
                var cell = new GameObject($"Slot_{s + 1:00}");
                cell.transform.SetParent(whGridGo.transform, false);
                var cimg = cell.AddComponent<UIImage>();
                cimg.sprite = _vaSlotTile;
                cimg.type = UIImage.Type.Sliced;
                cimg.color = s < 11 ? hud.colorSlotOn : hud.colorSlotOff;
                hud.warehouseSlotCells[s] = cimg;
            }

            // Minimal 2-line Live Status Feed Pill
            var logBox = CreateUiRect("MiniLog", dashRoot.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -294));
            AddSliced(logBox, _vaSubcard, Color.white);
            hud.eventLogText = CreateTmp("LogTxt", logBox.transform, "<color=#34D399>●</color> 全厂 9 大纺纱工段运行平稳", 12, false, ColWhite,
                TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8));
            hud.eventLogText.lineSpacing = 8f;

            // --- 2B. Teaching Tour Left Content (Cleanly styled in Style A VisionOS) ---
            var teachRoot = CreateUiRect("TeachingContent", leftCard.transform, Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -18));
            hud.teachingLeftContent = teachRoot;

            hud.teachingProgressText = CreateTmp("ProgTxt", teachRoot.transform, "实训进度  已学 1/9  ·  考核 0/9", 13, true, ColWhite,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -22), Vector2.zero);
            var pt = CreateUiRect("ProgTrack", teachRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -36), new Vector2(0, -28));
            AddSliced(pt, _vaBarPill, ColTrack);
            hud.teachingProgressBar = CreateBarFill(pt.transform, ColSky, 0.11f);

            hud.btnPrevStation = CreateStyleAButton("BtnPrevSt", teachRoot.transform, "< 上一工序", 12, true, _vaPillIdle, ColWhite,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(-4, -44));
            hud.btnNextStation = CreateStyleAButton("BtnNextSt", teachRoot.transform, "下一工序 >", 12, true, _vaPillActiveWhite, ColDarkText,
                new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(4, -74), new Vector2(0, -44));

            // 3x3 Clean Rounded Step Pills (compact 648px total Teaching Island for wide Game Views)
            var sgrid = CreateUiRect("StepGrid", teachRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -196), new Vector2(0, -82));
            var sglg = sgrid.AddComponent<GridLayoutGroup>();
            sglg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            sglg.constraintCount = 3;
            sglg.cellSize = new Vector2(82, 34);
            sglg.spacing = new Vector2(6, 5);
            sglg.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 9; i++)
            {
                var st = catalog.stations[i];
                var sbGo = new GameObject($"TStep_{i}");
                sbGo.transform.SetParent(sgrid.transform, false);
                var img = AddSliced(sbGo, _vaSubcard, Color.white);
                var btn = sbGo.AddComponent<Button>();
                btn.targetGraphic = img;
                hud.teachingStepButtons[i] = btn;
                CreateTmp("N", sbGo.transform, $"{i + 1:00} {st.shortName}", 11, true, ColWhite, TextAlignmentOptions.Center,
                    new Vector2(0, 0.44f), new Vector2(1, 1), new Vector2(2, 0), new Vector2(-2, -2));
                hud.teachingStepCheckTexts[i] = CreateTmp("C", sbGo.transform, "待学", 10, false, ColSubText, TextAlignmentOptions.Center,
                    new Vector2(0, 0), new Vector2(1, 0.48f), Vector2.zero, Vector2.zero);
            }

            // Teaching Goal & Key Points Subcard
            var goalBox = CreateUiRect("GoalBox", teachRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -396), new Vector2(0, -204));
            AddSliced(goalBox, _vaSubcard, Color.white);
            hud.teachingGoalText = CreateTmp("GoalTxt", goalBox.transform, "", 11.5f, false, ColWhite, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0.56f), new Vector2(1, 1), new Vector2(12, 2), new Vector2(-12, -8));
            hud.teachingKeyPointsText = CreateTmp("KeyPtsTxt", goalBox.transform, "", 10.5f, false, ColSubText, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0.56f), new Vector2(12, 6), new Vector2(-12, 0));
            hud.teachingKeyPointsText.lineSpacing = 2.0f;

            // Interactive Quiz Subcard
            var quizBox = CreateUiRect("QuizBox", teachRoot.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -404));
            AddSliced(quizBox, _vaSubcard, Color.white);
            hud.quizQuestionText = CreateTmp("QuizQ", quizBox.transform, "随堂考核...", 12f, true, ColWhite, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -52), new Vector2(-12, -8));
            for (int q = 0; q < 3; q++)
            {
                float yTop = -54 - q * 36;
                var qBtn = CreateStyleAButton($"QuizOpt_{q}", quizBox.transform, $"选项 {q + 1}", 11f, false, _vaPillIdle, ColWhite,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, yTop - 30), new Vector2(-10, yTop));
                var qTxt = qBtn.GetComponentInChildren<TextMeshProUGUI>();
                qTxt.alignment = TextAlignmentOptions.MidlineLeft;
                qTxt.margin = new Vector4(12, 2, 10, 2);
                hud.quizOptionButtons[q] = qBtn;
                hud.quizOptionTexts[q] = qTxt;
            }
            hud.quizFeedbackText = CreateTmp("QuizFb", quizBox.transform, "", 11f, false, ColSubText, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 6), new Vector2(-12, 46));
            teachRoot.SetActive(false);

            // ==================== 3. RIGHT FLOATING AIRY DEVICE INSPECTOR (626px tall, crisp breathing room!) ====================
            var rightCard = CreateUiRect("RightGlassDrawer", main.transform, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-350, -712), new Vector2(-24, -86));
            AddSliced(rightCard, _vaCardGlass, Color.white);
            hud.rightPanelRoot = rightCard;

            var rb = CreateUiRect("RightBody", rightCard.transform, Vector2.zero, Vector2.one, new Vector2(20, 18), new Vector2(-20, -18));

            // Top badge + Name + Emerald Remote Toggle Pill
            var badgePill = CreateUiRect("CodeBadge", rb.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -24), new Vector2(138, 0));
            AddSliced(badgePill, _vaPillActiveSky, Color.white);
            hud.inspectorCodeText = CreateTmp("Code", badgePill.transform, "ST-01 · 抓棉开松", 11.5f, true, ColSkySoft,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            hud.btnToggleStationRun = CreateStyleAButton("BtnRemoteToggle", rb.transform, "● 运行中", 12, true, _vaPillGreen,
                new Color(0.43f, 0.91f, 0.72f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-94, -30), new Vector2(0, 0));
            hud.btnToggleStationRunLabel = hud.btnToggleStationRun.GetComponentInChildren<TextMeshProUGUI>();

            hud.inspectorNameText = CreateTmp("DevName", rb.transform, "往复抓棉机（开松混棉）", 19, true, ColWhite,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, -30));
            hud.inspectorEngNameText = CreateTmp("DevEng", rb.transform, "AUTOMATIC BALE OPENER", 11, false, ColSubText,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -78), new Vector2(0, -58));

            // Clean Rounded Preview Photo
            var pFrame = CreateUiRect("PreviewFrame", rb.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -210), new Vector2(0, -86));
            AddSliced(pFrame, _vaSubcard, Color.white);
            var pImgGo = CreateUiRect("PreviewImg", pFrame.transform, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
            hud.inspectorPreviewImage = pImgGo.AddComponent<UIImage>();
            hud.inspectorPreviewImage.preserveAspect = true;

            // 3 Sleek Rounded Telemetry Bars (44px clean vertical stride, zero label/bar touch!)
            CreateStyleATelemetryRow(rb.transform, "工作转速", -222, ColSky, out hud.telemetryRpmText, out hud.telemetryRpmBar);
            CreateStyleATelemetryRow(rb.transform, "电机温度", -266, ColAmber, out hud.telemetryTempText, out hud.telemetryTempBar);
            CreateStyleATelemetryRow(rb.transform, "实时负荷与功耗", -310, ColMint, out hud.telemetryLoadText, out hud.telemetryLoadBar);

            // Hairline Divider
            var hdiv = CreateUiRect("HDiv", rb.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -360), new Vector2(0, -359));
            hdiv.AddComponent<UIImage>().color = ColSep;

            // Clean Spec Rows (5 crisp rows with generous non-overlapping spacing)
            hud.specsTableText = CreateTmp("SpecsTxt", rb.transform, "", 12.5f, false, ColWhite, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -490), new Vector2(0, -370));
            hud.specsTableText.lineSpacing = 7f;

            // Clean Airy Summary
            hud.processSummaryText = CreateTmp("SumTxt", rb.transform, "", 12f, false, ColSubText, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -498));
            hud.processSummaryText.lineSpacing = 5f;

            // ==================== 4. BOTTOM FLOATING CENTERED MACOS-STYLE DOCK ====================
            var dock = CreateUiRect("BottomProcessDock", main.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-536, 22), new Vector2(536, 74));
            AddSliced(dock, _vaPillGlass, Color.white);

            float dx = 10f;
            for (int i = 0; i < 9; i++)
            {
                var st = catalog.stations[i];
                string shortLbl = st.shortName.Length > 3 ? st.shortName.Substring(0, 2) : st.shortName;
                if (i == 8) shortLbl = "立体库";
                float w = i == 8 ? 92f : 84f;
                var nodeBtn = CreateStyleAButton($"DockNode_{i + 1}", dock.transform, $"<color=#94A3B8>{i + 1:00}</color> {shortLbl}", 13, true,
                    _vaPillGhost, ColWhite, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(dx, -19), new Vector2(dx + w, 19));
                hud.flowNodeButtons[i] = nodeBtn;
                dx += w + 4f;
            }

            dx += 4f; CreateDividerV(dock.transform, dx, 22); dx += 10f;

            hud.btnSimPlayPause = CreateStyleAButton("BtnPlayPause", dock.transform, "暂停", 13, true, _vaPillActiveWhite, ColDarkText,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(dx, -19), new Vector2(dx + 68, 19));
            hud.btnSimPlayPauseLabel = hud.btnSimPlayPause.GetComponentInChildren<TextMeshProUGUI>();
            dx += 74f;

            hud.btnSpeedCycle = CreateStyleAButton("BtnSpeed", dock.transform, "1x", 13, true, _vaPillIdle, ColWhite,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(dx, -19), new Vector2(dx + 52, 19));
            hud.btnSpeedCycleLabel = hud.btnSpeedCycle.GetComponentInChildren<TextMeshProUGUI>();
            dx += 58f;

            hud.btnCollapseSidePanels = CreateStyleAButton("BtnCollapse", dock.transform, "极简", 12.5f, false, _vaPillIdle, ColSubText,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(dx, -19), new Vector2(dx + 58, 19));
            hud.btnCollapseLabel = hud.btnCollapseSidePanels.GetComponentInChildren<TextMeshProUGUI>();

            // ==================== 5. CROSSHAIR, FPS HINT PILL, TOOLTIP & MODALS ====================
            var crossGo = CreateUiRect("FPS_Crosshair", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-18, -18), new Vector2(18, 18));
            var crossImg = crossGo.AddComponent<UIImage>();
            crossImg.sprite = _spCrosshair;
            crossImg.color = new Color(1f, 1f, 1f, 0.78f);
            crossImg.raycastTarget = false;
            crossGo.SetActive(false);
            hud.crosshairObject = crossGo;

            var hintGo = CreateUiRect("FPS_HintPill", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-286, -112), new Vector2(286, -82));
            var hintImg = AddSliced(hintGo, _vaPillGlass, new Color(0.75f, 0.80f, 0.88f, 0.96f));
            hintImg.raycastTarget = false;
            var hintTxt = CreateTmp("HintTxt", hintGo.transform, "自由漫游：WASD 行走 · Shift 加速 · 右键拖拽看四周 · Tab 锁准星 · R 复位", 12, true, ColWhite,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-14, 0));
            hintTxt.raycastTarget = false;
            hud.fpsHintBanner = hintGo;

            var tipGo = CreateUiRect("HoverTooltip", canvasGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(260, 58));
            var tipBg = AddSliced(tipGo, _vaCardGlass, Color.white);
            tipBg.raycastTarget = false;
            hud.hoverTooltipText = CreateTmp("TipTxt", tipGo.transform, "", 12.5f, true, ColWhite, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, new Vector2(14, 4), new Vector2(-14, -4));
            hud.hoverTooltipText.raycastTarget = false;
            tipGo.SetActive(false);
            hud.hoverTooltipRoot = tipGo;

            hud.settingsModal = BuildStyleASettingsModal(canvasGo.transform, out hud.btnQualMed, out hud.btnQualHigh,
                out hud.btnQualUltra, out hud.sensSlider, out hud.sensValueText, out hud.btnCloseSettings);
            hud.helpModal = BuildStyleAHelpModal(canvasGo.transform, out hud.btnCloseHelp);
            hud.quitConfirmModal = BuildStyleAQuitModal(canvasGo.transform, out hud.btnConfirmQuit, out hud.btnCancelQuit);
        }

        // ==================== STYLE A UI COMPONENT FACTORIES ====================

        private static GameObject CreateResponsiveCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.65f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreateUiRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static UIImage AddSliced(GameObject go, Sprite sp, Color tint)
        {
            var img = go.GetComponent<UIImage>() ?? go.AddComponent<UIImage>();
            img.sprite = sp;
            img.type = sp != null ? UIImage.Type.Sliced : UIImage.Type.Simple;
            img.color = tint;
            return img;
        }

        private static TextMeshProUGUI CreateTmp(string name, Transform parent, string text, float size, bool bold, Color color,
            TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = CreateUiRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = (bold && _fontBold != null) ? _fontBold : _fontReg;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static void CreateDividerV(Transform parent, float xPos, float height)
        {
            var d = CreateUiRect("DivV", parent, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(xPos, -height * 0.5f), new Vector2(xPos + 1f, height * 0.5f));
            d.AddComponent<UIImage>().color = ColSep;
        }

        private static Button CreateStyleAButton(string name, Transform parent, string label, float fontSize, bool bold,
            Sprite sprite, Color textColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = CreateUiRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = AddSliced(go, sprite, Color.white);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.82f, 0.90f, 1f, 0.9f);
            btn.colors = colors;
            var tmp = CreateTmp("Label", go.transform, label, fontSize, bold, textColor, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(6, 2), new Vector2(-6, -2));
            tmp.raycastTarget = false;
            return btn;
        }

        private static Button CreateSegBtn(string name, Transform parent, string label, ref float xCursor, float w, Sprite sp, Color textCol)
        {
            var btn = CreateStyleAButton(name, parent, label, 13.5f, true, sp, textCol,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(xCursor, -18), new Vector2(xCursor + w, 18));
            xCursor += w + 4f;
            return btn;
        }

        private static Button CreateCircleIconBtn(string name, Transform parent, string glyph, ref float xCursor)
        {
            var go = CreateUiRect(name, parent, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(xCursor, -16), new Vector2(xCursor + 32, 16));
            var img = go.AddComponent<UIImage>();
            img.sprite = _vaCircleBtn;
            img.color = Color.white;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var tmp = CreateTmp("G", go.transform, glyph, 13, true, ColWhite, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tmp.raycastTarget = false;
            xCursor += 36f;
            return btn;
        }

        private static TextMeshProUGUI CreateKpiSubcard(Transform parent, string label, string val, Color valColor, float x, float yTop)
        {
            var box = CreateUiRect($"Kpi_{label}", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(x, yTop - 74), new Vector2(x + 124, yTop));
            AddSliced(box, _vaSubcard, Color.white);
            CreateTmp("L", box.transform, label, 11.5f, false, ColSubText, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -28), new Vector2(-8, -8));
            return CreateTmp("V", box.transform, val, 21, true, valColor, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 0), new Vector2(1, 0.62f), new Vector2(12, 8), new Vector2(-8, 0));
        }

        private static void CreateStyleATelemetryRow(Transform parent, string label, float yTop, Color fillCol,
            out TextMeshProUGUI valText, out UIImage fillImg)
        {
            CreateTmp("L", parent, label, 12, false, ColSubText, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(0, yTop - 18), new Vector2(0, yTop));
            valText = CreateTmp("V", parent, "0", 12.5f, true, ColWhite, TextAlignmentOptions.MidlineRight,
                new Vector2(0.45f, 1), new Vector2(1, 1), new Vector2(0, yTop - 18), new Vector2(0, yTop));
            var trk = CreateUiRect("Trk", parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, yTop - 31), new Vector2(0, yTop - 24));
            AddSliced(trk, _vaBarPill, ColTrack);
            fillImg = CreateBarFill(trk.transform, fillCol, 0.72f);
        }

        private static UIImage CreateBarFill(Transform trackParent, Color col, float initialFill)
        {
            var go = CreateUiRect("Fill", trackParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = go.AddComponent<UIImage>();
            img.sprite = _vaBarPill;
            img.type = UIImage.Type.Filled;
            img.fillMethod = UIImage.FillMethod.Horizontal;
            img.color = col;
            img.fillAmount = initialFill;
            return img;
        }

        private static GameObject BuildStyleASettingsModal(Transform parent, out Button btnMed, out Button btnHigh, out Button btnUltra,
            out Slider sensSlider, out TextMeshProUGUI sensVal, out Button btnClose)
        {
            var overlay = CreateUiRect("SettingsModal", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<UIImage>().color = new Color(0.02f, 0.04f, 0.08f, 0.68f);

            var card = CreateUiRect("Card", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-270, -185), new Vector2(270, 185));
            AddSliced(card, _vaCardGlass, Color.white);

            CreateTmp("Title", card.transform, "画质与操控设置", 20, true, ColWhite, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -54), new Vector2(-20, -18));

            CreateTmp("QLbl", card.transform, "桌面端 URP 画质等级", 13.5f, true, ColSubText, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -92), new Vector2(-32, -68));

            btnMed   = CreateStyleAButton("BtnQMed",   card.transform, "中画质", 13.5f, true, _vaPillIdle, ColWhite, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -138), new Vector2(180, -100));
            btnHigh  = CreateStyleAButton("BtnQHigh",  card.transform, "高画质", 13.5f, true, _vaPillIdle, ColWhite, new Vector2(0, 1), new Vector2(0, 1), new Vector2(194, -138), new Vector2(342, -100));
            btnUltra = CreateStyleAButton("BtnQUltra", card.transform, "极致 SSAO", 13.5f, true, _vaPillActiveWhite, ColDarkText, new Vector2(0, 1), new Vector2(0, 1), new Vector2(356, -138), new Vector2(508, -100));

            CreateTmp("SLbl", card.transform, "视角旋转灵敏度", 13.5f, true, ColSubText, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.7f, 1), new Vector2(32, -196), new Vector2(0, -170));
            sensVal = CreateTmp("SVal", card.transform, "2.0", 15, true, ColSkySoft, TextAlignmentOptions.MidlineRight,
                new Vector2(0.7f, 1), new Vector2(1, 1), new Vector2(0, -196), new Vector2(-32, -170));

            var sliderGo = CreateUiRect("SensSlider", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -234), new Vector2(-32, -206));
            var bg = CreateUiRect("Background", sliderGo.transform, new Vector2(0, 0.3f), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero);
            AddSliced(bg, _vaBarPill, ColTrack);
            var fillArea = CreateUiRect("FillArea", sliderGo.transform, new Vector2(0, 0.3f), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero);
            var fill = CreateUiRect("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddSliced(fill, _vaBarPill, ColSky);
            var handleArea = CreateUiRect("HandleArea", sliderGo.transform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            var handle = CreateUiRect("Handle", handleArea.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-12, 0), new Vector2(12, 0));
            var hImg = handle.AddComponent<UIImage>();
            hImg.sprite = _spDot;
            hImg.color = ColWhite;

            sensSlider = sliderGo.AddComponent<Slider>();
            sensSlider.fillRect = fill.GetComponent<RectTransform>();
            sensSlider.handleRect = handle.GetComponent<RectTransform>();
            sensSlider.targetGraphic = hImg;
            sensSlider.minValue = 0.5f;
            sensSlider.maxValue = 5.0f;
            sensSlider.value = 2.0f;

            btnClose = CreateStyleAButton("BtnCloseSettings", card.transform, "完成", 15, true, _vaPillActiveWhite, ColDarkText,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, 26), new Vector2(90, 68));

            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject BuildStyleAHelpModal(Transform parent, out Button btnClose)
        {
            var overlay = CreateUiRect("HelpModal", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<UIImage>().color = new Color(0.02f, 0.04f, 0.08f, 0.68f);

            var card = CreateUiRect("Card", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-300, -210), new Vector2(300, 210));
            AddSliced(card, _vaCardGlass, Color.white);

            CreateTmp("Title", card.transform, "操控与快捷键指引", 20, true, ColWhite, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -54), new Vector2(-20, -18));

            string body =
                "<color=#7DD3FC><b>自由漫游（默认 · F2）</b></color>\n" +
                "• WASD 行走 / Shift 加速 / R 回到车间入口\n" +
                "• 按住鼠标右键平滑转向；按 Tab 键可随时切换/退出准星锁定\n\n" +
                "<color=#7DD3FC><b>鸟瞰全景与工位聚焦（F1 / 点击底部工序）</b></color>\n" +
                "• 右键旋转视角 / 滚轮缩放 / WASD 与 Q、E 自由平移升降\n" +
                "• F3 开启全厂自动巡检 · F4 开启屋顶透视 · H 隐藏全部界面";

            var txt = CreateTmp("Body", card.transform, body, 14, false, ColWhite, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(36, 86), new Vector2(-36, -68));
            txt.lineSpacing = 8f;

            btnClose = CreateStyleAButton("BtnCloseHelp", card.transform, "我知道了", 15, true, _vaPillActiveWhite, ColDarkText,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-90, 26), new Vector2(90, 68));
            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject BuildStyleAQuitModal(Transform parent, out Button btnConfirm, out Button btnCancel)
        {
            var overlay = CreateUiRect("QuitConfirmModal", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<UIImage>().color = new Color(0.02f, 0.04f, 0.08f, 0.72f);

            var card = CreateUiRect("Card", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-210, -104), new Vector2(210, 104));
            AddSliced(card, _vaCardGlass, Color.white);

            CreateTmp("Title", card.transform, "退出系统", 20, true, ColWhite, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -52), new Vector2(-20, -18));
            CreateTmp("Desc", card.transform, "确定要退出棉纺智能工厂数字孪生平台吗？", 14.5f, false, ColSubText, TextAlignmentOptions.Center,
                new Vector2(0, 0.36f), new Vector2(1, 0.72f), new Vector2(24, 0), new Vector2(-24, 0));

            btnCancel = CreateStyleAButton("BtnCancel", card.transform, "取消", 14, true, _vaPillIdle, ColWhite,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-156, 24), new Vector2(-10, 64));
            btnConfirm = CreateStyleAButton("BtnConfirm", card.transform, "确认退出", 14, true, _vaPillDanger, new Color(1f, 0.8f, 0.8f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(10, 24), new Vector2(156, 64));
            overlay.SetActive(false);
            return overlay;
        }
    }
}
