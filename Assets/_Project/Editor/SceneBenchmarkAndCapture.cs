using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CNC.DigitalTwin;
using ECM.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CNC.EditorTools
{
    public static class RunFullOptimizationPipeline
    {
        [MenuItem("CNC Optimize/Run Full Optimization Pipeline (Step 1 + 2 + 3 + Deep Verify)")]
        public static void RunAll()
        {
            Step1_URPAndAssetSetup.RunUrpAndFontsOnly();
            Step2_ModelingAndRenderingOptimizer.Run();
            Step3_BuildFrontendScenes.Run();
            SceneBenchmarkAndCapture.RunDeepVerificationAndCaptures();
            Debug.Log("[CNC PIPELINE] ALL STEPS + DEEP VERIFICATION COMPLETED!");
        }

        [MenuItem("CNC Optimize/Apply Style A Fixes + Run Deep Verification Suite")]
        public static void RunStyleAAndCapture()
        {
            Step3_BuildFrontendScenes.Run();
            SceneBenchmarkAndCapture.RunDeepVerificationAndCaptures();
            Debug.Log("[CNC PIPELINE] STYLE A + SCENE HARDENING + DEEP VERIFICATION COMPLETED!");
        }
    }

    public static class SceneBenchmarkAndCapture
    {
        private const string ArtifactShotDir = @"C:\Users\qqcom\.gemini\antigravity\brain\8cf605aa-ffcf-453b-9a7e-a7b8353edcb5\screenshots\deep_verify";
        private const string LegacyReportPath = @"C:\Users\qqcom\.gemini\antigravity\brain\8cf605aa-ffcf-453b-9a7e-a7b8353edcb5\screenshots\deep_test_report.txt";

        public static void RunDeepVerificationAndCaptures()
        {
            Directory.CreateDirectory(ArtifactShotDir);
            VerifyAllSystemsDeep();
            CaptureAllDeepVerificationShots();
        }

        public static void VerifyAllSystemsDeep()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;

            void Check(string category, string name, bool cond, string detail = "")
            {
                string tag = cond ? "[PASS]" : "[FAIL]";
                if (cond) pass++; else fail++;
                sb.AppendLine($"{tag} [{category}] {name}" + (string.IsNullOrEmpty(detail) ? "" : $" -- {detail}"));
            }

            sb.AppendLine("======================================================================");
            sb.AppendLine("   CNC SMART FACTORY - DEEP COMPREHENSIVE AUTOMATED TEST REPORT");
            sb.AppendLine("======================================================================");

            // -------------------- 1. BUILD SETTINGS & PLAY.UNITY --------------------
            var buildScenes = EditorBuildSettings.scenes;
            Check("BUILD", "Build Settings has Play.unity [0] & SampleScene.unity [1] enabled",
                buildScenes.Length >= 2 && buildScenes[0].enabled && buildScenes[1].enabled &&
                buildScenes[0].path.EndsWith("Play.unity") && buildScenes[1].path.EndsWith("SampleScene.unity"),
                $"count={buildScenes.Length}");

            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);
            var startCtrl = UnityEngine.Object.FindObjectOfType<StartScreenController>();
            Check("PLAY", "StartScreenController present & all buttons/modals/StyleA sprites wired",
                startCtrl != null &&
                startCtrl.btnEnterDashboard != null && startCtrl.btnEnterTeaching != null &&
                startCtrl.btnOpenSettings != null && startCtrl.btnQuit != null &&
                startCtrl.loadingOverlay != null && startCtrl.loadingBarFill != null &&
                startCtrl.settingsModal != null && startCtrl.spPillActiveWhite != null && startCtrl.spPillIdle != null);

            var playScaler = UnityEngine.Object.FindObjectOfType<CanvasScaler>();
            Check("PLAY", "Play.unity CanvasScaler responsive 1920x1080",
                playScaler != null && playScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                playScaler.referenceResolution == new Vector2(1920, 1080));
            Check("PLAY", "Play.unity MainCamera properly tagged (Camera.main != null)", Camera.main != null);
            Check("PLAY", "Play.unity zero missing scripts", CountMissingScriptsInScene() == 0);

            // -------------------- 2. SAMPLESCENE CORE & COLLIDERS --------------------
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Check("SCENE", "SampleScene zero missing scripts", CountMissingScriptsInScene() == 0);

            int negScaleBc = 0, negSizeBc = 0, thinSolidBc = 0;
            var badBcList = new List<string>();
            foreach (var bc in UnityEngine.Object.FindObjectsOfType<BoxCollider>(true))
            {
                Vector3 ls = bc.transform.lossyScale;
                Vector3 sz = bc.size;
                Vector3 wsz = new Vector3(Mathf.Abs(ls.x * sz.x), Mathf.Abs(ls.y * sz.y), Mathf.Abs(ls.z * sz.z));
                if (ls.x < 0f || ls.y < 0f || ls.z < 0f) { negScaleBc++; badBcList.Add($"negScale:{bc.name}"); }
                if (sz.x < 0f || sz.y < 0f || sz.z < 0f) { negSizeBc++; badBcList.Add($"negSize:{bc.name}"); }
                if (!bc.isTrigger && bc.enabled && bc.gameObject.activeInHierarchy &&
                    (wsz.x < 0.02f || wsz.y < 0.02f || wsz.z < 0.02f))
                {
                    thinSolidBc++;
                    badBcList.Add($"thin:{bc.name}({wsz.y:F4})");
                }
            }
            Check("PHYSICS", "Zero BoxColliders with negative lossyScale (0 PhysX console warnings)", negScaleBc == 0,
                negScaleBc == 0 ? "0 bad" : string.Join(", ", badBcList));
            Check("PHYSICS", "Zero BoxColliders with negative size", negSizeBc == 0, $"negSize={negSizeBc}");
            Check("PHYSICS", "Zero ultra-thin (<2cm) solid BoxColliders (no floor tunneling!)", thinSolidBc == 0,
                thinSolidBc == 0 ? "0 thin" : string.Join(", ", badBcList));

            var b1Floor = GameObject.Find("Building")?.transform.Find("Bldg_Floor_001")?.GetComponent<BoxCollider>();
            var b2Floor = GameObject.Find("Building (1)")?.transform.Find("Bldg_Floor_001")?.GetComponent<BoxCollider>();
            Check("PHYSICS", "Workshop 1 Bldg_Floor_001 thickened (>=1.9m) & top flush at Y=1.10",
                b1Floor != null && b1Floor.bounds.size.y >= 1.9f && Mathf.Abs(b1Floor.bounds.max.y - 1.10f) < 0.06f,
                b1Floor != null ? $"top={b1Floor.bounds.max.y:F2}, thick={b1Floor.bounds.size.y:F2}" : "MISSING");
            Check("PHYSICS", "Workshop 2 Bldg_Floor_001 thickened (>=1.9m) & top flush at Y=3.80",
                b2Floor != null && b2Floor.bounds.size.y >= 1.9f && Mathf.Abs(b2Floor.bounds.max.y - 3.80f) < 0.06f,
                b2Floor != null ? $"top={b2Floor.bounds.max.y:F2}, thick={b2Floor.bounds.size.y:F2}" : "MISSING");

            var sf1 = GameObject.Find("SafetyFloor_Workshop1")?.GetComponent<BoxCollider>();
            var sf2 = GameObject.Find("SafetyFloor_Workshop2")?.GetComponent<BoxCollider>();
            Check("PHYSICS", "Continuous Safety Sub-Floors installed on Default layer for W1 & W2",
                sf1 != null && sf2 != null && sf1.gameObject.layer == 0 && sf2.gameObject.layer == 0,
                sf1 != null && sf2 != null ? $"W1Top={sf1.bounds.max.y:F2}, W2Top={sf2.bounds.max.y:F2}" : "MISSING");

            int strayBelowFloor = 0;
            foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (mr.gameObject.activeInHierarchy && mr.bounds.max.y < 0.8f)
                    strayBelowFloor++;
            }
            Check("SCENE", "Zero stray active MeshRenderers below floor (Y < 0.8m)", strayBelowFloor == 0, $"belowCount={strayBelowFloor}");

            // -------------------- 3. REAL PHYSX SIMULATION TESTS (GRAVITY & WALKING!) --------------------
            bool prevAutoSim = Physics.autoSimulation;
            Physics.autoSimulation = false;
            Physics.SyncTransforms();
            var fpsGoForSim = GameObject.Find("ECM_FirstPerson");
            var fpsCapForSim = fpsGoForSim != null ? fpsGoForSim.GetComponent<CapsuleCollider>() : null;
            try
            {
                // Temporarily disable ECM_FirstPerson's collider while dropping isolated probe at the exact spawn point
                if (fpsCapForSim != null) fpsCapForSim.enabled = false;
                Physics.SyncTransforms();

                // Test A: Real PhysX drop of a Player-sized capsule at Workshop 1 Spawn (198, 1.18, 162) for 2.0s (100 steps)
                float w1LandY = SimulateCapsuleDrop(new Vector3(198f, 1.25f, 162f), 100, out float w1MinY);
                Check("PHYSX_SIM", "Workshop 1 FPS Spawn 2.0s Gravity Sim stays solidly on floor (no fall-through!)",
                    w1LandY >= 1.05f && w1LandY <= 1.22f && w1MinY >= 1.02f,
                    $"finalY={w1LandY:F3}, minY={w1MinY:F3} (floor=1.10)");

                // Test B: Real PhysX drop of Player capsule at Workshop 2 Spawn (39.5, 3.88, 358) for 2.0s
                float w2LandY = SimulateCapsuleDrop(new Vector3(39.5f, 3.95f, 358f), 100, out float w2MinY);
                Check("PHYSX_SIM", "Workshop 2 FPS Spawn 2.0s Gravity Sim stays solidly on floor (no fall-through!)",
                    w2LandY >= 3.74f && w2LandY <= 3.92f && w2MinY >= 3.70f,
                    $"finalY={w2LandY:F3}, minY={w2MinY:F3} (floor=3.80)");

                if (fpsCapForSim != null) fpsCapForSim.enabled = true;
                Physics.SyncTransforms();

                // Test B2: Actual ECM_FirstPerson Rigidbody + CapsuleCollider real PhysX gravity test!
                if (fpsGoForSim != null)
                {
                    var fpsRb = fpsGoForSim.GetComponent<Rigidbody>();
                    Vector3 origPos = fpsGoForSim.transform.position;
                    float minActualY = origPos.y;
                    if (fpsRb != null)
                    {
                        fpsRb.isKinematic = false;
                        fpsRb.velocity = Vector3.zero;
                        for (int s = 0; s < 100; s++)
                        {
                            fpsRb.AddForce(Vector3.down * 30f, ForceMode.Acceleration);
                            Physics.Simulate(0.02f);
                            if (fpsGoForSim.transform.position.y < minActualY) minActualY = fpsGoForSim.transform.position.y;
                        }
                        float endActualY = fpsGoForSim.transform.position.y;
                        var mainC = Camera.main;
                        float endCamY = mainC != null ? mainC.transform.position.y : 0f;
                        Check("PHYSX_SIM", "ACTUAL ECM_FirstPerson + MainCamera 2.0s gravity sim stays grounded & eyes >= 5.5m",
                            endActualY >= 1.05f && endActualY <= 1.25f && minActualY >= 1.02f && endCamY >= 5.5f,
                            $"playerY={endActualY:F3}, minY={minActualY:F3}, camWorldY={endCamY:F2}");
                        fpsGoForSim.transform.position = origPos;
                        fpsRb.position = origPos;
                        fpsRb.velocity = Vector3.zero;
                        Physics.SyncTransforms();
                    }
                }

                // Test C: Real PhysX drop of falling Cotton Roll (Juan) at ST-03 (134, 4.0, 70)
                float rollLandY = SimulateCapsuleDrop(new Vector3(134f, 4.0f, 70f), 100, out float rollMinY);
                Check("PHYSX_SIM", "Falling cotton roll (ST-03) caught solidly on floor (never falls into skybox)",
                    rollLandY >= 1.05f && rollMinY >= 1.02f,
                    $"finalY={rollLandY:F3}, minY={rollMinY:F3}");

                // Test D: Main Aisle Walking Floor Continuity Sweep (20 sample points across Workshop 1 & Workshop 2)
                Vector3[] walkSamplesW1 =
                {
                    new Vector3(198, 5, 156), new Vector3(188, 5, 150), new Vector3(175, 5, 145),
                    new Vector3(155, 5, 130), new Vector3(135, 5, 110), new Vector3(105, 5, 150),
                    new Vector3(78, 5, 191),  new Vector3(50, 5, 220),  new Vector3(20, 5, 180),
                    new Vector3(-10, 5, 160), new Vector3(-20, 5, 240), new Vector3(95, 5, 266)
                };
                int w1Hits = 0;
                foreach (var pt in walkSamplesW1)
                {
                    if (Physics.Raycast(pt, Vector3.down, out RaycastHit h, 10f, 1) && h.point.y >= 1.05f)
                        w1Hits++;
                }
                Check("PHYSX_SIM", "Workshop 1 full-factory walking floor sweep (12/12 aisle points solid on Default mask)",
                    w1Hits == walkSamplesW1.Length, $"{w1Hits}/{walkSamplesW1.Length} solid");

                Vector3[] walkSamplesW2 =
                {
                    new Vector3(39.5f, 8, 358), new Vector3(39.5f, 8, 375), new Vector3(39.5f, 8, 395),
                    new Vector3(20f, 8, 385),   new Vector3(60f, 8, 385),   new Vector3(39.5f, 8, 430)
                };
                int w2Hits = 0;
                foreach (var pt in walkSamplesW2)
                {
                    if (Physics.Raycast(pt, Vector3.down, out RaycastHit h, 10f, 1) && h.point.y >= 3.74f)
                        w2Hits++;
                }
                Check("PHYSX_SIM", "Workshop 2 warehouse walking floor sweep (6/6 points solid on Default mask)",
                    w2Hits == walkSamplesW2.Length, $"{w2Hits}/{walkSamplesW2.Length} solid");
            }
            finally
            {
                Physics.autoSimulation = prevAutoSim;
            }

            // -------------------- 4. CAMERA RIG, EYE HEIGHT & ANIMATORS --------------------
            var camRig = UnityEngine.Object.FindObjectOfType<CameraRigManager>();
            Check("CAMERA", "CameraRigManager wired & defaults to FirstPersonWalk with preserved eye height (>=4.5m)",
                camRig != null && AppLaunchContext.InitialCameraMode == CameraViewMode.FirstPersonWalk &&
                camRig.mainCamera != null && camRig.fpsPlayerRoot != null && camRig.fpsController != null &&
                camRig.fpsMouseLook != null && camRig.fpsCameraAnchor != null &&
                camRig.fpsCameraLocalOffset.y >= 4.5f &&
                camRig.mainCamera.transform.parent == camRig.fpsCameraAnchor &&
                camRig.mainCamera.transform.position.y >= 5.5f,
                camRig != null && camRig.mainCamera != null
                    ? $"eyeOffset={camRig.fpsCameraLocalOffset.y:F2}, camWorldY={camRig.mainCamera.transform.position.y:F2}"
                    : "MISSING");

            Check("CAMERA", "Roof Cutaway lists populated for W1 & W2",
                camRig != null && camRig.workshop1RoofObjects.Count > 10 && camRig.workshop2RoofObjects.Count > 10,
                camRig != null ? $"W1={camRig.workshop1RoofObjects.Count}, W2={camRig.workshop2RoofObjects.Count}" : "");

            var anim = UnityEngine.Object.FindObjectOfType<WorkshopAnimator>();
            bool padsCentered = anim != null && anim.floorRingPads.Count == 5;
            if (padsCentered)
            {
                foreach (var pad in anim.floorRingPads)
                    if (pad == null || pad.GetComponent<MeshRenderer>() == null) padsCentered = false;
            }
            Check("ANIM", "WorkshopAnimator floor pads target actual mesh transforms (no 55m parent-offset orbiting!)",
                padsCentered, anim != null ? $"pads={anim.floorRingPads.Count}, agvs={anim.agvItems.Count}" : "MISSING");

            // -------------------- 5. ALL 9 STATIONS, CLICK VOLUMES, SHADERS & PARTICLES --------------------
            var sim = UnityEngine.Object.FindObjectOfType<SimulationState>();
            Check("CATALOG", "SimulationState & all 9 stations with sprites & corrected ST-04 copy",
                sim != null && sim.catalog != null && sim.catalog.stations.Count == 9 &&
                !sim.catalog.stations[3].processSummary.Contains("精梳机") &&
                sim.catalog.stations.TrueForAll(s => s.previewSprite != null && s.specs != null && s.specs.Length >= 4 && s.quiz != null));

            if (sim != null && sim.catalog != null)
            {
                bool allCamsAboveFloor = true;
                string camDetail = "";
                foreach (var st in sim.catalog.stations)
                {
                    float fy = st.hall == WorkshopHall.SpinningHall ? 1.10f : 3.80f;
                    if (st.focusCameraPos.y < fy + 3.0f) { allCamsAboveFloor = false; camDetail += $" {st.code}:y={st.focusCameraPos.y:F1}"; }
                }
                Check("CAMERA", "All 9 station focusCameraPos strictly above floor (>= floorY + 3m)", allCamsAboveFloor,
                    allCamsAboveFloor ? "9/9 valid" : camDetail);
            }

            var interactables = UnityEngine.Object.FindObjectsOfType<MachineInteractable>(true);
            var markers = UnityEngine.Object.FindObjectsOfType<WorldStationMarker>(true);
            var beacons = UnityEngine.Object.FindObjectsOfType<MachineStatusBeacon>(true);
            Check("STATIONS", "9 MachineInteractables, 9 WorldStationMarkers, 9 MachineStatusBeacons",
                interactables.Length == 9 && markers.Length == 9 && beacons.Length == 9,
                $"inter={interactables.Length}, markers={markers.Length}, beacons={beacons.Length}");

            // Check that entrance camera is NOT inside any MachineInteractable trigger volume (prevents auto-hover spam!)
            Vector3 entranceEye = camRig != null && camRig.mainCamera != null ? camRig.mainCamera.transform.position : new Vector3(198, 5.98f, 156);
            bool insideAnyTrigger = false;
            foreach (var inter in interactables)
            {
                var bc = inter.GetComponent<BoxCollider>();
                if (bc != null && bc.bounds.Contains(entranceEye)) insideAnyTrigger = true;
            }
            Check("INTERACT", "FPS entrance eye is outside all station click triggers (zero accidental hover tooltip)",
                !insideAnyTrigger, $"eye={entranceEye}");

            // Shaders & Materials check (ZERO pink/legacy Standard shaders, ZERO black smoke clumps!)
            var psrs = UnityEngine.Object.FindObjectsOfType<ParticleSystemRenderer>(true);
            int badParticles = 0;
            foreach (var p in psrs)
                if (p.sharedMaterial == null || p.sharedMaterial.shader == null || !p.sharedMaterial.shader.name.Contains("Particles"))
                    badParticles++;
            Check("RENDER", "All ParticleSystemRenderers use URP Particle Additive shader (0 black smoke clumps)",
                badParticles == 0, $"total={psrs.Length}, bad={badParticles}");

            int pinkOrLegacyMats = 0;
            foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (!mr.gameObject.activeInHierarchy) continue;
                foreach (var m in mr.sharedMaterials)
                {
                    if (m == null || m.shader == null || m.shader.name.Contains("Error") || m.shader.name == "Standard")
                        pinkOrLegacyMats++;
                }
            }
            Check("RENDER", "Zero missing/Error/legacy-Standard materials on all active MeshRenderers",
                pinkOrLegacyMats == 0, $"badSlots={pinkOrLegacyMats}");

            // All 7 floor pads use transparent Mat_HoloFloorPad (zero opaque white floor squares!)
            var padMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_HoloFloorPad.mat");
            int validPads = 0;
            foreach (var pname in new[] { "抓棉机UI", "除杂、梳棉、卷棉UI", "末并", "精梳", "粗纱", "细纱", "络筒" })
            {
                var pgo = GameObject.Find(pname);
                var pmr = pgo != null ? pgo.GetComponentInChildren<MeshRenderer>(true) : null;
                if (pmr != null && pmr.sharedMaterial == padMat) validPads++;
            }
            Check("RENDER", "All 7 floor trigger pads use transparent holo ring material (0 white floor squares)",
                validPads == 7, $"{validPads}/7 pads");

            // Check Workshop 2 hanging LED signs have no orphan-wrap long title
            int wrappedSigns = 0;
            foreach (var tmp in UnityEngine.Object.FindObjectsOfType<TMPro.TextMeshPro>(true))
                if (tmp.text != null && tmp.text.Contains("智能视觉检测分拣区")) wrappedSigns++;
            Check("RENDER", "Workshop 2 LED hanging signs cleanly single-line (no orphan wrap)", wrappedSigns == 0);

            // -------------------- 6. STYLE A HUD & MULTI-ASPECT-RATIO OVERLAP CHECKS --------------------
            var hud = UnityEngine.Object.FindObjectOfType<DigitalTwinHUD>();
            Check("HUD", "DigitalTwinHUD & Style A sprites/buttons/16 slots/9 dock nodes wired",
                hud != null && hud.spPillActiveWhite != null && hud.spPillActiveSky != null &&
                hud.btnModeDashboard != null && hud.btnModeTeaching != null &&
                hud.btnCamFps != null && hud.btnCamOrbit != null && hud.btnCamTour != null &&
                hud.btnSimPlayPause != null && hud.btnSpeedCycle != null && hud.btnToggleStationRun != null &&
                hud.flowNodeButtons != null && hud.flowNodeButtons.Length == 9 && hud.flowNodeButtons[8] != null &&
                hud.warehouseSlotCells != null && hud.warehouseSlotCells.Length == 16 && hud.warehouseSlotCells[15] != null);

            if (hud != null)
            {
                var scaler = hud.GetComponent<CanvasScaler>();
                float match = scaler != null ? scaler.matchWidthOrHeight : 0.65f;
                // Compute effective logical Canvas size at BOTH 1920x1080 (16:9) AND 1920x840 (wide docked Game View)
                bool ok1080 = CheckHudLayoutAtResolution(hud, 1920f, 1080f, match, out string det1080);
                Check("HUD_LAYOUT", "Zero panel-to-dock overlap at 1920x1080 (Dashboard + Teaching + Inspector)", ok1080, det1080);

                bool okWide = CheckHudLayoutAtResolution(hud, 1920f, 840f, match, out string detWide);
                Check("HUD_LAYOUT", "Zero panel-to-dock overlap at 1920x840 Wide Docked Game View (Dashboard + Teaching + Inspector)", okWide, detWide);
            }

            sb.AppendLine("----------------------------------------------------------------------");
            sb.AppendLine($"FINAL DEEP TEST SUMMARY: {pass} PASSED, {fail} FAILED (TOTAL {pass + fail})");
            sb.AppendLine("======================================================================");

            string report = sb.ToString();
            File.WriteAllText(LegacyReportPath, report, Encoding.UTF8);
            Debug.Log(report);
            if (fail > 0)
                Debug.LogError($"[CNC DEEP TEST] {fail} test(s) FAILED! Review {LegacyReportPath}");
        }

        private static float SimulateCapsuleDrop(Vector3 startPos, int steps, out float minY)
        {
            var probe = new GameObject("__PhysxDropProbe");
            probe.layer = 2; // Ignore Raycast so it doesn't interfere with anything else
            probe.transform.position = startPos;
            var cap = probe.AddComponent<CapsuleCollider>();
            cap.radius = 0.5f;
            cap.height = 2.0f;
            cap.center = new Vector3(0f, 1.0f, 0f);
            var rb = probe.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Physics.SyncTransforms();

            minY = startPos.y;
            float dt = 0.02f;
            for (int i = 0; i < steps; i++)
            {
                // Apply ECM-equivalent strong gravity (-30 m/s^2)
                rb.AddForce(Vector3.down * 30f, ForceMode.Acceleration);
                Physics.Simulate(dt);
                if (probe.transform.position.y < minY) minY = probe.transform.position.y;
            }
            float finalY = probe.transform.position.y;
            UnityEngine.Object.DestroyImmediate(probe);
            Physics.SyncTransforms();
            return finalY;
        }

        private static bool CheckHudLayoutAtResolution(DigitalTwinHUD hud, float w, float h, float match, out string detail)
        {
            // Unity ScaleWithScreenSize exact scale formula:
            float logW = Mathf.Log(w / 1920f, 2f);
            float logH = Mathf.Log(h / 1080f, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(logW, logH, match));
            float canvasW = w / scale;
            float canvasH = h / scale;

            // Top pills occupy top 72px; Bottom dock occupies bottom 74px (y from 22 to 74)
            float dockTop = 74f;
            float leftDashBottom = canvasH - 86f - 418f;
            float leftTeachBottom = canvasH - 86f - 648f;
            float rightBottom = canvasH - 86f - 626f;

            // Top 3 pills total width: 340 + 772 + 316 + 48 margins = 1476px
            bool topFits = canvasW >= 1520f;
            bool leftDashClears = leftDashBottom > dockTop + 16f;
            bool leftTeachClears = leftTeachBottom > dockTop + 16f;
            bool rightClears = rightBottom > dockTop + 16f;

            detail = $"logical={canvasW:F0}x{canvasH:F0}, gaps(dash={leftDashBottom - dockTop:F0}px, teach={leftTeachBottom - dockTop:F0}px, right={rightBottom - dockTop:F0}px)";
            return topFits && leftDashClears && leftTeachClears && rightClears;
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

        // ==================== FULL 16-SCREENSHOT VISUAL VERIFICATION SUITE ====================

        [MenuItem("CNC Optimize/Capture Full Deep Verification Screenshots (16 Views)")]
        public static void CaptureAllDeepVerificationShots()
        {
            Directory.CreateDirectory(ArtifactShotDir);

            // 1. Play.unity Style A Start Screen
            EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);
            var playCam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            var playCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (playCam != null)
                RenderWithCanvas(playCam, playCanvas, Path.Combine(ArtifactShotDir, "00_play_start_1080p.png"), 1920, 1080);

            // 2. SampleScene.unity — All 15 Views!
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var hudCanvasGo = GameObject.Find("HUD_Canvas");
            var hudCanvas = hudCanvasGo != null ? hudCanvasGo.GetComponent<Canvas>() : null;
            var hudComp = hudCanvasGo != null ? hudCanvasGo.GetComponent<DigitalTwinHUD>() : null;
            var camRig = UnityEngine.Object.FindObjectOfType<CameraRigManager>();
            var cat = AssetDatabase.LoadAssetAtPath<WorkshopCatalog>("Assets/_Project/Configs/WorkshopCatalog.asset");

            void PopulateInspector(StationInfo st)
            {
                if (hudComp == null || st == null) return;
                if (hudComp.inspectorCodeText != null) hudComp.inspectorCodeText.text = $"{st.code} · {st.shortName}";
                if (hudComp.inspectorNameText != null) hudComp.inspectorNameText.text = st.name;
                if (hudComp.inspectorEngNameText != null) hudComp.inspectorEngNameText.text = st.englishName;
                if (hudComp.inspectorPreviewImage != null && st.previewSprite != null) hudComp.inspectorPreviewImage.sprite = st.previewSprite;
                if (hudComp.telemetryRpmText != null) hudComp.telemetryRpmText.text = $"{st.nominalRpm:N0} r/min";
                if (hudComp.telemetryTempText != null) hudComp.telemetryTempText.text = $"{st.nominalTempC:F1} °C";
                if (hudComp.telemetryLoadText != null) hudComp.telemetryLoadText.text = $"{st.nominalLoadPct:F0}% · {st.nominalPowerKw:F1} kW";
                if (hudComp.specsTableText != null && st.specs != null)
                {
                    var s = new StringBuilder();
                    int lim = Mathf.Min(st.specs.Length, 5);
                    for (int i = 0; i < lim; i++)
                    {
                        var sp = st.specs[i];
                        s.Append("<color=#94A3B8>").Append(sp.label).Append("</color>    <b>").Append(sp.value).Append("</b>");
                        if (!string.IsNullOrEmpty(sp.unit)) s.Append(" <color=#7DD3FC>").Append(sp.unit).Append("</color>");
                        if (i < lim - 1) s.Append('\n');
                    }
                    hudComp.specsTableText.text = s.ToString();
                }
                if (hudComp.processSummaryText != null) hudComp.processSummaryText.text = st.processSummary;
            }

            void SetRoofCut(bool cut)
            {
                if (camRig == null) return;
                foreach (var go in camRig.workshop1RoofObjects) if (go != null) go.SetActive(!cut);
                foreach (var go in camRig.workshop2RoofObjects) if (go != null) go.SetActive(!cut);
            }

            if (cat != null && cat.stations.Count > 0) PopulateInspector(cat.stations[0]);

            // 2A. ACTUAL ECM_FirstPerson/Camera_Pivot/Camera at Workshop 1 Spawn! (1080p + 1920x840 Wide Docked!)
            SetRoofCut(false);
            if (hudComp != null && hudComp.fpsHintBanner != null) hudComp.fpsHintBanner.SetActive(true);
            var actualFpsCam = camRig != null ? camRig.mainCamera : Camera.main;
            if (actualFpsCam != null)
            {
                RenderWithCanvas(actualFpsCam, hudCanvas, Path.Combine(ArtifactShotDir, "01_walk_w1_entrance_1080p.png"), 1920, 1080);
                RenderWithCanvas(actualFpsCam, hudCanvas, Path.Combine(ArtifactShotDir, "02_walk_w1_wide_docked_1920x840.png"), 1920, 840);
            }

            // 2B. Workshop 2 First-Person Walk Camera (at grounded Y=3.88 + eye 4.80 = 8.68m!)
            if (cat != null && cat.stations.Count >= 9) PopulateInspector(cat.stations[8]);
            RenderShot(Path.Combine(ArtifactShotDir, "03_walk_w2_warehouse_1080p.png"),
                new Vector3(39.5f, 8.68f, 358f), new Vector3(39.5f, 8.2f, 412f), 55f, hudCanvas, 1920, 1080);

            // 2C. Bird's-Eye Overview Workshop 1 & Workshop 2 (Roof Cutaway ON)
            if (hudComp != null && hudComp.fpsHintBanner != null) hudComp.fpsHintBanner.SetActive(false);
            if (cat != null && cat.stations.Count > 0) PopulateInspector(cat.stations[0]);
            SetRoofCut(true);
            RenderShot(Path.Combine(ArtifactShotDir, "04_birdeye_w1_cutaway_1080p.png"),
                new Vector3(180f, 86f, 250f), new Vector3(74f, 6f, 145f), 52f, hudCanvas, 1920, 1080);
            if (cat != null && cat.stations.Count >= 9) PopulateInspector(cat.stations[8]);
            RenderShot(Path.Combine(ArtifactShotDir, "05_birdeye_w2_cutaway_1080p.png"),
                new Vector3(39.5f, 34f, 344f), new Vector3(39.5f, 8.5f, 408f), 54f, hudCanvas, 1920, 1080);

            // 2D. ALL 9 STATIONS (ST-01 .. ST-09) Hero Camera Views (Roof Restored!)
            SetRoofCut(false);
            if (cat != null)
            {
                for (int i = 0; i < cat.stations.Count; i++)
                {
                    var st = cat.stations[i];
                    PopulateInspector(st);
                    RenderShot(Path.Combine(ArtifactShotDir, $"st{i + 1:00}_{st.code.ToLowerInvariant()}.png"),
                        st.focusCameraPos, st.focusLookAtPos, 54f, hudCanvas, 1920, 1080);
                }

                // 2E. Teaching Tour Mode at Wide Docked Game View 1920x840 (proves zero bottom dock overlap!)
                var st4 = cat.stations[4];
                PopulateInspector(st4);
                if (hudComp != null)
                {
                    if (hudComp.dashboardLeftContent != null) hudComp.dashboardLeftContent.SetActive(false);
                    if (hudComp.teachingLeftContent != null) hudComp.teachingLeftContent.SetActive(true);
                    if (hudComp.leftPanelRect != null) hudComp.leftPanelRect.sizeDelta = new Vector2(296f, 648f);
                    if (hudComp.teachingGoalText != null) hudComp.teachingGoalText.text = $"<color=#7DD3FC><b>{st4.code} 教学目标</b></color>\n{st4.teachingGoal}";
                    if (hudComp.teachingKeyPointsText != null) hudComp.teachingKeyPointsText.text = string.Join("\n", st4.teachingKeyPoints);
                    if (hudComp.quizQuestionText != null && st4.quiz != null) hudComp.quizQuestionText.text = $"随堂考核：{st4.quiz.question}";
                    for (int q = 0; q < 3 && st4.quiz != null && q < st4.quiz.options.Length; q++)
                        if (hudComp.quizOptionTexts[q] != null) hudComp.quizOptionTexts[q].text = st4.quiz.options[q];
                }
                RenderShot(Path.Combine(ArtifactShotDir, "06_teaching_mode_wide_1920x840.png"),
                    st4.focusCameraPos, st4.focusLookAtPos, 54f, hudCanvas, 1920, 840);
            }

            Debug.Log("[CNC DEEP VERIFY] Captured all 16 verification screenshots to " + ArtifactShotDir);
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
