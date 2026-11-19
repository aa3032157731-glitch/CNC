using System;
using System.IO;
using System.Text;
using CNC.DigitalTwin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

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
            SceneBenchmarkAndCapture.CaptureAfter();
            Debug.Log("[CNC PIPELINE] ALL STEPS + AFTER BENCHMARK COMPLETED SUCCESSFULLY!");
        }

        public static void RunStep2And3AndCapture()
        {
            Step2_ModelingAndRenderingOptimizer.Run();
            Step3_BuildFrontendScenes.Run();
            SceneBenchmarkAndCapture.CaptureAfter();
            Debug.Log("[CNC PIPELINE] STEP 2 + 3 + AFTER BENCHMARK COMPLETED SUCCESSFULLY!");
        }
    }

    public static class SceneBenchmarkAndCapture
    {
        private const string ArtifactShotDir = @"C:\Users\qqcom\.gemini\antigravity\brain\8cf605aa-ffcf-453b-9a7e-a7b8353edcb5\screenshots";

        public static void CaptureBefore()
        {
            CaptureSuite("before", false);
        }

        [MenuItem("CNC Optimize/Capture After Screenshots & Metrics")]
        public static void CaptureAfter()
        {
            CaptureSuite("after", true);
        }

        private static void CaptureSuite(string tag, bool isAfter)
        {
            Directory.CreateDirectory(ArtifactShotDir);
            var sb = new StringBuilder();
            sb.AppendLine($"=== CNC BENCHMARK ({tag.ToUpper()}) ===");
            sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"RenderPipeline: {(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name : "Built-in RP")}");

            // 1. Open SampleScene and collect accurate metrics
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            int totalRenderers = 0;
            int activeRenderers = 0;
            int lod0Renderers = 0;
            int staticRenderers = 0;
            int instancedMats = 0;
            int totalMatSlots = 0;
            long totalTrisAllLods = 0;
            long totalTrisLod0Only = 0;
            long totalVertsLod0Only = 0;
            int lodGroups = 0;
            int nonConvexMeshColliders = 0;
            int pinkShaderSlots = 0;

            var meshRenderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                totalRenderers++;
                if (mr.gameObject.activeInHierarchy && mr.enabled) activeRenderers++;
                if (GameObjectUtility.GetStaticEditorFlags(mr.gameObject) != 0) staticRenderers++;

                bool isLowerLod = mr.gameObject.name == "LOD1" || mr.gameObject.name == "LOD2";
                if (!isLowerLod) lod0Renderers++;

                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    long tris = 0;
                    for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                        tris += (long)(mf.sharedMesh.GetIndexCount(s) / 3);
                    totalTrisAllLods += tris;
                    if (!isLowerLod)
                    {
                        totalTrisLod0Only += tris;
                        totalVertsLod0Only += mf.sharedMesh.vertexCount;
                    }
                }

                foreach (var mat in mr.sharedMaterials)
                {
                    if (mat == null) continue;
                    totalMatSlots++;
                    if (mat.enableInstancing) instancedMats++;
                    if (isAfter && mat.shader != null && (mat.shader.name == "Standard" || mat.shader.name.Contains("Error")))
                        pinkShaderSlots++;
                }
            }

            lodGroups = UnityEngine.Object.FindObjectsOfType<LODGroup>(true).Length;
            foreach (var mc in UnityEngine.Object.FindObjectsOfType<MeshCollider>(true))
                if (!mc.convex) nonConvexMeshColliders++;

            sb.AppendLine($"Total MeshRenderers: {totalRenderers} (LOD0/Base: {lod0Renderers}, Active: {activeRenderers}, Static: {staticRenderers})");
            sb.AppendLine($"LOD0 Vertices: {totalVertsLod0Only:N0}");
            sb.AppendLine($"LOD0 Triangles: {totalTrisLod0Only:N0} (All LODs stored: {totalTrisAllLods:N0})");
            sb.AppendLine($"Material Slots: {totalMatSlots} (GPU Instancing enabled: {instancedMats}, Legacy/Error shaders: {pinkShaderSlots})");
            sb.AppendLine($"LODGroups: {lodGroups}");
            sb.AppendLine($"Non-Convex MeshColliders: {nonConvexMeshColliders}");

            // Render 3D Workshop Shots (Pure 3D first, then UI + 3D composite shots!)
            var hudCanvasGo = GameObject.Find("HUD_Canvas");
            var hudCanvas = hudCanvasGo != null ? hudCanvasGo.GetComponent<Canvas>() : null;
            var hudComp = hudCanvasGo != null ? hudCanvasGo.GetComponent<DigitalTwinHUD>() : null;
            var camRig = UnityEngine.Object.FindObjectOfType<CameraRigManager>();

            // Shot 1: Pure 3D Workshop 1 Machines Close-Up (matches before_02 exact framing!)
            RenderShot(Path.Combine(ArtifactShotDir, $"{tag}_02_workshop1_machines.png"),
                new Vector3(195f, 12f, 95f), new Vector3(135f, 5f, 70f), 55f, null);

            // Shot 2: Pure 3D Workshop 2 Warehouse & Inspection (matches before_03 exact framing!)
            RenderShot(Path.Combine(ArtifactShotDir, $"{tag}_03_workshop2_warehouse.png"),
                new Vector3(38f, 14f, 365f), new Vector3(52f, 10f, 435f), 55f, null);

            // Shot 3: Pure 3D Spinning Hall Mid (matches before_04 exact framing!)
            RenderShot(Path.Combine(ArtifactShotDir, $"{tag}_04_spinning_hall_mid.png"),
                new Vector3(105f, 14f, 250f), new Vector3(20f, 6f, 180f), 55f, null);

            // Shot 4: Bird's-Eye Overview WITH Roof Cutaway + Full Digital Twin Dashboard HUD (1920x1080)!
            if (isAfter && hudCanvas != null)
            {
                // Populate sample preview & text in editor mode for accurate capture
                if (hudComp != null)
                {
                    var cat = AssetDatabase.LoadAssetAtPath<WorkshopCatalog>("Assets/_Project/Configs/WorkshopCatalog.asset");
                    if (cat != null && cat.stations.Count > 0)
                    {
                        var st = cat.stations[0];
                        if (hudComp.inspectorPreviewImage != null) hudComp.inspectorPreviewImage.sprite = st.previewSprite;
                        if (hudComp.specsTableText != null)
                            hudComp.specsTableText.text = "<color=#8BB5D9>抓棉打手宽度：</color><b>2300</b> <color=#5CE6FF>mm</color>\n<color=#8BB5D9>额定产量：</color><b>≤ 2400</b> <color=#5CE6FF>kg/h</color>\n<color=#8BB5D9>安装功率：</color><b>14.7</b> <color=#5CE6FF>kW</color>\n<color=#8BB5D9>整机尺寸：</color><b>2580×1230×2975</b> <color=#5CE6FF>mm</color>\n<color=#8BB5D9>设备净重：</color><b>4050</b> <color=#5CE6FF>kg</color>";
                        if (hudComp.processSummaryText != null) hudComp.processSummaryText.text = st.processSummary;
                        if (hudComp.eventLogText != null)
                            hudComp.eventLogText.text = "<color=#6E8EAD>[20:45:12]</color> <color=#2AE87A>URP 高画质渲染引擎就绪</color>\n<color=#6E8EAD>[20:45:14]</color> <color=#5CE6FF>全厂 9 大纺纱工段在线联机</color>\n<color=#6E8EAD>[20:45:18]</color> <color=#5CE6FF>AGV 编队（6台）进入巡航状态</color>\n<color=#6E8EAD>[20:45:21]</color> <color=#FAB024>ST-09 视觉检测拦截 1 件不良纱筒</color>";
                        if (hudComp.telemetryRpmText != null) hudComp.telemetryRpmText.text = "964 r/min";
                        if (hudComp.telemetryTempText != null) hudComp.telemetryTempText.text = "41.3 °C";
                        if (hudComp.telemetryPowerText != null) hudComp.telemetryPowerText.text = "14.6 kW";
                        if (hudComp.telemetryLoadText != null) hudComp.telemetryLoadText.text = "84%";
                    }
                }

                // Hide roof for Bird's-Eye shot
                if (camRig != null)
                    foreach (var ro in camRig.workshop1RoofObjects) if (ro != null) ro.SetActive(false);

                RenderShot(Path.Combine(ArtifactShotDir, $"{tag}_01_workshop1_birdeye_dashboard.png"),
                    new Vector3(195f, 82f, 255f), new Vector3(72f, 6f, 138f), 50f, hudCanvas, 1920, 1080);

                // Restore roof & capture Teaching Tour Mode shot focused on ST-01/ST-03!
                if (camRig != null)
                    foreach (var ro in camRig.workshop1RoofObjects) if (ro != null) ro.SetActive(true);

                if (hudComp != null)
                {
                    if (hudComp.dashboardLeftContent != null) hudComp.dashboardLeftContent.SetActive(false);
                    if (hudComp.teachingLeftContent != null) hudComp.teachingLeftContent.SetActive(true);
                    if (hudComp.btnModeDashboard != null) hudComp.btnModeDashboard.GetComponent<UnityEngine.UI.Image>().color = hudComp.colorBtnIdle;
                    if (hudComp.btnModeTeaching != null) hudComp.btnModeTeaching.GetComponent<UnityEngine.UI.Image>().color = hudComp.colorAccentCyan;
                    var cat = AssetDatabase.LoadAssetAtPath<WorkshopCatalog>("Assets/_Project/Configs/WorkshopCatalog.asset");
                    if (cat != null && cat.stations.Count > 0)
                    {
                        var st = cat.stations[0];
                        if (hudComp.teachingGoalText != null) hudComp.teachingGoalText.text = $"<color=#00DCFF>【{st.code} 教学目标】</color>\n{st.teachingGoal}";
                        if (hudComp.teachingKeyPointsText != null) hudComp.teachingKeyPointsText.text = string.Join("\n", st.teachingKeyPoints);
                        if (hudComp.quizQuestionText != null && st.quiz != null) hudComp.quizQuestionText.text = $"随堂问答：{st.quiz.question}";
                        for (int q = 0; q < 3 && st.quiz != null && q < st.quiz.options.Length; q++)
                            if (hudComp.quizOptionTexts[q] != null) hudComp.quizOptionTexts[q].text = st.quiz.options[q];
                    }
                }

                RenderShot(Path.Combine(ArtifactShotDir, $"{tag}_05_teaching_tour_hud.png"),
                    new Vector3(200f, 14f, 138f), new Vector3(148f, 6f, 92f), 52f, hudCanvas, 1920, 1080);
            }

            // 2. Open Play.unity and capture Start Screen with UI!
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);
            var playCam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            var playCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (playCam != null)
            {
                RenderWithCanvas(playCam, playCanvas, Path.Combine(ArtifactShotDir, $"{tag}_00_play_start.png"), 1920, 1080);
            }

            string reportPath = Path.Combine(ArtifactShotDir, $"{tag}_metrics.txt");
            File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
            Debug.Log(sb.ToString());
        }

        private static void RenderShot(string outPath, Vector3 camPos, Vector3 lookAt, float fov, Canvas uiCanvas, int width = 1280, int height = 720)
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
