using UnityEngine;

namespace CNC.TrainAR
{
    public enum CardingBeaconState
    {
        GreenNormal = 0,
        AmberWarning = 1,
        RedAlarm = 2
    }

    /// <summary>
    /// Drives full real-time dynamic raw-material -> intermediate-fiber -> output-product animation
    /// across all 6 stages of the ST-03 Carding Machine (梳棉机):
    /// 1. Descending stacked raw cotton batt layers inside cutaway feed chute
    /// 2. Falling cotton seed-hull / leaf impurities beneath Licker-in mote knives
    /// 3. High-speed cotton tufts stretching into aligned single fibers around Main Cylinder & Flats
    /// 4. Continuously streaming carded cotton web + live nep defects when Flat Gauge is out of spec
    /// 5. Sliver fiber flow gliding through condenser trumpet -> calender rollers -> overhead coiler
    /// 6. Live rotating coiler nozzle strand + 11 progressively stacking helical sliver coils
    ///    compressing the spring-loaded follower disc & visible internal spring inside the cutaway sliver can!
    /// </summary>
    public class TrainAR_MaterialFlowAnimator : MonoBehaviour
    {
        [Header("Core Animator Link")]
        public TrainAR_CardingAnimator cardingAnimator;

        [Header("Stage 1: Feed Chute Descending Cotton Batt")]
        public Transform[] feedBattLayers;
        public float feedTopY = 1.94f;
        public float feedBottomY = 0.96f;
        public float feedCenterX = -1.39f;
        public float feedCenterZ = -0.02f;

        [Header("Stage 2: Licker-in Shedding Impurities")]
        public Transform[] impurityMotes;
        public Vector2 lickerCenterXY = new Vector2(-0.88f, 0.94f);

        [Header("Stage 3: Carding Tufts -> Stretched Single Fibers")]
        public Transform[] cardingFlocks;
        public Vector2 mainCylinderCenterXY = new Vector2(-0.04f, 1.04f);

        [Header("Stage 4: Flowing Cotton Web, Sliver Pulses & Fault Neps")]
        public Renderer cottonWebRenderer;
        public Transform[] sliverFlowNodes;
        public Transform[] nepDefectBeads;
        public Vector2 dofferCenterXY = new Vector2(0.94f, 0.88f);

        [Header("Stage 5: Growing Helical Sliver Coils & Compressing Can Spring")]
        public Transform coilerDisc;
        public Transform coilerLiveStrand;
        public Transform sliverFollowerPlate;
        public Transform canInternalSpring;
        public Transform[] sliverCoilLayers;
        public Vector2 coilerCenterXZ = new Vector2(1.96f, 0.0f);

        [Header("Live Output Product State")]
        [Range(0f, 1f)] public float canFillProgress = 0.65f;
        public bool autoCycleCanFill = true;
        public float fullHoldSeconds = 2.5f;

        [Header("3D Fault Visuals & Live Status Beacon")]
        public GameObject faultJamCottonClump;
        public GameObject faultCanOverflowMass;
        public Renderer beaconGreenRenderer;
        public Renderer beaconAmberRenderer;
        public Renderer beaconRedRenderer;
        public Light beaconGlowLight;
        public CardingBeaconState beaconState = CardingBeaconState.GreenNormal;
        public bool calenderJamPrecursor;
        public bool calenderJamFault;
        public bool canOverflowFault;

        private float _feedPhase;
        private float _impurityTime;
        private float _flockPhase;
        private float _webUvOffset;
        private float _sliverPhase;
        private float _nepPhase;
        private float _fullHoldTimer;
        private MaterialPropertyBlock _webMpb;
        private MaterialPropertyBlock _beaconMpb;

        private static readonly int PropBaseMapSt = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int PropBumpMapSt = Shader.PropertyToID("_BumpMap_ST");
        private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int PropEmissionColor = Shader.PropertyToID("_EmissionColor");

