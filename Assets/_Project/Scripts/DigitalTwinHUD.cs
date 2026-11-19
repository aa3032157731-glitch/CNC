using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Unified Industrial Deep-Blue Digital Twin Monitoring Dashboard & Teaching Tour HUD.
    /// All references are serialized & wired cleanly by Step3_BuildFrontendScenes.
    /// </summary>
    public class DigitalTwinHUD : MonoBehaviour
    {
        public static DigitalTwinHUD Instance { get; private set; }

        [Header("Root Containers")]
        public GameObject hudMainContainer;
        public GameObject leftPanelRoot;
        public GameObject rightPanelRoot;
        public GameObject dashboardLeftContent;
        public GameObject teachingLeftContent;

        [Header("Top Header Bar")]
        public TextMeshProUGUI clockText;
        public TextMeshProUGUI statusSummaryText;
        public Button btnModeDashboard;
        public Button btnModeTeaching;
        public Button btnCamOrbit;
        public Button btnCamFps;
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

        [Header("Left Panel - Dashboard KPIs")]
        public TextMeshProUGUI kpiOutputText;
        public TextMeshProUGUI kpiPassRateText;
        public UIImage kpiPassRateRing;
        public TextMeshProUGUI kpiOeeText;
        public UIImage kpiOeeBar;
        public TextMeshProUGUI warehouseOccupancyText;
        public UIImage[] warehouseSlotCells = new UIImage[16];
        public UIImage[] stationStatusDots = new UIImage[9];
        public TextMeshProUGUI[] stationStatusLabels = new TextMeshProUGUI[9];
        public Button[] stationQuickButtons = new Button[9];
        public TextMeshProUGUI eventLogText;

        [Header("Left Panel - Teaching Tour")]
        public TextMeshProUGUI teachingProgressText;
        public UIImage teachingProgressBar;
        public Button[] teachingStepButtons = new Button[9];
        public TextMeshProUGUI[] teachingStepCheckTexts = new TextMeshProUGUI[9];
        public TextMeshProUGUI teachingGoalText;
        public TextMeshProUGUI teachingKeyPointsText;
        public TextMeshProUGUI quizQuestionText;
        public Button[] quizOptionButtons = new Button[3];
        public TextMeshProUGUI[] quizOptionTexts = new TextMeshProUGUI[3];
        public TextMeshProUGUI quizFeedbackText;
        public Button btnPrevStation;
        public Button btnNextStation;

        [Header("Right Panel - Device Inspector")]
        public TextMeshProUGUI inspectorCodeText;
        public TextMeshProUGUI inspectorNameText;
        public TextMeshProUGUI inspectorEngNameText;
        public UIImage inspectorStatusDot;
        public TextMeshProUGUI inspectorStatusText;
        public Button btnToggleStationRun;
        public TextMeshProUGUI btnToggleStationRunLabel;
        public UIImage inspectorPreviewImage;
        public TextMeshProUGUI telemetryRpmText;
        public UIImage telemetryRpmBar;
        public TextMeshProUGUI telemetryTempText;
        public UIImage telemetryTempBar;
        public TextMeshProUGUI telemetryPowerText;
        public UIImage telemetryPowerBar;
        public TextMeshProUGUI telemetryLoadText;
        public UIImage telemetryLoadBar;
        public TextMeshProUGUI specsTableText;
        public TextMeshProUGUI processSummaryText;

        [Header("Bottom Process Flow & Transport Bar")]
        public Button[] flowNodeButtons = new Button[9];
        public UIImage[] flowNodeIndicators = new UIImage[9];
        public Button btnSimPlayPause;
        public TextMeshProUGUI btnSimPlayPauseLabel;
        public Button btnSpeed05;
        public Button btnSpeed10;
        public Button btnSpeed20;
        public Button btnSpeed40;
        public Button btnSimReset;
        public Button btnCollapseSidePanels;

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

        [Header("Theme Colors")]
        public Color colorAccentCyan = new Color(0.0f, 0.86f, 1.0f, 1f);
        public Color colorBtnIdle = new Color(0.75f, 0.88f, 1.0f, 0.88f);
        public Color colorOkGreen = new Color(0.16f, 0.92f, 0.48f, 1f);
        public Color colorWarnAmber = new Color(0.98f, 0.68f, 0.14f, 1f);
        public Color colorDangerRed = new Color(0.96f, 0.30f, 0.36f, 1f);
        public Color colorSlotOccupied = new Color(0.0f, 0.78f, 0.96f, 0.92f);
        public Color colorSlotEmpty = new Color(0.10f, 0.22f, 0.36f, 0.65f);

        private bool _hudHidden;
        private bool _sidePanelsCollapsed;
        private int _displayedStationIndex = 0;
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
            HandleModeChanged(sim != null ? sim.currentMode : AppMode.Dashboard);
            RefreshInspector(_displayedStationIndex);
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
                clockText.text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

            if (Input.GetKeyDown(KeyCode.H))
                ToggleHudVisibility();

            // Update hover tooltip position near pointer
            if (hoverTooltipRoot != null && hoverTooltipRoot.activeSelf && Cursor.lockState != CursorLockMode.Locked)
            {
                hoverTooltipRoot.transform.position = Input.mousePosition + new Vector3(18f, -24f, 0f);
            }

            // Update crosshair visibility in FPS locked mode
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
                if (SimulationState.Instance != null && SimulationState.Instance.selectedStationIndex < 0)
                    CameraRigManager.Instance?.FocusStation(0, false);
            });

            if (btnCamOrbit != null) btnCamOrbit.onClick.AddListener(() => CameraRigManager.Instance?.SetViewMode(CameraViewMode.BirdEyeOrbit));
            if (btnCamFps != null) btnCamFps.onClick.AddListener(() => CameraRigManager.Instance?.SetViewMode(CameraViewMode.FirstPersonWalk));
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

            if (btnToggleStationRun != null) btnToggleStationRun.onClick.AddListener(() =>
            {
                SimulationState.Instance?.ToggleStationRun(_displayedStationIndex);
                RefreshInspector(_displayedStationIndex);
            });

            if (btnSimPlayPause != null) btnSimPlayPause.onClick.AddListener(() => SimulationState.Instance?.ToggleGlobalRun());
            if (btnSpeed05 != null) btnSpeed05.onClick.AddListener(() => SimulationState.Instance?.SetSimSpeed(0.5f));
            if (btnSpeed10 != null) btnSpeed10.onClick.AddListener(() => SimulationState.Instance?.SetSimSpeed(1.0f));
            if (btnSpeed20 != null) btnSpeed20.onClick.AddListener(() => SimulationState.Instance?.SetSimSpeed(2.0f));
            if (btnSpeed40 != null) btnSpeed40.onClick.AddListener(() => SimulationState.Instance?.SetSimSpeed(4.0f));
            if (btnSimReset != null) btnSimReset.onClick.AddListener(() => SimulationState.Instance?.ResetSimulation());

            if (btnCollapseSidePanels != null) btnCollapseSidePanels.onClick.AddListener(() =>
            {
                _sidePanelsCollapsed = !_sidePanelsCollapsed;
                if (leftPanelRoot != null) leftPanelRoot.SetActive(!_sidePanelsCollapsed);
                if (rightPanelRoot != null) rightPanelRoot.SetActive(!_sidePanelsCollapsed);
            });

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

            for (int i = 0; i < 9; i++)
            {
                int idx = i;
                if (i < flowNodeButtons.Length && flowNodeButtons[i] != null)
                    flowNodeButtons[i].onClick.AddListener(() => CameraRigManager.Instance?.FocusStation(idx, false));
                if (i < stationQuickButtons.Length && stationQuickButtons[i] != null)
                    stationQuickButtons[i].onClick.AddListener(() => CameraRigManager.Instance?.FocusStation(idx, false));
                if (i < teachingStepButtons.Length && teachingStepButtons[i] != null)
                    teachingStepButtons[i].onClick.AddListener(() => CameraRigManager.Instance?.FocusStation(idx, false));
            }

            for (int q = 0; q < 3; q++)
            {
                int optIdx = q;
                if (q < quizOptionButtons.Length && quizOptionButtons[q] != null)
                    quizOptionButtons[q].onClick.AddListener(() => OnQuizOptionClicked(optIdx));
            }

            // Quality & Sensitivity Settings
            if (btnQualMed != null) btnQualMed.onClick.AddListener(() => SetQuality(2));
            if (btnQualHigh != null) btnQualHigh.onClick.AddListener(() => SetQuality(4));
            if (btnQualUltra != null) btnQualUltra.onClick.AddListener(() => SetQuality(5));
            if (sensSlider != null)
            {
                sensSlider.value = AppLaunchContext.MouseSensitivity;
                sensSlider.onValueChanged.AddListener(v =>
                {
                    CameraRigManager.Instance?.ApplySensitivity(v);
                    if (sensValueText != null) sensValueText.text = $"{v:0.0}";
                });
                if (sensValueText != null) sensValueText.text = $"{AppLaunchContext.MouseSensitivity:0.0}";
            }
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

        private void HandleModeChanged(AppMode mode)
        {
            bool isDash = mode == AppMode.Dashboard;
            if (dashboardLeftContent != null) dashboardLeftContent.SetActive(isDash);
            if (teachingLeftContent != null) teachingLeftContent.SetActive(!isDash);
            RefreshHeaderButtons();
            RefreshTeachingPanel(_displayedStationIndex);
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
            if (fpsHintBanner != null)
                fpsHintBanner.SetActive(mode == CameraViewMode.FirstPersonWalk);
            RefreshHeaderButtons();
        }

        private void HandleHallChanged(WorkshopHall hall)
        {
            RefreshHeaderButtons();
        }

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
                    hoverTooltipText.text = $"[{st.code}] {st.name}\n<color=#5CE6FF>左键点击聚焦查看参数与控制</color>";
                hoverTooltipRoot.SetActive(true);
            }
        }

        private void RefreshAllStates()
        {
            RefreshHeaderButtons();

            var sim = SimulationState.Instance;
            if (sim == null) return;

            if (btnSimPlayPauseLabel != null)
                btnSimPlayPauseLabel.text = sim.isRunning ? "暂停" : "启动";

            SetBtnHighlight(btnSpeed05, Mathf.Approximately(sim.simSpeed, 0.5f));
            SetBtnHighlight(btnSpeed10, Mathf.Approximately(sim.simSpeed, 1.0f));
            SetBtnHighlight(btnSpeed20, Mathf.Approximately(sim.simSpeed, 2.0f));
            SetBtnHighlight(btnSpeed40, Mathf.Approximately(sim.simSpeed, 4.0f));

            // Flow nodes & station status list
            int runningCount = 0;
            for (int i = 0; i < 9; i++)
            {
                bool stRun = sim.isRunning && sim.stationRunning[i];
                if (stRun) runningCount++;
                bool selected = (i == _displayedStationIndex && sim.selectedStationIndex >= 0);

                if (i < flowNodeButtons.Length && flowNodeButtons[i] != null)
                    SetBtnHighlight(flowNodeButtons[i], selected);
                if (i < flowNodeIndicators.Length && flowNodeIndicators[i] != null)
                    flowNodeIndicators[i].color = selected ? colorAccentCyan : (stRun ? colorOkGreen : colorWarnAmber);

                if (i < stationStatusDots.Length && stationStatusDots[i] != null)
                    stationStatusDots[i].color = stRun ? colorOkGreen : colorWarnAmber;
                if (i < stationStatusLabels.Length && stationStatusLabels[i] != null)
                    stationStatusLabels[i].text = stRun ? "<color=#2AE87A>运行中</color>" : "<color=#FAB024>已停机</color>";
            }

            if (statusSummaryText != null)
                statusSummaryText.text = sim.isRunning
                    ? $"<color=#2AE87A>● 在线运行</color>  ({runningCount}/9 工段)"
                    : "<color=#FAB024>● 全厂暂停</color>";
        }

        private void RefreshHeaderButtons()
        {
            var sim = SimulationState.Instance;
            var cam = CameraRigManager.Instance;
            if (sim != null)
            {
                SetBtnHighlight(btnModeDashboard, sim.currentMode == AppMode.Dashboard);
                SetBtnHighlight(btnModeTeaching, sim.currentMode == AppMode.TeachingTour);
                SetBtnHighlight(btnToggleMarkers, sim.showWorldMarkers);
            }
            if (cam != null)
            {
                SetBtnHighlight(btnCamOrbit, cam.CurrentViewMode == CameraViewMode.BirdEyeOrbit);
                SetBtnHighlight(btnCamFps, cam.CurrentViewMode == CameraViewMode.FirstPersonWalk);
                SetBtnHighlight(btnCamTour, cam.CurrentViewMode == CameraViewMode.AutoTour);
                SetBtnHighlight(btnHall1, cam.CurrentHall == WorkshopHall.SpinningHall);
                SetBtnHighlight(btnHall2, cam.CurrentHall == WorkshopHall.InspectionWarehouse);
                SetBtnHighlight(btnRoofCutaway, cam.IsRoofCutaway);
            }

            int q = QualitySettings.GetQualityLevel();
            SetBtnHighlight(btnQualMed, q <= 2);
            SetBtnHighlight(btnQualHigh, q == 3 || q == 4);
            SetBtnHighlight(btnQualUltra, q >= 5);
        }

        private void RefreshLiveTelemetry()
        {
            var sim = SimulationState.Instance;
            if (sim == null) return;

            if (kpiOutputText != null) kpiOutputText.text = $"{sim.totalOutputKg:N1} <size=13><color=#8CB4D8>kg</color></size>";
            if (kpiPassRateText != null) kpiPassRateText.text = $"{sim.passRatePct:0.0}%";
            if (kpiPassRateRing != null) kpiPassRateRing.fillAmount = Mathf.Clamp01(sim.passRatePct / 100f);
            if (kpiOeeText != null) kpiOeeText.text = $"{sim.oeePct:0.0}%";
            if (kpiOeeBar != null) kpiOeeBar.fillAmount = Mathf.Clamp01(sim.oeePct / 100f);

            if (warehouseOccupancyText != null)
                warehouseOccupancyText.text = $"立体库货位占用  <color=#00DCFF>{sim.occupiedWarehouseSlots} / 16</color>";

            for (int i = 0; i < warehouseSlotCells.Length && i < sim.warehouseSlotStates.Length; i++)
            {
                if (warehouseSlotCells[i] != null)
                    warehouseSlotCells[i].color = sim.warehouseSlotStates[i] ? colorSlotOccupied : colorSlotEmpty;
            }

            // Refresh inspector live sensor bars
            sim.GetLiveTelemetry(_displayedStationIndex, out float rpm, out float tempC, out float powerKw, out float loadPct);
            var st = sim.Stations[_displayedStationIndex];

            if (telemetryRpmText != null) telemetryRpmText.text = $"{rpm:N0} r/min";
            if (telemetryRpmBar != null) telemetryRpmBar.fillAmount = Mathf.Clamp01(rpm / Mathf.Max(1f, st.nominalRpm * 1.35f));

            if (telemetryTempText != null) telemetryTempText.text = $"{tempC:0.1} °C";
            if (telemetryTempBar != null) telemetryTempBar.fillAmount = Mathf.Clamp01(tempC / 75f);

            if (telemetryPowerText != null) telemetryPowerText.text = $"{powerKw:0.1} kW";
            if (telemetryPowerBar != null) telemetryPowerBar.fillAmount = Mathf.Clamp01(powerKw / Mathf.Max(1f, st.nominalPowerKw * 1.3f));

            if (telemetryLoadText != null) telemetryLoadText.text = $"{loadPct:0}%";
            if (telemetryLoadBar != null) telemetryLoadBar.fillAmount = Mathf.Clamp01(loadPct / 100f);

            // Refresh teaching progress counters
            if (teachingProgressText != null)
                teachingProgressText.text = $"实训进度：已学 {sim.GetLearnedCount()}/9   考核通过 {sim.GetQuizPassedCount()}/9";
            if (teachingProgressBar != null)
                teachingProgressBar.fillAmount = sim.GetLearnedCount() / 9f;

            for (int i = 0; i < 9 && i < teachingStepCheckTexts.Length; i++)
            {
                if (teachingStepCheckTexts[i] == null) continue;
                if (sim.stationQuizPassed[i]) teachingStepCheckTexts[i].text = "<color=#2AE87A>★ 已通</color>";
                else if (sim.stationLearned[i]) teachingStepCheckTexts[i].text = "<color=#00DCFF>✓ 已学</color>";
                else teachingStepCheckTexts[i].text = "<color=#6F8AA6>待学习</color>";
            }
        }

        private void RefreshInspector(int idx)
        {
            var sim = SimulationState.Instance;
            if (sim == null || idx < 0 || idx >= sim.Stations.Count) return;
            var st = sim.Stations[idx];

            if (inspectorCodeText != null) inspectorCodeText.text = st.code;
            if (inspectorNameText != null) inspectorNameText.text = st.name;
            if (inspectorEngNameText != null) inspectorEngNameText.text = st.englishName;

            bool active = sim.isRunning && sim.stationRunning[idx];
            if (inspectorStatusDot != null) inspectorStatusDot.color = active ? colorOkGreen : colorWarnAmber;
            if (inspectorStatusText != null) inspectorStatusText.text = active ? "运行正常" : "待机/停机";
            if (btnToggleStationRunLabel != null) btnToggleStationRunLabel.text = sim.stationRunning[idx] ? "远程停机" : "远程启动";

            if (inspectorPreviewImage != null && st.previewSprite != null)
            {
                inspectorPreviewImage.sprite = st.previewSprite;
                inspectorPreviewImage.preserveAspect = true;
            }

            if (specsTableText != null && st.specs != null)
            {
                _sb.Clear();
                for (int i = 0; i < st.specs.Length; i++)
                {
                    var sp = st.specs[i];
                    _sb.Append("<color=#8BB5D9>").Append(sp.label).Append("：</color>");
                    _sb.Append("<color=#FFFFFF><b>").Append(sp.value).Append("</b></color>");
                    if (!string.IsNullOrEmpty(sp.unit))
                        _sb.Append(" <color=#5CE6FF>").Append(sp.unit).Append("</color>");
                    if (i < st.specs.Length - 1) _sb.Append('\n');
                }
                specsTableText.text = _sb.ToString();
            }

            if (processSummaryText != null)
                processSummaryText.text = st.processSummary;

            RefreshLiveTelemetry();
        }

        private void RefreshTeachingPanel(int idx)
        {
            var sim = SimulationState.Instance;
            if (sim == null || idx < 0 || idx >= sim.Stations.Count) return;
            var st = sim.Stations[idx];

            if (teachingGoalText != null)
                teachingGoalText.text = $"<color=#00DCFF>【{st.code} 教学目标】</color>\n{st.teachingGoal}";

            if (teachingKeyPointsText != null && st.teachingKeyPoints != null)
                teachingKeyPointsText.text = string.Join("\n", st.teachingKeyPoints);

            if (st.quiz != null)
            {
                if (quizQuestionText != null) quizQuestionText.text = $"随堂问答：{st.quiz.question}";
                for (int i = 0; i < 3; i++)
                {
                    if (i < quizOptionTexts.Length && quizOptionTexts[i] != null && st.quiz.options != null && i < st.quiz.options.Length)
                        quizOptionTexts[i].text = st.quiz.options[i];
                    if (i < quizOptionButtons.Length && quizOptionButtons[i] != null)
                        SetBtnHighlight(quizOptionButtons[i], false);
                }
                if (quizFeedbackText != null)
                    quizFeedbackText.text = sim.stationQuizPassed[idx]
                        ? $"<color=#2AE87A>✓ 已掌握！</color>{st.quiz.explanation}"
                        : "<color=#8BB5D9>请选择正确答案检验本工段工艺理解。</color>";
            }
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
                    quizFeedbackText.text = $"<color=#2AE87A>✓ 回答正确！</color>{st.quiz.explanation}";
            }
            else
            {
                if (quizFeedbackText != null)
                    quizFeedbackText.text = $"<color=#FF6470>✕ 回答有误，请结合工艺要点重试。</color>";
            }

            for (int i = 0; i < quizOptionButtons.Length; i++)
                SetBtnHighlight(quizOptionButtons[i], i == optionIdx);
        }

        private void RefreshLogs()
        {
            var sim = SimulationState.Instance;
            if (sim == null || eventLogText == null) return;

            _sb.Clear();
            int count = Mathf.Min(sim.eventLogs.Count, 6);
            for (int i = 0; i < count; i++)
            {
                var e = sim.eventLogs[i];
                string col = e.level == "OK" ? "#2AE87A" : (e.level == "WARN" ? "#FAB024" : "#5CE6FF");
                _sb.Append("<color=#6E8EAD>[").Append(e.timestamp).Append("]</color> ");
                _sb.Append("<color=").Append(col).Append(">").Append(e.message).Append("</color>");
                if (i < count - 1) _sb.Append('\n');
            }
            eventLogText.text = _sb.ToString();
        }

        private void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(level, 0, QualitySettings.names.Length - 1), true);
            RefreshHeaderButtons();
        }

        private void SetBtnHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UIImage>();
            if (img != null)
                img.color = active ? colorAccentCyan : colorBtnIdle;
        }
    }
}
