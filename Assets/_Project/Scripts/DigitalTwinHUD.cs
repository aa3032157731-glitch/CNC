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
    /// Style A (Apple VisionOS Spatial Frosted Glass) Unified Digital Twin Dashboard & Teaching Tour HUD.
    /// Minimalist floating glass pills, 80%+ open 3D factory viewport, zero visual clutter.
    /// </summary>
    public class DigitalTwinHUD : MonoBehaviour
    {
        public static DigitalTwinHUD Instance { get; private set; }

        [Header("Style A Pill Sprites")]
        public Sprite spPillActiveWhite;
        public Sprite spPillActiveSky;
        public Sprite spPillIdle;
        public Sprite spPillGhost;
        public Sprite spPillGreen;
        public Sprite spPillDanger;

        [Header("Root Containers")]
        public GameObject hudMainContainer;
        public RectTransform leftPanelRect;
        public GameObject leftPanelRoot;
        public GameObject rightPanelRoot;
        public GameObject dashboardLeftContent;
        public GameObject teachingLeftContent;

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

        [Header("Left Floating Island - Teaching Tour")]
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

        [Header("Right Floating Drawer - Device Inspector")]
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

        [Header("Bottom Floating Dock")]
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
        public Color colorDarkOnWhite = new Color(0.06f, 0.09f, 0.16f, 1f);
        public Color colorSoftWhite = new Color(0.95f, 0.97f, 1.0f, 0.92f);
        public Color colorMutedText = new Color(0.82f, 0.87f, 0.94f, 0.78f);
        public Color colorSky = new Color(0.22f, 0.74f, 0.97f, 0.88f);
        public Color colorMint = new Color(0.20f, 0.83f, 0.60f, 1f);
        public Color colorAmber = new Color(0.98f, 0.74f, 0.14f, 1f);
        public Color colorSlotOn = new Color(0.22f, 0.74f, 0.97f, 0.85f);
        public Color colorSlotOff = new Color(1.0f, 1.0f, 1.0f, 0.12f);

        private bool _hudHidden;
        private bool _sidePanelsCollapsed;
        private int _displayedStationIndex = 0;
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
            HandleModeChanged(sim != null ? sim.currentMode : AppMode.Dashboard);
            HandleViewModeChanged(cam != null ? cam.CurrentViewMode : CameraViewMode.FirstPersonWalk);
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
                if (SimulationState.Instance != null && SimulationState.Instance.selectedStationIndex < 0)
                    CameraRigManager.Instance?.FocusStation(0, false);
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

            if (btnToggleStationRun != null) btnToggleStationRun.onClick.AddListener(() =>
            {
                SimulationState.Instance?.ToggleStationRun(_displayedStationIndex);
                RefreshInspector(_displayedStationIndex);
            });

            if (btnSimPlayPause != null) btnSimPlayPause.onClick.AddListener(() => SimulationState.Instance?.ToggleGlobalRun());
            if (btnSpeedCycle != null) btnSpeedCycle.onClick.AddListener(() =>
            {
                _speedIdx = (_speedIdx + 1) % _speeds.Length;
                SimulationState.Instance?.SetSimSpeed(_speeds[_speedIdx]);
            });

            if (btnCollapseSidePanels != null) btnCollapseSidePanels.onClick.AddListener(() =>
            {
                _sidePanelsCollapsed = !_sidePanelsCollapsed;
                if (leftPanelRoot != null) leftPanelRoot.SetActive(!_sidePanelsCollapsed);
                if (rightPanelRoot != null) rightPanelRoot.SetActive(!_sidePanelsCollapsed);
                if (btnCollapseLabel != null) btnCollapseLabel.text = _sidePanelsCollapsed ? "展开" : "极简";
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

            // Compact 418px floating island in Dashboard mode -> huge open 3D space below!
            // Smoothly expands to 700px only when viewing Teaching Tour curriculum & quiz.
            if (leftPanelRect != null)
                leftPanelRect.sizeDelta = new Vector2(296f, isDash ? 418f : 700f);

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
                    hoverTooltipText.text = $"{st.code} · {st.name}\n<color=#7DD3FC>左键点击查看详情与远程控制</color>";
                hoverTooltipRoot.SetActive(true);
            }
        }

        private void RefreshAllStates()
        {
            RefreshHeaderButtons();
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

            if (kpiOutputText != null) kpiOutputText.text = $"{sim.totalOutputKg:N0}<size=12><color=#CBD5E1> kg</color></size>";
            if (kpiPassRateText != null) kpiPassRateText.text = $"{sim.passRatePct:0.0}<size=12>%</size>";
            if (kpiOeeText != null) kpiOeeText.text = $"{sim.oeePct:0.0}<size=12>%</size>";
            if (kpiWarehouseShortText != null) kpiWarehouseShortText.text = $"{sim.occupiedWarehouseSlots}<size=12><color=#94A3B8> / 16</color></size>";

            if (warehouseOccupancyText != null)
                warehouseOccupancyText.text = $"{(sim.occupiedWarehouseSlots * 100f / 16f):0.0}%";

            for (int i = 0; i < warehouseSlotCells.Length && i < sim.warehouseSlotStates.Length; i++)
                if (warehouseSlotCells[i] != null)
                    warehouseSlotCells[i].color = sim.warehouseSlotStates[i] ? colorSlotOn : colorSlotOff;

            sim.GetLiveTelemetry(_displayedStationIndex, out float rpm, out float tempC, out float powerKw, out float loadPct);
            var st = sim.Stations[_displayedStationIndex];

            if (telemetryRpmText != null) telemetryRpmText.text = $"{rpm:N0} r/min";
            if (telemetryRpmBar != null) telemetryRpmBar.fillAmount = Mathf.Clamp01(rpm / Mathf.Max(1f, st.nominalRpm * 1.35f));

            if (telemetryTempText != null) telemetryTempText.text = $"{tempC:0.1} °C";
            if (telemetryTempBar != null) telemetryTempBar.fillAmount = Mathf.Clamp01(tempC / 75f);

            if (telemetryLoadText != null) telemetryLoadText.text = $"{loadPct:0}% · {powerKw:0.1} kW";
            if (telemetryLoadBar != null) telemetryLoadBar.fillAmount = Mathf.Clamp01(loadPct / 100f);

            if (teachingProgressText != null)
                teachingProgressText.text = $"实训进度  已学 {sim.GetLearnedCount()}/9  ·  考核 {sim.GetQuizPassedCount()}/9";
            if (teachingProgressBar != null)
                teachingProgressBar.fillAmount = sim.GetLearnedCount() / 9f;

            for (int i = 0; i < 9 && i < teachingStepCheckTexts.Length; i++)
            {
                if (teachingStepCheckTexts[i] == null) continue;
                if (sim.stationQuizPassed[i]) teachingStepCheckTexts[i].text = "<color=#34D399>★ 通过</color>";
                else if (sim.stationLearned[i]) teachingStepCheckTexts[i].text = "<color=#7DD3FC>✓ 已学</color>";
                else teachingStepCheckTexts[i].text = "<color=#94A3B8>待学</color>";
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
                btnToggleStationRunLabel.color = active ? new Color(0.43f, 0.91f, 0.72f) : new Color(0.99f, 0.65f, 0.65f);
            }

            if (inspectorPreviewImage != null && st.previewSprite != null)
            {
                inspectorPreviewImage.sprite = st.previewSprite;
                inspectorPreviewImage.preserveAspect = true;
            }

            if (specsTableText != null && st.specs != null)
            {
                _sb.Clear();
                int lim = Mathf.Min(st.specs.Length, 5);
                for (int i = 0; i < lim; i++)
                {
                    var sp = st.specs[i];
                    _sb.Append("<color=#94A3B8>").Append(sp.label).Append("</color>    ");
                    _sb.Append("<color=#F8FAFC><b>").Append(sp.value).Append("</b></color>");
                    if (!string.IsNullOrEmpty(sp.unit))
                        _sb.Append(" <color=#7DD3FC>").Append(sp.unit).Append("</color>");
                    if (i < lim - 1) _sb.Append('\n');
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
                teachingGoalText.text = $"<color=#7DD3FC><b>{st.code} 教学目标</b></color>\n{st.teachingGoal}";

            if (teachingKeyPointsText != null && st.teachingKeyPoints != null)
                teachingKeyPointsText.text = string.Join("\n", st.teachingKeyPoints);

            if (st.quiz != null)
            {
                if (quizQuestionText != null) quizQuestionText.text = $"随堂考核：{st.quiz.question}";
                for (int i = 0; i < 3; i++)
                {
                    if (i < quizOptionTexts.Length && quizOptionTexts[i] != null && st.quiz.options != null && i < st.quiz.options.Length)
                        quizOptionTexts[i].text = st.quiz.options[i];
                    if (i < quizOptionButtons.Length && quizOptionButtons[i] != null)
                        ApplyPillStyle(quizOptionButtons[i], false, false);
                }
                if (quizFeedbackText != null)
                    quizFeedbackText.text = sim.stationQuizPassed[idx]
                        ? $"<color=#34D399>✓ 已通过！</color>{st.quiz.explanation}"
                        : "<color=#94A3B8>选择正确选项完成本工段工艺考核。</color>";
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
                    quizFeedbackText.text = $"<color=#34D399>✓ 回答正确！</color>{st.quiz.explanation}";
            }
            else
            {
                if (quizFeedbackText != null)
                    quizFeedbackText.text = "<color=#F87171>✕ 回答有误，请参考上方工艺要点再试。</color>";
            }

            for (int i = 0; i < quizOptionButtons.Length; i++)
                ApplyPillStyle(quizOptionButtons[i], i == optionIdx, false);
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
