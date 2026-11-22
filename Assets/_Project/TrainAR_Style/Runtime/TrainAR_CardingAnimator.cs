using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.TrainAR
{
    public enum TrainARViewMode
    {
        Cutaway = 0,
        Solid = 1,
        Exploded = 2
    }

    [Serializable]
    public class TrainARAssemblyPart
    {
        public string partId;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Transform root;
        public Vector3 explodedOffset;
        public bool hideInCutaway;
        public bool onlyInSolid;
        public Vector3 focusCameraOffset = new Vector3(1.6f, 0.8f, 3.2f);
        public Vector3 focusLookOffset = Vector3.zero;

        [NonSerialized] public Vector3 baseLocalPos;
        [NonSerialized] public Renderer[] renderers;
    }

    /// <summary>
    /// Animates the ST-03 Carding Machine (梳棉机) mechanical kinematics:
    /// Main Cylinder, Licker-in, Doffer, Feed/Stripping/Calender Rollers, Coiler Head,
    /// 26 Revolving Flat-Top Bars traveling continuously along the curved arch track,
    /// smooth Cutaway / Solid / Exploded transitions, and TrainAR-style teaching focus highlight.
    /// </summary>
    public class TrainAR_CardingAnimator : MonoBehaviour
    {
        [Header("Rotating Mechanical Drums")]
        public Transform mainCylinder;
        public Transform lickerIn;
        public Transform doffer;
        public Transform feedRoller;
        public Transform strippingRoller;
        public Transform calenderUpper;
        public Transform calenderLower;
        public Transform coilerDisc;
        public Transform flatSprocketRear;
        public Transform flatSprocketFront;
        public Transform driveMotorPulley;

        [Header("Revolving Flat-Top Loop")]
        public Transform[] flatTopBars;
        public Vector2 mainCylinderCenterXY = new Vector2(-0.04f, 1.04f);
        public float flatWorkingRadius = 0.675f;
        public float flatReturnHeight = 1.94f;
        public float flatRearX = -0.62f;
        public float flatFrontX = 0.54f;

        [Header("Cotton Web UV Animation")]
        public Renderer cottonWebRenderer;

        [Header("Assembly Parts (Cutaway / Exploded / Focus)")]
        public List<TrainARAssemblyPart> parts = new List<TrainARAssemblyPart>();

        [Header("Kinematics Controls")]
        public bool isRunning = true;
        [Range(0f, 2.5f)] public float speedScale = 1.0f;
        [Range(360f, 560f)] public float cylinderRpm = 500f;
        [Range(60f, 140f)] public float dofferSpeedMpm = 100f;
        [Range(0.15f, 0.35f)] public float flatGaugeMm = 0.20f;
        public TrainARViewMode viewMode = TrainARViewMode.Cutaway;
        [Range(0f, 1f)] public float explodeFactor = 0f;

        private float _flatPhase;
        private float _targetExplode;
        private string _focusedPartId = "";
        private readonly HashSet<string> _pulsingPartIds = new HashSet<string>();
        private readonly Dictionary<string, float> _partExplodeCurrent = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _partExplodeTarget = new Dictionary<string, float>();
        private bool _assemblyTrainingActive;
        public bool IsAssemblyTrainingActive => _assemblyTrainingActive;
        private MaterialPropertyBlock _mpb;
        private Vector2[] _flatTrackPoints;
        private float[] _flatTrackCumulative;
        private float _flatTrackLength;

        private static readonly int HighlightAmountId = Shader.PropertyToID("_HighlightAmount");
        private static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            CacheInitialTransforms();
            BuildFlatTrackLut();
            ApplyViewModeImmediate(viewMode);
        }

        public void CacheInitialTransforms()
        {
            foreach (var p in parts)
            {
                if (p == null || p.root == null) continue;
                p.baseLocalPos = p.root.localPosition;
                p.renderers = p.root.GetComponentsInChildren<Renderer>(true);
                if (!string.IsNullOrEmpty(p.partId))
                {
                    if (!_partExplodeCurrent.ContainsKey(p.partId)) _partExplodeCurrent[p.partId] = 0f;
                    if (!_partExplodeTarget.ContainsKey(p.partId)) _partExplodeTarget[p.partId] = 0f;
                }
            }
        }

        public void BuildFlatTrackLut()
        {
            // Closed loop:
            // 1) Lower working arc around main cylinder from angle 142 deg -> 38 deg (left to right)
            // 2) Front sprocket arc climbing from working end up to top return deck
            // 3) Top return straight run right -> left at Y = flatReturnHeight
            // 4) Rear sprocket arc descending back to working start
            var pts = new List<Vector2>();

            float startDeg = 142f;
            float endDeg = 38f;
            int workSteps = 32;
            for (int i = 0; i <= workSteps; i++)
            {
                float ang = Mathf.Lerp(startDeg, endDeg, i / (float)workSteps) * Mathf.Deg2Rad;
                pts.Add(mainCylinderCenterXY + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * flatWorkingRadius);
            }

            // Climb gently around front sprocket to return height
            Vector2 pFrontWork = pts[pts.Count - 1];
            Vector2 pFrontTop = new Vector2(flatFrontX - 0.06f, flatReturnHeight);
            Vector2 pFrontMid = new Vector2(flatFrontX + 0.14f, (pFrontWork.y + pFrontTop.y) * 0.5f + 0.04f);
            AddQuadBezier(pts, pFrontWork, pFrontMid, pFrontTop, 10);

            // Top return run right -> left
            Vector2 pRearTop = new Vector2(flatRearX + 0.06f, flatReturnHeight);
            for (int i = 1; i <= 16; i++)
                pts.Add(Vector2.Lerp(pFrontTop, pRearTop, i / 16f));

            // Descend around rear sprocket back to working start
            Vector2 pRearWork = pts[0];
            Vector2 pRearMid = new Vector2(flatRearX - 0.14f, (pRearWork.y + pRearTop.y) * 0.5f + 0.04f);
            AddQuadBezier(pts, pRearTop, pRearMid, pRearWork, 10);

            _flatTrackPoints = pts.ToArray();
            _flatTrackCumulative = new float[_flatTrackPoints.Length];
            _flatTrackLength = 0f;
            for (int i = 1; i < _flatTrackPoints.Length; i++)
            {
                _flatTrackLength += Vector2.Distance(_flatTrackPoints[i - 1], _flatTrackPoints[i]);
                _flatTrackCumulative[i] = _flatTrackLength;
            }
        }

        private static void AddQuadBezier(List<Vector2> dst, Vector2 p0, Vector2 p1, Vector2 p2, int steps)
        {
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float u = 1f - t;
                dst.Add(u * u * p0 + 2f * u * t * p1 + t * t * p2);
            }
        }

        public void SetViewMode(TrainARViewMode mode)
        {
            _assemblyTrainingActive = false;
            viewMode = mode;
            _targetExplode = (mode == TrainARViewMode.Exploded) ? 1f : 0f;
            foreach (var p in parts)
            {
                if (p != null && !string.IsNullOrEmpty(p.partId))
                    _partExplodeTarget[p.partId] = _targetExplode;
            }
            UpdatePartVisibility();
        }

        public void ApplyViewModeImmediate(TrainARViewMode mode)
        {
            _assemblyTrainingActive = false;
            viewMode = mode;
            _targetExplode = (mode == TrainARViewMode.Exploded) ? 1f : 0f;
            explodeFactor = _targetExplode;
            foreach (var p in parts)
            {
                if (p != null && !string.IsNullOrEmpty(p.partId))
                {
                    _partExplodeCurrent[p.partId] = _targetExplode;
                    _partExplodeTarget[p.partId] = _targetExplode;
                }
            }
            UpdatePartVisibility();
            ApplyExplodePositions();
            UpdateFlatTopPositions();
        }

        /// <summary>
        /// Enters TrainAR-style guided step-by-step assembly training:
        /// assembledIds sit cleanly at their installed positions, pendingIds float at exploded staging offsets,
        /// and activeTargetIds pulse with a guiding cyan highlight.
        /// </summary>
        public void SetAssemblyTrainingState(HashSet<string> assembledIds, IEnumerable<string> activeTargetIds, bool immediate = false)
        {
            _assemblyTrainingActive = true;
            viewMode = TrainARViewMode.Cutaway;
            _pulsingPartIds.Clear();
            if (activeTargetIds != null)
            {
                foreach (var id in activeTargetIds)
                    if (!string.IsNullOrEmpty(id)) _pulsingPartIds.Add(id);
            }

            foreach (var p in parts)
            {
                if (p == null || p.root == null) continue;
                if (p.onlyInSolid)
                {
                    p.root.gameObject.SetActive(false);
                    continue;
                }
                p.root.gameObject.SetActive(true);

                bool isAssembled = assembledIds != null && assembledIds.Contains(p.partId);
                float target = isAssembled ? 0f : 1f;
                _partExplodeTarget[p.partId] = target;
                if (immediate) _partExplodeCurrent[p.partId] = target;
            }

            if (immediate) ApplyExplodePositions();
            UpdateFocusHighlights();
        }

        public void ExitAssemblyTraining()
        {
            _assemblyTrainingActive = false;
            _pulsingPartIds.Clear();
            SetViewMode(TrainARViewMode.Cutaway);
            UpdateFocusHighlights();
        }

        public void SetFocusedPart(string partId)
        {
            _focusedPartId = partId ?? "";
            _pulsingPartIds.Clear();
            if (!string.IsNullOrEmpty(_focusedPartId))
                _pulsingPartIds.Add(_focusedPartId);
            UpdateFocusHighlights();
        }

        public void SetFocusedParts(IEnumerable<string> partIds)
        {
            _focusedPartId = "";
            _pulsingPartIds.Clear();
            if (partIds != null)
            {
                foreach (var id in partIds)
                    if (!string.IsNullOrEmpty(id)) _pulsingPartIds.Add(id);
            }
            UpdateFocusHighlights();
        }

        public string ResolvePartIdFromCollider(Transform hitTransform)
        {
            if (hitTransform == null) return "";
            foreach (var p in parts)
            {
                if (p == null || p.root == null) continue;
                if (hitTransform == p.root || hitTransform.IsChildOf(p.root))
                    return p.partId;
            }
            return "";
        }

        private void UpdatePartVisibility()
        {
            foreach (var p in parts)
            {
                if (p == null || p.root == null) continue;
                bool visible = true;
                if (viewMode == TrainARViewMode.Cutaway && p.hideInCutaway) visible = false;
                if (viewMode == TrainARViewMode.Exploded && p.onlyInSolid) visible = false;
                p.root.gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Smooth per-part explode / snap-assembly animation
            bool anyMoved = false;
            foreach (var p in parts)
            {
                if (p == null || p.root == null || string.IsNullOrEmpty(p.partId)) continue;
                float cur = _partExplodeCurrent.TryGetValue(p.partId, out float c) ? c : explodeFactor;
                float tgt = _partExplodeTarget.TryGetValue(p.partId, out float t) ? t : _targetExplode;
                if (Mathf.Abs(cur - tgt) > 0.0005f)
                {
                    cur = Mathf.MoveTowards(cur, tgt, dt * 2.6f);
                    _partExplodeCurrent[p.partId] = cur;
                    anyMoved = true;
                }
            }
            if (anyMoved)
            {
                ApplyExplodePositions();
            }

            if (isRunning && speedScale > 0.001f && !_assemblyTrainingActive)
            {
                float cylFactor = Mathf.Clamp(cylinderRpm / 500f, 0.5f, 1.6f);
                float doffFactor = Mathf.Clamp(dofferSpeedMpm / 100f, 0.4f, 1.8f);
                float sCyl = speedScale * cylFactor * dt;
                float sDoff = speedScale * doffFactor * dt;

                // Realistic relative carding directions & parameter-driven angular velocities!
                RotateZ(mainCylinder, -145f * sCyl);
                RotateZ(lickerIn, +310f * sCyl);
                RotateZ(feedRoller, -42f * sDoff);
                RotateZ(doffer, +68f * sDoff);
                RotateZ(strippingRoller, -195f * sDoff);
                RotateZ(calenderUpper, -110f * sDoff);
                RotateZ(calenderLower, +110f * sDoff);
                RotateZ(flatSprocketRear, -55f * sCyl);
                RotateZ(flatSprocketFront, -55f * sCyl);
                RotateZ(driveMotorPulley, -260f * sCyl);

                if (coilerDisc != null)
                    coilerDisc.Rotate(0f, 120f * sDoff, 0f, Space.Self);

                _flatPhase = (_flatPhase + 0.045f * sCyl) % 1f;
                UpdateFlatTopPositions();
            }

            // Smooth animated focus pulse for highlighted teaching parts
            if (_pulsingPartIds.Count > 0)
            {
                UpdateFocusHighlights();
            }
        }

        private static void RotateZ(Transform t, float deg)
        {
            if (t != null) t.Rotate(0f, 0f, deg, Space.Self);
        }

        public void ApplyExplodePositions()
        {
            foreach (var p in parts)
            {
                if (p == null || p.root == null) continue;
                float f = (!string.IsNullOrEmpty(p.partId) && _partExplodeCurrent.TryGetValue(p.partId, out float c)) ? c : explodeFactor;
                float eased = f * f * (3f - 2f * f);
                p.root.localPosition = p.baseLocalPos + p.explodedOffset * eased;
            }
        }

        public void UpdateFlatTopPositions()
        {
            if (flatTopBars == null || flatTopBars.Length == 0) return;
            if (_flatTrackPoints == null || _flatTrackPoints.Length < 2) BuildFlatTrackLut();

            int count = flatTopBars.Length;
            for (int i = 0; i < count; i++)
            {
                if (flatTopBars[i] == null) continue;
                float u = ((i / (float)count) + _flatPhase) % 1f;
                SampleTrack(u, out Vector2 pos, out Vector2 tangent);

                // Bar normal points outward from loop (to the left of clockwise travel in XY)
                Vector2 normalOut = new Vector2(-tangent.y, tangent.x);
                // On working arc (moving left->right, tangent roughly +X), outward should point +Y (away from cylinder center)
                if (pos.y < flatReturnHeight - 0.18f)
                {
                    Vector2 radial = (pos - mainCylinderCenterXY).normalized;
                    if (Vector2.Dot(normalOut, radial) < 0f) normalOut = -normalOut;
                }
                else
                {
                    if (normalOut.y < 0f) normalOut = -normalOut;
                }

                float angleDeg = Mathf.Atan2(normalOut.y, normalOut.x) * Mathf.Rad2Deg - 90f;
                flatTopBars[i].localPosition = new Vector3(pos.x, pos.y, 0f);
                flatTopBars[i].localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            }
        }

        private void SampleTrack(float u, out Vector2 pos, out Vector2 tangent)
        {
            float targetDist = Mathf.Repeat(u, 1f) * _flatTrackLength;
            int idx = 0;
            while (idx < _flatTrackCumulative.Length - 2 && _flatTrackCumulative[idx + 1] < targetDist)
                idx++;

            float segLen = Mathf.Max(_flatTrackCumulative[idx + 1] - _flatTrackCumulative[idx], 1e-5f);
            float t = Mathf.Clamp01((targetDist - _flatTrackCumulative[idx]) / segLen);
            pos = Vector2.Lerp(_flatTrackPoints[idx], _flatTrackPoints[idx + 1], t);
            tangent = (_flatTrackPoints[idx + 1] - _flatTrackPoints[idx]).normalized;
        }

        private void UpdateFocusHighlights()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Color focusCol = new Color(0.18f, 0.78f, 1.0f, 1f);
            float pulse = Application.isPlaying ? (0.65f + 0.35f * Mathf.Sin(Time.time * 4.2f)) : 0.85f;

            foreach (var p in parts)
            {
                if (p == null || p.root == null) continue;
                if (p.renderers == null || p.renderers.Length == 0)
                    p.renderers = p.root.GetComponentsInChildren<Renderer>(true);

                bool isFocused = _pulsingPartIds.Contains(p.partId);
                float amt = isFocused ? 0.65f : 0f;

                foreach (var r in p.renderers)
                {
                    if (r == null) continue;
                    // Preserve dynamic UV scrolling on CottonWeb & live HDR beacon glow on Beacon_* lenses!
                    if (r == cottonWebRenderer || r.gameObject.name.StartsWith("Beacon_")) continue;
                    if (isFocused)
                    {
                        r.GetPropertyBlock(_mpb);
                        _mpb.SetFloat(HighlightAmountId, amt);
                        _mpb.SetColor(HighlightColorId, focusCol);
                        _mpb.SetColor(EmissionColorId, new Color(0.05f, 0.36f, 0.54f, 1f) * pulse);
                        r.SetPropertyBlock(_mpb);
                    }
                    else
                    {
                        r.SetPropertyBlock(null);
                    }
                }
            }
        }
    }
}