        private static readonly Vector3[] SliverSpline =
        {
            new Vector3(1.52f, 0.86f, 0.00f),
            new Vector3(1.57f, 0.89f, 0.07f),
            new Vector3(1.58f, 1.08f, 0.16f),
            new Vector3(1.60f, 1.32f, 0.22f),
            new Vector3(1.68f, 1.54f, 0.16f),
            new Vector3(1.83f, 1.62f, 0.06f),
            new Vector3(1.96f, 1.60f, 0.00f),
            new Vector3(1.96f, 1.50f, 0.00f),
        };

        private void Awake()
        {
            if (cardingAnimator == null)
                cardingAnimator = GetComponent<TrainAR_CardingAnimator>();
        }

        private void Start()
        {
            ApplyAllImmediate(canFillProgress);
        }

        public void ResetEmptyCan()
        {
            canFillProgress = 0.06f;
            _fullHoldTimer = 0f;
            UpdateCoilerAndSliverCan(0f);
        }

        public void SetCanFill(float normalizedFill)
        {
            canFillProgress = Mathf.Clamp01(normalizedFill);
            _fullHoldTimer = 0f;
            UpdateCoilerAndSliverCan(0f);
        }

        public void SetFaultVisuals(bool jamPrecursor, bool jamFault, bool canOverflow, CardingBeaconState beacon)
        {
            calenderJamPrecursor = jamPrecursor;
            calenderJamFault = jamFault;
            canOverflowFault = canOverflow;
            beaconState = beacon;
            UpdateFaultMeshes();
            UpdateBeaconVisuals();
            UpdateSliverFlowNodes(!calenderJamFault && (cardingAnimator == null || !cardingAnimator.IsAssemblyTrainingActive));
            UpdateCoilerAndSliverCan(0f);
        }

        public void ClearAllFaultVisuals()
        {
            SetFaultVisuals(false, false, false, CardingBeaconState.GreenNormal);
        }

        public void SetLooseParticlesVisible(bool visible)
        {
            SetArrayActive(impurityMotes, visible);
            SetArrayActive(cardingFlocks, visible);
            SetArrayActive(sliverFlowNodes, visible && !calenderJamFault);
            if (!visible)
            {
                SetArrayActive(nepDefectBeads, false);
                if (coilerLiveStrand != null) coilerLiveStrand.gameObject.SetActive(false);
            }
            else
            {
                UpdateNepDefects();
                UpdateCoilerAndSliverCan(0f);
            }
        }

        public void ApplyAllImmediate(float fill01, float simPhase = 0.35f)
        {
            canFillProgress = Mathf.Clamp01(fill01);
            _feedPhase = simPhase;
            _impurityTime = simPhase * 4f;
            _flockPhase = simPhase;
            _sliverPhase = simPhase;
            _nepPhase = simPhase;

            UpdateFeedBattLayers();
            UpdateImpurityMotes(true);
            UpdateCardingFlocks(true);
            UpdateSliverFlowNodes(!calenderJamFault);
            UpdateNepDefects();
            UpdateCoilerAndSliverCan(0f);
            UpdateFaultMeshes();
            UpdateBeaconVisuals();
        }

