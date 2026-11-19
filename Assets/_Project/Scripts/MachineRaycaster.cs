using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Zero-GC machine hover & click raycaster with Fresnel highlight synchronization.
    /// Works in both pointer-cursor mode (BirdEye/Focus/Unlocked FPS) and crosshair mode (Locked FPS).
    /// </summary>
    public class MachineRaycaster : MonoBehaviour
    {
        public static MachineRaycaster Instance { get; private set; }

        public Camera targetCamera;
        public Material hoverHighlightMaterial;
        public Material selectedHighlightMaterial;
        public float maxRayDistance = 350f;

        public MachineInteractable HoveredMachine { get; private set; }
        public event Action<MachineInteractable> OnHoverChanged;

        private MachineInteractable[] _allMachines;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            _allMachines = FindObjectsOfType<MachineInteractable>(true);
            for (int i = 0; i < _allMachines.Length; i++)
                _allMachines[i].SetHighlightMaterials(hoverHighlightMaterial, selectedHighlightMaterial);

            if (SimulationState.Instance != null)
                SimulationState.Instance.OnStationSelected += HandleStationSelected;
        }

        private void OnDestroy()
        {
            if (SimulationState.Instance != null)
                SimulationState.Instance.OnStationSelected -= HandleStationSelected;
        }

        private void HandleStationSelected(int stationIndex)
        {
            if (_allMachines == null) return;
            for (int i = 0; i < _allMachines.Length; i++)
            {
                if (_allMachines[i] != null)
                    _allMachines[i].SetSelected(_allMachines[i].stationIndex == stationIndex);
            }
        }

        private void Update()
        {
            if (targetCamera == null) return;

            bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
            bool overUI = !cursorLocked && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool draggingCamera = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            bool pointerInsideScreen = cursorLocked ||
                (Input.mousePosition.x >= 0f && Input.mousePosition.x <= Screen.width &&
                 Input.mousePosition.y >= 0f && Input.mousePosition.y <= Screen.height);

            MachineInteractable hitMachine = null;
            if (!overUI && !draggingCamera && pointerInsideScreen)
            {
                Ray ray = cursorLocked
                    ? targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                    : targetCamera.ScreenPointToRay(Input.mousePosition);

                var hits = Physics.RaycastAll(ray, maxRayDistance, ~0, QueryTriggerInteraction.Collide);
                if (hits != null && hits.Length > 0)
                {
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var col = hits[i].collider;
                        if (col == null) continue;
                        var inter = col.GetComponentInParent<MachineInteractable>();
                        if (inter != null)
                        {
                            // Skip triggers the camera is currently standing inside or right against (< 2m)
                            if (hits[i].distance < 2.0f || col.bounds.Contains(ray.origin))
                                continue;
                            hitMachine = inter;
                            break;
                        }
                        // Stop at opaque solid walls/machines that block line of sight
                        if (!col.isTrigger && hits[i].distance >= 1.0f)
                            break;
                    }
                }
            }

            if (hitMachine != HoveredMachine)
            {
                if (HoveredMachine != null) HoveredMachine.SetHovered(false);
                HoveredMachine = hitMachine;
                if (HoveredMachine != null) HoveredMachine.SetHovered(true);
                OnHoverChanged?.Invoke(HoveredMachine);
            }

            if (HoveredMachine != null && !overUI)
            {
                bool clicked = cursorLocked ? Input.GetKeyDown(KeyCode.E) : Input.GetMouseButtonDown(0);
                if (clicked)
                {
                    if (CameraRigManager.Instance != null && !cursorLocked)
                        CameraRigManager.Instance.FocusStation(HoveredMachine.stationIndex, false);
                    else if (SimulationState.Instance != null)
                        SimulationState.Instance.SelectStation(HoveredMachine.stationIndex);
                }
            }
        }
    }
}
