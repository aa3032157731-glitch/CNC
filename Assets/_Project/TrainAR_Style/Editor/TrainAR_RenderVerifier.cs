using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CNC.TrainAR.Editor
{
    /// <summary>
    /// Batchmode build + 6-shot 1920x1080 verification capture for Layout 1 (Cinema Bottom Guide Strip,
    /// 88% unobstructed 3D viewport, progressive disclosure details & view tools, 3-stage linear flow).
    /// </summary>
    public static class TrainAR_RenderVerifier
    {
        private const string OutputDir = @"C:\Users\qqcom\.gemini\antigravity\brain\74f93622-fb00-4d38-9dcd-d3628cc49125\renders_layout1";

        [MenuItem("CNC TrainAR/2. Build Prototype & Capture Verification Renders")]
        public static void BuildAndCapture()
        {
            TrainAR_CardingMachineBuilder.BuildAll();
            CaptureAllViews();
        }

        public static void CaptureAllViews()
        {
            Directory.CreateDirectory(OutputDir);
            EditorSceneManager.OpenScene(TrainAR_CardingMachineBuilder.ScenePath, OpenSceneMode.Single);

            var cam = Camera.main;
            var anim = Object.FindObjectOfType<TrainAR_CardingAnimator>();
            var flow = Object.FindObjectOfType<TrainAR_MaterialFlowAnimator>();
            var switcher = Object.FindObjectOfType<TrainAR_StyleSwitcher>();
            var ctrl = Object.FindObjectOfType<TrainAR_ShowcaseController>();
            if (cam == null || anim == null || flow == null || ctrl == null)
            {
                Debug.LogError("[TrainAR Verifier] Missing Main Camera, CardingAnimator, MaterialFlowAnimator, or ShowcaseController!");
                return;
            }

            if (switcher != null) switcher.ApplyStyle(VisualRenderStyle.RealisticPBR);
            anim.CacheInitialTransforms();
            anim.BuildFlatTrackLut();
            anim.ApplyViewModeImmediate(TrainARViewMode.Cutaway);
            anim.flatGaugeMm = 0.20f;
            flow.ClearAllFaultVisuals();
            flow.ApplyAllImmediate(0.62f, 0.40f);

            WarmupCamera(cam, new Vector3(2.30f, 2.38f, -5.85f), new Vector3(0.08f, 1.08f, 0f), 31f);

            Canvas hud = ctrl.BuildTrainingStudioHud(forceScreenSpaceCamera: true);

            // 1. Stage 1 Default Collapsed -> Clean 3D + 「高产梳棉机 +」 + Big Readable Bottom Bar!
            ctrl.SwitchTrainingModule(TrainingModule.Structure);
            ctrl.SelectStructureStage(4);
            anim.SetFocusedPart("");
            Canvas.ForceUpdateCanvases();
            CaptureAngle(cam, "01_Layout1_Stage1_Clean88Pct.png",
                new Vector3(2.38f, 2.34f, -6.02f), new Vector3(0.08f, 0.96f, 0f), 31f);

            // 2. Clicked 「高产梳棉机」 -> Equipment Science & Safety Precautions Popover + Details Open!
            ctrl.SetDeviceInfoOpen(true);
            ctrl.SetToolsOpen(true);
            Canvas.ForceUpdateCanvases();
            CaptureAngle(cam, "02_Layout1_Stage1_DetailsAndToolsOpen.png",
                new Vector3(2.38f, 2.34f, -6.02f), new Vector3(0.08f, 0.96f, 0f), 31f);

            // 3. Stage 2 Fault A Collapsed Clean
            ctrl.SetDeviceInfoOpen(false);
            ctrl.SetToolsOpen(false);
            ctrl.SwitchTrainingModule(TrainingModule.FaultOps);
            ctrl.StartFaultScenario(FaultScenarioType.CalenderJam, escalatedToRed: true);
            Canvas.ForceUpdateCanvases();
            CaptureAngle(cam, "03_Layout1_Stage2_FaultA_Clean.png",
                new Vector3(2.38f, 2.34f, -6.02f), new Vector3(0.08f, 0.96f, 0f), 31f);

            // 4. Stage 2 Safety Violation Banner + Expanded HMI/SOP Detail Drawer!
            ctrl.OnFaultActionPrimaryClearOrInspect();
            ctrl.SetDetailsOpen(true);
            Canvas.ForceUpdateCanvases();
            CaptureAngle(cam, "04_Layout1_Stage2_SafetyViolation_Details.png",
                new Vector3(2.38f, 2.34f, -6.02f), new Vector3(0.08f, 0.96f, 0f), 31f);

            // 5. Stage 2 Fault C Can Overflow Collapsed
            ctrl.SetDetailsOpen(false);
            ctrl.StartFaultScenario(FaultScenarioType.CanOverflow, escalatedToRed: true);
            Canvas.ForceUpdateCanvases();
            CaptureAngle(cam, "05_Layout1_Stage2_FaultC_CanOverflow.png",
                new Vector3(2.60f, 2.10f, -4.90f), new Vector3(0.68f, 0.94f, 0f), 31f);

            // 6. Stage 3 Big-Font Quiz Strip + Auto-Expanded Explanation & Scorecard!
            ctrl.SwitchTrainingModule(TrainingModule.Assessment);
            ctrl.SubmitQuizChoice(1);
            Canvas.ForceUpdateCanvases();
            CaptureAngle(cam, "06_Layout1_Stage3_Quiz_Scorecard.png",
                new Vector3(2.38f, 2.34f, -6.02f), new Vector3(0.08f, 0.96f, 0f), 31f);

            // Clean up temporary HUD & restore clean resting scene state
            if (hud != null) Object.DestroyImmediate(hud.gameObject);
            var es = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (es != null) Object.DestroyImmediate(es.gameObject);

            if (switcher != null) switcher.ApplyStyle(VisualRenderStyle.RealisticPBR);
            anim.ExitAssemblyTraining();
            anim.ApplyViewModeImmediate(TrainARViewMode.Cutaway);
            anim.isRunning = true;
            anim.speedScale = 1.0f;
            anim.flatGaugeMm = 0.20f;
            anim.SetFocusedPart("");
            flow.ClearAllFaultVisuals();
            flow.autoCycleCanFill = true;
            flow.ApplyAllImmediate(0.65f, 0.35f);
            cam.fieldOfView = 31f;
            cam.transform.position = new Vector3(2.30f, 2.38f, -5.85f);
            cam.transform.LookAt(new Vector3(0.08f, 1.08f, 0f));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log($"[TrainAR Verifier] Captured 6 Layout-1 Cinema Minimalist HUD renders to: {OutputDir}");
        }

        private static void WarmupCamera(Camera cam, Vector3 camPos, Vector3 lookAt, float fov)
        {
            cam.fieldOfView = fov;
            cam.transform.position = camPos;
            cam.transform.LookAt(lookAt);
            var rt = new RenderTexture(512, 288, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            cam.targetTexture = prev;
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        private static void CaptureAngle(Camera cam, string fileName, Vector3 camPos, Vector3 lookAt, float fov)
        {
            const int width = 1920;
            const int height = 1080;

            cam.fieldOfView = fov;
            cam.transform.position = camPos;
            cam.transform.LookAt(lookAt);

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();

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
            Object.DestroyImmediate(rt);

            string fullPath = Path.Combine(OutputDir, fileName);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[TrainAR Verifier] Saved: {fullPath}");
        }
    }
}
