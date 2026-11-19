using System;
using System.IO;
using System.Text;
using CNC.DigitalTwin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CNC.EditorTools
{
    public static class RunFullOptimizationPipeline
    {
        [MenuItem("CNC Optimize/Run Full Optimization Pipeline (Step 1 + 2 + 3 + Capture)")]
        public static void RunAll()
        {
            Step1_URPAndAssetSetup.RunUrpAndFontsOnly();
            Step2_ModelingAndRenderingOptimizer.Run();
            Step3_BuildFrontendScenes.Run();
            SceneBenchmarkAndCapture.CaptureStyleA();
            Debug.Log("[CNC PIPELINE] ALL STEPS + STYLE A CAPTURE COMPLETED!");
        }

        public static void RunStyleAAndCapture()
        {
            Step3_BuildFrontendScenes.Run();
            SceneBenchmarkAndCapture.VerifyStyleAScenesAndTests();
            SceneBenchmarkAndCapture.CaptureStyleA();
            Debug.Log("[CNC PIPELINE] STYLE A + SCENE FIXES + TESTS + CAPTURE COMPLETED!");
        }
    }

    public static class SceneBenchmarkAndCapture
    {
        private const string ArtifactShotDir = @"C:\Users\qqcom\.gemini\antigravity\brain\8cf605aa-ffcf-453b-9a7e-a7b8353edcb5\screenshots";

        public static void VerifyStyleAScenesAndTests()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;

            void Check(string name, bool cond, string detail = "")
            {
                if (cond) { pass++; sb.AppendLine($"[PASS] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" ({detail})")); }
                else { fail++; sb.AppendLine($"[FAIL] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" ({detail})")); }
            }

            sb.AppendLine("=== CNC STYLE A AUTOMATED TEST REPORT ===");

            // 1. Test Play.unity
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);
            var startCtrl = UnityEngine.Object.FindObjectOfType<StartScreenController>();
            Check("Play.unity: StartScreenController present", startCtrl != null);
            if (startCtrl != null)
            {
                Check("Play.unity: All CTA & Modal buttons wired",
                    startCtrl.btnEnterDashboard != null && startCtrl.btnEnterTeaching != null &&
                    startCtrl.btnOpenSettings != null && startCtrl.btnQuit != null &&
                    startCtrl.loadingOverlay != null && startCtrl.loadingBarFill != null &&
                    startCtrl.settingsModal != null);
            }
            var playScaler = UnityEngine.Object.FindObjectOfType<UnityEngine.UI.CanvasScaler>();
            Check("Play.unity: CanvasScaler 1920x1080", playScaler != null &&
                playScaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                playScaler.referenceResolution == new Vector2(1920, 1080));
            Check("Play.unity: Zero missing scripts", CountMissingScriptsInScene() == 0);

            // 2. Test SampleScene.unity
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Check("SampleScene: Zero missing scripts", CountMissingScriptsInScene() == 0);

            var sim = UnityEngine.Object.FindObjectOfType<SimulationState>();
            Check("SampleScene: SimulationState & 9-station catalog", sim != null && sim.catalog != null && sim.catalog.stations.Count == 9);
            if (sim != null && sim.catalog != null && sim.catalog.stations.Count == 9)
            {
                bool allSprites = true;
                foreach (var st in sim.catalog.stations) if (st.previewSprite == null) allSprites = false;
                Check("SampleScene: All 9 stations have preview sprites", allSprites);
                Check("SampleScene: ST-04 末并 text fixed (not 精梳机)", !sim.catalog.stations[3].processSummary.Contains("精梳机"));
            }

            var camRig = UnityEngine.Object.FindObjectOfType<CameraRigManager>();
            Check("SampleScene: CameraRigManager defaults to FirstPersonWalk",
                camRig != null && AppLaunchContext.InitialCameraMode == CameraViewMode.FirstPersonWalk &&
                camRig.mainCamera != null && camRig.fpsPlayerRoot != null && camRig.fpsController != null && camRig.fpsMouseLook != null);
            Check("SampleScene: Roof Cutaway meshes registered",
                camRig != null && camRig.workshop1RoofObjects.Count > 0 && camRig.workshop2RoofObjects.Count > 0,
                camRig != null ? $"W1={camRig.workshop1RoofObjects.Count}, W2={camRig.workshop2RoofObjects.Count}" : "");

            if (camRig != null && camRig.fpsPlayerRoot != null)
            {
                Vector3 sp = camRig.fpsPlayerRoot.transform.position;
                bool hasFloor = Physics.Raycast(sp + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 15f);
                Check("SampleScene: FPS spawn grounded over solid floor collider", hasFloor && sp.y >= 1.5f,
                    hasFloor ? $"spawn={sp}, floorY={hit.point.y:F2} ({hit.collider.name})" : $"spawn={sp}, NO FLOOR HIT");
            }

            var hud = UnityEngine.Object.FindObjectOfType<DigitalTwinHUD>();
            Check("SampleScene: DigitalTwinHUD & Style A sprites wired",
                hud != null && hud.spPillActiveWhite != null && hud.spPillActiveSky != null &&
                hud.btnModeDashboard != null && hud.btnModeTeaching != null &&
                hud.btnCamFps != null && hud.btnCamOrbit != null && hud.btnCamTour != null &&
                hud.btnSimPlayPause != null && hud.btnSpeedCycle != null && hud.btnToggleStationRun != null &&
                hud.flowNodeButtons != null && hud.flowNodeButtons.Length == 9 && hud.flowNodeButtons[8] != null &&
                hud.warehouseSlotCells != null && hud.warehouseSlotCells.Length == 16 && hud.warehouseSlotCells[15] != null);

            // Particle smoke shader verification
            var psrs = UnityEngine.Object.FindObjectsOfType<ParticleSystemRenderer>(true);
            int badParticles = 0;
            foreach (var p in psrs)
            {
                if (p.sharedMaterial == null || p.sharedMaterial.shader == null ||
                    !p.sharedMaterial.shader.name.Contains("Particles"))
                    badParticles++;
            }
            Check("SampleScene: All ParticleSystemRenderers use URP Particle shader (no black clumps)",
                badParticles == 0, $"total={psrs.Length}, bad={badParticles}");

            // Floor holo pad shader verification
            var padMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_HoloFloorPad.mat");
            Check("SampleScene: Floor pad uses transparent URP/Unlit ring",
                padMat != null && padMat.shader != null && padMat.shader.name == "Universal Render Pipeline/Unlit" && padMat.GetFloat("_Surface") > 0.5f);

            var interactables = UnityEngine.Object.FindObjectsOfType<MachineInteractable>(true);
            var markers = UnityEngine.Object.FindObjectsOfType<WorldStationMarker>(true);
            Check("SampleScene: 9 clickable MachineInteractables & 9 WorldStationMarkers",
                interactables.Length == 9 && markers.Length == 9, $"inter={interactables.Length}, markers={markers.Length}");

            sb.AppendLine($"SUMMARY: {pass} PASSED, {fail} FAILED");
            string report = sb.ToString();
            Directory.CreateDirectory(ArtifactShotDir);
            File.WriteAllText(Path.Combine(ArtifactShotDir, "styleA_test_report.txt"), report, Encoding.UTF8);
            Debug.Log(report);
            if (fail > 0)
                Debug.LogError($"[CNC TEST] {fail} test(s) failed! See styleA_test_report.txt");
        }

        private static int CountMissingScriptsInScene()
        {
            int missing = 0;
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
            {
                var comps = go.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                    if (comps[i] == null) missing++;
            }
            return missing;
        }

        [MenuItem("CNC Optimize/Capture Style A Verification Screenshots")]
        public static void CaptureStyleA()
        {
            Directory.CreateDirectory(ArtifactShotDir);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            var hudCanvasGo = GameObject.Find("HUD_Canvas");
            var hudCanvas = hudCanvasGo != null ? hudCanvasGo.GetComponent<Canvas>() : null;
            var hudComp = hudCanvasGo != null ? hudCanvasGo.GetComponent<DigitalTwinHUD>() : null;
            var cat = AssetDatabase.LoadAssetAtPath<WorkshopCatalog>("Assets/_Project/Configs/WorkshopCatalog.asset");

            if (hudComp != null && cat != null && cat.stations.Count > 0)
            {
                var st0 = cat.stations[0];
                if (hudComp.inspectorPreviewImage != null) hudComp.inspectorPreviewImage.sprite = st0.previewSprite;
                if (hudComp.specsTableText != null)
                    hudComp.specsTableText.text = "<color=#94A3B8>打手宽度</color>    <b>2300</b> <color=#7DD3FC>mm</color>\n<color=#94A3B8>额定产量</color>    <b>≤ 2400</b> <color=#7DD3FC>kg/h</color>\n<color=#94A3B8>装机功率</color>    <b>14.7</b> <color=#7DD3FC>kW</color>\n<color=#94A3B8>设备净重</color>    <b>4050</b> <color=#7DD3FC>kg</color>";
                if (hudComp.processSummaryText != null) hudComp.processSummaryText.text = st0.processSummary;
                if (hudComp.telemetryRpmText != null) hudComp.telemetryRpmText.text = "964 r/min";
                if (hudComp.telemetryTempText != null) hudComp.telemetryTempText.text = "41.3 °C";
                if (hudComp.telemetryLoadText != null) hudComp.telemetryLoadText.text = "84% · 14.6 kW";
            }

            // 1. Default First-Person Walk Entrance View + Style A HUD
            RenderShot(Path.Combine(ArtifactShotDir, "styleA_01_walk_entrance.png"),
                new Vector3(198f, 4.2f, 156f), new Vector3(145f, 5.5f, 128f), 55f, hudCanvas, 1920, 1080);

            // 2. ST-01 Focus (Verify Black Smoke Clumps ELIMINATED!)
            if (cat != null && cat.stations.Count > 0)
            {
                var st0 = cat.stations[0];
                RenderShot(Path.Combine(ArtifactShotDir, "styleA_02_st01_smoke_fixed.png"),
                    st0.focusCameraPos, st0.focusLookAtPos, 54f, hudCanvas, 1920, 1080);
            }

            // 3. ST-05 Focus (Verify Unobstructed Hero View + Zero Cyan Floor Square!)
            if (cat != null && cat.stations.Count > 4)
            {
                var st4 = cat.stations[4];
                if (hudComp != null)
                {
                    if (hudComp.inspectorCodeText != null) hudComp.inspectorCodeText.text = $"{st4.code} · {st4.shortName}";
                    if (hudComp.inspectorNameText != null) hudComp.inspectorNameText.text = st4.name;
                    if (hudComp.inspectorEngNameText != null) hudComp.inspectorEngNameText.text = st4.englishName;
                    if (hudComp.inspectorPreviewImage != null) hudComp.inspectorPreviewImage.sprite = st4.previewSprite;
                    if (hudComp.processSummaryText != null) hudComp.processSummaryText.text = st4.processSummary;
                }
                RenderShot(Path.Combine(ArtifactShotDir, "styleA_03_st05_fixed_angle.png"),
                    st4.focusCameraPos, st4.focusLookAtPos, 54f, hudCanvas, 1920, 1080);

                // 3b. Teaching Tour Mode capture (expanded left island + quiz)
                if (hudComp != null)
                {
                    if (hudComp.dashboardLeftContent != null) hudComp.dashboardLeftContent.SetActive(false);
                    if (hudComp.teachingLeftContent != null) hudComp.teachingLeftContent.SetActive(true);
                    if (hudComp.leftPanelRect != null) hudComp.leftPanelRect.sizeDelta = new Vector2(296f, 700f);
                    if (hudComp.teachingGoalText != null) hudComp.teachingGoalText.text = $"<color=#7DD3FC><b>{st4.code} 教学目标</b></color>\n{st4.teachingGoal}";
                    if (hudComp.teachingKeyPointsText != null) hudComp.teachingKeyPointsText.text = string.Join("\n", st4.teachingKeyPoints);
                    if (hudComp.quizQuestionText != null && st4.quiz != null) hudComp.quizQuestionText.text = $"随堂考核：{st4.quiz.question}";
                    for (int q = 0; q < 3 && st4.quiz != null && q < st4.quiz.options.Length; q++)
                        if (hudComp.quizOptionTexts[q] != null) hudComp.quizOptionTexts[q].text = st4.quiz.options[q];
                }
                RenderShot(Path.Combine(ArtifactShotDir, "styleA_04_teaching_mode.png"),
                    st4.focusCameraPos, st4.focusLookAtPos, 54f, hudCanvas, 1920, 1080);
            }

            // 4. Play.unity Style A VisionOS Start Screen
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);
            var playCam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            var playCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (playCam != null)
                RenderWithCanvas(playCam, playCanvas, Path.Combine(ArtifactShotDir, "styleA_00_play_start.png"), 1920, 1080);

            Debug.Log("[CNC StyleA] Captured all Style A verification screenshots!");
        }

        private static void RenderShot(string outPath, Vector3 camPos, Vector3 lookAt, float fov, Canvas uiCanvas, int width, int height)
        {
            var go = new GameObject("__TempCaptureCam");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = camPos;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1500f;
            cam.allowHDR = true;
            cam.allowMSAA = true;

            var uac = go.AddComponent<UniversalAdditionalCameraData>();
            uac.renderPostProcessing = true;
            uac.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            uac.antialiasingQuality = AntialiasingQuality.High;
            uac.renderShadows = true;

            RenderWithCanvas(cam, uiCanvas, outPath, width, height);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static void RenderWithCanvas(Camera cam, Canvas uiCanvas, string outPath, int width, int height)
        {
            RenderMode origMode = RenderMode.ScreenSpaceOverlay;
            Camera origCam = null;
            float origPlane = 100f;

            if (uiCanvas != null)
            {
                origMode = uiCanvas.renderMode;
                origCam = uiCanvas.worldCamera;
                origPlane = uiCanvas.planeDistance;
                uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                uiCanvas.worldCamera = cam;
                uiCanvas.planeDistance = 1.5f;
                Canvas.ForceUpdateCanvases();
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            if (uiCanvas != null)
            {
                uiCanvas.renderMode = origMode;
                uiCanvas.worldCamera = origCam;
                uiCanvas.planeDistance = origPlane;
            }

            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
