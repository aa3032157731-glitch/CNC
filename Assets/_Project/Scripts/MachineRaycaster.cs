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

            MachineInteractable hitMachine = null;
            if (!overUI)
            {
                Ray ray = cursorLocked
                    ? targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                    : targetCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, ~0, QueryTriggerInteraction.Collide))
                {
                    hitMachine = hit.collider.GetComponentInParent<MachineInteractable>();
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
