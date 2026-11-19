using System;
using System.Collections.Generic;
using ECM.Components;
using ECM.Controllers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// 4-Mode Desktop Camera Rig:
    /// 1. BirdEyeOrbit: Smooth orbit/pan/zoom overview of Workshop 1 & Workshop 2, with Roof Cutaway support.
    /// 2. FirstPersonWalk: Integrates seamlessly with ECM_FirstPerson (with fixed UI mouse-lock handling).
    /// 3. StationFocus: Smooth cubic flight to curated hero vantage points for all 9 stations + right-drag inspect orbit.
    /// 4. AutoTour: Automated station-by-station teaching/showcase tour across ST-01 -> ST-09.
    /// </summary>
    public class CameraRigManager : MonoBehaviour
    {
        public static CameraRigManager Instance { get; private set; }

        [Header("Scene References")]
        public Camera mainCamera;
        public GameObject fpsPlayerRoot;
        public BaseFirstPersonController fpsController;
        public MouseLook fpsMouseLook;
        public Transform fpsCameraAnchor; // Original parent inside ECM_FirstPerson/Camera_Pivot

        [Header("Roof Cutaway Targets")]
        public List<GameObject> workshop1RoofObjects = new List<GameObject>();
        public List<GameObject> workshop2RoofObjects = new List<GameObject>();

        [Header("Orbit Settings")]
        public float orbitSensitivity = 2.6f;
        public float panSpeed = 0.08f;
        public float zoomSpeed = 24f;
        public float minDistance = 12f;
        public float maxDistance = 280f;
        public float smoothFactor = 7.5f;

        [Header("Auto Tour Settings")]
        public float tourDwellSeconds = 8.5f;

        public CameraViewMode CurrentViewMode { get; private set; } = CameraViewMode.FirstPersonWalk;
        public WorkshopHall CurrentHall { get; private set; } = WorkshopHall.SpinningHall;
        public bool IsRoofCutaway { get; private set; }
        public bool IsAutoTourPaused { get; private set; }

        public event Action<CameraViewMode> OnViewModeChanged;
        public event Action<WorkshopHall> OnHallChanged;
        public event Action<bool> OnRoofCutawayChanged;

        // Orbit / Transition State
        private Vector3 _targetPivot = new Vector3(74f, 6f, 145f);
        private Vector3 _currentPivot = new Vector3(74f, 6f, 145f);
        private float _targetYaw = -42f;
        private float _currentYaw = -42f;
        private float _targetPitch = 36f;
        private float _currentPitch = 36f;
        private float _targetDistance = 175f;
        private float _currentDistance = 175f;

        private bool _isFlying;
        private Vector3 _flyStartPos, _flyEndPos;
        private Quaternion _flyStartRot, _flyEndRot;
        private float _flyProgress;
        private float _flyDuration = 1.15f;

        private float _tourTimer;
        private int _tourStationIndex;
        private bool _isRightOrbitDragging;
        private bool _isMiddlePanDragging;

        [Header("First-Person Grounded Spawns & Eye Height")]
        public Vector3 fpsCameraLocalOffset = new Vector3(0f, 4.8f, 0f);
        public float hall1FloorY = 1.10f;
        public float hall2FloorY = 3.80f;

        private readonly Vector3 _fpsHall1Spawn = new Vector3(198f, 1.18f, 162f);
        private readonly Vector3 _fpsHall2Spawn = new Vector3(39.5f, 3.88f, 358f);
        private CharacterMovement _fpsMovement;
        private Rigidbody _fpsRigidbody;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (fpsPlayerRoot == null) fpsPlayerRoot = GameObject.Find("ECM_FirstPerson");
            if (fpsPlayerRoot != null)
            {
                if (fpsController == null) fpsController = fpsPlayerRoot.GetComponent<BaseFirstPersonController>();
                if (fpsMouseLook == null) fpsMouseLook = fpsPlayerRoot.GetComponent<MouseLook>();
                _fpsMovement = fpsPlayerRoot.GetComponent<CharacterMovement>();
                _fpsRigidbody = fpsPlayerRoot.GetComponent<Rigidbody>();
                if (fpsCameraAnchor == null && mainCamera != null && mainCamera.transform.IsChildOf(fpsPlayerRoot.transform))
                    fpsCameraAnchor = mainCamera.transform.parent;

                // Preserve calibrated eye height from scene camera if parented inside ECM_FirstPerson
                if (mainCamera != null && fpsCameraAnchor != null && mainCamera.transform.parent == fpsCameraAnchor)
                {
                    if (mainCamera.transform.localPosition.y >= 1.5f)
                        fpsCameraLocalOffset = mainCamera.transform.localPosition;
                }

                // Crisp industrial hall walking speed (large 280m factory)
                if (fpsController != null)
                {
                    fpsController.forwardSpeed = Mathf.Max(fpsController.forwardSpeed, 12f);
                    fpsController.backwardSpeed = Mathf.Max(fpsController.backwardSpeed, 9f);
                    fpsController.strafeSpeed = Mathf.Max(fpsController.strafeSpeed, 10f);
                    fpsController.runSpeedMultiplier = Mathf.Max(fpsController.runSpeedMultiplier, 2.4f);
                }
            }

            ApplySensitivity(AppLaunchContext.MouseSensitivity);

            // Launch into requested view mode (Default: FirstPersonWalk so WASD works immediately!)
            if (AppLaunchContext.InitialMode == AppMode.TeachingTour)
            {
                SetViewMode(CameraViewMode.StationFocus);
                FocusStation(0, true);
            }
            else
            {
                SetViewMode(AppLaunchContext.InitialCameraMode);
                if (CurrentViewMode == CameraViewMode.BirdEyeOrbit)
                {
                    SetRoofCutaway(true);
                    FocusHall(WorkshopHall.SpinningHall, true);
                }
            }
        }

        private void Update()
        {
            HandleGlobalHotkeys();

            if (CurrentViewMode == CameraViewMode.FirstPersonWalk)
            {
                if (Input.GetKeyDown(KeyCode.R))
                    ResetFpsPosition();

                // Rock-solid anti-void safety guard: never allow player or camera beneath the floor plane
                if (fpsPlayerRoot != null)
                {
                    float floorY = CurrentHall == WorkshopHall.SpinningHall ? hall1FloorY : hall2FloorY;
                    if (fpsPlayerRoot.transform.position.y < floorY - 0.55f)
                    {
                        TeleportFpsToHall(CurrentHall);
                    }
                }
                return;
            }

            if (CurrentViewMode == CameraViewMode.AutoTour)
            {
                UpdateAutoTour();
            }

            if (_isFlying)
            {
                UpdateFlight();
            }
            else
            {
                UpdateOrbitControl();
            }
        }

        private void HandleGlobalHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) SetViewMode(CameraViewMode.BirdEyeOrbit);
            else if (Input.GetKeyDown(KeyCode.F2)) SetViewMode(CameraViewMode.FirstPersonWalk);
            else if (Input.GetKeyDown(KeyCode.F3)) SetViewMode(CameraViewMode.AutoTour);
            else if (Input.GetKeyDown(KeyCode.F4)) ToggleRoofCutaway();
        }

        public void SetViewMode(CameraViewMode mode)
        {
            CurrentViewMode = mode;
            _isFlying = false;

            if (mode == CameraViewMode.FirstPersonWalk)
            {
                // Restore roof when walking inside & unfreeze FPS player safely on floor
                SetRoofCutaway(false);
                float floorY = CurrentHall == WorkshopHall.SpinningHall ? hall1FloorY : hall2FloorY;
                if (fpsPlayerRoot != null && fpsPlayerRoot.transform.position.y < floorY - 0.25f)
                    TeleportFpsToHall(CurrentHall);
                if (_fpsRigidbody != null)
                {
                    _fpsRigidbody.isKinematic = false;
                    _fpsRigidbody.velocity = Vector3.zero;
                    _fpsRigidbody.angularVelocity = Vector3.zero;
                }
                if (_fpsMovement != null) _fpsMovement.enabled = true;
                AttachCameraToFps();
                if (fpsController != null) fpsController.enabled = true;
                // Keep cursor free by default: WASD walks immediately + Right-Drag rotates view + Tab toggles locked FPS!
                if (fpsMouseLook != null) fpsMouseLook.SetCursorLock(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                DetachCameraFromFps();
                if (fpsController != null) fpsController.enabled = false;
                if (_fpsMovement != null) _fpsMovement.enabled = false;
                if (_fpsRigidbody != null)
                {
                    _fpsRigidbody.velocity = Vector3.zero;
                    _fpsRigidbody.isKinematic = true;
                }
                if (fpsMouseLook != null) fpsMouseLook.SetCursorLock(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (mode == CameraViewMode.BirdEyeOrbit)
                {
                    SetRoofCutaway(true);
                    FocusHall(CurrentHall, false);
                }
                else if (mode == CameraViewMode.AutoTour)
                {
                    IsAutoTourPaused = false;
                    _tourTimer = 0f;
                    var sim = SimulationState.Instance;
                    _tourStationIndex = (sim != null && sim.selectedStationIndex >= 0) ? sim.selectedStationIndex : 0;
                    FocusStation(_tourStationIndex, false);
                }
            }

            OnViewModeChanged?.Invoke(CurrentViewMode);
        }

        public void FocusHall(WorkshopHall hall, bool instant)
        {
            CurrentHall = hall;
            OnHallChanged?.Invoke(hall);

            if (CurrentViewMode == CameraViewMode.FirstPersonWalk)
            {
                TeleportFpsToHall(hall);
                return;
            }

            if (hall == WorkshopHall.SpinningHall)
            {
                _targetPivot = new Vector3(74f, 6f, 145f);
                _targetYaw = 46f;
                _targetPitch = 28f;
                _targetDistance = 168f;
            }
            else
            {
                _targetPivot = new Vector3(39.5f, 8.5f, 412f);
                _targetYaw = 180f;
                _targetPitch = 22f;
                _targetDistance = 64f;
            }

            if (instant)
            {
                _isFlying = false;
                _currentPivot = _targetPivot;
                _currentYaw = _targetYaw;
                _currentPitch = _targetPitch;
                _currentDistance = _targetDistance;
                ApplyOrbitTransform();
            }
            else
            {
                Vector3 endPos = ComputeOrbitPosition(_targetPivot, _targetYaw, _targetPitch, _targetDistance);
                Quaternion endRot = Quaternion.LookRotation((_targetPivot - endPos).normalized, Vector3.up);
                StartFlight(endPos, endRot, 1.0f);
            }
        }

        public void FocusStation(int stationIndex, bool instant = false)
        {
            var sim = SimulationState.Instance;
            if (sim == null || stationIndex < 0 || stationIndex >= sim.Stations.Count) return;

            var st = sim.Stations[stationIndex];
            CurrentHall = st.hall;
            OnHallChanged?.Invoke(CurrentHall);

            // Switch to StationFocus if currently walking or in BirdEye
            if (CurrentViewMode == CameraViewMode.FirstPersonWalk)
            {
                DetachCameraFromFps();
                if (fpsController != null) fpsController.enabled = false;
                if (_fpsMovement != null) _fpsMovement.enabled = false;
                if (_fpsRigidbody != null)
                {
                    _fpsRigidbody.velocity = Vector3.zero;
                    _fpsRigidbody.isKinematic = true;
                }
                if (fpsMouseLook != null) fpsMouseLook.SetCursorLock(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (CurrentViewMode != CameraViewMode.AutoTour)
            {
                CurrentViewMode = CameraViewMode.StationFocus;
                OnViewModeChanged?.Invoke(CurrentViewMode);
            }

            // Inside view looks best with ceiling/lights visible!
            SetRoofCutaway(false);

            sim.SelectStation(stationIndex);

            Vector3 camPos = st.focusCameraPos;
            Vector3 lookAt = st.focusLookAtPos;
            Vector3 offset = camPos - lookAt;
            float dist = Mathf.Max(offset.magnitude, 10f);
            Vector3 dir = offset / dist;

            _targetPivot = lookAt;
            _targetDistance = dist;
            _targetPitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(dir.y, -0.95f, 0.95f)) * Mathf.Rad2Deg, 5f, 75f);
            _targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            if (instant)
            {
                _isFlying = false;
                _currentPivot = _targetPivot;
                _currentYaw = _targetYaw;
                _currentPitch = _targetPitch;
                _currentDistance = _targetDistance;
                ApplyOrbitTransform();
            }
            else
            {
                Quaternion endRot = Quaternion.LookRotation((lookAt - camPos).normalized, Vector3.up);
                StartFlight(camPos, endRot, 1.15f);
            }
        }

        public void NextTourStation()
        {
            var sim = SimulationState.Instance;
            if (sim == null) return;
            _tourStationIndex = (_tourStationIndex + 1) % sim.Stations.Count;
            _tourTimer = 0f;
            FocusStation(_tourStationIndex, false);
        }

        public void PrevTourStation()
        {
            var sim = SimulationState.Instance;
            if (sim == null) return;
            _tourStationIndex = (_tourStationIndex - 1 + sim.Stations.Count) % sim.Stations.Count;
            _tourTimer = 0f;
            FocusStation(_tourStationIndex, false);
        }

        public void ToggleAutoTourPause()
        {
            IsAutoTourPaused = !IsAutoTourPaused;
        }

        public void ToggleRoofCutaway()
        {
            SetRoofCutaway(!IsRoofCutaway);
        }

        public void SetRoofCutaway(bool cutaway)
        {
            IsRoofCutaway = cutaway;
            bool visible = !cutaway;
            for (int i = 0; i < workshop1RoofObjects.Count; i++)
                if (workshop1RoofObjects[i] != null) workshop1RoofObjects[i].SetActive(visible);
            for (int i = 0; i < workshop2RoofObjects.Count; i++)
                if (workshop2RoofObjects[i] != null) workshop2RoofObjects[i].SetActive(visible);

            if (SimulationState.Instance != null)
                SimulationState.Instance.roofCutawayEnabled = cutaway;
            OnRoofCutawayChanged?.Invoke(cutaway);
        }

        public void ApplySensitivity(float sens)
        {
            AppLaunchContext.MouseSensitivity = sens;
            orbitSensitivity = sens * 1.3f;
            if (fpsMouseLook != null)
            {
                fpsMouseLook.lateralSensitivity = sens;
                fpsMouseLook.verticalSensitivity = sens;
            }
        }

        public void ResetFpsPosition()
        {
            TeleportFpsToHall(CurrentHall);
        }

        private void TeleportFpsToHall(WorkshopHall hall)
        {
            if (fpsPlayerRoot == null) return;
            Vector3 target = hall == WorkshopHall.SpinningHall ? _fpsHall1Spawn : _fpsHall2Spawn;
            Quaternion targetRot = hall == WorkshopHall.SpinningHall
                ? Quaternion.Euler(0f, -122f, 0f)
                : Quaternion.Euler(0f, 0f, 0f);

            fpsPlayerRoot.transform.position = target;
            fpsPlayerRoot.transform.rotation = targetRot;
            if (_fpsRigidbody == null) _fpsRigidbody = fpsPlayerRoot.GetComponent<Rigidbody>();
            if (_fpsRigidbody != null)
            {
                _fpsRigidbody.position = target;
                _fpsRigidbody.rotation = targetRot;
                if (!_fpsRigidbody.isKinematic)
                {
                    _fpsRigidbody.velocity = Vector3.zero;
                    _fpsRigidbody.angularVelocity = Vector3.zero;
                }
            }
            Physics.SyncTransforms();

            if (mainCamera != null && fpsCameraAnchor != null && mainCamera.transform.parent == fpsCameraAnchor)
            {
                mainCamera.transform.localPosition = fpsCameraLocalOffset;
                mainCamera.transform.localRotation = Quaternion.identity;
            }
            if (fpsMouseLook != null && mainCamera != null)
                fpsMouseLook.Init(fpsPlayerRoot.transform, mainCamera.transform);
        }

        private void AttachCameraToFps()
        {
            if (mainCamera == null || fpsCameraAnchor == null) return;
            mainCamera.transform.SetParent(fpsCameraAnchor, false);
            // CRITICAL: preserve factory-scaled eye height (~4.8m above feet) so camera never clips beneath floor!
            mainCamera.transform.localPosition = fpsCameraLocalOffset;
            mainCamera.transform.localRotation = Quaternion.identity;
            if (fpsMouseLook != null && fpsPlayerRoot != null)
                fpsMouseLook.Init(fpsPlayerRoot.transform, mainCamera.transform);
        }

        private void DetachCameraFromFps()
        {
            if (mainCamera == null) return;
            if (mainCamera.transform.parent != null)
                mainCamera.transform.SetParent(null, true);
        }

        private void StartFlight(Vector3 endPos, Quaternion endRot, float duration)
        {
            if (mainCamera == null) return;
            _flyStartPos = mainCamera.transform.position;
            _flyStartRot = mainCamera.transform.rotation;
            _flyEndPos = endPos;
            _flyEndRot = endRot;
            _flyDuration = Mathf.Max(0.25f, duration);
            _flyProgress = 0f;
            _isFlying = true;
        }

        private void UpdateFlight()
        {
            _flyProgress += Time.unscaledDeltaTime / _flyDuration;
            float t = Mathf.Clamp01(_flyProgress);
            // Smooth cubic ease-in-out
            float eased = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

            mainCamera.transform.position = Vector3.Lerp(_flyStartPos, _flyEndPos, eased);
            mainCamera.transform.rotation = Quaternion.Slerp(_flyStartRot, _flyEndRot, eased);

            if (t >= 1f)
            {
                _isFlying = false;
                _currentPivot = _targetPivot;
                _currentYaw = _targetYaw;
                _currentPitch = _targetPitch;
                _currentDistance = _targetDistance;
            }
        }

        private void UpdateAutoTour()
        {
            if (IsAutoTourPaused || _isFlying) return;
            _targetYaw += 6.5f * Time.unscaledDeltaTime; // Gentle hero showcase orbit while dwelling
            _tourTimer += Time.unscaledDeltaTime;
            if (_tourTimer >= tourDwellSeconds)
            {
                NextTourStation();
            }
        }

        private void UpdateOrbitControl()
        {
            if (mainCamera == null) return;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (Input.GetMouseButtonDown(1) && !overUI) _isRightOrbitDragging = true;
            if (!Input.GetMouseButton(1)) _isRightOrbitDragging = false;
            if (Input.GetMouseButtonDown(2) && !overUI) _isMiddlePanDragging = true;
            if (!Input.GetMouseButton(2)) _isMiddlePanDragging = false;

            // Right-mouse drag to orbit (continues smoothly even if cursor passes over a UI pill)
            if (_isRightOrbitDragging)
            {
                _targetYaw += Input.GetAxis("Mouse X") * orbitSensitivity * 1.4f;
                _targetPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity * 1.2f;
                _targetPitch = Mathf.Clamp(_targetPitch, 8f, 82f);
            }

            // Middle-mouse drag to pan
            if (_isMiddlePanDragging)
            {
                float scale = _targetDistance * panSpeed * 0.025f;
                Vector3 right = mainCamera.transform.right;
                Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
                _targetPivot -= (right * Input.GetAxis("Mouse X") + forward * Input.GetAxis("Mouse Y")) * scale * 12f;
            }

            if (!overUI)
            {
                // Scroll wheel zoom
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomSpeed * (_targetDistance * 0.03f + 1.5f), minDistance, maxDistance);
                }
            }

            // Smooth WASD + Q/E free movement works in BOTH BirdEyeOrbit AND StationFocus modes!
            if (CurrentViewMode == CameraViewMode.BirdEyeOrbit || CurrentViewMode == CameraViewMode.StationFocus)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                float upDown = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
                if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f || Mathf.Abs(upDown) > 0.01f)
                {
                    Vector3 right = mainCamera.transform.right;
                    Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
                    float shiftMult = Input.GetKey(KeyCode.LeftShift) ? 2.2f : 1.0f;
                    float moveSpd = Mathf.Clamp(_targetDistance * 0.45f, 16f, 88f) * shiftMult;
                    _targetPivot += (right * h + forward * v + Vector3.up * (upDown * 0.6f)) * (moveSpd * Time.unscaledDeltaTime);
                    float minPivotY = (CurrentHall == WorkshopHall.SpinningHall ? hall1FloorY : hall2FloorY) + 1.0f;
                    _targetPivot.y = Mathf.Clamp(_targetPivot.y, minPivotY, 55f);
                }
            }

            float k = 1f - Mathf.Exp(-smoothFactor * Time.unscaledDeltaTime);
            _currentPivot = Vector3.Lerp(_currentPivot, _targetPivot, k);
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, k);
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, k);
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, k);
            ApplyOrbitTransform();
        }

        private void ApplyOrbitTransform()
        {
            if (mainCamera == null) return;
            Vector3 pos = ComputeOrbitPosition(_currentPivot, _currentYaw, _currentPitch, _currentDistance);
            float minCamY = (CurrentHall == WorkshopHall.SpinningHall ? hall1FloorY : hall2FloorY) + 1.8f;
            if (pos.y < minCamY) pos.y = minCamY; // Never clip beneath either workshop floor
            mainCamera.transform.position = pos;
            mainCamera.transform.LookAt(_currentPivot, Vector3.up);
        }

        private static Vector3 ComputeOrbitPosition(Vector3 pivot, float yawDeg, float pitchDeg, float dist)
        {
            // Spherical elevation coordinates: positive pitchDeg ALWAYS elevates camera ABOVE pivot
            float yawRad = yawDeg * Mathf.Deg2Rad;
            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            float cosP = Mathf.Cos(pitchRad);
            Vector3 offset = new Vector3(
                Mathf.Sin(yawRad) * cosP,
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * cosP
            ) * dist;
            return pivot + offset;
        }
    }
}