        private void Update()
        {
            if (cardingAnimator == null) return;

            UpdateBeaconVisuals();
            UpdateFaultMeshes();

            // Keep assembly mode super crisp & uncluttered (pause flying loose motes/tufts while assembling!)
            bool inAssembly = cardingAnimator.IsAssemblyTrainingActive;
            bool running = cardingAnimator.isRunning && cardingAnimator.speedScale > 0.001f && !inAssembly;

            if (!running)
            {
                if (inAssembly)
                {
                    SetArrayActive(impurityMotes, false);
                    SetArrayActive(cardingFlocks, false);
                    SetArrayActive(sliverFlowNodes, false);
                    SetArrayActive(nepDefectBeads, false);
                    if (coilerLiveStrand != null) coilerLiveStrand.gameObject.SetActive(false);
                }
                else
                {
                    UpdateNepDefects();
                }
                return;
            }

            float dt = Time.deltaTime;
            float cylScale = Mathf.Clamp(cardingAnimator.cylinderRpm / 500f, 0.5f, 1.6f) * cardingAnimator.speedScale;
            float doffScale = Mathf.Clamp(cardingAnimator.dofferSpeedMpm / 100f, 0.4f, 1.8f) * cardingAnimator.speedScale;

            // Advance phases synchronized to real engineering process sliders
            _feedPhase = (_feedPhase + dt * 0.16f * doffScale) % 1f;
            _impurityTime += dt * 1.45f * cylScale;
            _flockPhase = (_flockPhase + dt * 0.42f * cylScale) % 1f;
            _sliverPhase = (_sliverPhase + dt * 0.45f * doffScale) % 1f;
            _nepPhase = (_nepPhase + dt * 0.32f * doffScale) % 1f;

            // Advance live sliver can filling cycle (pause filling when jammed or already overflowing)
            if (autoCycleCanFill && !calenderJamFault && !canOverflowFault)
            {
                if (canFillProgress < 1f)
                {
                    canFillProgress = Mathf.Min(1f, canFillProgress + dt * 0.055f * doffScale);
                }
                else
                {
                    _fullHoldTimer += dt;
                    if (_fullHoldTimer >= fullHoldSeconds)
                    {
                        _fullHoldTimer = 0f;
                        canFillProgress = 0.05f; // Automatic doffing / fresh empty can cycle!
                    }
                }
            }

            UpdateFeedBattLayers();
            UpdateImpurityMotes(true);
            UpdateCardingFlocks(true);
            UpdateCottonWebScroll(dt, doffScale);
            UpdateSliverFlowNodes(!calenderJamFault);
            UpdateNepDefects();
            UpdateCoilerAndSliverCan(dt * doffScale);
        }

        private void UpdateFaultMeshes()
        {
            bool inAssembly = cardingAnimator != null && cardingAnimator.IsAssemblyTrainingActive;

            if (faultJamCottonClump != null)
            {
                bool showJam = !inAssembly && (calenderJamPrecursor || calenderJamFault);
                faultJamCottonClump.SetActive(showJam);
                if (showJam)
                {
                    float pulse = Application.isPlaying ? (1f + 0.06f * Mathf.Sin(Time.time * 16f)) : 1f;
                    float s = calenderJamFault ? 1.0f : (0.52f * pulse);
                    faultJamCottonClump.transform.localScale = Vector3.one * s;
                }
            }

            if (faultCanOverflowMass != null)
            {
                bool showOver = !inAssembly && canOverflowFault;
                faultCanOverflowMass.SetActive(showOver);
                if (showOver)
                {
                    float s = Application.isPlaying ? (1f + 0.03f * Mathf.Sin(Time.time * 5f)) : 1f;
                    faultCanOverflowMass.transform.localScale = Vector3.one * s;
                }
            }
        }

