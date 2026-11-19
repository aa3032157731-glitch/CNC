using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Style A (Apple VisionOS Spatial Frosted Glass) Unified Digital Twin Dashboard & Visual Teaching Studio HUD.
    /// - Dashboard Mode: Crisp high-contrast floating KPI island, telemetry inspector, and process dock.
    /// - Teaching Mode: Dedicated classroom-grade 9-step visual spinning flowchart ribbon + large-typography
    ///   material transformation diagram (Input -> Action -> Output) + 3 clean study tabs (Principle / Equipment / Quiz).
    /// </summary>
    public class DigitalTwinHUD : MonoBehaviour
    {
        public static DigitalTwinHUD Instance { get; private set; }

        [Header("Style A Pill & Teaching Card Sprites")]
        public Sprite spPillActiveWhite;
        public Sprite spPillActiveSky;
        public Sprite spPillIdle;
        public Sprite spPillGhost;
        public Sprite spPillGreen;
        public Sprite spPillDanger;
        public Sprite spTeachStepIdle;
        public Sprite spTeachStepActive;
        public Sprite spTeachStepDone;

        [Header("Root Mode Containers")]
        public GameObject hudMainContainer;
        public RectTransform leftPanelRect;
        public GameObject leftPanelRoot;
        public GameObject rightPanelRoot;
        public GameObject bottomDockRoot;
        public GameObject dashboardLeftContent;
        public GameObject teachingFlowRibbonRoot;
        public GameObject teachingStudioCardRoot;
        public GameObject teachingHintPill;

        [Header("Top Floating Pills")]
        public TextMeshProUGUI clockText;
        public TextMeshProUGUI statusSummaryText;
        public Button btnModeDashboard;
        public Button btnModeTeaching;
        public Button btnCamFps;
        public Button btnCamOrbit;
        public Button btnCamTour;
        public Button btnHall1;
        public Button btnHall2;
        public Button btnRoofCutaway;
        public Button btnToggleMarkers;
        public Button btnToggleHud;
        public Button btnOpenHelp;
        public Button btnOpenSettings;
        public Button btnReturnHome;
        public Button btnRequestQuit;

        [Header("Left Floating Island - Dashboard")]
        public TextMeshProUGUI kpiOutputText;
        public TextMeshProUGUI kpiPassRateText;
        public TextMeshProUGUI kpiOeeText;
        public TextMeshProUGUI kpiWarehouseShortText;
        public TextMeshProUGUI warehouseOccupancyText;
        public UIImage[] warehouseSlotCells = new UIImage[16];
        public TextMeshProUGUI eventLogText;

        [Header("Teaching Mode - Top 9-Step Illustrated Flowchart Ribbon")]
        public Button[] teachingStepButtons = new Button[9];
        public UIImage[] teachingStepIcons = new UIImage[9];
        public TextMeshProUGUI[] teachingStepCheckTexts = new TextMeshProUGUI[9];

        [Header("Teaching Mode - Large Visual Studio Card")]
        public TextMeshProUGUI teachStageBadgeText;
        public TextMeshProUGUI teachingProgressText;
        public UIImage teachingProgressBar;
        public TextMeshProUGUI teachStationTitleText;

        [Header("Teaching Mode - Input -> Process -> Output Diagram")]
        public UIImage teachInputIcon;
        public TextMeshProUGUI teachInputNameText;
        public TextMeshProUGUI teachActionLabelText;
        public UIImage teachOutputIcon;
        public TextMeshProUGUI teachOutputNameText;

        [Header("Teaching Mode - 3 Clean Study Tabs")]
        public Button btnTeachTabPrinciple;
        public Button btnTeachTabEquipment;
        public Button btnTeachTabQuiz;
        public GameObject teachTabPrincipleRoot;
        public GameObject teachTabEquipmentRoot;
        public GameObject teachTabQuizRoot;

        [Header("Teaching Tab 1 - Principle & Key Points")]
        public TextMeshProUGUI teachingGoalText;
        public TextMeshProUGUI teachPrincipleBodyText;
        public TextMeshProUGUI[] teachKeyPointTexts = new TextMeshProUGUI[3];

        [Header("Teaching Tab 2 - Equipment Photo & Specs")]
        public UIImage teachEquipmentPhoto;
        public TextMeshProUGUI teachEquipmentSpecsText;
        public Button btnTeachToggleRun;
        public TextMeshProUGUI btnTeachToggleRunLabel;

        [Header("Teaching Tab 3 - Interactive Quiz")]
        public TextMeshProUGUI quizQuestionText;
        public Button[] quizOptionButtons = new Button[3];
        public TextMeshProUGUI[] quizOptionTexts = new TextMeshProUGUI[3];
        public TextMeshProUGUI quizFeedbackText;

        [Header("Teaching Mode - Bottom Navigation")]
        public Button btnPrevStation;
        public Button btnTeachAutoTour;
        public TextMeshProUGUI btnTeachAutoTourLabel;
        public Button btnNextStation;

        [Header("Right Floating Drawer - Dashboard Device Inspector")]
        public TextMeshProUGUI inspectorCodeText;
        public TextMeshProUGUI inspectorNameText;
        public TextMeshProUGUI inspectorEngNameText;
        public Button btnToggleStationRun;
        public TextMeshProUGUI btnToggleStationRunLabel;
        public UIImage inspectorPreviewImage;
        public TextMeshProUGUI telemetryRpmText;
        public UIImage telemetryRpmBar;
        public TextMeshProUGUI telemetryTempText;
        public UIImage telemetryTempBar;
        public TextMeshProUGUI telemetryLoadText;
        public UIImage telemetryLoadBar;
        public TextMeshProUGUI specsTableText;
        public TextMeshProUGUI processSummaryText;

        [Header("Bottom Floating Dock (Dashboard)")]
        public Button[] flowNodeButtons = new Button[9];
        public Button btnSimPlayPause;
        public TextMeshProUGUI btnSimPlayPauseLabel;
        public Button btnSpeedCycle;
        public TextMeshProUGUI btnSpeedCycleLabel;
        public Button btnCollapseSidePanels;
        public TextMeshProUGUI btnCollapseLabel;

        [Header("Overlays & Modals")]
        public GameObject crosshairObject;
        public GameObject fpsHintBanner;
        public GameObject hoverTooltipRoot;
        public TextMeshProUGUI hoverTooltipText;
        public GameObject settingsModal;
        public Button btnQualMed;
        public Button btnQualHigh;
        public Button btnQualUltra;
        public Slider sensSlider;
        public TextMeshProUGUI sensValueText;
        public Button btnCloseSettings;
        public GameObject helpModal;
        public Button btnCloseHelp;
        public GameObject quitConfirmModal;
        public Button btnConfirmQuit;
        public Button btnCancelQuit;

        [Header("Style A Palette")]
        public Color colorDarkOnWhite = new Color(0.05f, 0.08f, 0.14f, 1f);
        public Color colorSoftWhite = new Color(0.97f, 0.98f, 1.0f, 0.96f);
        public Color colorMutedText = new Color(0.84f, 0.89f, 0.96f, 0.86f);
        public Color colorSky = new Color(0.22f, 0.74f, 0.97f, 0.95f);
        public Color colorMint = new Color(0.20f, 0.85f, 0.62f, 1f);
        public Color colorAmber = new Color(0.98f, 0.76f, 0.18f, 1f);
        public Color colorSlotOn = new Color(0.22f, 0.74f, 0.97f, 0.90f);
        public Color colorSlotOff = new Color(1.0f, 1.0f, 1.0f, 0.14f);

        private bool _hudHidden;
        private bool _sidePanelsCollapsed;
        private int _displayedStationIndex = 0;
        private int _activeTeachTab = 0;
        private readonly float[] _speeds = { 0.5f, 1.0f, 2.0f, 4.0f };
        private int _speedIdx = 1;
        private readonly StringBuilder _sb = new StringBuilder(512);

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BindButtons();

            var sim = SimulationState.Instance;
            if (sim != null)
            {
                sim.OnModeChanged += HandleModeChanged;
                sim.OnStationSelected += HandleStationSelected;
                sim.OnSimulationChanged += RefreshAllStates;
                sim.OnTelemetryTick += RefreshLiveTelemetry;
                sim.OnLogsChanged += RefreshLogs;
            }

            var cam = CameraRigManager.Instance;
            if (cam != null)
            {
                cam.OnViewModeChanged += HandleViewModeChanged;
                cam.OnHallChanged += HandleHallChanged;
                cam.OnRoofCutawayChanged += _ => RefreshHeaderButtons();
            }

            if (MachineRaycaster.Instance != null)
                MachineRaycaster.Instance.OnHoverChanged += HandleHoverChanged;

            if (settingsModal != null) settingsModal.SetActive(false);
            if (helpModal != null) helpModal.SetActive(false);
            if (quitConfirmModal != null) quitConfirmModal.SetActive(false);
            if (hoverTooltipRoot != null) hoverTooltipRoot.SetActive(false);

            _displayedStationIndex = (sim != null && sim.selectedStationIndex >= 0) ? sim.selectedStationIndex : 0;
            SelectTeachingTab(0);
            HandleModeChanged(sim != null ? sim.currentMode : AppMode.Dashboard);
            HandleViewModeChanged(cam != null ? cam.CurrentViewMode : CameraViewMode.FirstPersonWalk);
            RefreshInspector(_displayedStationIndex);
            RefreshTeachingPanel(_displayedStationIndex);
            RefreshAllStates();
            RefreshLiveTelemetry();
            RefreshLogs();
        }

        private void OnDestroy()
        {
            var sim = SimulationState.Instance;
            if (sim != null)
            {
                sim.OnModeChanged -= HandleModeChanged;
                sim.OnStationSelected -= HandleStationSelected;
                sim.OnSimulationChanged -= RefreshAllStates;
                sim.OnTelemetryTick -= RefreshLiveTelemetry;
                sim.OnLogsChanged -= RefreshLogs;
            }
            var cam = CameraRigManager.Instance;
            if (cam != null)
            {
                cam.OnViewModeChanged -= HandleViewModeChanged;
                cam.OnHallChanged -= HandleHallChanged;
            }
            if (MachineRaycaster.Instance != null)
                MachineRaycaster.Instance.OnHoverChanged -= HandleHoverChanged;
        }

        private void Update()
        {
            if (clockText != null)
                clockText.text = DateTime.Now.ToString("HH:mm:ss");

            if (Input.GetKeyDown(KeyCode.H))
                ToggleHudVisibility();

            if (hoverTooltipRoot != null && hoverTooltipRoot.activeSelf && Cursor.lockState != CursorLockMode.Locked)
                hoverTooltipRoot.transform.position = Input.mousePosition + new Vector3(18f, -24f, 0f);

            if (crosshairObject != null)
            {
                bool showCross = CameraRigManager.Instance != null
                    && CameraRigManager.Instance.CurrentViewMode == CameraViewMode.FirstPersonWalk
                    && Cursor.lockState == CursorLockMode.Locked;
                if (crosshairObject.activeSelf != showCross)
                    crosshairObject.SetActive(showCross);
            }
        }

        private void BindButtons()
        {
            if (btnModeDashboard != null) btnModeDashboard.onClick.AddListener(() => SimulationState.Instance?.SetMode(AppMode.Dashboard));
            if (btnModeTeaching != null) btnModeTeaching.onClick.AddListener(() =>
            {
                SimulationState.Instance?.SetMode(AppMode.TeachingTour);
                int idx = (SimulationState.Instance != null && SimulationState.Instance.selectedStationIndex >= 0)
                    ? SimulationState.Instance.selectedStationIndex : 0;
                CameraRigManager.Instance?.FocusStation(idx, false);
            });

            if (btnCamFps != null) btnCamFps.onClick.AddListener(() => CameraRigManager.Instance?.SetViewMode(CameraViewMode.FirstPersonWalk));
            if (btnCamOrbit != null) btnCamOrbit.onClick.AddListener(() => CameraRigManager.Instance?.SetViewMode(CameraViewMode.BirdEyeOrbit));
            if (btnCamTour != null) btnCamTour.onClick.AddListener(() => CameraRigManager.Instance?.SetViewMode(CameraViewMode.AutoTour));

            if (btnHall1 != null) btnHall1.onClick.AddListener(() => CameraRigManager.Instance?.FocusHall(WorkshopHall.SpinningHall, false));
            if (btnHall2 != null) btnHall2.onClick.AddListener(() => CameraRigManager.Instance?.FocusHall(WorkshopHall.InspectionWarehouse, false));
            if (btnRoofCutaway != null) btnRoofCutaway.onClick.AddListener(() => CameraRigManager.Instance?.ToggleRoofCutaway());

            if (btnToggleMarkers != null) btnToggleMarkers.onClick.AddListener(() =>
            {
                if (SimulationState.Instance == null) return;
                SimulationState.Instance.showWorldMarkers = !SimulationState.Instance.showWorldMarkers;
                RefreshHeaderButtons();
            });

            if (btnToggleHud != null) btnToggleHud.onClick.AddListener(ToggleHudVisibility);
            if (btnOpenHelp != null) btnOpenHelp.onClick.AddListener(() => { if (helpModal != null) helpModal.SetActive(!helpModal.activeSelf); });
            if (btnCloseHelp != null) btnCloseHelp.onClick.AddListener(() => { if (helpModal != null) helpModal.SetActive(false); });

            if (btnOpenSettings != null) btnOpenSettings.onClick.AddListener(() => { if (settingsModal != null) settingsModal.SetActive(!settingsModal.activeSelf); });
            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(() => { if (settingsModal != null) settingsModal.SetActive(false); });

            if (btnReturnHome != null) btnReturnHome.onClick.AddListener(() =>
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(0);
            });

            if (btnRequestQuit != null) btnRequestQuit.onClick.AddListener(ShowQuitModal);
            if (btnCancelQuit != null) btnCancelQuit.onClick.AddListener(() => { if (quitConfirmModal != null) quitConfirmModal.SetActive(false); });
            if (btnConfirmQuit != null) btnConfirmQuit.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            if (btnToggleStationRun != null) btnToggleStationRun.onClick.AddListener(OnToggleDisplayedStationRun);
            if (btnTeachToggleRun != null) btnTeachToggleRun.onClick.AddListener(OnToggleDisplayedStationRun);

            if (btnSimPlayPause != null) btnSimPlayPause.onClick.AddListener(() => SimulationState.Instance?.ToggleGlobalRun());
            if (btnSpeedCycle != null) btnSpeedCycle.onClick.AddListener(() =>
            {
                _speedIdx = (_speedIdx + 1) % _speeds.Length;
                SimulationState.Instance?.SetSimSpeed(_speeds[_speedIdx]);
            });

            if (btnCollapseSidePanels != null) btnCollapseSidePanels.onClick.AddListener(() =>
            {
                _sidePanelsCollapsed = !_sidePanelsCollapsed;
                ApplyModePanelVisibility();
                if (btnCollapseLabel != null) btnCollapseLabel.text = _sidePanelsCollapsed ? "展开" : "极简";
            });

            // Teaching Tabs
            if (btnTeachTabPrinciple != null) btnTeachTabPrinciple.onClick.AddListener(() => SelectTeachingTab(0));
            if (btnTeachTabEquipment != null) btnTeachTabEquipment.onClick.AddListener(() => SelectTeachingTab(1));
            if (btnTeachTabQuiz != null) btnTeachTabQuiz.onClick.AddListener(() => SelectTeachingTab(2));

            // Teaching Prev / AutoTour / Next
            if (btnPrevStation != null) btnPrevStation.onClick.AddListener(() =>
            {
                int prev = (_displayedStationIndex - 1 + 9) % 9;
                CameraRigManager.Instance?.FocusStation(prev, false);
            });
            if (btnNextStation != null) btnNextStation.onClick.AddListener(() =>
            {
                int next = (_displayedStationIndex + 1) % 9;
                CameraRigManager.Instance?.FocusStation(next, false);
            });
            if (btnTeachAutoTour != null) btnTeachAutoTour.onClick.AddListener(() =>
            {
                var cam = CameraRigManager.Instance;
                if (cam == null) return;
                if (cam.CurrentViewMode == CameraViewMode.AutoTour)
                    cam.FocusStation(_displayedStationIndex, true);
                else
                    cam.SetViewMode(CameraViewMode.AutoTour);
                RefreshTeachingAutoTourButton();
            });

            for (int i = 0; i < 9; i++)
            {
                int idx = i;
                if (i < flowNodeButtons.Length && flowNodeButtons[i] != null)
                    flowNodeButtons[i].onClick.AddListener(() => CameraRigManager.Instance?.FocusStation(idx, false));
                if (i < teachingStepButtons.Length && teachingStepButtons[i] != null)
                    teachingStepButtons[i].onClick.AddListener(() => CameraRigManager.Instance?.FocusStation(idx, false));
            }

            for (int q = 0; q < 3; q++)
            {
                int optIdx = q;
                if (q < quizOptionButtons.Length && quizOptionButtons[q] != null)
                    quizOptionButtons[q].onClick.AddListener(() => OnQuizOptionClicked(optIdx));
            }

            if (btnQualMed != null) btnQualMed.onClick.AddListener(() => SetQuality(2));
            if (btnQualHigh != null) btnQualHigh.onClick.AddListener(() => SetQuality(4));
            if (btnQualUltra != null) btnQualUltra.onClick.AddListener(() => SetQuality(5));
            if (sensSlider != null)
            {
                sensSlider.value = AppLaunchContext.MouseSensitivity;
                sensSlider.onValueChanged.AddListener(v =>
                {
                    CameraRigManager.Instance?.ApplySensitivity(v);
                    if (sensValueText != null) sensValueText.text = $"{v:F1}";
                });
                if (sensValueText != null) sensValueText.text = $"{AppLaunchContext.MouseSensitivity:F1}";
            }
        }

        private void OnToggleDisplayedStationRun()
        {
            SimulationState.Instance?.ToggleStationRun(_displayedStationIndex);
            RefreshInspector(_displayedStationIndex);
            RefreshTeachingPanel(_displayedStationIndex);
        }

        public void SelectTeachingTab(int tabIdx)
        {
            _activeTeachTab = Mathf.Clamp(tabIdx, 0, 2);
            if (teachTabPrincipleRoot != null) teachTabPrincipleRoot.SetActive(_activeTeachTab == 0);
            if (teachTabEquipmentRoot != null) teachTabEquipmentRoot.SetActive(_activeTeachTab == 1);
            if (teachTabQuizRoot != null) teachTabQuizRoot.SetActive(_activeTeachTab == 2);

            ApplyPillStyle(btnTeachTabPrinciple, _activeTeachTab == 0, true);
            ApplyPillStyle(btnTeachTabEquipment, _activeTeachTab == 1, true);
            ApplyPillStyle(btnTeachTabQuiz, _activeTeachTab == 2, true);
        }

        public void ShowQuitModal()
        {
            if (quitConfirmModal != null) quitConfirmModal.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ToggleHudVisibility()
        {
            _hudHidden = !_hudHidden;
            if (hudMainContainer != null) hudMainContainer.SetActive(!_hudHidden);
        }

        private void ApplyModePanelVisibility()
        {
            var sim = SimulationState.Instance;
            bool isDash = sim == null || sim.currentMode == AppMode.Dashboard;

            if (leftPanelRoot != null) leftPanelRoot.SetActive(isDash && !_sidePanelsCollapsed);
            if (rightPanelRoot != null) rightPanelRoot.SetActive(isDash && !_sidePanelsCollapsed);
            if (bottomDockRoot != null) bottomDockRoot.SetActive(isDash);
            if (dashboardLeftContent != null) dashboardLeftContent.SetActive(isDash);

            if (teachingFlowRibbonRoot != null) teachingFlowRibbonRoot.SetActive(!isDash);
            if (teachingStudioCardRoot != null) teachingStudioCardRoot.SetActive(!isDash);
            if (teachingHintPill != null) teachingHintPill.SetActive(!isDash);

            if (fpsHintBanner != null)
            {
                bool walk = CameraRigManager.Instance != null &&
                            CameraRigManager.Instance.CurrentViewMode == CameraViewMode.FirstPersonWalk;
                fpsHintBanner.SetActive(isDash && walk);
            }
        }

        private void HandleModeChanged(AppMode mode)
        {
            ApplyModePanelVisibility();
            RefreshHeaderButtons();
            RefreshTeachingPanel(_displayedStationIndex);
            RefreshTeachingAutoTourButton();
        }

        private void HandleStationSelected(int idx)
        {
            if (idx >= 0) _displayedStationIndex = idx;
            RefreshInspector(_displayedStationIndex);
            RefreshTeachingPanel(_displayedStationIndex);
            RefreshAllStates();
        }

        private void HandleViewModeChanged(CameraViewMode mode)
        {
            ApplyModePanelVisibility();
            RefreshHeaderButtons();
            RefreshTeachingAutoTourButton();
        }

        private void HandleHallChanged(WorkshopHall hall) => RefreshHeaderButtons();

        private void HandleHoverChanged(MachineInteractable hovered)
        {
            if (hoverTooltipRoot == null) return;
            if (hovered == null)
            {
                hoverTooltipRoot.SetActive(false);
                return;
            }
            var sim = SimulationState.Instance;
            if (sim != null && hovered.stationIndex >= 0 && hovered.stationIndex < sim.Stations.Count)
            {
                var st = sim.Stations[hovered.stationIndex];
                if (hoverTooltipText != null)
                    hoverTooltipText.text = $"{st.code} · {st.name}\n<color=#7DD3FC>左键点击聚焦查看详情</color>";
                hoverTooltipRoot.SetActive(true);
            }
        }

        private void RefreshAllStates()
        {
            RefreshHeaderButtons();
            RefreshTeachingAutoTourButton();
            var sim = SimulationState.Instance;
            if (sim == null) return;

            if (btnSimPlayPauseLabel != null)
                btnSimPlayPauseLabel.text = sim.isRunning ? "暂停" : "运行";
            if (btnSpeedCycleLabel != null)
                btnSpeedCycleLabel.text = $"{sim.simSpeed:0.#}x";

            int runningCount = 0;
            for (int i = 0; i < 9; i++)
            {
                if (sim.isRunning && sim.stationRunning[i]) runningCount++;
                bool selected = (i == _displayedStationIndex && sim.selectedStationIndex >= 0);
                if (i < flowNodeButtons.Length && flowNodeButtons[i] != null)
                    ApplyPillStyle(flowNodeButtons[i], selected, false);
            }

            if (statusSummaryText != null)
                statusSummaryText.text = sim.isRunning
                    ? $"{runningCount}/9 工段在线"
                    : "产线已暂停";

            RefreshTeachingFlowStepStyles();
        }

        private void RefreshTeachingAutoTourButton()
        {
            if (btnTeachAutoTour == null) return;
            bool touring = CameraRigManager.Instance != null &&
                           CameraRigManager.Instance.CurrentViewMode == CameraViewMode.AutoTour;
            ApplyPillStyle(btnTeachAutoTour, touring, false);
            if (btnTeachAutoTourLabel != null)
                btnTeachAutoTourLabel.text = touring ? "暂停导览" : "自动导览";
        }

        private void RefreshHeaderButtons()
        {
            var sim = SimulationState.Instance;
            var cam = CameraRigManager.Instance;
            if (sim != null)
            {
                ApplyPillStyle(btnModeDashboard, sim.currentMode == AppMode.Dashboard, true);
                ApplyPillStyle(btnModeTeaching, sim.currentMode == AppMode.TeachingTour, true);
                ApplyPillStyle(btnToggleMarkers, sim.showWorldMarkers, false);
            }
            if (cam != null)
            {
                ApplyPillStyle(btnCamFps, cam.CurrentViewMode == CameraViewMode.FirstPersonWalk, false);
                ApplyPillStyle(btnCamOrbit, cam.CurrentViewMode == CameraViewMode.BirdEyeOrbit, false);
                ApplyPillStyle(btnCamTour, cam.CurrentViewMode == CameraViewMode.AutoTour, false);
                ApplyPillStyle(btnHall1, cam.CurrentHall == WorkshopHall.SpinningHall, false);
                ApplyPillStyle(btnHall2, cam.CurrentHall == WorkshopHall.InspectionWarehouse, false);
                ApplyPillStyle(btnRoofCutaway, cam.IsRoofCutaway, false);
            }

            int q = QualitySettings.GetQualityLevel();
            ApplyPillStyle(btnQualMed, q <= 2, true);
            ApplyPillStyle(btnQualHigh, q == 3 || q == 4, true);
            ApplyPillStyle(btnQualUltra, q >= 5, true);
        }

        private void RefreshLiveTelemetry()
        {
            var sim = SimulationState.Instance;
            if (sim == null) return;

            if (kpiOutputText != null) kpiOutputText.text = $"{sim.totalOutputKg:N0}<size=14><color=#CBD5E1> kg</color></size>";
            if (kpiPassRateText != null) kpiPassRateText.text = $"{sim.passRatePct:F1}<size=14>%</size>";
            if (kpiOeeText != null) kpiOeeText.text = $"{sim.oeePct:F1}<size=14>%</size>";
            if (kpiWarehouseShortText != null) kpiWarehouseShortText.text = $"{sim.occupiedWarehouseSlots}<size=14><color=#94A3B8> / 16</color></size>";

            if (warehouseOccupancyText != null)
                warehouseOccupancyText.text = $"{(sim.occupiedWarehouseSlots * 100f / 16f):F1}%";

            for (int i = 0; i < warehouseSlotCells.Length && i < sim.warehouseSlotStates.Length; i++)
                if (warehouseSlotCells[i] != null)
                    warehouseSlotCells[i].color = sim.warehouseSlotStates[i] ? colorSlotOn : colorSlotOff;

            sim.GetLiveTelemetry(_displayedStationIndex, out float rpm, out float tempC, out float powerKw, out float loadPct);
            var st = sim.Stations[_displayedStationIndex];

            if (telemetryRpmText != null) telemetryRpmText.text = $"{rpm:N0} r/min";
            if (telemetryRpmBar != null) telemetryRpmBar.fillAmount = Mathf.Clamp01(rpm / Mathf.Max(1f, st.nominalRpm * 1.35f));

            if (telemetryTempText != null) telemetryTempText.text = $"{tempC:F1} °C";
            if (telemetryTempBar != null) telemetryTempBar.fillAmount = Mathf.Clamp01(tempC / 75f);

            if (telemetryLoadText != null) telemetryLoadText.text = $"{loadPct:F0}% · {powerKw:F1} kW";
            if (telemetryLoadBar != null) telemetryLoadBar.fillAmount = Mathf.Clamp01(loadPct / 100f);

            if (teachingProgressText != null)
                teachingProgressText.text = $"已学 {sim.GetLearnedCount()}/9  ·  通过 {sim.GetQuizPassedCount()}/9";
            if (teachingProgressBar != null)
                teachingProgressBar.fillAmount = sim.GetLearnedCount() / 9f;

            RefreshTeachingFlowStepStyles();
        }

        private void RefreshTeachingFlowStepStyles()
        {
            var sim = SimulationState.Instance;
            if (sim == null) return;

            for (int i = 0; i < 9; i++)
            {
                bool isCurrent = i == _displayedStationIndex;
                bool passed = sim.stationQuizPassed[i];
                bool learned = sim.stationLearned[i];

                if (i < teachingStepButtons.Length && teachingStepButtons[i] != null)
                {
                    var img = teachingStepButtons[i].GetComponent<UIImage>();
                    if (img != null)
                    {
                        if (isCurrent && spTeachStepActive != null) img.sprite = spTeachStepActive;
                        else if (passed && spTeachStepDone != null) img.sprite = spTeachStepDone;
                        else if (spTeachStepIdle != null) img.sprite = spTeachStepIdle;
                        img.color = Color.white;
                    }
                }

                if (i < teachingStepCheckTexts.Length && teachingStepCheckTexts[i] != null)
                {
                    if (isCurrent) teachingStepCheckTexts[i].text = $"<color=#BAE6FD>{i + 1:00} 当前讲解</color>";
                    else if (passed) teachingStepCheckTexts[i].text = $"<color=#34D399>{i + 1:00} ★ 已通过</color>";
                    else if (learned) teachingStepCheckTexts[i].text = $"<color=#7DD3FC>{i + 1:00} ✓ 已学习</color>";
                    else teachingStepCheckTexts[i].text = $"<color=#94A3B8>{i + 1:00} 待学习</color>";
                }
            }
        }

        private void RefreshInspector(int idx)
        {
            var sim = SimulationState.Instance;
            if (sim == null || idx < 0 || idx >= sim.Stations.Count) return;
            var st = sim.Stations[idx];

            if (inspectorCodeText != null) inspectorCodeText.text = $"{st.code} · {st.shortName}";
            if (inspectorNameText != null) inspectorNameText.text = st.name;
            if (inspectorEngNameText != null) inspectorEngNameText.text = st.englishName;

            bool active = sim.isRunning && sim.stationRunning[idx];
            if (btnToggleStationRun != null)
            {
                var bimg = btnToggleStationRun.GetComponent<UIImage>();
                if (bimg != null) bimg.sprite = active ? spPillGreen : spPillDanger;
            }
            if (btnToggleStationRunLabel != null)
            {
                btnToggleStationRunLabel.text = active ? "● 运行中" : "● 已停机";
                btnToggleStationRunLabel.color = active ? new Color(0.43f, 0.93f, 0.74f) : new Color(0.99f, 0.65f, 0.65f);
            }

            if (inspectorPreviewImage != null && st.previewSprite != null)
            {
                inspectorPreviewImage.sprite = st.previewSprite;
                inspectorPreviewImage.preserveAspect = true;
            }

            string formattedSpecs = BuildSpecsText(st);
            if (specsTableText != null) specsTableText.text = formattedSpecs;
            if (processSummaryText != null) processSummaryText.text = st.processSummary;

            RefreshLiveTelemetry();
        }

        private string BuildSpecsText(StationInfo st)
        {
            if (st == null || st.specs == null) return "";
            _sb.Clear();
            int lim = Mathf.Min(st.specs.Length, 5);
            for (int i = 0; i < lim; i++)
            {
                var sp = st.specs[i];
                _sb.Append("<color=#94A3B8>").Append(sp.label).Append("：</color>");
                _sb.Append("<color=#FFFFFF><b>").Append(sp.value).Append("</b></color>");
                if (!string.IsNullOrEmpty(sp.unit))
                    _sb.Append(" <color=#7DD3FC>").Append(sp.unit).Append("</color>");
                if (i < lim - 1) _sb.Append('\n');
            }
            return _sb.ToString();
        }

        private void RefreshTeachingPanel(int idx)
        {
            var sim = SimulationState.Instance;
            if (sim == null || idx < 0 || idx >= sim.Stations.Count) return;
            var st = sim.Stations[idx];

            // Header
            if (teachStageBadgeText != null)
                teachStageBadgeText.text = $"第 {idx + 1}/9 步  ·  {(string.IsNullOrEmpty(st.stageGroup) ? st.shortName : st.stageGroup)}";
            if (teachStationTitleText != null)
                teachStationTitleText.text = st.name;

            // Input -> Process -> Output Visual Cotton Transformation Diagram
            if (teachInputIcon != null && st.inputMaterialSprite != null) teachInputIcon.sprite = st.inputMaterialSprite;
            if (teachInputNameText != null) teachInputNameText.text = string.IsNullOrEmpty(st.inputMaterialName) ? "上道物料" : st.inputMaterialName;
            if (teachActionLabelText != null) teachActionLabelText.text = string.IsNullOrEmpty(st.processActionLabel) ? st.shortName : st.processActionLabel;
            if (teachOutputIcon != null && st.outputMaterialSprite != null) teachOutputIcon.sprite = st.outputMaterialSprite;
            if (teachOutputNameText != null) teachOutputNameText.text = string.IsNullOrEmpty(st.outputMaterialName) ? "本道成品" : st.outputMaterialName;

            // Tab 1: Goal + Plain Process Principle + 3 Numbered Key-Point Cards
            if (teachingGoalText != null)
                teachingGoalText.text = $"<color=#7DD3FC><b>学习目标：</b></color>{st.teachingGoal}";
            if (teachPrincipleBodyText != null)
                teachPrincipleBodyText.text = st.processSummary;

            if (teachKeyPointTexts != null && st.teachingKeyPoints != null)
            {
                for (int i = 0; i < teachKeyPointTexts.Length; i++)
                {
                    if (teachKeyPointTexts[i] == null) continue;
                    string raw = i < st.teachingKeyPoints.Length ? st.teachingKeyPoints[i] : "";
                    // Strip leading "1. " because the visual numbered badge displays 1/2/3 cleanly
                    if (raw.Length > 3 && char.IsDigit(raw[0]) && raw[1] == '.')
                        raw = raw.Substring(2).Trim();
                    int colon = raw.IndexOf('：');
                    if (colon > 0)
                        raw = $"<color=#FFFFFF><b>{raw.Substring(0, colon + 1)}</b></color>{raw.Substring(colon + 1)}";
                    teachKeyPointTexts[i].text = raw;
                }
            }

            // Tab 2: Large Equipment Photo + Specs + Interactive Demo Toggle
            if (teachEquipmentPhoto != null && st.previewSprite != null)
            {
                teachEquipmentPhoto.sprite = st.previewSprite;
                teachEquipmentPhoto.preserveAspect = true;
            }
            if (teachEquipmentSpecsText != null)
                teachEquipmentSpecsText.text = BuildSpecsText(st);

            bool active = sim.isRunning && sim.stationRunning[idx];
            if (btnTeachToggleRun != null)
            {
                var img = btnTeachToggleRun.GetComponent<UIImage>();
                if (img != null) img.sprite = active ? spPillGreen : spPillDanger;
            }
            if (btnTeachToggleRunLabel != null)
            {
                btnTeachToggleRunLabel.text = active ? "● 设备运行中（点击演示远程停机）" : "● 设备已停机（点击演示远程启动）";
                btnTeachToggleRunLabel.color = active ? new Color(0.46f, 0.96f, 0.76f) : new Color(1.0f, 0.70f, 0.70f);
            }

            // Tab 3: Large Clear Quiz
            if (st.quiz != null)
            {
                if (quizQuestionText != null) quizQuestionText.text = $"考核题目：{st.quiz.question}";
                for (int i = 0; i < 3; i++)
                {
                    if (i < quizOptionTexts.Length && quizOptionTexts[i] != null && st.quiz.options != null && i < st.quiz.options.Length)
                        quizOptionTexts[i].text = st.quiz.options[i];
                    if (i < quizOptionButtons.Length && quizOptionButtons[i] != null)
                        ApplyPillStyle(quizOptionButtons[i], false, false);
                }
                if (quizFeedbackText != null)
                    quizFeedbackText.text = sim.stationQuizPassed[idx]
                        ? $"<color=#34D399><b>✓ 本工段考核已通过！</b></color>\n{st.quiz.explanation}"
                        : "<color=#CBD5E1>请根据“工艺原理”选择正确答案，通过后自动点亮本工段绿色徽章。</color>";
            }

            RefreshTeachingFlowStepStyles();
        }

        private void OnQuizOptionClicked(int optionIdx)
        {
            var sim = SimulationState.Instance;
            if (sim == null) return;
            var st = sim.Stations[_displayedStationIndex];
            if (st.quiz == null) return;

            bool correct = optionIdx == st.quiz.correctIndex;
            if (correct)
            {
                sim.MarkQuizPassed(_displayedStationIndex);
                if (quizFeedbackText != null)
                    quizFeedbackText.text = $"<color=#34D399><b>✓ 回答正确！已掌握本工段核心原理。</b></color>\n{st.quiz.explanation}";
            }
            else
            {
                if (quizFeedbackText != null)
                    quizFeedbackText.text = "<color=#F87171><b>✕ 回答有误</b></color>\n可切换至左侧【工艺原理与要点】复习物料流向与关键指标后再试。";
            }

            for (int i = 0; i < quizOptionButtons.Length; i++)
                ApplyPillStyle(quizOptionButtons[i], i == optionIdx, false);
            RefreshTeachingFlowStepStyles();
        }

        private void RefreshLogs()
        {
            var sim = SimulationState.Instance;
            if (sim == null || eventLogText == null) return;
            _sb.Clear();
            int count = Mathf.Min(sim.eventLogs.Count, 2);
            for (int i = 0; i < count; i++)
            {
                var e = sim.eventLogs[i];
                string col = e.level == "OK" ? "#34D399" : (e.level == "WARN" ? "#FBBF24" : "#38BDF8");
                _sb.Append("<color=").Append(col).Append(">●</color> ").Append(e.message);
                if (i < count - 1) _sb.Append('\n');
            }
            eventLogText.text = _sb.ToString();
        }

        private void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(level, 0, QualitySettings.names.Length - 1), true);
            RefreshHeaderButtons();
        }

        private void ApplyPillStyle(Button btn, bool active, bool whitePrimary)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UIImage>();
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (img != null)
            {
                if (active)
                    img.sprite = whitePrimary ? spPillActiveWhite : spPillActiveSky;
                else
                    img.sprite = spPillGhost != null ? spPillGhost : spPillIdle;
                img.color = Color.white;
            }
            if (txt != null)
            {
                txt.color = (active && whitePrimary) ? colorDarkOnWhite : (active ? Color.white : colorMutedText);
            }
        }
    }
}
