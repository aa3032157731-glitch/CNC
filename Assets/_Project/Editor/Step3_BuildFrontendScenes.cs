using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNC.DigitalTwin;
using ECM.Components;
using ECM.Controllers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

namespace CNC.EditorTools
{
    public static class Step3_BuildFrontendScenes
    {
        private const string ConfigsDir = "Assets/_Project/Configs";
        private const string SpritesDir = "Assets/_Project/UI/Sprites";
        private const string FontsDir = "Assets/_Project/UI/Fonts";

        private static TMP_FontAsset _fontReg;
        private static TMP_FontAsset _fontBold;
        private static Sprite _spGlass;
        private static Sprite _spCard;
        private static Sprite _spBtnNormal;
        private static Sprite _spBtnActive;
        private static Sprite _spBtnDanger;
        private static Sprite _spBtnSuccess;
        private static Sprite _spHeaderBar;
        private static Sprite _spRing;
        private static Sprite _spDot;
        private static Sprite _spPill;
        private static Sprite _spCrosshair;
        private static Sprite _spWorldTag;

        private static readonly Color ColCyan = new Color(0.0f, 0.86f, 1.0f, 1f);
        private static readonly Color ColTextMain = new Color(0.94f, 0.97f, 1.0f, 1f);
        private static readonly Color ColTextSub = new Color(0.58f, 0.76f, 0.92f, 1f);
        private static readonly Color ColDarkTrack = new Color(0.07f, 0.16f, 0.28f, 0.9f);

        [MenuItem("CNC Optimize/Step 3 - Build Frontend Scenes (Start Screen + Digital Twin HUD)")]
        public static void Run()
        {
            Debug.Log("[CNC Step3] Building upgraded Frontend Scenes (Play.unity & SampleScene.unity)...");
            Directory.CreateDirectory(ConfigsDir);
            LoadUiAssets();

            var catalog = CreateOrUpdateWorkshopCatalog();
            BuildPlayStartScene();
            BuildSampleSceneFrontend(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CNC Step3] Frontend Scenes built & saved cleanly!");
        }

        private static void LoadUiAssets()
        {
            _fontReg = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/CNC_Sans_Regular_SDF.asset");
            _fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/CNC_Sans_Bold_SDF.asset") ?? _fontReg;

            _spGlass = LoadSp("ui_panel_glass.png");
            _spCard = LoadSp("ui_card_dark.png");
            _spBtnNormal = LoadSp("ui_btn_normal.png");
            _spBtnActive = LoadSp("ui_btn_active.png");
            _spBtnDanger = LoadSp("ui_btn_danger.png");
            _spBtnSuccess = LoadSp("ui_btn_success.png");
            _spHeaderBar = LoadSp("ui_header_bar.png");
            _spRing = LoadSp("ui_circle_ring.png");
            _spDot = LoadSp("ui_circle_dot.png");
            _spPill = LoadSp("ui_pill_bar.png");
            _spCrosshair = LoadSp("ui_crosshair.png");
            _spWorldTag = LoadSp("ui_world_tag.png");
        }

        private static Sprite LoadSp(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/{fileName}");
        }

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
            Debug.Log($"[CNC Step3] Saved WorkshopCatalog with {catalog.stations.Count} stations.");
            return catalog;
        }

        // ==================== A. REBUILD PLAY.UNITY START SCREEN ====================