        private void UpdateBeaconVisuals()
        {
            if (_beaconMpb == null) _beaconMpb = new MaterialPropertyBlock();

            float blinkAmber = Application.isPlaying ? (0.55f + 0.45f * Mathf.Sin(Time.time * 5.2f)) : 1.0f;
            float blinkRed = Application.isPlaying ? (Mathf.Sin(Time.time * 9.5f) > -0.15f ? 1.0f : 0.22f) : 1.0f;

            bool gOn = beaconState == CardingBeaconState.GreenNormal;
            bool aOn = beaconState == CardingBeaconState.AmberWarning;
            bool rOn = beaconState == CardingBeaconState.RedAlarm;

            ApplyLens(beaconGreenRenderer,
                gOn ? new Color(0.16f, 0.94f, 0.50f, 1f) : new Color(0.14f, 0.28f, 0.20f, 1f),
                gOn ? new Color(0.20f, 2.20f, 0.85f, 1f) : Color.black);

            ApplyLens(beaconAmberRenderer,
                aOn ? new Color(1.00f, 0.78f, 0.14f, 1f) : new Color(0.32f, 0.25f, 0.10f, 1f),
                aOn ? new Color(2.60f, 1.65f, 0.12f, 1f) * blinkAmber : Color.black);

            ApplyLens(beaconRedRenderer,
                rOn ? new Color(1.00f, 0.20f, 0.18f, 1f) : new Color(0.30f, 0.12f, 0.12f, 1f),
                rOn ? new Color(3.20f, 0.28f, 0.22f, 1f) * blinkRed : Color.black);

            if (beaconGlowLight != null)
            {
                if (rOn)
                {
                    beaconGlowLight.color = new Color(1.0f, 0.22f, 0.18f);
                    beaconGlowLight.intensity = 2.6f * blinkRed;
                }
                else if (aOn)
                {
                    beaconGlowLight.color = new Color(1.0f, 0.72f, 0.14f);
                    beaconGlowLight.intensity = 2.0f * blinkAmber;
                }
                else
                {
                    beaconGlowLight.color = new Color(0.20f, 0.92f, 0.54f);
                    beaconGlowLight.intensity = 0.85f;
                }
            }
        }

        private void ApplyLens(Renderer r, Color baseCol, Color emissiveCol)
        {
            if (r == null) return;
            r.GetPropertyBlock(_beaconMpb);
            _beaconMpb.SetColor(PropBaseColor, baseCol);
            _beaconMpb.SetColor(PropEmissionColor, emissiveCol);
            r.SetPropertyBlock(_beaconMpb);
        }

        private void UpdateFeedBattLayers()
        {
            if (feedBattLayers == null || feedBattLayers.Length == 0) return;
            int n = feedBattLayers.Length;
            float span = feedTopY - feedBottomY;

            for (int i = 0; i < n; i++)
            {
                var t = feedBattLayers[i];
                if (t == null) continue;

                // u = 0 at top hopper -> u = 1 at bottom fluted feed roller
                float u = ((i / (float)n) + _feedPhase) % 1f;
                float y = Mathf.Lerp(feedTopY, feedBottomY, u);

                // Smooth entry growth at top & gentle mechanical compression into feed plate at bottom!
                float enterScale = Mathf.SmoothStep(0.35f, 1.0f, Mathf.Clamp01(u * 6.5f));
                float compressY = Mathf.Lerp(1.0f, 0.56f, Mathf.Clamp01((u - 0.78f) / 0.22f));
                float compressX = Mathf.Lerp(1.0f, 0.86f, Mathf.Clamp01((u - 0.82f) / 0.18f));
                float swayX = Mathf.Sin(u * Mathf.PI * 2f + i * 1.1f) * 0.008f;

                t.localPosition = new Vector3(feedCenterX + swayX, y, feedCenterZ);
                t.localScale = new Vector3(enterScale * compressX, enterScale * compressY, enterScale);
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -14f, Mathf.Clamp01((u - 0.80f) / 0.20f)));
            }
        }

        private void UpdateImpurityMotes(bool active)
        {
            if (impurityMotes == null || impurityMotes.Length == 0) return;
            int n = impurityMotes.Length;

            for (int i = 0; i < n; i++)
            {
                var m = impurityMotes[i];
                if (m == null) continue;
                m.gameObject.SetActive(active);
                if (!active) continue;

                float phase = ((_impurityTime * 0.55f) + (i / (float)n) * 1.37f) % 1f;
                // Spread across visible front-half cutaway drop zone beneath Licker-in & brass mote knives
                float zSpread = Mathf.Lerp(-0.44f, 0.18f, 0.5f + 0.5f * Mathf.Sin(i * 17.3f + 0.7f));
                // Drops cleanly in open air gap (Y=0.73 -> 0.47) past the 2 brass mote knives into lower trash collector
                float knifeOffset = (i % 2 == 0) ? -0.15f : 0.06f;
                float startX = lickerCenterXY.x + knifeOffset + Mathf.Cos(i * 9.1f) * 0.045f;
                float startY = lickerCenterXY.y - 0.21f;

                float x = startX + (knifeOffset * 0.45f - 0.03f) * phase;
                float y = startY - 0.06f * phase - 0.21f * phase * phase; // parabolic gravity drop into collector tray

                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phase * 9f)) *
                          Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - phase) * 5f));

                m.localPosition = new Vector3(x, y, zSpread);
                m.localRotation = Quaternion.Euler(phase * 420f + i * 35f, phase * 280f, i * 47f);
                m.localScale = Vector3.one * (0.95f + 0.45f * Mathf.Abs(Mathf.Sin(i * 3.7f))) * s;
            }
        }

        private void UpdateCardingFlocks(bool active)
        {
            if (cardingFlocks == null || cardingFlocks.Length == 0) return;
            int n = cardingFlocks.Length;

            for (int i = 0; i < n; i++)
            {
                var f = cardingFlocks[i];
                if (f == null) continue;
                f.gameObject.SetActive(active);
                if (!active) continue;

                float u = ((i / (float)n) + _flockPhase) % 1f;
                float zLane = Mathf.Sin(i * 13.7f) * 0.37f;

                Vector2 pos;
                float tangentDeg;
                Vector3 scale;

                if (u < 0.28f)
                {
                    // Stage A: Riding over upper arc of Licker-in (chunky raw cotton flock!)
                    float t = u / 0.28f;
                    float angDeg = Mathf.Lerp(168f, 12f, t);
                    float ang = angDeg * Mathf.Deg2Rad;
                    pos = lickerCenterXY + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.226f;
                    tangentDeg = angDeg - 90f;
                    scale = new Vector3(1.05f, 0.95f, 1.0f);
                }
                else
                {
                    // Stage B: Swept onto Main Cylinder -> visibly stretched & aligned into fine single fibers under Revolving Flats!
                    float t = (u - 0.28f) / 0.72f;
                    float angDeg = Mathf.Lerp(166f, 8f, t);
                    float ang = angDeg * Mathf.Deg2Rad;
                    pos = mainCylinderCenterXY + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.634f;
                    tangentDeg = angDeg - 90f;

                    // Visual transformation: chunky flock -> elongated sleek single-fiber ribbon!
                    float stretch = Mathf.SmoothStep(0f, 1f, t);
                    scale = new Vector3(
                        Mathf.Lerp(1.05f, 2.15f, stretch),
                        Mathf.Lerp(0.92f, 0.34f, stretch),
                        Mathf.Lerp(1.00f, 1.28f, stretch));
                }

                // Smooth fade in/out at entry and transfer to doffer
                float edgeFade = Mathf.Clamp01(u * 14f) * Mathf.Clamp01((1f - u) * 12f);
                f.localPosition = new Vector3(pos.x, pos.y, zLane);
                f.localRotation = Quaternion.Euler(0f, 0f, tangentDeg);
                f.localScale = scale * edgeFade;
            }
        }

        private void UpdateCottonWebScroll(float dt, float doffScale)
        {
            if (cottonWebRenderer == null) return;
            _webUvOffset = (_webUvOffset - dt * 0.55f * doffScale) % 10f;

            // Zero-allocation MaterialPropertyBlock UV scroll (preserves sharedMaterials across StyleSwitcher toggles!)
            if (_webMpb == null) _webMpb = new MaterialPropertyBlock();
            cottonWebRenderer.GetPropertyBlock(_webMpb, 0);
            var st = new Vector4(1.8f, 2.6f, 0f, _webUvOffset);
            _webMpb.SetVector(PropBaseMapSt, st);
            _webMpb.SetVector(PropBumpMapSt, st);
            cottonWebRenderer.SetPropertyBlock(_webMpb, 0);
        }

        private void UpdateSliverFlowNodes(bool active)
        {
            if (sliverFlowNodes == null || sliverFlowNodes.Length == 0) return;
            int n = sliverFlowNodes.Length;

            for (int i = 0; i < n; i++)
            {
                var node = sliverFlowNodes[i];
                if (node == null) continue;
                node.gameObject.SetActive(active);
                if (!active) continue;

                float u = ((i / (float)n) + _sliverPhase) % 1f;
                SampleSpline(SliverSpline, u, out Vector3 pos, out Vector3 dir);
                node.localPosition = pos;
                if (dir.sqrMagnitude > 1e-5f)
                    node.localRotation = Quaternion.LookRotation(dir);
                float fade = Mathf.Clamp01(u * 10f) * Mathf.Clamp01((1f - u) * 10f);
                node.localScale = new Vector3(1.12f, 1.12f, 1.25f) * fade;
            }
        }

        private void UpdateNepDefects()
        {
            if (nepDefectBeads == null || nepDefectBeads.Length == 0 || cardingAnimator == null) return;

            // Live engineering defect visualization: when Flat Gauge > 0.25mm (or Fault preset!),
            // uncarded cotton nep specks visibly ride across the Doffer cotton web!
            float severity = Mathf.InverseLerp(0.24f, 0.32f, cardingAnimator.flatGaugeMm);
            bool showNeps = severity > 0.05f && !cardingAnimator.IsAssemblyTrainingActive;

            int activeCount = Mathf.RoundToInt(severity * nepDefectBeads.Length);
            for (int i = 0; i < nepDefectBeads.Length; i++)
            {
                var b = nepDefectBeads[i];
                if (b == null) continue;
                bool visible = showNeps && i < activeCount;
                b.gameObject.SetActive(visible);
                if (!visible) continue;

                float u = ((i / (float)nepDefectBeads.Length) + _nepPhase) % 1f;
                // Rides from top of Doffer (102 deg) -> front of Doffer (14 deg) -> tapering toward trumpet!
                float ang = Mathf.Lerp(102f, 14f, Mathf.Clamp01(u / 0.72f)) * Mathf.Deg2Rad;
                Vector2 xy;
                float zHalf;
                if (u < 0.72f)
                {
                    xy = dofferCenterXY + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.372f;
                    zHalf = 0.37f;
                }
                else
                {
                    float t = (u - 0.72f) / 0.28f;
                    Vector2 p0 = dofferCenterXY + new Vector2(Mathf.Cos(14f * Mathf.Deg2Rad), Mathf.Sin(14f * Mathf.Deg2Rad)) * 0.372f;
                    xy = Vector2.Lerp(p0, new Vector2(1.48f, 0.865f), t);
                    zHalf = Mathf.Lerp(0.37f, 0.05f, t);
                }
                float zPos = Mathf.Sin(i * 19.1f) * zHalf;
                b.localPosition = new Vector3(xy.x, xy.y, zPos);
                b.localScale = Vector3.one * (0.85f + 0.4f * severity);
            }
        }

        private void UpdateCoilerAndSliverCan(float dtScaled)
        {
            if (sliverCoilLayers == null || sliverCoilLayers.Length == 0) return;

            int totalLayers = sliverCoilLayers.Length;
            float layerStep = 0.075f;
            float topSurfaceY = 0.96f; // Top coil always stays near the top rim as the spring follower sinks!
            float filledHeight = Mathf.Max(0.08f, canFillProgress * (totalLayers * layerStep));

            // 1. Move internal spring-loaded follower plate downward as sliver accumulates!
            float plateY = Mathf.Clamp(topSurfaceY - filledHeight, 0.14f, 0.88f);
            if (sliverFollowerPlate != null)
                sliverFollowerPlate.localPosition = new Vector3(coilerCenterXZ.x, plateY, coilerCenterXZ.y);

            // 2. Compress the visible helical steel pantograph spring cleanly between can bottom (Y=0.09) and follower plate!
            if (canInternalSpring != null)
            {
                float springBot = 0.095f;
                float springH = Mathf.Max(0.035f, plateY - 0.015f - springBot);
                canInternalSpring.localPosition = new Vector3(coilerCenterXZ.x, springBot, coilerCenterXZ.y);
                canInternalSpring.localScale = new Vector3(1f, springH, 1f);
            }

            // 3. Stack the 11 individual helical sliver coil layers neatly atop the descending follower plate!
            float activeFloat = canFillProgress * totalLayers;
            float topCoilActualY = plateY + 0.045f;

            for (int i = 0; i < totalLayers; i++)
            {
                var coil = sliverCoilLayers[i];
                if (coil == null) continue;

                float layerProgress = Mathf.Clamp01(activeFloat - i);
                bool show = layerProgress > 0.02f;
                coil.gameObject.SetActive(show);
                if (!show) continue;

                float targetY = plateY + 0.052f + i * layerStep;
                topCoilActualY = targetY;

                // Newly laying top coil scales cleanly into place + gentle spin sync with coiler!
                float grow = Mathf.SmoothStep(0.15f, 1.0f, layerProgress);
                coil.localPosition = new Vector3(coilerCenterXZ.x, targetY, coilerCenterXZ.y);
                coil.localScale = new Vector3(grow, grow, grow);
                if (dtScaled > 0f && layerProgress < 0.99f)
                    coil.Rotate(0f, 95f * dtScaled, 0f, Space.Self);
            }

            // 4. Dynamically bridge the rotating brass Coiler Nozzle tip down to the top growing sliver coil!
            if (coilerLiveStrand != null)
            {
                bool showStrand = !calenderJamFault && (cardingAnimator == null || !cardingAnimator.IsAssemblyTrainingActive);
                coilerLiveStrand.gameObject.SetActive(showStrand);
                if (!showStrand) return;
                Vector3 nozzleLocal = new Vector3(coilerCenterXZ.x + 0.075f, 1.21f, coilerCenterXZ.y - 0.02f);
                if (coilerDisc != null)
                {
                    // Compute eccentric nozzle tip position in machine local space from coilerDisc's real rotation!
                    Vector3 worldNozzle = coilerDisc.TransformPoint(new Vector3(0.082f, -0.045f, -0.02f));
                    nozzleLocal = coilerLiveStrand.parent.InverseTransformPoint(worldNozzle);
                }

                Vector3 landingLocal = new Vector3(
                    coilerCenterXZ.x + (nozzleLocal.x - coilerCenterXZ.x) * 1.35f,
                    Mathf.Min(topCoilActualY + 0.015f, 1.04f),
                    coilerCenterXZ.y + (nozzleLocal.z - coilerCenterXZ.y) * 1.35f);

                Vector3 delta = landingLocal - nozzleLocal;
                float len = Mathf.Max(0.08f, delta.magnitude);
                coilerLiveStrand.localPosition = (nozzleLocal + landingLocal) * 0.5f;
                coilerLiveStrand.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
                coilerLiveStrand.localScale = new Vector3(1f, len, 1f);
            }
        }

        private static void SetArrayActive(Transform[] arr, bool active)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != null) arr[i].gameObject.SetActive(active);
        }

        private static void SampleSpline(Vector3[] pts, float u, out Vector3 pos, out Vector3 dir)
        {
            float f = Mathf.Clamp01(u) * (pts.Length - 1);
            int idx = Mathf.Min(Mathf.FloorToInt(f), pts.Length - 2);
            float t = f - idx;
            pos = Vector3.Lerp(pts[idx], pts[idx + 1], t);
            dir = (pts[idx + 1] - pts[idx]).normalized;
        }
    }
}