        private static void BuildPlayStartScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);

            // Configure Camera for URP
            var cam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.03f, 0.08f, 0.15f, 1f);
                var uac = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                uac.renderPostProcessing = false;
            }

            // Remove old broken Canvas (2470x600)
            foreach (var oldCanvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
                UnityEngine.Object.DestroyImmediate(oldCanvas.gameObject);

            // Ensure EventSystem
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Create Responsive 1920x1080 Start Canvas
            var canvasGo = CreateResponsiveCanvas("StartScreen_Canvas", 0);
            var ctrl = canvasGo.AddComponent<StartScreenController>();

            // 1. Full-bleed Hero Background with AspectRatioFitter (zero distortion!)
            var bgGo = CreateUiRect("HeroBackground", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bgGo.AddComponent<UIImage>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Scenes/Play/摄图网_600865141_工业棉纱纺织工厂(仅交流学习使用).jpg");
            bgImg.color = new Color(0.32f, 0.56f, 0.80f, 1f); // Cool industrial blue color grade
            var arf = bgGo.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = 1.5f;
            ctrl.heroBackground = bgGo.GetComponent<RectTransform>();

            // 2. Dark Navy Scrim Overlay for high contrast typography
            var scrim = CreateUiRect("NavyScrim", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var scrimImg = scrim.AddComponent<UIImage>();
            scrimImg.color = new Color(0.02f, 0.06f, 0.13f, 0.72f);

            // 3. Top Header Accent Bar
            var topBar = CreateUiRect("TopTechBar", canvasGo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -76), Vector2.zero);
            var topImg = topBar.AddComponent<UIImage>();
            topImg.sprite = _spHeaderBar;
            topImg.color = new Color(1f, 1f, 1f, 0.92f);
            CreateTmp("TopBadge", topBar.transform, "SMART TEXTILE INDUSTRY 4.0  //  DIGITAL TWIN & TRAINING PLATFORM", 16, true, ColCyan,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 4. Center Hero Glass Card (940 x 560)
            var heroCard = CreateUiRect("HeroCard", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-470, -290), new Vector2(470, 270));
            AddSlicedImage(heroCard, _spGlass, Color.white);

            // Pill tag
            var tagBox = CreateUiRect("VersionTag", heroCard.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-210, -72), new Vector2(210, -36));
            AddSlicedImage(tagBox, _spCard, new Color(0.0f, 0.65f, 0.92f, 0.35f));
            CreateTmp("TagTxt", tagBox.transform, "URP 高精度三维工业数字孪生系统  v2.0", 16, true, ColCyan,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Main Title & Subtitle
            CreateTmp("MainTitle", heroCard.transform, "棉纺智能工厂三维数字孪生与实训平台", 44, true, Color.white,
                TextAlignmentOptions.Center, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -152), new Vector2(-36, -84));

            CreateTmp("SubTitle", heroCard.transform, "清花抓棉 · 除杂梳棉 · 精梳并条 · 粗细纱络筒 · 视觉分拣与自动化立体仓储", 19, false, ColTextSub,
                TextAlignmentOptions.Center, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -196), new Vector2(-36, -154));

            // Glowing divider line
            var div = CreateUiRect("Divider", heroCard.transform, new Vector2(0.1f, 1), new Vector2(0.9f, 1), new Vector2(0, -214), new Vector2(0, -212));
            div.AddComponent<UIImage>().color = new Color(0f, 0.85f, 1f, 0.55f);

            // 3 Feature Highlights Row
            string[] featTitles = { "数字孪生监控大屏", "交互式工艺实训导览", "URP 工业 PBR 渲染引擎" };
            string[] featDescs  = { "实时产量 / 良率 / 16货位状态", "9大工序原理 / 知识点 / 随堂考核", "高帧率 LOD 合批 / 多视角屋顶透视" };
            for (int i = 0; i < 3; i++)
            {
                float x0 = 42 + i * 292;
                var fc = CreateUiRect($"FeatureCard_{i}", heroCard.transform, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(x0, -322), new Vector2(x0 + 272, -234));
                AddSlicedImage(fc, _spCard, Color.white);
                CreateTmp("T", fc.transform, featTitles[i], 18, true, ColCyan, TextAlignmentOptions.Center,
                    new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(8, 2), new Vector2(-8, -8));
                CreateTmp("D", fc.transform, featDescs[i], 13, false, ColTextSub, TextAlignmentOptions.Center,
                    new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(8, 8), new Vector2(-8, 2));
            }

            // 2 Main Mode CTA Buttons
            ctrl.btnEnterDashboard = CreateTechButton("BtnEnterDashboard", heroCard.transform, "进入数字孪生监控大屏", 22, true,
                _spBtnActive, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-416, 118), new Vector2(-18, 192));

            ctrl.btnEnterTeaching = CreateTechButton("BtnEnterTeaching", heroCard.transform, "进入教学实训工艺导览", 22, true,
                _spBtnNormal, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(18, 118), new Vector2(416, 192));

            // 2 Secondary Buttons (Settings & Quit)
            ctrl.btnOpenSettings = CreateTechButton("BtnSettings", heroCard.transform, "画质与操控设置", 17, false,
                _spBtnNormal, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-240, 44), new Vector2(-14, 96));

            ctrl.btnQuit = CreateTechButton("BtnQuit", heroCard.transform, "退出系统", 17, false,
                _spBtnDanger, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(14, 44), new Vector2(240, 96));

            // Footer hint
            CreateTmp("FooterText", canvasGo.transform, "桌面端高画质版本 (Windows Standalone)  |  支持鸟瞰全景 / 第一人称漫游 / 自动巡检 / 远程设备控制",
                14, false, new Color(0.6f, 0.78f, 0.94f, 0.8f), TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 18), new Vector2(0, 46));

            // 5. Settings Modal
            var setModal = BuildSettingsModal(canvasGo.transform, out ctrl.btnQualityMed, out ctrl.btnQualityHigh,
                out ctrl.btnQualityUltra, out ctrl.sensitivitySlider, out ctrl.sensitivityValueText, out ctrl.btnCloseSettings);
            ctrl.settingsModal = setModal;

            // 6. Async Loading Overlay
            var loadOverlay = CreateUiRect("LoadingOverlay", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            loadOverlay.AddComponent<UIImage>().color = new Color(0.02f, 0.06f, 0.12f, 0.92f);
            var loadCard = CreateUiRect("LoadCard", loadOverlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-360, -100), new Vector2(360, 100));
            AddSlicedImage(loadCard, _spGlass, Color.white);
            CreateTmp("LoadTitle", loadCard.transform, "正在加载棉纺智能工厂三维场景...", 22, true, Color.white,
                TextAlignmentOptions.Center, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -56), new Vector2(-24, -18));

            var track = CreateUiRect("ProgressTrack", loadCard.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(40, -14), new Vector2(-110, 12));
            AddSlicedImage(track, _spPill, ColDarkTrack);
            var fillGo = CreateUiRect("ProgressFill", track.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = fillGo.AddComponent<UIImage>();
            fillImg.sprite = _spPill;
            fillImg.type = UIImage.Type.Filled;
            fillImg.fillMethod = UIImage.FillMethod.Horizontal;
            fillImg.color = ColCyan;
            fillImg.fillAmount = 0f;
            ctrl.loadingBarFill = fillImg;

            ctrl.loadingPercentText = CreateTmp("LoadPct", loadCard.transform, "0%", 20, true, ColCyan,
                TextAlignmentOptions.MidlineRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-102, -18), new Vector2(-36, 18));

            ctrl.loadingTipText = CreateTmp("LoadTip", loadCard.transform, "正在准备场景资源...", 15, false, ColTextSub,
                TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(1, 0), new Vector2(28, 22), new Vector2(-28, 62));

            loadOverlay.SetActive(false);
            ctrl.loadingOverlay = loadOverlay;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[CNC Step3] Rebuilt Play.unity Start Screen cleanly.");
        }

        // ==================== B. BUILD SAMPLESCENE FRONTEND & SYSTEMS ====================

        private static void BuildSampleSceneFrontend(WorkshopCatalog catalog)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            // 1. Cleanly disable old broken legacy canvases (keep inactive rather than hard-deleting legacy script refs)
            string[] legacyCanvases = { "Canvas (1)", "UI", "Canvas" };
            foreach (var cname in legacyCanvases)
            {
                var go = GameObject.Find(cname);
                if (go != null) go.SetActive(false);
            }

            // Ensure EventSystem exists
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Remove previous generated roots if re-running
            foreach (var oldName in new[] { "__DigitalTwinSystem", "HUD_Canvas", "WorldMarkers_Root", "__StationClickVolumes" })
            {
                var old = GameObject.Find(oldName);
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            }

            // 2. Build __DigitalTwinSystem Core Managers
            var sysRoot = new GameObject("__DigitalTwinSystem");
            var simState = sysRoot.AddComponent<SimulationState>();
            simState.catalog = catalog;

            var camRig = sysRoot.AddComponent<CameraRigManager>();
            var mainCam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            var fpsRoot = GameObject.Find("ECM_FirstPerson");
            camRig.mainCamera = mainCam;
            camRig.fpsPlayerRoot = fpsRoot;
            if (fpsRoot != null)
            {
                camRig.fpsController = fpsRoot.GetComponent<BaseFirstPersonController>();
                camRig.fpsMouseLook = fpsRoot.GetComponent<MouseLook>();
                if (mainCam != null && mainCam.transform.IsChildOf(fpsRoot.transform))
                    camRig.fpsCameraAnchor = mainCam.transform.parent;
                else
                    camRig.fpsCameraAnchor = fpsRoot.transform.Find("Camera_Pivot");
            }
            CollectRoofCutawayObjects(camRig);

            var raycaster = sysRoot.AddComponent<MachineRaycaster>();
            raycaster.targetCamera = mainCam;
            raycaster.hoverHighlightMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_HighlightHover.mat");
            raycaster.selectedHighlightMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_HighlightSelected.mat");

            var animator = sysRoot.AddComponent<WorkshopAnimator>();
            WireWorkshopAnimator(animator);

            // 3. Attach clickable MachineInteractable volumes & highlight targets for all 9 stations
            SetupStationInteractables(catalog);

            // 4. Build floating 3D WorldSpace Station Markers
            BuildWorldStationMarkers(catalog);

            // 5. Build Responsive 1920x1080 Digital Twin & Teaching Tour HUD
            BuildDigitalTwinHud(catalog);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[CNC Step3] Built complete Digital Twin & Teaching Tour Frontend in SampleScene.unity.");
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
            Debug.Log($"[CNC Step3] Registered Roof Cutaway meshes: W1={camRig.workshop1RoofObjects.Count}, W2={camRig.workshop2RoofObjects.Count}");
        }

        private static void WireWorkshopAnimator(WorkshopAnimator anim)
        {
            // Find all 6 AGV instances
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

            // Inspection robotic arms
            var armRoot = GameObject.Find("__InspectionArms");
            if (armRoot != null)
            {
                foreach (Transform child in armRoot.transform)
                    anim.inspectionArms.Add(child);
            }

            // Holographic floor pads
            foreach (var pname in new[] { "末并", "精梳", "粗纱", "细纱", "络筒" })
            {
                var go = GameObject.Find(pname);
                if (go != null) anim.floorRingPads.Add(go.transform);
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

                // Create clean clickable volume centered at station position
                var clickVol = new GameObject($"Interact_{st.code}_{st.shortName}");
                clickVol.transform.SetParent(volRoot.transform, false);
                clickVol.transform.position = st.worldPosition;

                var bc = clickVol.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = st.index == 8 ? new Vector3(48f, 16f, 54f) : new Vector3(18f, 14f, 22f);

                var inter = clickVol.AddComponent<MachineInteractable>();
                inter.stationIndex = st.index;
                inter.displayName = st.name;
                inter.highlightRoot = targetGo != null ? targetGo.transform : clickVol.transform;
            }
        }

        private static void BuildWorldStationMarkers(WorkshopCatalog catalog)
        {
            var root = new GameObject("WorldMarkers_Root");

            foreach (var st in catalog.stations)
            {
                var go = new GameObject($"Marker_{st.code}_{st.shortName}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = st.worldPosition + new Vector3(0f, 9.2f, 0f);
                // Orient front-facing toward station hero camera so it reads un-mirrored immediately
                Vector3 toFromCam = go.transform.position - st.focusCameraPos;
                toFromCam.y = 0f;
                if (toFromCam.sqrMagnitude > 0.01f)
                    go.transform.rotation = Quaternion.LookRotation(toFromCam.normalized, Vector3.up);
                go.transform.localScale = Vector3.one * 0.045f;

                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(260, 74);

                var cg = go.AddComponent<CanvasGroup>();
                cg.interactable = false;
                cg.blocksRaycasts = false;

                AddSlicedImage(go, _spWorldTag, Color.white);

                var dotGo = CreateUiRect("StatusDot", go.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(18, -8), new Vector2(34, 8));
                var dotImg = dotGo.AddComponent<UIImage>();
                dotImg.sprite = _spDot;
                dotImg.color = new Color(0.16f, 0.92f, 0.48f, 1f);

                var codeTxt = CreateTmp("Code", go.transform, st.code, 18, true, ColCyan,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(44, 0), new Vector2(-12, -8));
                var nameTxt = CreateTmp("Name", go.transform, st.shortName, 22, true, Color.white,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(1, 0.55f), new Vector2(44, 8), new Vector2(-12, 0));

                var marker = go.AddComponent<WorldStationMarker>();
                marker.stationIndex = st.index;
                marker.codeText = codeTxt;
                marker.nameText = nameTxt;
                marker.statusDot = dotImg;
                marker.canvasGroup = cg;
            }
        }

        private static void BuildDigitalTwinHud(WorkshopCatalog catalog)
        {
            var canvasGo = CreateResponsiveCanvas("HUD_Canvas", 10);
            var hud = canvasGo.AddComponent<DigitalTwinHUD>();

            var mainContainer = CreateUiRect("HUD_MainContainer", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hud.hudMainContainer = mainContainer;

            // ==================== 1. TOP HEADER BAR ====================
            var topBar = CreateUiRect("TopHeaderBar", mainContainer.transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -64), Vector2.zero);
            var topImg = topBar.AddComponent<UIImage>();
            topImg.sprite = _spHeaderBar;
            topImg.color = new Color(1f, 1f, 1f, 0.95f);

            // Title & online badge (Left)
            CreateTmp("SysTitle", topBar.transform, "棉纺智能工厂三维数字孪生平台", 22, true, Color.white,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 6), new Vector2(360, -4));
            hud.statusSummaryText = CreateTmp("StatusSummary", topBar.transform, "<color=#2AE87A>● 在线运行</color> (9/9)", 14, true, ColTextSub,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(0, 1), new Vector2(358, 4), new Vector2(540, -4));

            // Center control strip (Mode | View | Hall | Tools)
            float cx = 548f;
            hud.btnModeDashboard = CreateTopPillBtn("BtnModeDash", topBar.transform, "监控大屏", ref cx, 88, _spBtnActive);
            hud.btnModeTeaching  = CreateTopPillBtn("BtnModeTeach", topBar.transform, "教学导览", ref cx, 88, _spBtnNormal);
            cx += 10f;
            hud.btnCamOrbit = CreateTopPillBtn("BtnCamOrbit", topBar.transform, "鸟瞰", ref cx, 62, _spBtnNormal);
            hud.btnCamFps   = CreateTopPillBtn("BtnCamFps", topBar.transform, "漫游", ref cx, 62, _spBtnNormal);
            hud.btnCamTour  = CreateTopPillBtn("BtnCamTour", topBar.transform, "巡检", ref cx, 62, _spBtnNormal);
            cx += 10f;
            hud.btnHall1 = CreateTopPillBtn("BtnHall1", topBar.transform, "主车间", ref cx, 72, _spBtnNormal);
            hud.btnHall2 = CreateTopPillBtn("BtnHall2", topBar.transform, "立体库", ref cx, 72, _spBtnNormal);
            cx += 10f;
            hud.btnRoofCutaway   = CreateTopPillBtn("BtnRoofCut", topBar.transform, "屋顶透视", ref cx, 86, _spBtnNormal);
            hud.btnToggleMarkers = CreateTopPillBtn("BtnMarkers", topBar.transform, "标签", ref cx, 58, _spBtnNormal);

            // Right utility strip (Clock + Help/Settings/Home/Quit)
            hud.clockText = CreateTmp("Clock", topBar.transform, "2026-11-19  20:00:00", 14, false, ColCyan,
                TextAlignmentOptions.MidlineRight, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-520, 4), new Vector2(-334, -4));

            float rx = -324f;
            hud.btnToggleHud    = CreateTopRightBtn("BtnHide", topBar.transform, "隐藏", ref rx, 56, _spBtnNormal);
            hud.btnOpenHelp     = CreateTopRightBtn("BtnHelp", topBar.transform, "帮助", ref rx, 56, _spBtnNormal);
            hud.btnOpenSettings = CreateTopRightBtn("BtnSet",  topBar.transform, "设置", ref rx, 56, _spBtnNormal);
            hud.btnReturnHome   = CreateTopRightBtn("BtnHome", topBar.transform, "主页", ref rx, 56, _spBtnNormal);
            hud.btnRequestQuit  = CreateTopRightBtn("BtnQuit", topBar.transform, "退出", ref rx, 56, _spBtnDanger);

            // ==================== 2. LEFT PANEL (DASHBOARD + TEACHING DUAL MODE) ====================
            var leftPanel = CreateUiRect("LeftPanel", mainContainer.transform, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(14, 88), new Vector2(364, -74));
            AddSlicedImage(leftPanel, _spGlass, Color.white);
            hud.leftPanelRoot = leftPanel;

            // --- 2A. Dashboard Left Content ---
            var dashRoot = CreateUiRect("DashboardContent", leftPanel.transform, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));
            hud.dashboardLeftContent = dashRoot;

            CreateSectionHeader(dashRoot.transform, "产线实时核心 KPI", 0);

            // KPI Row (Output + PassRate Radial Ring + OEE)
            var kpiCard = CreateUiRect("KpiBox", dashRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -158), new Vector2(0, -32));
            AddSlicedImage(kpiCard, _spCard, Color.white);

            // Output & OEE (Left half of KPI box)
            CreateTmp("LblOut", kpiCard.transform, "今日累计纺纱产量", 13, false, ColTextSub, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(12, -26), new Vector2(0, -8));
            hud.kpiOutputText = CreateTmp("ValOut", kpiCard.transform, "1,842.5 kg", 23, true, Color.white, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.62f, 1), new Vector2(12, -58), new Vector2(0, -26));

            CreateTmp("LblOee", kpiCard.transform, "设备综合效率 OEE", 13, false, ColTextSub, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 0), new Vector2(0.45f, 0), new Vector2(12, 34), new Vector2(0, 54));
            hud.kpiOeeText = CreateTmp("ValOee", kpiCard.transform, "91.4%", 15, true, ColCyan, TextAlignmentOptions.MidlineRight,
                new Vector2(0.35f, 0), new Vector2(0.60f, 0), new Vector2(0, 34), new Vector2(0, 54));
            var oeeTrack = CreateUiRect("OeeTrack", kpiCard.transform, new Vector2(0, 0), new Vector2(0.60f, 0), new Vector2(12, 16), new Vector2(0, 28));
            AddSlicedImage(oeeTrack, _spPill, ColDarkTrack);
            hud.kpiOeeBar = CreateBarFill(oeeTrack.transform, ColCyan, 0.91f);

            // Pass Rate Radial Gauge (Right side of KPI box)
            var ringBg = CreateUiRect("PassRingBg", kpiCard.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-108, -44), new Vector2(-14, 50));
            var rbgImg = ringBg.AddComponent<UIImage>();
            rbgImg.sprite = _spRing;
            rbgImg.color = ColDarkTrack;
            var ringFillGo = CreateUiRect("PassRingFill", ringBg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var ringFill = ringFillGo.AddComponent<UIImage>();
            ringFill.sprite = _spRing;
            ringFill.type = UIImage.Type.Filled;
            ringFill.fillMethod = UIImage.FillMethod.Radial360;
            ringFill.fillOrigin = (int)UIImage.Origin360.Top;
            ringFill.color = new Color(0.16f, 0.92f, 0.48f, 1f);
            ringFill.fillAmount = 0.976f;
            hud.kpiPassRateRing = ringFill;

            hud.kpiPassRateText = CreateTmp("PassPct", ringBg.transform, "97.6%", 17, true, Color.white, TextAlignmentOptions.Center,
                new Vector2(0, 0.42f), new Vector2(1, 0.85f), Vector2.zero, Vector2.zero);
            CreateTmp("PassSub", ringBg.transform, "分拣良率", 11, false, ColTextSub, TextAlignmentOptions.Center,
                new Vector2(0, 0.14f), new Vector2(1, 0.46f), Vector2.zero, Vector2.zero);

            // Section 2: 16-Slot Warehouse Occupancy Matrix
            hud.warehouseOccupancyText = CreateSectionHeader(dashRoot.transform, "立体库货位占用  <color=#00DCFF>11 / 16</color>", -168);
            var whCard = CreateUiRect("WarehouseMatrixCard", dashRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -290), new Vector2(0, -198));
            AddSlicedImage(whCard, _spCard, Color.white);
            var gridGo = CreateUiRect("SlotGrid", whCard.transform, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -10));
            var glg = gridGo.AddComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 8;
            glg.cellSize = new Vector2(32, 30);
            glg.spacing = new Vector2(6, 8);
            glg.childAlignment = TextAnchor.MiddleCenter;
            for (int s = 0; s < 16; s++)
            {
                var cell = new GameObject($"Slot_{s + 1:00}");
                cell.transform.SetParent(gridGo.transform, false);
                var cimg = cell.AddComponent<UIImage>();
                cimg.sprite = _spCard;
                cimg.type = UIImage.Type.Sliced;
                cimg.color = s < 11 ? hud.colorSlotOccupied : hud.colorSlotEmpty;
                CreateTmp("N", cell.transform, $"{s + 1:00}", 11, true, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                hud.warehouseSlotCells[s] = cimg;
            }

            // Section 3: 9-Station Live Running Status List (clickable!)
            CreateSectionHeader(dashRoot.transform, "全厂 9 工段运行状态（点击聚焦）", -300);
            var stListBox = CreateUiRect("StationListBox", dashRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -618), new Vector2(0, -330));
            AddSlicedImage(stListBox, _spCard, Color.white);
            for (int i = 0; i < 9; i++)
            {
                var st = catalog.stations[i];
                float yTop = -8 - i * 30.5f;
                var rowGo = CreateUiRect($"StRow_{i}", stListBox.transform, new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(8, yTop - 27), new Vector2(-8, yTop));
                var rimg = rowGo.AddComponent<UIImage>();
                rimg.color = i % 2 == 0 ? new Color(0.06f, 0.16f, 0.28f, 0.55f) : new Color(0.04f, 0.11f, 0.21f, 0.35f);
                var btn = rowGo.AddComponent<Button>();
                btn.targetGraphic = rimg;
                hud.stationQuickButtons[i] = btn;

                var dot = CreateUiRect("Dot", rowGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -5), new Vector2(18, 5));
                var dimg = dot.AddComponent<UIImage>();
                dimg.sprite = _spDot;
                dimg.color = hud.colorOkGreen;
                hud.stationStatusDots[i] = dimg;

                CreateTmp("Nm", rowGo.transform, $"<color=#00DCFF>{st.code}</color>  {st.shortName}", 13, false, Color.white,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(0.72f, 1), new Vector2(24, 0), Vector2.zero);
                hud.stationStatusLabels[i] = CreateTmp("St", rowGo.transform, "<color=#2AE87A>运行中</color>", 12, true, hud.colorOkGreen,
                    TextAlignmentOptions.MidlineRight, new Vector2(0.68f, 0), new Vector2(1, 1), Vector2.zero, new Vector2(-8, 0));
            }

            // Section 4: Event & Alert Log Stream
            CreateSectionHeader(dashRoot.transform, "实时事件与告警日志", -628);
            var logBox = CreateUiRect("EventLogBox", dashRoot.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -658));
            AddSlicedImage(logBox, _spCard, Color.white);
            hud.eventLogText = CreateTmp("LogText", logBox.transform, "系统初始化中...", 12, false, ColTextMain,
                TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-10, -8));
            hud.eventLogText.lineSpacing = 6f;

            // --- 2B. Teaching Tour Left Content ---
            var teachRoot = CreateUiRect("TeachingContent", leftPanel.transform, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));
            hud.teachingLeftContent = teachRoot;

            CreateSectionHeader(teachRoot.transform, "棉纺实训导览与工艺考核", 0);
            var progCard = CreateUiRect("TeachProgCard", teachRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -108), new Vector2(0, -32));
            AddSlicedImage(progCard, _spCard, Color.white);
            hud.teachingProgressText = CreateTmp("ProgTxt", progCard.transform, "实训进度：已学 1/9   考核通过 0/9", 13, true, Color.white,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -28), new Vector2(-12, -6));
            var pt = CreateUiRect("ProgTrack", progCard.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -44), new Vector2(-12, -32));
            AddSlicedImage(pt, _spPill, ColDarkTrack);
            hud.teachingProgressBar = CreateBarFill(pt.transform, ColCyan, 0.11f);
            hud.btnPrevStation = CreateTechButton("BtnPrevSt", progCard.transform, "< 上一工序", 13, true, _spBtnNormal,
                new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(10, 6), new Vector2(-4, 30));
            hud.btnNextStation = CreateTechButton("BtnNextSt", progCard.transform, "下一工序 >", 13, true, _spBtnActive,
                new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(4, 6), new Vector2(-10, 30));

            // Compact 9-step curriculum grid (3x3)
            var stepBox = CreateUiRect("TeachStepsBox", teachRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -252), new Vector2(0, -116));
            AddSlicedImage(stepBox, _spCard, Color.white);
            var sgrid = CreateUiRect("SGrid", stepBox.transform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            var sglg = sgrid.AddComponent<GridLayoutGroup>();
            sglg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            sglg.constraintCount = 3;
            sglg.cellSize = new Vector2(96, 36);
            sglg.spacing = new Vector2(6, 6);
            sglg.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 9; i++)
            {
                var st = catalog.stations[i];
                var sbGo = new GameObject($"TStep_{i}");
                sbGo.transform.SetParent(sgrid.transform, false);
                var img = AddSlicedImage(sbGo, _spBtnNormal, new Color(0.75f, 0.88f, 1f, 0.9f));
                var btn = sbGo.AddComponent<Button>();
                btn.targetGraphic = img;
                hud.teachingStepButtons[i] = btn;
                CreateTmp("N", sbGo.transform, $"{i + 1:00} {st.shortName}", 11, true, Color.white, TextAlignmentOptions.Center,
                    new Vector2(0, 0.45f), new Vector2(1, 1), new Vector2(2, 0), new Vector2(-2, -2));
                hud.teachingStepCheckTexts[i] = CreateTmp("C", sbGo.transform, "待学习", 10, false, ColTextSub, TextAlignmentOptions.Center,
                    new Vector2(0, 0), new Vector2(1, 0.48f), Vector2.zero, Vector2.zero);
            }

            // Teaching Goal & Key Points
            CreateSectionHeader(teachRoot.transform, "本工段教学目标与工艺要点", -262);
            var goalBox = CreateUiRect("GoalBox", teachRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -496), new Vector2(0, -292));
            AddSlicedImage(goalBox, _spCard, Color.white);
            hud.teachingGoalText = CreateTmp("GoalTxt", goalBox.transform, "", 13, false, Color.white, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0.56f), new Vector2(1, 1), new Vector2(10, 2), new Vector2(-10, -8));
            hud.teachingKeyPointsText = CreateTmp("KeyPtsTxt", goalBox.transform, "", 12, false, ColTextSub, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0.56f), new Vector2(10, 6), new Vector2(-10, 0));
            hud.teachingKeyPointsText.lineSpacing = 4f;

            // Interactive Quiz Card
            CreateSectionHeader(teachRoot.transform, "随堂工艺考核小测", -506);
            var quizBox = CreateUiRect("QuizBox", teachRoot.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -536));
            AddSlicedImage(quizBox, _spCard, Color.white);
            hud.quizQuestionText = CreateTmp("QuizQ", quizBox.transform, "随堂问答...", 13, true, Color.white, TextAlignmentOptions.TopLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -58), new Vector2(-10, -8));
            for (int q = 0; q < 3; q++)
            {
                float yTop = -62 - q * 44;
                var qBtn = CreateTechButton($"QuizOpt_{q}", quizBox.transform, $"选项 {q + 1}", 12, false, _spBtnNormal,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, yTop - 38), new Vector2(-10, yTop));
                var qTxt = qBtn.GetComponentInChildren<TextMeshProUGUI>();
                qTxt.alignment = TextAlignmentOptions.MidlineLeft;
                qTxt.margin = new Vector4(10, 2, 10, 2);
                hud.quizOptionButtons[q] = qBtn;
                hud.quizOptionTexts[q] = qTxt;
            }
            hud.quizFeedbackText = CreateTmp("QuizFb", quizBox.transform, "", 12, false, ColTextSub, TextAlignmentOptions.TopLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 8), new Vector2(-10, 84));
            teachRoot.SetActive(false);

            // ==================== 3. RIGHT PANEL (DEVICE INSPECTOR & TELEMETRY) ====================
            var rightPanel = CreateUiRect("RightPanel", mainContainer.transform, new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-374, 88), new Vector2(-14, -74));
            AddSlicedImage(rightPanel, _spGlass, Color.white);
            hud.rightPanelRoot = rightPanel;

            var rBody = CreateUiRect("RightBody", rightPanel.transform, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));

            // Device Header Card
            var devHead = CreateUiRect("DeviceHeader", rBody.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -104), Vector2.zero);
            AddSlicedImage(devHead, _spCard, Color.white);
            var codePill = CreateUiRect("CodePill", devHead.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -34), new Vector2(78, -10));
            AddSlicedImage(codePill, _spBtnActive, ColCyan);
            hud.inspectorCodeText = CreateTmp("Code", codePill.transform, "ST-01", 14, true, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var stDotGo = CreateUiRect("StatusDot", devHead.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(88, -28), new Vector2(100, -16));
            hud.inspectorStatusDot = stDotGo.AddComponent<UIImage>();
            hud.inspectorStatusDot.sprite = _spDot;
            hud.inspectorStatusDot.color = hud.colorOkGreen;
            hud.inspectorStatusText = CreateTmp("StatusTxt", devHead.transform, "运行正常", 13, true, hud.colorOkGreen,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(0.65f, 1), new Vector2(106, -34), new Vector2(0, -10));

            hud.btnToggleStationRun = CreateTechButton("BtnRemoteToggle", devHead.transform, "远程停机", 13, true, _spBtnDanger,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-98, -36), new Vector2(-10, -8));
            hud.btnToggleStationRunLabel = hud.btnToggleStationRun.GetComponentInChildren<TextMeshProUGUI>();

            hud.inspectorNameText = CreateTmp("DevName", devHead.transform, "往复抓棉机（开松混棉）", 19, true, Color.white,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -72), new Vector2(-10, -40));
            hud.inspectorEngNameText = CreateTmp("DevEng", devHead.transform, "AUTOMATIC BALE OPENER", 12, false, ColCyan,
                TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 6), new Vector2(-10, 28));

            // Equipment Preview Image
            var imgFrame = CreateUiRect("PreviewFrame", rBody.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -242), new Vector2(0, -112));
            AddSlicedImage(imgFrame, _spCard, Color.white);
            var imgGo = CreateUiRect("PreviewImg", imgFrame.transform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            hud.inspectorPreviewImage = imgGo.AddComponent<UIImage>();
            hud.inspectorPreviewImage.preserveAspect = true;

            // Live Telemetry Bars
            CreateSectionHeader(rBody.transform, "实时运行传感器遥测", -250);
            var telCard = CreateUiRect("TelemetryCard", rBody.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -412), new Vector2(0, -280));
            AddSlicedImage(telCard, _spCard, Color.white);
            CreateTelemetryRow(telCard.transform, "主轴 / 工作转速", -10, ColCyan, out hud.telemetryRpmText, out hud.telemetryRpmBar);
            CreateTelemetryRow(telCard.transform, "驱动电机温度", -42, new Color(1f, 0.68f, 0.16f), out hud.telemetryTempText, out hud.telemetryTempBar);
            CreateTelemetryRow(telCard.transform, "实时运行功耗", -74, new Color(0.32f, 0.86f, 1f), out hud.telemetryPowerText, out hud.telemetryPowerBar);
            CreateTelemetryRow(telCard.transform, "设备运行负荷", -106, new Color(0.16f, 0.92f, 0.48f), out hud.telemetryLoadText, out hud.telemetryLoadBar);

            // Specs Table
            CreateSectionHeader(rBody.transform, "设备技术规格参数", -420);
            var specCard = CreateUiRect("SpecsCard", rBody.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -616), new Vector2(0, -450));
            AddSlicedImage(specCard, _spCard, Color.white);
            hud.specsTableText = CreateTmp("SpecsTxt", specCard.transform, "", 13, false, Color.white, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -10));
            hud.specsTableText.lineSpacing = 8f;

            // Process Principle Summary
            CreateSectionHeader(rBody.transform, "工艺原理说明", -624);
            var sumCard = CreateUiRect("SummaryCard", rBody.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -654));
            AddSlicedImage(sumCard, _spCard, Color.white);
            hud.processSummaryText = CreateTmp("SumTxt", sumCard.transform, "", 13, false, ColTextMain, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8));
            hud.processSummaryText.lineSpacing = 5f;

            // ==================== 4. BOTTOM PROCESS FLOW & SIMULATION TRANSPORT BAR ====================
            var bottomBar = CreateUiRect("BottomFlowBar", mainContainer.transform, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(14, 10), new Vector2(-14, 78));
            AddSlicedImage(bottomBar, _spGlass, Color.white);

            // 9 Spinning Process Station Nodes (Left 1280px)
            float fx = 16f;
            for (int i = 0; i < 9; i++)
            {
                var st = catalog.stations[i];
                var nodeBtn = CreateTechButton($"FlowNode_{i + 1}", bottomBar.transform, $"{i + 1:00} {st.shortName}", 14, true,
                    _spBtnNormal, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(fx, -22), new Vector2(fx + 124, 22));
                var ind = CreateUiRect("Indicator", nodeBtn.transform, new Vector2(0.1f, 0), new Vector2(0.9f, 0), new Vector2(0, 2), new Vector2(0, 5));
                var indImg = ind.AddComponent<UIImage>();
                indImg.color = hud.colorOkGreen;
                hud.flowNodeButtons[i] = nodeBtn;
                hud.flowNodeIndicators[i] = indImg;

                if (i < 8)
                {
                    CreateTmp($"Arr_{i}", bottomBar.transform, ">", 16, true, ColCyan, TextAlignmentOptions.Center,
                        new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(fx + 124, -14), new Vector2(fx + 140, 14));
                }
                fx += 140f;
            }

            // Right Simulation Transport Controls
            float tx = -548f;
            hud.btnSimPlayPause = CreateBottomRightBtn("BtnPlayPause", bottomBar.transform, "暂停", ref tx, 76, _spBtnActive);
            hud.btnSimPlayPauseLabel = hud.btnSimPlayPause.GetComponentInChildren<TextMeshProUGUI>();
            hud.btnSpeed05 = CreateBottomRightBtn("BtnSpd05", bottomBar.transform, "0.5x", ref tx, 56, _spBtnNormal);
            hud.btnSpeed10 = CreateBottomRightBtn("BtnSpd10", bottomBar.transform, "1x",   ref tx, 50, _spBtnActive);
            hud.btnSpeed20 = CreateBottomRightBtn("BtnSpd20", bottomBar.transform, "2x",   ref tx, 50, _spBtnNormal);
            hud.btnSpeed40 = CreateBottomRightBtn("BtnSpd40", bottomBar.transform, "4x",   ref tx, 50, _spBtnNormal);
            hud.btnSimReset = CreateBottomRightBtn("BtnReset", bottomBar.transform, "复位", ref tx, 64, _spBtnNormal);
            hud.btnCollapseSidePanels = CreateBottomRightBtn("BtnCollapse", bottomBar.transform, "收起侧栏", ref tx, 94, _spBtnNormal);

            // ==================== 5. CROSSHAIR, FPS HINT, TOOLTIP & MODALS ====================
            var crossGo = CreateUiRect("FPS_Crosshair", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-20, -20), new Vector2(20, 20));
            var crossImg = crossGo.AddComponent<UIImage>();
            crossImg.sprite = _spCrosshair;
            crossImg.raycastTarget = false;
            crossGo.SetActive(false);
            hud.crosshairObject = crossGo;

            var hintGo = CreateUiRect("FPS_HintBanner", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-340, -108), new Vector2(340, -76));
            AddSlicedImage(hintGo, _spCard, new Color(0.05f, 0.16f, 0.28f, 0.88f));
            CreateTmp("HintTxt", hintGo.transform, "漫游模式：WASD 移动 | Shift 加速 | Tab/Esc 释放鼠标 | E 选中设备 | R 回到入口", 13, false, ColCyan,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hintGo.SetActive(false);
            hud.fpsHintBanner = hintGo;

            var tipGo = CreateUiRect("HoverTooltip", canvasGo.transform, new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(0, 0), new Vector2(280, 62));
            var tipBg = AddSlicedImage(tipGo, _spCard, new Color(0.04f, 0.14f, 0.26f, 0.94f));
            tipBg.raycastTarget = false;
            hud.hoverTooltipText = CreateTmp("TipTxt", tipGo.transform, "", 13, true, Color.white, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
            hud.hoverTooltipText.raycastTarget = false;
            tipGo.SetActive(false);
            hud.hoverTooltipRoot = tipGo;

            // Settings Modal
            hud.settingsModal = BuildSettingsModal(canvasGo.transform, out hud.btnQualMed, out hud.btnQualHigh,
                out hud.btnQualUltra, out hud.sensSlider, out hud.sensValueText, out hud.btnCloseSettings);

            // Help Modal
            hud.helpModal = BuildHelpModal(canvasGo.transform, out hud.btnCloseHelp);

            // Quit Confirmation Modal
            hud.quitConfirmModal = BuildQuitModal(canvasGo.transform, out hud.btnConfirmQuit, out hud.btnCancelQuit);
        }

        // ==================== REUSABLE UI FACTORY HELPERS ====================

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
            scaler.matchWidthOrHeight = 0.5f;

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

        private static UIImage AddSlicedImage(GameObject go, Sprite sprite, Color tint)
        {
            var img = go.GetComponent<UIImage>() ?? go.AddComponent<UIImage>();
            img.sprite = sprite;
            img.type = sprite != null ? UIImage.Type.Sliced : UIImage.Type.Simple;
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

        private static TextMeshProUGUI CreateSectionHeader(Transform parent, string title, float yTop)
        {
            var bar = CreateUiRect("SecBar", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, yTop - 22), new Vector2(4, yTop - 4));
            bar.AddComponent<UIImage>().color = ColCyan;
            return CreateTmp($"Hdr_{title}", parent, title, 15, true, Color.white, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, yTop - 26), new Vector2(0, yTop));
        }

        private static UIImage CreateBarFill(Transform trackParent, Color col, float initialFill)
        {
            var go = CreateUiRect("Fill", trackParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = go.AddComponent<UIImage>();
            img.sprite = _spPill;
            img.type = UIImage.Type.Filled;
            img.fillMethod = UIImage.FillMethod.Horizontal;
            img.color = col;
            img.fillAmount = initialFill;
            return img;
        }

        private static void CreateTelemetryRow(Transform parent, string label, float yTop, Color barColor,
            out TextMeshProUGUI valText, out UIImage barFill)
        {
            CreateTmp("Lbl", parent, label, 12, false, ColTextSub, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(12, yTop - 18), new Vector2(0, yTop));
            valText = CreateTmp("Val", parent, "0", 13, true, Color.white, TextAlignmentOptions.MidlineRight,
                new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, yTop - 18), new Vector2(-12, yTop));
            var trk = CreateUiRect("Trk", parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, yTop - 28), new Vector2(-12, yTop - 20));
            AddSlicedImage(trk, _spPill, ColDarkTrack);
            barFill = CreateBarFill(trk.transform, barColor, 0.7f);
        }

        private static Button CreateTechButton(string name, Transform parent, string label, float fontSize, bool bold,
            Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = CreateUiRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = AddSlicedImage(go, sprite, Color.white);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 1.0f, 1.0f, 1f);
            colors.pressedColor = new Color(0.65f, 0.88f, 1f, 1f);
            btn.colors = colors;

            var tmp = CreateTmp("Label", go.transform, label, fontSize, bold, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
            tmp.raycastTarget = false;
            return btn;
        }

        private static Button CreateTopPillBtn(string name, Transform parent, string label, ref float xCursor, float width, Sprite sp)
        {
            var btn = CreateTechButton(name, parent, label, 14, true, sp,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(xCursor, -18), new Vector2(xCursor + width, 18));
            xCursor += width + 6f;
            return btn;
        }

        private static Button CreateTopRightBtn(string name, Transform parent, string label, ref float xCursor, float width, Sprite sp)
        {
            var btn = CreateTechButton(name, parent, label, 13, true, sp,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(xCursor, -17), new Vector2(xCursor + width, 17));
            xCursor += width + 6f;
            return btn;
        }

        private static Button CreateBottomRightBtn(string name, Transform parent, string label, ref float xCursor, float width, Sprite sp)
        {
            var btn = CreateTechButton(name, parent, label, 14, true, sp,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(xCursor, -20), new Vector2(xCursor + width, 20));
            xCursor += width + 8f;
            return btn;
        }

        private static GameObject BuildSettingsModal(Transform canvasParent, out Button btnMed, out Button btnHigh, out Button btnUltra,
            out Slider sensSlider, out TextMeshProUGUI sensVal, out Button btnClose)
        {
            var overlay = CreateUiRect("SettingsModal", canvasParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<UIImage>().color = new Color(0.01f, 0.04f, 0.09f, 0.72f);

            var card = CreateUiRect("Card", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-290, -195), new Vector2(290, 195));
            AddSlicedImage(card, _spGlass, Color.white);

            CreateTmp("Title", card.transform, "系统画质与操控设置", 22, true, ColCyan, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -54), new Vector2(-20, -16));

            CreateTmp("QLbl", card.transform, "桌面端 URP 渲染画质等级", 15, true, Color.white, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -98), new Vector2(-36, -70));

            btnMed   = CreateTechButton("BtnQMed",   card.transform, "中画质 (2x MSAA)", 14, true, _spBtnNormal, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -148), new Vector2(198, -106));
            btnHigh  = CreateTechButton("BtnQHigh",  card.transform, "高画质 (4x MSAA)", 14, true, _spBtnNormal, new Vector2(0, 1), new Vector2(0, 1), new Vector2(209, -148), new Vector2(371, -106));
            btnUltra = CreateTechButton("BtnQUltra", card.transform, "极致画质 (SSAO)",  14, true, _spBtnActive, new Vector2(0, 1), new Vector2(0, 1), new Vector2(382, -148), new Vector2(544, -106));

            CreateTmp("SLbl", card.transform, "鼠标视角旋转灵敏度", 15, true, Color.white, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0.7f, 1), new Vector2(36, -206), new Vector2(0, -176));
            sensVal = CreateTmp("SVal", card.transform, "2.0", 16, true, ColCyan, TextAlignmentOptions.MidlineRight,
                new Vector2(0.7f, 1), new Vector2(1, 1), new Vector2(0, -206), new Vector2(-36, -176));

            var sliderGo = CreateUiRect("SensSlider", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -246), new Vector2(-36, -216));
            var bg = CreateUiRect("Background", sliderGo.transform, new Vector2(0, 0.25f), new Vector2(1, 0.75f), Vector2.zero, Vector2.zero);
            AddSlicedImage(bg, _spPill, ColDarkTrack);
            var fillArea = CreateUiRect("FillArea", sliderGo.transform, new Vector2(0, 0.25f), new Vector2(1, 0.75f), Vector2.zero, Vector2.zero);
            var fill = CreateUiRect("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddSlicedImage(fill, _spPill, ColCyan);
            var handleArea = CreateUiRect("HandleArea", sliderGo.transform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            var handle = CreateUiRect("Handle", handleArea.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-12, 0), new Vector2(12, 0));
            var hImg = handle.AddComponent<UIImage>();
            hImg.sprite = _spDot;
            hImg.color = Color.white;

            sensSlider = sliderGo.AddComponent<Slider>();
            sensSlider.fillRect = fill.GetComponent<RectTransform>();
            sensSlider.handleRect = handle.GetComponent<RectTransform>();
            sensSlider.targetGraphic = hImg;
            sensSlider.minValue = 0.5f;
            sensSlider.maxValue = 5.0f;
            sensSlider.value = 2.0f;

            CreateTmp("Tip", card.transform, "提示：按 F1 鸟瞰全景 / F2 漫游 / F3 自动巡检 / F4 屋顶透视 / H 隐藏界面", 13, false, ColTextSub,
                TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 78), new Vector2(-24, 114));

            btnClose = CreateTechButton("BtnCloseSettings", card.transform, "保存并返回", 16, true, _spBtnActive,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-110, 24), new Vector2(110, 68));

            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject BuildHelpModal(Transform canvasParent, out Button btnClose)
        {
            var overlay = CreateUiRect("HelpModal", canvasParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<UIImage>().color = new Color(0.01f, 0.04f, 0.09f, 0.72f);

            var card = CreateUiRect("Card", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-320, -230), new Vector2(320, 230));
            AddSlicedImage(card, _spGlass, Color.white);

            CreateTmp("Title", card.transform, "快捷键与系统操作说明", 22, true, ColCyan, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -54), new Vector2(-20, -16));

            string helpBody =
                "<color=#00DCFF><b>【视角操控】</b></color>\n" +
                "• <b>F1 / 鸟瞰</b>：鼠标右键旋转、中键或 WASD 平移、滚轮缩放\n" +
                "• <b>F2 / 漫游</b>：WASD 步入行走、Shift 加速、Tab/Esc 释放鼠标、R 回入口\n" +
                "• <b>F3 / 巡检</b>：自动沿 ST-01 → ST-09 纺纱流程环绕导览\n" +
                "• <b>F4 / 屋顶透视</b>：一键隐藏/恢复厂房顶棚，俯瞰全局设备布局\n\n" +
                "<color=#00DCFF><b>【数字孪生与实训交互】</b></color>\n" +
                "• <b>左键点击 3D 设备</b>（漫游中按 <b>E</b>）：高亮设备并弹出参数/远程启停\n" +
                "• <b>顶部模式切换</b>：在“监控大屏”与“教学导览（含随堂小测）”间无缝切换\n" +
                "• <b>H 键</b>：一键隐藏/恢复所有 HUD 界面，便于沉浸观览与论文截图";

            var txt = CreateTmp("HelpBody", card.transform, helpBody, 15, false, ColTextMain, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(36, 86), new Vector2(-36, -68));
            txt.lineSpacing = 8f;

            btnClose = CreateTechButton("BtnCloseHelp", card.transform, "我知道了", 16, true, _spBtnActive,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-100, 24), new Vector2(100, 68));

            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject BuildQuitModal(Transform canvasParent, out Button btnConfirm, out Button btnCancel)
        {
            var overlay = CreateUiRect("QuitConfirmModal", canvasParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<UIImage>().color = new Color(0.01f, 0.04f, 0.09f, 0.78f);

            var card = CreateUiRect("Card", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-220, -110), new Vector2(220, 110));
            AddSlicedImage(card, _spGlass, Color.white);

            CreateTmp("Title", card.transform, "确认退出系统", 22, true, ColCyan, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -56), new Vector2(-20, -18));
            CreateTmp("Desc", card.transform, "确定要退出棉纺智能工厂三维数字孪生平台吗？", 16, false, ColTextMain, TextAlignmentOptions.Center,
                new Vector2(0, 0.35f), new Vector2(1, 0.72f), new Vector2(24, 0), new Vector2(-24, 0));

            btnCancel = CreateTechButton("BtnCancelQuit", card.transform, "取消返回", 15, true, _spBtnNormal,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-168, 24), new Vector2(-12, 68));
            btnConfirm = CreateTechButton("BtnConfirmQuit", card.transform, "确认退出", 15, true, _spBtnDanger,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(12, 24), new Vector2(168, 68));

            overlay.SetActive(false);
            return overlay;
        }
    }
}
