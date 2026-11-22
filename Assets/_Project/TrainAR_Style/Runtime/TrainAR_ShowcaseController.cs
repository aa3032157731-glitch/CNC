using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

namespace CNC.TrainAR
{
    public enum TrainingModule
    {
        Structure = 0,   // 阶段 1：安全与结构认知（末尾直切阶段 2）
        FaultOps = 1,    // 阶段 2：异常前兆与故障排障（末尾直切阶段 3）
        Assessment = 2   // 阶段 3：上岗安全考核
    }

    public enum FaultScenarioType
    {
        CalenderJam = 0,    // A. 喇叭口堵花 / 大压辊绕花
        NepGaugeDrift = 1,  // B. 锡林-盖板隔距漂移 / 棉结超标
        CanOverflow = 2     // C. 条筒满筒预警与溢条卡阻
    }

    /// <summary>
    /// Minimalist Cinema Guide Frontend (方案一 · 放大高可读字号 + 可点击设备科普卡 + 渐进式信息披露):
    /// - Removed noisy 3D hover tooltip completely.
    /// - Top-left displays clickable equipment name 「高产梳棉机」 (opens full equipment encyclopedia & safety notes).
    /// - Generous card sizing (1360px cinema bar) & crisp 18-25px typography across all 3 stages.
    /// </summary>
    public class TrainAR_ShowcaseController : MonoBehaviour
    {
        [Header("Scene References")]
        public Camera targetCamera;
        public TrainAR_CardingAnimator cardingAnimator;
        public TrainAR_MaterialFlowAnimator materialFlowAnimator;
        public TrainAR_StyleSwitcher styleSwitcher;

        [Header("Crisp SDF Fonts (72pt Noto Sans SC)")]
        public TMP_FontAsset uiFont;
        public TMP_FontAsset uiFontBold;

        [Header("Style A VisionOS Frosted Glass Sprites")]
        public Sprite spCardGlass;
        public Sprite spSubcard;
        public Sprite spPillGlass;
        public Sprite spPillActiveSky;
        public Sprite spPillActiveWhite;
        public Sprite spPillIdle;
        public Sprite spPillGreen;
        public Sprite spPillDanger;

        [Header("Orbit Camera (Centered Hero Framing Above Bottom Bar)")]
        public Vector3 defaultPivot = new Vector3(0.08f, 0.96f, 0.0f);
        public float defaultYaw = 158f;
        public float defaultPitch = 13.0f;
        public float defaultDistance = 6.45f;
        public float minDistance = 2.0f;
        public float maxDistance = 12.5f;

        // Camera state
        private Vector3 _pivot, _targetPivot;
        private float _yaw, _targetYaw, _pitch, _targetPitch, _distance, _targetDistance;
        private Vector3 _mouseDownPos;
        private bool _dragged;

        // Progressive disclosure toggles (HIDDEN by default!)
        private bool _detailsOpen = false;
        private bool _toolsOpen = false;
        private bool _deviceInfoOpen = false;

        // 3-Stage linear flow state
        private TrainingModule _activeModule = TrainingModule.Structure;

        // Stage 1: Structure & Safety
        private int _structureIdx = 0;
        private readonly HashSet<int> _visitedStructures = new HashSet<int>();

        // Stage 2: Fault & Precursor SOP Simulator
        private FaultScenarioType _faultScenario = FaultScenarioType.CalenderJam;
        private bool _faultEscalatedToRed = true;
        private int _faultSopStep = 0;
        private bool _faultMachineStopped = false;
        private bool _faultResolved = false;
        private int _safetyViolations = 0;
        private bool _lastActionWasViolation = false;
        private string _faultFeedbackMsg = "";
        private readonly HashSet<FaultScenarioType> _passedFaultScenarios = new HashSet<FaultScenarioType>();

        // Stage 3: Assessment
        private int _quizIdx = 0;
        private readonly Dictionary<int, int> _quizAnswers = new Dictionary<int, int>();

        // Root HUD refs
        private Canvas _hudCanvas;
        private bool _uiBuilt;
        private Material _crispRegularMat;
        private Material _crispBoldMat;
        private Button _deviceNameBtn;
        private TextMeshProUGUI _deviceNameBtnText;
        private GameObject _deviceInfoPopover;
        private TextMeshProUGUI _topStageText;
        private UIImage _topSafetyBg;
        private TextMeshProUGUI _topSafetyText;
        private TextMeshProUGUI _toolsToggleBtnText;
        private GameObject _toolsPopover;

        // Collapsible View Tool Buttons
        private Button[] _viewModeButtons;
        private TextMeshProUGUI _playBtnText;
        private TextMeshProUGUI _styleBtnText;
        private TextMeshProUGUI _canFillBtnText;

        // Bottom-Center Cinema Stack
        private GameObject _violationBannerGo;
        private TextMeshProUGUI _violationBannerText;
        private GameObject _detailsDrawerGo;
        private GameObject[] _detailStageRoots;
        private GameObject[] _stripStageRoots;

        // Stage 1 Refs
        private TextMeshProUGUI _s1StripTitle, _s1StripSubtitle, _s1DetailBtnText, _s1NextBtnText;
        private Button _s1PrevBtn, _s1NextBtn;
        private Button[] _s1StepChips;
        private TextMeshProUGUI _s1DetailFlow, _s1DetailSummary, _s1DetailSafety;

        // Stage 2 Refs
        private TextMeshProUGUI _s2StripTitle, _s2StripPrompt, _s2DetailBtnText;
        private Button _s2StopBtn, _s2ClearBtn, _s2RecoverBtn, _s2AdvanceBtn;
        private TextMeshProUGUI _s2StopText, _s2ClearText, _s2RecoverText, _s2AdvanceText;
        private Button[] _s2ScenarioBtns;
        private Button _s2PrecursorBtn, _s2AlarmBtn;
        private TextMeshProUGUI _s2MonRpm, _s2MonDoffer, _s2MonGauge, _s2DetailFeedback;
        private TextMeshProUGUI[] _s2StepTexts;

        // Stage 3 Refs
        private TextMeshProUGUI _s3StripQuestion, _s3DetailBtnText, _s3NextBtnText;
        private Button[] _s3OptBtns;
        private TextMeshProUGUI[] _s3OptTexts;
        private TextMeshProUGUI _s3DetailExplain, _s3DetailScore;

        // High-Contrast VisionOS Glass Palette
        private static readonly Color ColWhite = new Color(0.97f, 0.985f, 1.0f, 1f);
        private static readonly Color ColSub = new Color(0.82f, 0.89f, 0.96f, 1f);
        private static readonly Color ColSky = new Color(0.24f, 0.78f, 1.0f, 1f);
        private static readonly Color ColMint = new Color(0.22f, 0.88f, 0.62f, 1f);
        private static readonly Color ColAmber = new Color(0.99f, 0.76f, 0.22f, 1f);
        private static readonly Color ColDanger = new Color(0.98f, 0.42f, 0.42f, 1f);
        private static readonly Color ColGlassBg = new Color(0.07f, 0.11f, 0.18f, 0.88f);
        private static readonly Color ColSubcardBg = new Color(0.11f, 0.16f, 0.25f, 0.86f);
        private static readonly Color ColIdleBtn = new Color(0.16f, 0.23f, 0.34f, 0.92f);

        private struct StructureStage
        {
            public string shortName, title, oneLine, inputMat, action, outputMat, summary, factorySafety;
            public string[] partIds;
            public Vector3 pivot;
            public float yaw, pitch, dist;
        }

        private struct QuizItem
        {
            public string question;
            public string[] options;
            public int correctIndex;
            public string explanation;
            public string focusPartId;
            public Vector3 pivot;
            public float yaw, pitch, dist;
        }

        private static readonly StructureStage[] StructureStages =
        {
            new StructureStage
            {
                shortName = "01 喂棉箱",
                title = "01 · 喂棉箱与给棉罗拉",
                oneLine = "原棉层均匀沉降并由给棉罗拉稳定握持喂入；严禁运转时伸手清理喂棉口。",
                inputMat = "清花原棉层", action = "定量均匀喂入", outputMat = "握持均匀棉须",
                summary = "将松散原棉整理为均匀棉层，经给棉板与沟槽给棉罗拉连续定量输送给刺辊。",
                factorySafety = "• 巡检：观察原棉层均匀连续下移，防堵箱断棉\n• 红线：严禁给棉罗拉运转时伸手清理喂棉口\n• 处置：给棉过载跳动须立即停机排查硬物",
                partIds = new[] { "feed_chute" },
                pivot = new Vector3(-1.18f, 1.10f, 0f), yaw = 164f, pitch = 11f, dist = 3.25f
            },
            new StructureStage
            {
                shortName = "02 刺辊除杂",
                title = "02 · 高速刺辊与黄铜除尘刀",
                oneLine = "锯齿刺辊高速预分梳，下方双黄铜除尘刀排出棉籽壳；运转中严禁开落杂门。",
                inputMat = "握持棉须层", action = "高速打击 · 早落除杂", outputMat = "开松棉束 + 落杂",
                summary = "高速锯齿刺辊预分梳棉层，借离心力与除尘刀、漏底遵循“早落少碎”原则排除大杂。",
                factorySafety = "• 巡检：确认落杂箱籽壳下落顺畅无反涌\n• 红线：刺辊高速锯齿危险区，严禁运转中开罩\n• 听诊：闻及金属撞击异响立刻拍下 HMI 急停",
                partIds = new[] { "lickerin" },
                pivot = new Vector3(-0.82f, 0.88f, 0f), yaw = 166f, pitch = 10f, dist = 2.95f
            },
            new StructureStage
            {
                shortName = "03 主锡林",
                title = "03 · 主锡林大滚筒（整机心脏）",
                oneLine = "大惯量高速主锡林承担核心单纤分梳；停机后必须等滚筒完全静止方可开罩。",
                inputMat = "开松小棉束", action = "高速剥取 · 单纤分散", outputMat = "高速单纤维流",
                summary = "六辐高刚性主锡林覆以密集金属针布，转速 500 r/min 且惯量极大，工艺转速由工艺单锁定。",
                factorySafety = "• 红线：大惯量滚筒刹停慢，未完全静止严禁开罩\n• 巡检：关注 HMI 主轴承温度(≤50°C)与异响\n• 权限：核心转速工艺单锁定，挡车工不得私改",
                partIds = new[] { "main_cylinder" },
                pivot = new Vector3(-0.04f, 0.96f, 0f), yaw = 170f, pitch = 10f, dist = 3.35f
            },
            new StructureStage
            {
                shortName = "04 回转盖板",
                title = "04 · 回转盖板精细分梳区",
                oneLine = "26 块盖板沿曲轨与锡林相对精细分梳除棉结；严禁运转时调校黄铜隔距螺钉。",
                inputMat = "锡林纤维流", action = "针齿交叉 · 单纤分梳", outputMat = "顺直单纤维层",
                summary = "26 块活动盖板在主锡林上方缓慢回转，隔距约 0.20 mm，剔除棉结与短绒。",
                factorySafety = "• 红线：严禁运转中触碰盖板链条或拧曲轨螺钉\n• 巡检：若道夫棉结激增，立即停机报保全校距\n• 清洁：定时清除回转链周边积花防止卡滞",
                partIds = new[] { "revolving_flats" },
                pivot = new Vector3(-0.04f, 1.42f, 0f), yaw = 166f, pitch = 12f, dist = 3.15f
            },
            new StructureStage
            {
                shortName = "05 喇叭口压辊",
                title = "05 · 道夫棉网、集棉喇叭口与大压辊",
                oneLine = "道夫薄棉网经黄铜喇叭口与大压辊收束成生条；堵花时必须先停机再清花生头。",
                inputMat = "顺直单纤维层", action = "凝聚成网 · 集束成条", outputMat = "连续均匀生条",
                summary = "低速道夫凝聚均匀棉网，经剥棉罗拉、黄铜喇叭口及带螺旋压簧的大压辊压实为连续生条。",
                factorySafety = "• 高频：堵喇叭口或断条时须先停机清花再慢速生头\n• 红线：严禁带电用手掏大压辊与喇叭口绕花\n• 前兆：压簧剧跳或棉网云斑即堵花断条前兆",
                partIds = new[] { "doffer" },
                pivot = new Vector3(1.16f, 0.84f, 0f), yaw = 152f, pitch = 12f, dist = 3.05f
            },
            new StructureStage
            {
                shortName = "06 圈条灯塔",
                title = "06 · 圈条器、剖视条筒与三色灯塔",
                oneLine = "生条螺旋盘叠压下弹簧托盘；绿=正常 / 黄=满筒或质量预警 / 红=报警停机。",
                inputMat = "连续均匀生条", action = "偏心斜管螺旋盘叠", outputMat = "满装生条筒",
                summary = "偏心斜管将生条盘叠入筒并压缩底部 6 圈钢丝弹簧；顶塔三色灯与 HMI 实时告警。",
                factorySafety = "• 灯塔：绿=运行，黄=预警提前干预，红=停机报警\n• 换筒：满度 >90% 亮黄灯时提前换空筒防溢条\n• 急停：遇险第一时间拍下前方 HMI 红色急停键",
                partIds = new[] { "coiler", "console_ducts" },
                pivot = new Vector3(1.48f, 1.04f, 0f), yaw = 148f, pitch = 12f, dist = 3.95f
            }
        };

        private static readonly QuizItem[] QuizItems =
        {
            new QuizItem
            {
                question = "第 1/5 题【安全红线】发现集棉喇叭口堵花或大压辊绕花时，第一步规范操作是？",
                options = new[] { "A. 趁低速直接用手拽出棉团", "B. 立即停机断电，静止后清花再慢速生头", "C. 加大道夫速度把堵花硬拉过去" },
                correctIndex = 1,
                explanation = "严禁带电手掏绕花或堵团，极易引发手指卷入绞伤；必须严格按「停机断电 → 清花 → 慢速生头 → 提速」执行。",
                focusPartId = "doffer", pivot = new Vector3(1.28f, 0.84f, 0f), yaw = 150f, pitch = 12f, dist = 3.25f
            },
            new QuizItem
            {
                question = "第 2/5 题【安全红线】棉结激增需用塞尺校核锡林—盖板隔距时，必须遵守？",
                options = new[] { "A. 停机挂牌上锁(LOTO)待主锡林静止后校准", "B. 保持运转以便实时观察棉结变化", "C. 由挡车工直接调高 HMI 主锡林转速" },
                correctIndex = 0,
                explanation = "主锡林大惯量高速旋转，必须停机挂牌上锁并等待完全静止后方可开罩校距；核心工艺参数受工艺单权限锁定。",
                focusPartId = "revolving_flats", pivot = new Vector3(-0.04f, 1.38f, 0f), yaw = 164f, pitch = 12f, dist = 3.55f
            },
            new QuizItem
            {
                question = "第 3/5 题 梳棉机顶塔三色警示灯（绿 / 黄 / 红）分别代表什么工况？",
                options = new[] { "A. 绿=停机、黄=正常、红=满筒", "B. 绿=正常、黄=前兆预警、红=报警停机", "C. 仅作照明指示无工艺报警含义" },
                correctIndex = 1,
                explanation = "黄灯前兆预警（如 94% 满筒或棉结上升）提示提前干预，可有效避免恶化为红灯停机事故。",
                focusPartId = "console_ducts", pivot = new Vector3(0.42f, 1.68f, 0f), yaw = 154f, pitch = 12f, dist = 3.85f
            },
            new QuizItem
            {
                question = "第 4/5 题 刺辊下方配置两把黄铜除尘刀与落杂区的主要工艺目的是？",
                options = new[] { "A. 提高原棉含水率", "B. “早落少碎”，进锡林前优先排出籽壳大杂", "C. 给生条施加永久强捻度" },
                correctIndex = 1,
                explanation = "大颗粒杂质若带入高速锡林—盖板区会被击碎成微杂并轧伤金属针布，必须在刺辊区及早排除。",
                focusPartId = "lickerin", pivot = new Vector3(-0.82f, 0.88f, 0f), yaw = 165f, pitch = 11f, dist = 3.25f
            },
            new QuizItem
            {
                question = "第 5/5 题 当条筒满度超 90% 亮起黄灯预警时，最规范高效的处置是？",
                options = new[] { "A. 等溢条顶死圈条盘红灯停机再处理", "B. 黄灯阶段及时换入空筒，避免溢条停机", "C. 拆掉筒底弹簧让条子装得更满" },
                correctIndex = 1,
                explanation = "及时响应黄灯满筒预警更换空筒（弹簧回弹舒展），既保连续高产，又防止溢条卡死偏心斜管。",
                focusPartId = "coiler", pivot = new Vector3(1.78f, 0.80f, 0f), yaw = 144f, pitch = 13f, dist = 3.35f
            }
        };

        private void OnDestroy()
        {
            if (_crispRegularMat != null) { if (Application.isPlaying) Destroy(_crispRegularMat); else DestroyImmediate(_crispRegularMat); }
            if (_crispBoldMat != null) { if (Application.isPlaying) Destroy(_crispBoldMat); else DestroyImmediate(_crispBoldMat); }
        }

        private void EnsureCrispFontMaterials()
        {
            // Eliminate Triple-Bolding blur: clone dedicated runtime TMP materials with crisp SDF sharpness
            // and clean stroke separation (_FaceDilate = -0.025f, _Sharpness = 0.42f) without altering shared assets!
            if (_crispRegularMat == null && uiFont != null && uiFont.material != null)
            {
                _crispRegularMat = new Material(uiFont.material);
                if (_crispRegularMat.HasProperty("_FaceDilate")) _crispRegularMat.SetFloat("_FaceDilate", -0.02f);
                if (_crispRegularMat.HasProperty("_Sharpness")) _crispRegularMat.SetFloat("_Sharpness", 0.42f);
                if (_crispRegularMat.HasProperty("_OutlineWidth")) _crispRegularMat.SetFloat("_OutlineWidth", 0f);
            }
            if (_crispBoldMat == null && uiFontBold != null && uiFontBold.material != null)
            {
                _crispBoldMat = new Material(uiFontBold.material);
                if (_crispBoldMat.HasProperty("_FaceDilate")) _crispBoldMat.SetFloat("_FaceDilate", -0.03f);
                if (_crispBoldMat.HasProperty("_Sharpness")) _crispBoldMat.SetFloat("_Sharpness", 0.45f);
                if (_crispBoldMat.HasProperty("_OutlineWidth")) _crispBoldMat.SetFloat("_OutlineWidth", 0f);
            }
        }

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (cardingAnimator == null) cardingAnimator = FindObjectOfType<TrainAR_CardingAnimator>();
            if (materialFlowAnimator == null) materialFlowAnimator = FindObjectOfType<TrainAR_MaterialFlowAnimator>();
            if (styleSwitcher == null) styleSwitcher = FindObjectOfType<TrainAR_StyleSwitcher>();

            _pivot = _targetPivot = defaultPivot;
            _yaw = _targetYaw = defaultYaw;
            _pitch = _targetPitch = defaultPitch;
            _distance = _targetDistance = defaultDistance;

            ApplyCameraImmediate();
            if (styleSwitcher != null) styleSwitcher.ApplyStyle(styleSwitcher.activeStyle);

            BuildTrainingStudioHud();
            SwitchTrainingModule(TrainingModule.Structure);
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
            HandleMouseOrbitAndPicking();
            SmoothUpdateCamera();
            if (_canFillBtnText != null && materialFlowAnimator != null)
            {
                int pct = Mathf.RoundToInt(materialFlowAnimator.canFillProgress * 100f);
                _canFillBtnText.text = $"换筒 · {pct}%";
            }
        }

        private void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectViewMode(TrainARViewMode.Cutaway);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectViewMode(TrainARViewMode.Solid);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectViewMode(TrainARViewMode.Exploded);
            if (Input.GetKeyDown(KeyCode.Space)) TogglePlay();
            if (Input.GetKeyDown(KeyCode.O)) ToggleStyle();
            if (Input.GetKeyDown(KeyCode.R)) ResetOverview();
        }

        private void HandleMouseOrbitAndPicking()
        {
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (!overUi && Input.GetMouseButtonDown(0))
            {
                _mouseDownPos = Input.mousePosition;
                _dragged = false;
            }

            if (!overUi && Input.GetMouseButton(0))
            {
                float dx = Input.GetAxis("Mouse X");
                float dy = Input.GetAxis("Mouse Y");
                if (Vector3.Distance(Input.mousePosition, _mouseDownPos) > 5f) _dragged = true;
                _targetYaw += dx * 4.5f;
                _targetPitch = Mathf.Clamp(_targetPitch - dy * 3.5f, 4f, 68f);
            }

            if (!overUi && Input.GetMouseButtonUp(0) && !_dragged)
                TryPick3DPartAtMouse();

            if (!overUi && Input.GetMouseButton(1) && targetCamera != null)
            {
                float panScale = _distance * 0.0028f;
                _targetPivot -= targetCamera.transform.right * Input.GetAxis("Mouse X") * panScale * 18f;
                _targetPivot -= targetCamera.transform.up * Input.GetAxis("Mouse Y") * panScale * 18f;
                _targetPivot.y = Mathf.Clamp(_targetPivot.y, 0.25f, 2.4f);
            }

            if (!overUi)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.0001f)
                    _targetDistance = Mathf.Clamp(_targetDistance - scroll * 3.8f, minDistance, maxDistance);
            }
        }

        private void TryPick3DPartAtMouse()
        {
            if (targetCamera == null || cardingAnimator == null) return;
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 30f)) return;

            string partId = cardingAnimator.ResolvePartIdFromCollider(hit.transform);
            if (string.IsNullOrEmpty(partId)) return;

            if (_activeModule == TrainingModule.FaultOps)
                HandleFault3DClick(partId);
            else if (_activeModule == TrainingModule.Structure)
            {
                for (int i = 0; i < StructureStages.Length; i++)
                {
                    if (Array.IndexOf(StructureStages[i].partIds, partId) >= 0)
                    {
                        SelectStructureStage(i);
                        return;
                    }
                }
            }
        }

        private void SmoothUpdateCamera()
        {
            float k = 1f - Mathf.Exp(-8f * Time.deltaTime);
            _pivot = Vector3.Lerp(_pivot, _targetPivot, k);
            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, k);
            _pitch = Mathf.Lerp(_pitch, _targetPitch, k);
            _distance = Mathf.Lerp(_distance, _targetDistance, k);
            ApplyCameraImmediate();
        }

        private void ApplyCameraImmediate()
        {
            if (targetCamera == null) return;
            Quaternion rot = Quaternion.Euler(-_pitch, _yaw, 0f);
            targetCamera.transform.position = _pivot + rot * new Vector3(0f, 0f, _distance);
            targetCamera.transform.LookAt(_pivot);
        }

        public void ResetOverview()
        {
            _targetPivot = defaultPivot;
            _targetYaw = defaultYaw;
            _targetPitch = defaultPitch;
            _targetDistance = defaultDistance;
            if (cardingAnimator != null) cardingAnimator.SetFocusedPart("");
        }

        // =========================================================================
        // PROGRESSIVE DISCLOSURE TOGGLES ("藏信息")
        // =========================================================================
        public void SetDeviceInfoOpen(bool open)
        {
            _deviceInfoOpen = open;
            if (_deviceInfoPopover != null) _deviceInfoPopover.SetActive(open);
            if (_deviceNameBtnText != null) _deviceNameBtnText.text = open ? "高产梳棉机 -" : "高产梳棉机 +";
            if (_deviceNameBtn != null)
            {
                var img = _deviceNameBtn.GetComponent<UIImage>();
                if (open) SetCardSprite(img, spPillActiveSky, ColSky);
                else SetCardSprite(img, spPillIdle, ColIdleBtn);
            }
        }

        public void ToggleDeviceInfo() => SetDeviceInfoOpen(!_deviceInfoOpen);

        public void SetDetailsOpen(bool open)
        {
            _detailsOpen = open;
            if (_detailsDrawerGo != null) _detailsDrawerGo.SetActive(open);
            string tag1 = open ? "收起详情 -" : "查看详情 +";
            string tag2 = open ? "收起工况 -" : "工况 / 详情 +";
            string tag3 = open ? "收起剖析 -" : "剖析 / 成绩 +";
            if (_s1DetailBtnText != null) _s1DetailBtnText.text = tag1;
            if (_s2DetailBtnText != null) _s2DetailBtnText.text = tag2;
            if (_s3DetailBtnText != null) _s3DetailBtnText.text = tag3;
            RebuildBottomLayout();
        }

        public void ToggleDetails() => SetDetailsOpen(!_detailsOpen);

        public void SetToolsOpen(bool open)
        {
            _toolsOpen = open;
            if (_toolsPopover != null) _toolsPopover.SetActive(open);
            if (_toolsToggleBtnText != null) _toolsToggleBtnText.text = open ? "收起工具 -" : "视角工具 +";
        }

        public void ToggleTools() => SetToolsOpen(!_toolsOpen);

        private void RebuildBottomLayout()
        {
            if (_detailsDrawerGo != null && _detailsDrawerGo.transform.parent is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        // =========================================================================
        // 3-STAGE LINEAR PROGRESSION
        // =========================================================================
        public void SwitchTrainingModule(TrainingModule module)
        {
            _activeModule = module;
            SetDetailsOpen(false);
            if (_violationBannerGo != null) _violationBannerGo.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                if (_stripStageRoots != null && _stripStageRoots[i] != null) _stripStageRoots[i].SetActive(i == (int)module);
                if (_detailStageRoots != null && _detailStageRoots[i] != null) _detailStageRoots[i].SetActive(i == (int)module);
            }

            if (module != TrainingModule.FaultOps && materialFlowAnimator != null)
            {
                materialFlowAnimator.ClearAllFaultVisuals();
                materialFlowAnimator.autoCycleCanFill = true;
                materialFlowAnimator.SetLooseParticlesVisible(true);
                if (cardingAnimator != null)
                {
                    cardingAnimator.speedScale = 1.0f;
                    cardingAnimator.isRunning = true;
                    cardingAnimator.flatGaugeMm = 0.20f;
                }
                if (_playBtnText != null) _playBtnText.text = "暂停运转";
            }

            if (module == TrainingModule.Structure) SelectStructureStage(_structureIdx);
            else if (module == TrainingModule.FaultOps) StartFaultScenario(_faultScenario, _faultEscalatedToRed);
            else if (module == TrainingModule.Assessment) SelectQuizQuestion(_quizIdx);

            RefreshTopCorners();
            RefreshViewModeButtons();
            RebuildBottomLayout();
        }

        private void RefreshTopCorners()
        {
            if (_topStageText != null)
            {
                string label = _activeModule == TrainingModule.Structure ? "阶段 1/3 · 安全与结构"
                    : (_activeModule == TrainingModule.FaultOps ? "阶段 2/3 · 巡检与排障" : "阶段 3/3 · 上岗安全考核");
                string d1 = _activeModule == TrainingModule.Structure ? "<color=#38BDF8>●</color>" : "<color=#34D399>●</color>";
                string d2 = _activeModule == TrainingModule.FaultOps ? "<color=#38BDF8>●</color>"
                    : (_activeModule == TrainingModule.Assessment ? "<color=#34D399>●</color>" : "<color=#64748B>○</color>");
                string d3 = _activeModule == TrainingModule.Assessment ? "<color=#38BDF8>●</color>" : "<color=#64748B>○</color>";
                _topStageText.text = $"<color=#38BDF8>{label}</color>   {d1} {d2} {d3}";
            }

            if (_topSafetyText != null && _topSafetyBg != null)
            {
                if (_safetyViolations > 0)
                {
                    _topSafetyText.text = $"● 违章 {_safetyViolations} 次";
                    _topSafetyText.color = ColWhite;
                    if (spPillActiveWhite != null) { _topSafetyBg.sprite = spPillActiveWhite; _topSafetyBg.type = UIImage.Type.Sliced; }
                    _topSafetyBg.color = new Color(0.82f, 0.16f, 0.20f, 0.96f);
                }
                else
                {
                    _topSafetyText.text = "● 安全 0 违章";
                    _topSafetyText.color = ColMint;
                    SetCardSprite(_topSafetyBg, spPillIdle, ColIdleBtn);
                }
            }
        }

        // =========================================================================
        // STAGE 1: STRUCTURE & SAFETY COGNITION
        // =========================================================================
        public void SelectStructureStage(int idx)
        {
            _structureIdx = Mathf.Clamp(idx, 0, StructureStages.Length - 1);
            _visitedStructures.Add(_structureIdx);
            var st = StructureStages[_structureIdx];

            if (_s1StripTitle != null) _s1StripTitle.text = st.title;
            if (_s1StripSubtitle != null) _s1StripSubtitle.text = st.oneLine;
            if (_s1DetailFlow != null) _s1DetailFlow.text = $"<color=#38BDF8>{st.inputMat}</color>  →  <b>{st.action}</b>  →  <color=#34D399>{st.outputMat}</color>";
            if (_s1DetailSummary != null) _s1DetailSummary.text = st.summary;
            if (_s1DetailSafety != null) _s1DetailSafety.text = st.factorySafety;

            bool isLast = _structureIdx == StructureStages.Length - 1;
            if (_s1NextBtnText != null) _s1NextBtnText.text = isLast ? "进入阶段 2：排障 →" : "下一工段 →";
            if (_s1NextBtn != null) SetPillButtonStyle(_s1NextBtn, !isLast, isLast);
            if (_s1PrevBtn != null) _s1PrevBtn.interactable = _structureIdx > 0;

            if (_s1StepChips != null)
                for (int i = 0; i < _s1StepChips.Length; i++)
                    SetPillButtonStyle(_s1StepChips[i], i == _structureIdx, _visitedStructures.Contains(i));

            if (cardingAnimator != null) cardingAnimator.SetFocusedParts(st.partIds);
            _targetPivot = st.pivot;
            _targetYaw = st.yaw;
            _targetPitch = st.pitch;
            _targetDistance = st.dist;
        }

        private void OnStructureNextClicked()
        {
            if (_structureIdx < StructureStages.Length - 1) SelectStructureStage(_structureIdx + 1);
            else SwitchTrainingModule(TrainingModule.FaultOps);
        }

        // =========================================================================
        // STAGE 2: 3D FAULT & PRECURSOR SOP SIMULATOR
        // =========================================================================
        public void StartFaultScenario(FaultScenarioType scenario, bool escalatedToRed)
        {
            _faultScenario = scenario;
            _faultEscalatedToRed = escalatedToRed;
            _faultSopStep = 0;
            _faultResolved = false;
            _lastActionWasViolation = false;
            if (_violationBannerGo != null) _violationBannerGo.SetActive(false);

            if (cardingAnimator != null)
            {
                cardingAnimator.SetFocusedPart("");
                cardingAnimator.speedScale = 1.0f;
                cardingAnimator.cylinderRpm = 500f;
                cardingAnimator.dofferSpeedMpm = 100f;
                cardingAnimator.flatGaugeMm = 0.20f;
            }
            if (materialFlowAnimator != null) materialFlowAnimator.autoCycleCanFill = false;

            switch (scenario)
            {
                case FaultScenarioType.CalenderJam:
                    _faultMachineStopped = false;
                    if (cardingAnimator != null) { cardingAnimator.isRunning = true; cardingAnimator.speedScale = escalatedToRed ? 0.18f : 0.85f; }
                    if (materialFlowAnimator != null)
                    {
                        materialFlowAnimator.SetCanFill(0.58f);
                        materialFlowAnimator.SetFaultVisuals(!escalatedToRed, escalatedToRed, false,
                            escalatedToRed ? CardingBeaconState.RedAlarm : CardingBeaconState.AmberWarning);
                    }
                    _faultFeedbackMsg = "第 1 步：请先点击「停机断电」锁停主传动，严禁带电清理堵花。";
                    _targetPivot = new Vector3(0.95f, 0.96f, 0f);
                    _targetYaw = 152f; _targetPitch = 12f; _targetDistance = 4.35f;
                    break;

                case FaultScenarioType.NepGaugeDrift:
                    _faultMachineStopped = false;
                    if (cardingAnimator != null) { cardingAnimator.isRunning = true; cardingAnimator.speedScale = 1.0f; cardingAnimator.flatGaugeMm = escalatedToRed ? 0.33f : 0.29f; }
                    if (materialFlowAnimator != null)
                    {
                        materialFlowAnimator.SetCanFill(0.62f);
                        materialFlowAnimator.SetFaultVisuals(false, false, false,
                            escalatedToRed ? CardingBeaconState.RedAlarm : CardingBeaconState.AmberWarning);
                        materialFlowAnimator.ApplyAllImmediate(0.62f, 0.45f);
                    }
                    _faultFeedbackMsg = "第 1 步：道夫棉网浮现琥珀色棉结，请先「巡检棉网」再停机校距。";
                    _targetPivot = new Vector3(0.52f, 1.08f, 0f);
                    _targetYaw = 154f; _targetPitch = 12f; _targetDistance = 4.45f;
                    break;

                case FaultScenarioType.CanOverflow:
                    _faultMachineStopped = false;
                    if (cardingAnimator != null) { cardingAnimator.isRunning = !escalatedToRed; cardingAnimator.speedScale = 1.0f; }
                    if (materialFlowAnimator != null)
                    {
                        float fill = escalatedToRed ? 1.0f : 0.94f;
                        materialFlowAnimator.SetCanFill(fill);
                        materialFlowAnimator.SetFaultVisuals(false, false, escalatedToRed,
                            escalatedToRed ? CardingBeaconState.RedAlarm : CardingBeaconState.AmberWarning);
                        materialFlowAnimator.ApplyAllImmediate(fill, 0.50f);
                    }
                    _faultFeedbackMsg = escalatedToRed
                        ? "第 1 步：溢条卡死斜管！依次点「停机断电」→「清理溢条」→「换空条筒」。"
                        : "黄灯预警：条筒满度 94%、弹簧压底！直接点「换空条筒」获预防满分！";
                    _targetPivot = new Vector3(1.32f, 0.90f, 0f);
                    _targetYaw = 148f; _targetPitch = 12f; _targetDistance = 4.25f;
                    break;
            }
            RefreshFaultUi();
        }

        private void TriggerSafetyViolation(string reason)
        {
            _safetyViolations++;
            _lastActionWasViolation = true;
            if (_violationBannerGo != null)
            {
                _violationBannerGo.SetActive(true);
                _violationBannerText.text = $"<b>【安全红线违章 · 第 {_safetyViolations} 次】</b>{reason}";
            }
            RefreshTopCorners();
            RefreshFaultUi();
            RebuildBottomLayout();
        }

        private void ClearViolationBanner()
        {
            _lastActionWasViolation = false;
            if (_violationBannerGo != null) _violationBannerGo.SetActive(false);
        }

        private void HandleFault3DClick(string partId)
        {
            if (_faultResolved) return;
            if (partId == "console_ducts") OnFaultActionStopOrRestart();
            else if (partId == "doffer") OnFaultActionPrimaryClearOrInspect();
            else if (partId == "revolving_flats" && _faultScenario == FaultScenarioType.NepGaugeDrift) OnFaultActionSecondaryRecover();
            else if (partId == "coiler" && _faultScenario == FaultScenarioType.CanOverflow)
            {
                if (!_faultEscalatedToRed || _faultSopStep >= 2) OnFaultActionSecondaryRecover();
                else OnFaultActionPrimaryClearOrInspect();
            }
        }

        public void OnFaultActionStopOrRestart()
        {
            if (_faultResolved) return;
            ClearViolationBanner();

            if (_faultScenario == FaultScenarioType.CalenderJam)
            {
                if (_faultSopStep == 0)
                {
                    _faultMachineStopped = true; _faultSopStep = 1;
                    if (cardingAnimator != null) cardingAnimator.isRunning = false;
                    _faultFeedbackMsg = "✓ 已断电锁机！第 2 步：安全点击「清理堵花」清除喇叭口棉团。";
                }
                else if (_faultSopStep == 3)
                {
                    _faultMachineStopped = false; _faultSopStep = 4; _faultResolved = true;
                    _passedFaultScenarios.Add(FaultScenarioType.CalenderJam);
                    if (cardingAnimator != null) { cardingAnimator.isRunning = true; cardingAnimator.speedScale = 1.0f; }
                    if (materialFlowAnimator != null) { materialFlowAnimator.ClearAllFaultVisuals(); materialFlowAnimator.autoCycleCanFill = true; }
                    _faultFeedbackMsg = "✓ 工况 A 通关！堵花已清、三色灯复绿，点击右侧进入下一工况。";
                }
            }
            else if (_faultScenario == FaultScenarioType.NepGaugeDrift)
            {
                if (_faultSopStep == 0) _faultFeedbackMsg = "提示：请先点「巡检棉网」确认棉结超标，再停机上锁。";
                else if (_faultSopStep == 1)
                {
                    _faultMachineStopped = true; _faultSopStep = 2;
                    if (cardingAnimator != null) cardingAnimator.isRunning = false;
                    _faultFeedbackMsg = "✓ 主锡林已断电刹停！第 3 步：点击「塞尺校距」校回 0.20 mm。";
                }
                else if (_faultSopStep == 3)
                {
                    _faultMachineStopped = false; _faultSopStep = 4; _faultResolved = true;
                    _passedFaultScenarios.Add(FaultScenarioType.NepGaugeDrift);
                    if (cardingAnimator != null) { cardingAnimator.isRunning = true; cardingAnimator.speedScale = 1.0f; cardingAnimator.flatGaugeMm = 0.20f; }
                    if (materialFlowAnimator != null) { materialFlowAnimator.ClearAllFaultVisuals(); materialFlowAnimator.ApplyAllImmediate(0.62f, 0.45f); materialFlowAnimator.autoCycleCanFill = true; }
                    _faultFeedbackMsg = "✓ 工况 B 通关！道夫棉网琥珀色棉结全部清零，点击进入下一工况。";
                }
            }
            else if (_faultScenario == FaultScenarioType.CanOverflow && _faultEscalatedToRed && _faultSopStep == 0)
            {
                _faultMachineStopped = true; _faultSopStep = 1;
                if (cardingAnimator != null) cardingAnimator.isRunning = false;
                _faultFeedbackMsg = "✓ 已断电锁机！第 2 步：点击「清理溢条」清出偏心斜管堵条。";
            }
            RefreshFaultUi();
        }

        public void OnFaultActionPrimaryClearOrInspect()
        {
            if (_faultResolved) return;
            ClearViolationBanner();

            if (_faultScenario == FaultScenarioType.CalenderJam)
            {
                if (!_faultMachineStopped) { TriggerSafetyViolation("严禁在压辊带电未锁停时手掏喇叭口堵花！请先点击【停机断电】！"); return; }
                if (_faultSopStep == 1)
                {
                    _faultSopStep = 2;
                    if (materialFlowAnimator != null) materialFlowAnimator.SetFaultVisuals(false, false, false, CardingBeaconState.AmberWarning);
                    _faultFeedbackMsg = "✓ 堵花已清除！第 3 步：点击「慢速生头」低速将棉网重新引入。";
                }
            }
            else if (_faultScenario == FaultScenarioType.NepGaugeDrift && _faultSopStep == 0)
            {
                _faultSopStep = 1;
                if (cardingAnimator != null) cardingAnimator.SetFocusedPart("doffer");
                _faultFeedbackMsg = "✓ 已确认棉结超标！第 2 步：请先点击「停机上锁」待主锡林刹停。";
            }
            else if (_faultScenario == FaultScenarioType.CanOverflow && _faultEscalatedToRed)
            {
                if (!_faultMachineStopped) { TriggerSafetyViolation("严禁未停机锁电直接用手清理圈条器溢条！请先点击【停机断电】！"); return; }
                if (_faultSopStep == 1)
                {
                    _faultSopStep = 2;
                    if (materialFlowAnimator != null) materialFlowAnimator.SetFaultVisuals(false, false, false, CardingBeaconState.AmberWarning);
                    _faultFeedbackMsg = "✓ 溢条已清空！第 3 步：点击「换空条筒」回弹弹簧并恢复生产。";
                }
            }
            RefreshFaultUi();
        }

        public void OnFaultActionSecondaryRecover()
        {
            if (_faultResolved) return;
            ClearViolationBanner();

            if (_faultScenario == FaultScenarioType.CalenderJam)
            {
                if (_faultSopStep < 2)
                {
                    if (!_faultMachineStopped) TriggerSafetyViolation("尚未停机清花，严禁直接生头运转！");
                    else _faultFeedbackMsg = "提示：请先点「清理堵花」后再进行慢速生头。";
                    RefreshFaultUi();
                    return;
                }
                if (_faultSopStep == 2)
                {
                    _faultSopStep = 3;
                    if (cardingAnimator != null) { cardingAnimator.isRunning = true; cardingAnimator.speedScale = 0.38f; }
                    if (materialFlowAnimator != null) materialFlowAnimator.SetFaultVisuals(false, false, false, CardingBeaconState.AmberWarning);
                    _faultFeedbackMsg = "✓ 慢速生头平稳！第 4 步：点击「恢复开机」切回高速验收。";
                }
            }
            else if (_faultScenario == FaultScenarioType.NepGaugeDrift)
            {
                if (!_faultMachineStopped) { TriggerSafetyViolation("主锡林高速运转时严禁开罩用塞尺调隔距！请先【停机上锁】！"); return; }
                if (_faultSopStep == 2)
                {
                    _faultSopStep = 3;
                    if (cardingAnimator != null) { cardingAnimator.flatGaugeMm = 0.20f; cardingAnimator.SetFocusedPart("revolving_flats"); }
                    if (materialFlowAnimator != null) materialFlowAnimator.ApplyAllImmediate(0.62f, 0.45f);
                    _faultFeedbackMsg = "✓ 已用塞尺校回 0.20 mm！第 4 步：点击「恢复开机」验看棉结清零。";
                }
            }
            else if (_faultScenario == FaultScenarioType.CanOverflow)
            {
                if (!_faultEscalatedToRed || _faultSopStep >= 2)
                {
                    _faultSopStep = 3; _faultResolved = true; _faultMachineStopped = false;
                    _passedFaultScenarios.Add(FaultScenarioType.CanOverflow);
                    if (cardingAnimator != null) { cardingAnimator.isRunning = true; cardingAnimator.speedScale = 1.0f; }
                    if (materialFlowAnimator != null) { materialFlowAnimator.ResetEmptyCan(); materialFlowAnimator.ClearAllFaultVisuals(); materialFlowAnimator.autoCycleCanFill = true; }
                    _faultFeedbackMsg = "✓ 换筒完成、底部 6 圈弹簧回弹舒展！点击右侧进入上岗考核。";
                }
                else
                {
                    if (!_faultMachineStopped) TriggerSafetyViolation("溢条卡死斜管时须先停机清条，不可硬拉条筒！");
                    else _faultFeedbackMsg = "提示：请先点「清理溢条」，再推入空条筒。";
                }
            }
            RefreshFaultUi();
        }

        private void OnFaultAdvanceClicked()
        {
            if (_faultScenario == FaultScenarioType.CalenderJam) StartFaultScenario(FaultScenarioType.NepGaugeDrift, true);
            else if (_faultScenario == FaultScenarioType.NepGaugeDrift) StartFaultScenario(FaultScenarioType.CanOverflow, false);
            else SwitchTrainingModule(TrainingModule.Assessment);
        }

        public void RefreshFaultUi()
        {
            if (_s2StripTitle == null) return;
            float rpm = cardingAnimator != null ? cardingAnimator.cylinderRpm : 500f;
            float doff = cardingAnimator != null ? cardingAnimator.dofferSpeedMpm : 100f;
            float gauge = cardingAnimator != null ? cardingAnimator.flatGaugeMm : 0.20f;
            bool stopped = cardingAnimator != null && !cardingAnimator.isRunning;

            if (_s2MonRpm != null) { _s2MonRpm.text = stopped ? "锡林 已刹停" : $"锡林 {rpm:F0}r"; _s2MonRpm.color = stopped ? ColAmber : ColMint; }
            bool jamNow = _faultScenario == FaultScenarioType.CalenderJam && !_faultResolved;
            if (_s2MonDoffer != null) { _s2MonDoffer.text = stopped ? "道夫 已停机" : (jamNow ? "压辊 堵花!" : $"道夫 {doff:F0}m"); _s2MonDoffer.color = (stopped || jamNow) ? ColDanger : ColMint; }
            bool gaugeBad = gauge > 0.25f;
            if (_s2MonGauge != null) { _s2MonGauge.text = gaugeBad ? $"隔距 {gauge:F2}!" : $"隔距 {gauge:F2}mm"; _s2MonGauge.color = gaugeBad ? ColDanger : ColMint; }

            if (_s2ScenarioBtns != null)
                for (int i = 0; i < _s2ScenarioBtns.Length; i++)
                    SetPillButtonStyle(_s2ScenarioBtns[i], i == (int)_faultScenario, _passedFaultScenarios.Contains((FaultScenarioType)i));
            SetPillButtonStyle(_s2PrecursorBtn, !_faultEscalatedToRed, false);
            SetPillButtonStyle(_s2AlarmBtn, _faultEscalatedToRed, false);

            string[] steps;
            if (_faultScenario == FaultScenarioType.CalenderJam)
            {
                _s2StripTitle.text = _faultResolved ? "<color=#34D399>● 绿灯恢复 · 喇叭口堵花排障通关</color>"
                    : (_faultEscalatedToRed ? "<color=#F87171>● 红灯报警 · 喇叭口堵花 & 大压辊绕花</color>" : "<color=#FBBF24>● 黄灯前兆 · 大压辊压簧剧跳 & 棉团积聚</color>");
                steps = new[] { "1. 停机断电(LOTO)锁停主传动", "2. 确认静止后清理喇叭口堵花", "3. 慢速生头将棉网引入喇叭口", "4. 确认出条稳定后恢复高速开机" };
                _s2StopText.text = _faultSopStep >= 3 ? "恢复开机" : "停机断电";
                _s2ClearText.text = "清理堵花";
                _s2RecoverText.text = "慢速生头";
                _s2AdvanceText.text = _faultResolved ? "下工况 B →" : "跳至 B →";
            }
            else if (_faultScenario == FaultScenarioType.NepGaugeDrift)
            {
                _s2StripTitle.text = _faultResolved ? "<color=#34D399>● 绿灯恢复 · 隔距 0.20mm 棉结已清零</color>"
                    : (_faultEscalatedToRed ? "<color=#F87171>● 质量报警 · 隔距跑偏 0.33mm 棉结超标</color>" : "<color=#FBBF24>● 黄灯预警 · 道夫棉网棉结上升</color>");
                steps = new[] { "1. 巡检道夫棉网确认棉结超标", "2. 停机挂牌上锁待主锡林刹停", "3. 用塞尺将曲轨螺钉校回 0.20mm", "4. 恢复高速开机验收棉结清零" };
                _s2StopText.text = _faultSopStep >= 3 ? "恢复开机" : "停机上锁";
                _s2ClearText.text = "巡检棉网";
                _s2RecoverText.text = "塞尺校距";
                _s2AdvanceText.text = _faultResolved ? "下工况 C →" : "跳至 C →";
            }
            else
            {
                _s2StripTitle.text = _faultResolved ? "<color=#34D399>● 绿灯恢复 · 空条筒就位且弹簧舒展</color>"
                    : (_faultEscalatedToRed ? "<color=#F87171>● 红灯溢条 · 条筒爆满卡死偏心斜管</color>" : "<color=#FBBF24>● 黄灯满筒预警 · 条筒 94% 弹簧压底</color>");
                steps = _faultEscalatedToRed
                    ? new[] { "1. 停机断电确认圈条盘静止", "2. 清理偏心斜管口溢条堵团", "3. 换入空条筒回弹弹簧复产", "（可在工况内切黄灯体验提前换筒）" }
                    : new[] { "1. 识别黄灯预警与 94% 满度", "2. 直接点「换空条筒」预防换筒", "3. 观察底部 6 圈钢丝弹簧回弹", "（可在工况内切红灯演练溢条排障）" };
                _s2StopText.text = "停机断电";
                _s2ClearText.text = "清理溢条";
                _s2RecoverText.text = "换空条筒";
                _s2AdvanceText.text = "进入考核 →";
            }

            _s2StripPrompt.text = _faultFeedbackMsg;
            if (_s2DetailFeedback != null) _s2DetailFeedback.text = $"排障进度：{_passedFaultScenarios.Count}/3 工况  ·  安全违章：{_safetyViolations} 次";

            if (_s2StepTexts != null)
            {
                for (int i = 0; i < _s2StepTexts.Length; i++)
                {
                    bool done = _faultResolved || i < _faultSopStep;
                    bool curr = !_faultResolved && i == _faultSopStep;
                    string icon = done ? "<color=#34D399>✓</color> " : (curr ? "<color=#38BDF8>▶</color> " : "○ ");
                    _s2StepTexts[i].text = icon + (i < steps.Length ? steps[i] : "");
                    _s2StepTexts[i].color = done ? ColMint : (curr ? ColWhite : ColSub);
                }
            }

            SetPillButtonStyle(_s2StopBtn, _lastActionWasViolation || (_faultSopStep >= 3 && !_faultResolved), false);
            SetPillButtonStyle(_s2ClearBtn, false, false);
            SetPillButtonStyle(_s2RecoverBtn, !_faultEscalatedToRed && _faultScenario == FaultScenarioType.CanOverflow && !_faultResolved, false);
            SetPillButtonStyle(_s2AdvanceBtn, false, _faultResolved);
        }

        // =========================================================================
        // STAGE 3: OPERATOR SAFETY ASSESSMENT
        // =========================================================================
        public void SelectQuizQuestion(int idx)
        {
            _quizIdx = Mathf.Clamp(idx, 0, QuizItems.Length - 1);
            var q = QuizItems[_quizIdx];

            if (_s3StripQuestion != null) _s3StripQuestion.text = q.question;
            bool answered = _quizAnswers.TryGetValue(_quizIdx, out int chosen);

            for (int i = 0; i < 3; i++)
            {
                _s3OptTexts[i].text = q.options[i];
                if (!answered) SetPillButtonStyle(_s3OptBtns[i], false, false);
                else if (i == q.correctIndex) SetQuizOptionColor(_s3OptBtns[i], spPillGreen, new Color(0.10f, 0.50f, 0.36f, 0.94f));
                else if (i == chosen) SetQuizOptionColor(_s3OptBtns[i], spPillDanger, new Color(0.62f, 0.18f, 0.22f, 0.94f));
                else SetPillButtonStyle(_s3OptBtns[i], false, false);
            }

            if (_s3DetailExplain != null)
            {
                if (!answered) _s3DetailExplain.text = "<color=#94A3B8>请结合上方 3D 聚焦部位直接点击下方 A / B / C 作答。</color>";
                else if (chosen == q.correctIndex) _s3DetailExplain.text = $"<color=#34D399>✓ 回答正确！</color>{q.explanation}";
                else _s3DetailExplain.text = $"<color=#F87171>× 回答有误（正确：{q.options[q.correctIndex].Substring(0, 1)}）</color>{q.explanation}";
            }

            if (_s3NextBtnText != null) _s3NextBtnText.text = _quizIdx == QuizItems.Length - 1 ? "完成并复训" : "下一题 →";
            if (cardingAnimator != null) cardingAnimator.SetFocusedPart(q.focusPartId);
            _targetPivot = q.pivot;
            _targetYaw = q.yaw;
            _targetPitch = q.pitch;
            _targetDistance = q.dist;
            UpdateQuizScorecard();
        }

        public void SubmitQuizChoice(int optIdx)
        {
            _quizAnswers[_quizIdx] = optIdx;
            SelectQuizQuestion(_quizIdx);
            SetDetailsOpen(true);
        }

        private void OnQuizNextClicked()
        {
            if (_quizIdx < QuizItems.Length - 1)
            {
                SetDetailsOpen(false);
                SelectQuizQuestion(_quizIdx + 1);
            }
            else
            {
                SwitchTrainingModule(TrainingModule.Structure);
            }
        }

        private void UpdateQuizScorecard()
        {
            int correctQuiz = 0;
            foreach (var kv in _quizAnswers)
                if (kv.Key >= 0 && kv.Key < QuizItems.Length && kv.Value == QuizItems[kv.Key].correctIndex)
                    correctQuiz++;

            int sStruct = Mathf.RoundToInt((_visitedStructures.Count / (float)StructureStages.Length) * 25f);
            int sFault = Mathf.RoundToInt((_passedFaultScenarios.Count / 3f) * 45f);
            int sQuiz = correctQuiz * 6;
            int total = sStruct + sFault + sQuiz;

            if (_s3DetailScore != null)
            {
                string gate = _safetyViolations > 0
                    ? $"<color=#F87171>上岗准入：未通过（存在 {_safetyViolations} 次安全违章）</color>"
                    : (total >= 80
                        ? "<color=#34D399>上岗准入：✓ 达标（安全 0 违章，准予真机跟班）</color>"
                        : $"<color=#FBBF24>当前综合得分 {total}/100（需 ≥80 分且安全 0 违章）</color>");
                _s3DetailScore.text = $"结构 {sStruct}/25 · 排障 {sFault}/45 · 考核 {sQuiz}/30\n{gate}";
            }
        }

        // --- VIEW TOOLS ---
        public void SelectViewMode(TrainARViewMode mode)
        {
            if (cardingAnimator != null) cardingAnimator.SetViewMode(mode);
            RefreshViewModeButtons();
        }

        public void TogglePlay()
        {
            if (cardingAnimator == null) return;
            cardingAnimator.isRunning = !cardingAnimator.isRunning;
            if (_playBtnText != null) _playBtnText.text = cardingAnimator.isRunning ? "暂停运转" : "继续运转";
        }

        public void ToggleStyle()
        {
            if (styleSwitcher != null)
            {
                var next = styleSwitcher.activeStyle == VisualRenderStyle.RealisticPBR
                    ? VisualRenderStyle.TrainARStylized
                    : VisualRenderStyle.RealisticPBR;
                styleSwitcher.ApplyStyle(next);
                if (_styleBtnText != null)
                    _styleBtnText.text = next == VisualRenderStyle.RealisticPBR ? "写实 PBR" : "TrainAR 卡通";
            }
        }

        private void RefreshViewModeButtons()
        {
            if (_viewModeButtons == null || cardingAnimator == null) return;
            for (int i = 0; i < _viewModeButtons.Length; i++)
                SetPillButtonStyle(_viewModeButtons[i], i == (int)cardingAnimator.viewMode, false);
        }

        // =========================================================================
        // 方案一 HUD BUILDER (BIG READABLE TYPOGRAPHY + CLICKABLE DEVICE ENCYCLOPEDIA!)
        // =========================================================================
        public Canvas BuildTrainingStudioHud(bool forceScreenSpaceCamera = false)
        {
            if (_uiBuilt && _hudCanvas != null) return _hudCanvas;
            _uiBuilt = true;
            EnsureCrispFontMaterials();

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("ST03_CinemaMinimalHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _hudCanvas = canvasGo.GetComponent<Canvas>();
            if (forceScreenSpaceCamera && targetCamera != null)
            {
                _hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                _hudCanvas.worldCamera = targetCamera;
                _hudCanvas.planeDistance = 0.5f;
            }
            else
            {
                _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            _hudCanvas.pixelPerfect = true;
            _hudCanvas.sortingOrder = 20;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // --- 1. TOP-LEFT FROSTED-GLASS DOCK (Matches Bottom Cinema Bar Glass Base!) ---
            var topLeftDock = CreateGlassPanel(canvasGo.transform, "TopLeftGlassDock", spCardGlass, ColGlassBg,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24, -18), new Vector2(564, 64));
            var tlHL = topLeftDock.AddComponent<HorizontalLayoutGroup>();
            tlHL.padding = new RectOffset(10, 10, 8, 8);
            tlHL.spacing = 10;
            tlHL.childControlWidth = true;
            tlHL.childControlHeight = true;
            tlHL.childForceExpandWidth = false;
            tlHL.childForceExpandHeight = true;

            _deviceNameBtn = CreatePillButton(topLeftDock.transform, "高产梳棉机 +", 19, ToggleDeviceInfo, out _deviceNameBtnText);
            _deviceNameBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 184;
            _deviceNameBtnText.color = ColWhite;

            var stagePill = CreateGlassPanel(topLeftDock.transform, "TopStagePill", spPillIdle, ColIdleBtn,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stagePill.AddComponent<LayoutElement>().preferredWidth = 350;
            _topStageText = CreateCenteredLabel(stagePill.transform, "<color=#38BDF8>阶段 1/3 · 安全与结构</color>   ● ○ ○", 18, true, ColWhite);

            // --- 1B. SPACIOUS 2-COLUMN EQUIPMENT ENCYCLOPEDIA & PRECAUTIONS POPOVER ---
            _deviceInfoPopover = BuildDeviceInfoPopover(canvasGo.transform);
            _deviceInfoPopover.SetActive(false);

            // --- 2. TOP-RIGHT FROSTED-GLASS DOCK (Matches Bottom Cinema Bar Glass Base!) ---
            var topRightDock = CreateGlassPanel(canvasGo.transform, "TopRightGlassDock", spCardGlass, ColGlassBg,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24, -18), new Vector2(388, 64));
            var trHL = topRightDock.AddComponent<HorizontalLayoutGroup>();
            trHL.padding = new RectOffset(10, 10, 8, 8);
            trHL.spacing = 10;
            trHL.childControlWidth = true;
            trHL.childControlHeight = true;
            trHL.childForceExpandWidth = false;
            trHL.childForceExpandHeight = true;

            var safetyGo = CreateGlassPanel(topRightDock.transform, "SafetyPill", spPillIdle, ColIdleBtn,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            safetyGo.AddComponent<LayoutElement>().preferredWidth = 194;
            _topSafetyBg = safetyGo.GetComponent<UIImage>();
            _topSafetyText = CreateCenteredLabel(safetyGo.transform, "● 安全 0 违章", 18.5f, true, ColMint);

            var toolsBtn = CreatePillButton(topRightDock.transform, "视角工具 +", 18.5f, ToggleTools, out _toolsToggleBtnText);
            toolsBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 164;
            _toolsToggleBtnText.color = ColWhite;

            // --- 3. HIDDEN COLLAPSIBLE VIEW TOOLS POPOVER ---
            _toolsPopover = CreateGlassPanel(canvasGo.transform, "ToolsPopover", spCardGlass, ColGlassBg,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24, -92), new Vector2(332, 266));
            AddVLayout(_toolsPopover, 10, new RectOffset(14, 14, 14, 14));
            CreateBlockText(_toolsPopover.transform, "视角与场景工具", 17.5f, true, ColSub, 26);

            var tGrid = new GameObject("ToolsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            tGrid.transform.SetParent(_toolsPopover.transform, false);
            tGrid.AddComponent<LayoutElement>().preferredHeight = 152;
            var tg = tGrid.GetComponent<GridLayoutGroup>();
            tg.cellSize = new Vector2(147, 44);
            tg.spacing = new Vector2(10, 10);
            tg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            tg.constraintCount = 2;

            _viewModeButtons = new Button[3];
            _viewModeButtons[0] = CreatePillButton(tGrid.transform, "半剖视", 17.5f, () => SelectViewMode(TrainARViewMode.Cutaway), out _);
            _viewModeButtons[1] = CreatePillButton(tGrid.transform, "完整外形", 17.5f, () => SelectViewMode(TrainARViewMode.Solid), out _);
            _viewModeButtons[2] = CreatePillButton(tGrid.transform, "爆炸视图", 17.5f, () => SelectViewMode(TrainARViewMode.Exploded), out _);
            CreatePillButton(tGrid.transform, "暂停运转", 17.5f, TogglePlay, out _playBtnText);
            int initPct = materialFlowAnimator != null ? Mathf.RoundToInt(materialFlowAnimator.canFillProgress * 100f) : 65;
            CreatePillButton(tGrid.transform, $"换筒 · {initPct}%", 17.5f, () => { if (materialFlowAnimator != null) materialFlowAnimator.ResetEmptyCan(); }, out _canFillBtnText);
            string styleInit = (styleSwitcher != null && styleSwitcher.activeStyle == VisualRenderStyle.TrainARStylized) ? "TrainAR 卡通" : "写实 PBR";
            CreatePillButton(tGrid.transform, styleInit, 17.5f, ToggleStyle, out _styleBtnText);

            var resetBtn = CreatePillButton(_toolsPopover.transform, "复位中心视角 (R)", 17.5f, ResetOverview, out _);
            resetBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            _toolsPopover.SetActive(false);

            // --- 4. BOTTOM-CENTER CINEMA GUIDE STACK (Wider 1360px + Big Effortless Typography!) ---
            var stackGo = new GameObject("BottomCinemaStack", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            stackGo.transform.SetParent(canvasGo.transform, false);
            var stRt = stackGo.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 0f);
            stRt.anchorMax = new Vector2(0.5f, 0f);
            stRt.pivot = new Vector2(0.5f, 0f);
            stRt.anchoredPosition = new Vector2(0, 22);
            stRt.sizeDelta = new Vector2(1360, 0);

            var stackVL = stackGo.GetComponent<VerticalLayoutGroup>();
            stackVL.spacing = 12;
            stackVL.childControlWidth = true;
            stackVL.childControlHeight = true;
            stackVL.childForceExpandWidth = true;
            stackVL.childForceExpandHeight = false;
            stackVL.childAlignment = TextAnchor.LowerCenter;

            var csf = stackGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 4A. High-Contrast Crimson Safety Violation Alert Strip
            _violationBannerGo = CreateGlassPanel(stackGo.transform, "SafetyViolationBanner", spPillActiveWhite, new Color(0.78f, 0.14f, 0.18f, 0.96f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _violationBannerGo.GetComponent<UIImage>().color = new Color(0.78f, 0.14f, 0.18f, 0.96f);
            _violationBannerGo.AddComponent<LayoutElement>().preferredHeight = 62;
            _violationBannerText = CreateCenteredLabel(_violationBannerGo.transform, "", 20, true, ColWhite);
            _violationBannerGo.SetActive(false);

            // 4B. Collapsible Detail Drawer (Generous 244px height, 18-20px fonts!)
            _detailsDrawerGo = CreateGlassPanel(stackGo.transform, "ExpandableDetailsDrawer", spCardGlass, ColGlassBg,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _detailsDrawerGo.AddComponent<LayoutElement>().preferredHeight = 244;
            _detailStageRoots = new GameObject[3];
            _detailStageRoots[0] = BuildStage1Details(_detailsDrawerGo.transform);
            _detailStageRoots[1] = BuildStage2Details(_detailsDrawerGo.transform);
            _detailStageRoots[2] = BuildStage3Details(_detailsDrawerGo.transform);
            _detailsDrawerGo.SetActive(false);

            // 4C. Main Cinema Strip (Generous 26px padding, big crisp 19-25px fonts!)
            var mainStrip = CreateGlassPanel(stackGo.transform, "MainCinemaStrip", spCardGlass, ColGlassBg,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var msCsf = mainStrip.AddComponent<ContentSizeFitter>();
            msCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            AddVLayout(mainStrip, 0, new RectOffset(26, 26, 18, 18));

            _stripStageRoots = new GameObject[3];
            _stripStageRoots[0] = BuildStage1Strip(mainStrip.transform);
            _stripStageRoots[1] = BuildStage2Strip(mainStrip.transform);
            _stripStageRoots[2] = BuildStage3Strip(mainStrip.transform);

            return _hudCanvas;
        }

        // --- SPACIOUS 2-COLUMN EQUIPMENT ENCYCLOPEDIA & SAFETY PRECAUTIONS POPOVER ---
        private GameObject BuildDeviceInfoPopover(Transform parent)
        {
            var card = CreateGlassPanel(parent, "DeviceInfoPopover", spCardGlass, ColGlassBg,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24, -92), new Vector2(1140, 742));
            AddVLayout(card, 14, new RectOffset(24, 24, 20, 20));

            // Header row + Close button
            var hdr = CreateHRow(card.transform, 42, 12);
            hdr.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var title = CreateBlockText(hdr.transform, "高产梳棉机（清梳联核心单机 · ST-03）  设备深度科普与上岗须知", 24, true, ColWhite, 42);
            title.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var closeBtn = CreatePillButton(hdr.transform, "收起 ×", 18, () => SetDeviceInfoOpen(false), out _);
            closeBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 112;

            // 2-Column Main Content Body (Exact 50/50 split -> zero uneven wrapping!)
            var cols = CreateHRow(card.transform, 646, 18);

            // LEFT COLUMN: Encyclopedia + 6-Stage Anatomy & Rated Specs
            var leftCol = new GameObject("LeftEncyclopediaCol", typeof(RectTransform));
            leftCol.transform.SetParent(cols.transform, false);
            var lcLe = leftCol.AddComponent<LayoutElement>();
            lcLe.preferredWidth = 536;
            lcLe.flexibleWidth = 1f;
            AddVLayout(leftCol, 14, new RectOffset(0, 0, 0, 0));

            var sec1 = CreateSubCard(leftCol.transform, 232);
            CreateBlockText(sec1.transform, "一、设备科普（“纺纱厂的心脏”）", 19.5f, true, ColSky, 30);
            var s1Txt = CreateBlockText(sec1.transform,
                "梳棉机位于清花与并条工序之间，承担【开松分梳 → 除杂去短绒 → 均匀混和 → 凝聚成条】四大核心使命：\n" +
                "• 将清花送来的块状棉束彻底分解为单根顺直纤维，消除绝大多数棉结与未成熟死棉；\n" +
                "• 遵循“早落少碎”原则清除棉籽壳、碎叶等杂质（除杂率 ≥ 92%），决定后续细纱条干、强力与布面光洁度。",
                17.5f, false, ColSub, 174);
            s1Txt.alignment = TextAlignmentOptions.TopLeft;
            s1Txt.lineSpacing = 6f;

            var sec2 = CreateSubCard(leftCol.transform, 400);
            CreateBlockText(sec2.transform, "二、六大核心机构与典型技术规格", 19.5f, true, ColMint, 30);
            var s2Txt = CreateBlockText(sec2.transform,
                "• 01 喂棉箱与给棉罗拉：稳压整理均匀棉层，定量握持喂入\n" +
                "• 02 高速刺辊与双除尘刀：高速预分梳，排除大颗粒棉籽壳\n" +
                "• 03 主锡林大滚筒：460–520 r/min，全机核心高速分梳中枢\n" +
                "• 04 26 块回转盖板：紧贴锡林上方 0.18–0.22 mm 精细除棉结\n" +
                "• 05 道夫、喇叭口与大压辊：凝聚薄棉网并压实收束为连续生条\n" +
                "• 06 圈条器与弹簧条筒：偏心斜管螺旋盘叠，托盘弹簧随重下压\n\n" +
                "<color=#38BDF8>【核心指标】</color>产能 80–120 kg/h · 条干 CV ≤ 1.5% · 除结率 ≥ 86% · 温升 ≤ 50°C",
                17.5f, false, ColWhite, 342);
            s2Txt.alignment = TextAlignmentOptions.TopLeft;
            s2Txt.lineSpacing = 6f;

            // RIGHT COLUMN: 5 Critical Safety/Ops Precautions + Precursor Quick-Lookup
            var rightCol = new GameObject("RightSafetyCol", typeof(RectTransform));
            rightCol.transform.SetParent(cols.transform, false);
            var rcLe = rightCol.AddComponent<LayoutElement>();
            rcLe.preferredWidth = 536;
            rcLe.flexibleWidth = 1f;
            AddVLayout(rightCol, 14, new RectOffset(0, 0, 0, 0));

            var sec3 = CreateSubCard(rightCol.transform, 418);
            CreateBlockText(sec3.transform, "三、一线操作与安全注意事项（必读红线）", 19.5f, true, ColAmber, 30);
            var s3Txt = CreateBlockText(sec3.transform,
                "1. 【严防绞伤 · 最高红线】主锡林与刺辊转速高且转动惯量大，严禁运转中开罩或带电手掏喇叭口/大压辊绕花；停机后须等滚筒彻底静止再操作。\n" +
                "2. 【规范启停与生头】先开滤尘负压吸风 → 主锡林升速 → 道夫慢速生头穿入喇叭口与圈条盘 → 观察稳定再切高速。\n" +
                "3. 【看灯听声早处置】绿=运行，黄=预警（满筒/质量异常可提前干预免停机），红=停机报警；闻及金属撞击立即拍 HMI 急停。\n" +
                "4. 【工艺权限隔离】转速与隔距由工艺单锁定，挡车工严禁私改；棉结骤增应及时停机报保全用塞尺校距。\n" +
                "5. 【防火防堵清洁】保持风道与落杂箱畅通并定时清飞花，严防高速针布摩擦积花引发阴燃火情。",
                17.5f, false, ColWhite, 360);
            s3Txt.alignment = TextAlignmentOptions.TopLeft;
            s3Txt.lineSpacing = 6f;

            var sec4 = CreateSubCard(rightCol.transform, 214);
            CreateBlockText(sec4.transform, "四、现场常见故障前兆速查（早发现免事故）", 19.5f, true, ColDanger, 30);
            var s4Txt = CreateBlockText(sec4.transform,
                "• 大压辊顶部弹簧剧烈跳动  →  喇叭口即将堵花或绕花前兆\n" +
                "• 落杂箱落棉泛白（落白花）  →  除尘刀角度或吸风负压失调\n" +
                "• 道夫棉网出现云斑/细黄点  →  喂棉不匀或锡林—盖板隔距偏大\n" +
                "• 顶塔亮黄灯且条筒满度 >90%  →  请及时换空筒防溢条卡斜管",
                17.5f, false, ColSub, 156);
            s4Txt.alignment = TextAlignmentOptions.TopLeft;
            s4Txt.lineSpacing = 8f;

            return card;
        }

        // --- STAGE 1: CINEMA STRIP + HIDDEN DETAILS (BIG READABLE FONTS) ---
        private GameObject BuildStage1Strip(Transform parent)
        {
            var row = CreateHRow(parent, 94, 20);
            row.name = "Strip_Stage1";
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;

            var leftCol = new GameObject("LeftText", typeof(RectTransform));
            leftCol.transform.SetParent(row.transform, false);
            var lLe = leftCol.AddComponent<LayoutElement>();
            lLe.flexibleWidth = 1f;
            lLe.preferredHeight = 92;
            AddVLayout(leftCol, 8, new RectOffset(0, 0, 2, 2));

            var titleLine = CreateHRow(leftCol.transform, 40, 12);
            titleLine.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            _s1StripTitle = CreateBlockText(titleLine.transform, "01 · 喂棉箱与给棉罗拉", 25, true, ColWhite, 40);
            _s1StripTitle.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var detBtn = CreatePillButton(titleLine.transform, "查看详情 +", 17.5f, ToggleDetails, out _s1DetailBtnText);
            detBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 144;

            _s1StripSubtitle = CreateBlockText(leftCol.transform, "", 19.5f, false, ColSub, 40);

            var rightRow = CreateHRow(row.transform, 56, 12);
            var rLe = rightRow.GetComponent<LayoutElement>();
            rLe.flexibleWidth = 0f;
            rLe.preferredWidth = 416;
            _s1PrevBtn = CreatePillButton(rightRow.transform, "← 上一工段", 19, () => SelectStructureStage(_structureIdx - 1), out _);
            _s1NextBtn = CreatePillButton(rightRow.transform, "下一工段 →", 19, OnStructureNextClicked, out _s1NextBtnText);
            SetPillButtonStyle(_s1NextBtn, true, false);

            return row;
        }

        private GameObject BuildStage1Details(Transform parent)
        {
            var wrap = CreateFullChild(parent, "Details_Stage1", 22, 16);
            var hl = wrap.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 20;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;

            var left = new GameObject("LeftCol", typeof(RectTransform));
            left.transform.SetParent(wrap.transform, false);
            AddVLayout(left, 10, new RectOffset(0, 0, 0, 0));
            CreateBlockText(left.transform, "快速跳转工段 & 物料流向", 18.5f, true, ColSky, 26);

            var gridGo = new GameObject("Grid6", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(left.transform, false);
            gridGo.AddComponent<LayoutElement>().preferredHeight = 104;
            var g = gridGo.GetComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(204, 46);
            g.spacing = new Vector2(10, 10);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;

            _s1StepChips = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                int cap = i;
                _s1StepChips[i] = CreatePillButton(gridGo.transform, StructureStages[i].shortName, 17.5f, () => SelectStructureStage(cap), out _);
            }
            var flowBox = CreateSubCard(left.transform, 52);
            _s1DetailFlow = CreateBlockText(flowBox.transform, "", 18.5f, false, ColWhite, 34);
            _s1DetailFlow.alignment = TextAlignmentOptions.Center;

            var right = new GameObject("RightCol", typeof(RectTransform));
            right.transform.SetParent(wrap.transform, false);
            AddVLayout(right, 8, new RectOffset(0, 0, 0, 0));
            CreateBlockText(right.transform, "结构作用与岗位安全红线", 18.5f, true, ColAmber, 26);
            _s1DetailSummary = CreateBlockText(right.transform, "", 18, false, ColSub, 56);
            _s1DetailSafety = CreateBlockText(right.transform, "", 18, false, ColWhite, 108);

            return wrap;
        }

        // --- STAGE 2: CINEMA STRIP + HIDDEN FAULT DETAILS (BIG READABLE FONTS) ---
        private GameObject BuildStage2Strip(Transform parent)
        {
            var row = CreateHRow(parent, 94, 18);
            row.name = "Strip_Stage2";
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;

            var leftCol = new GameObject("LeftText", typeof(RectTransform));
            leftCol.transform.SetParent(row.transform, false);
            var lLe = leftCol.AddComponent<LayoutElement>();
            lLe.flexibleWidth = 1f;
            lLe.preferredHeight = 92;
            AddVLayout(leftCol, 8, new RectOffset(0, 0, 2, 2));

            var titleLine = CreateHRow(leftCol.transform, 40, 12);
            titleLine.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            _s2StripTitle = CreateBlockText(titleLine.transform, "● 红灯报警 · 喇叭口堵花", 23, true, ColDanger, 40);
            _s2StripTitle.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var detBtn = CreatePillButton(titleLine.transform, "工况 / 详情 +", 17.5f, ToggleDetails, out _s2DetailBtnText);
            detBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 152;

            _s2StripPrompt = CreateBlockText(leftCol.transform, "", 19, false, ColSub, 40);

            var rightRow = CreateHRow(row.transform, 56, 10);
            var rLe = rightRow.GetComponent<LayoutElement>();
            rLe.flexibleWidth = 0f;
            rLe.preferredWidth = 576;
            _s2StopBtn = CreatePillButton(rightRow.transform, "停机断电", 18.5f, OnFaultActionStopOrRestart, out _s2StopText);
            _s2ClearBtn = CreatePillButton(rightRow.transform, "清理堵花", 18.5f, OnFaultActionPrimaryClearOrInspect, out _s2ClearText);
            _s2RecoverBtn = CreatePillButton(rightRow.transform, "慢速生头", 18.5f, OnFaultActionSecondaryRecover, out _s2RecoverText);
            _s2AdvanceBtn = CreatePillButton(rightRow.transform, "下工况 B →", 18.5f, OnFaultAdvanceClicked, out _s2AdvanceText);

            return row;
        }

        private GameObject BuildStage2Details(Transform parent)
        {
            var wrap = CreateFullChild(parent, "Details_Stage2", 22, 16);
            var hl = wrap.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 20;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;

            var left = new GameObject("LeftCol", typeof(RectTransform));
            left.transform.SetParent(wrap.transform, false);
            AddVLayout(left, 10, new RectOffset(0, 0, 0, 0));

            var scRow = CreateHRow(left.transform, 46, 10);
            _s2ScenarioBtns = new Button[3];
            _s2ScenarioBtns[0] = CreatePillButton(scRow.transform, "A. 堵花清花", 17.5f, () => StartFaultScenario(FaultScenarioType.CalenderJam, true), out _);
            _s2ScenarioBtns[1] = CreatePillButton(scRow.transform, "B. 棉结校距", 17.5f, () => StartFaultScenario(FaultScenarioType.NepGaugeDrift, true), out _);
            _s2ScenarioBtns[2] = CreatePillButton(scRow.transform, "C. 满筒溢条", 17.5f, () => StartFaultScenario(FaultScenarioType.CanOverflow, false), out _);

            var stRow = CreateHRow(left.transform, 44, 10);
            _s2PrecursorBtn = CreatePillButton(stRow.transform, "前兆预警态 (黄灯)", 17, () => StartFaultScenario(_faultScenario, false), out _);
            _s2AlarmBtn = CreatePillButton(stRow.transform, "故障报警态 (红灯)", 17, () => StartFaultScenario(_faultScenario, true), out _);
            CreatePillButton(stRow.transform, "重置工况", 17, () => StartFaultScenario(_faultScenario, _faultEscalatedToRed), out _);

            var monCard = CreateSubCard(left.transform, 88);
            CreateBlockText(monCard.transform, "HMI 实时看板（工艺单锁定 · 只读）", 16.5f, true, ColSub, 24);
            var monRow = CreateHRow(monCard.transform, 38, 10);
            _s2MonRpm = CreateMonitorChip(monRow.transform, "锡林 500r", ColMint);
            _s2MonDoffer = CreateMonitorChip(monRow.transform, "道夫 100m", ColMint);
            _s2MonGauge = CreateMonitorChip(monRow.transform, "隔距 0.20mm", ColMint);

            var right = new GameObject("RightCol", typeof(RectTransform));
            right.transform.SetParent(wrap.transform, false);
            AddVLayout(right, 8, new RectOffset(0, 0, 0, 0));
            _s2DetailFeedback = CreateBlockText(right.transform, "排障进度：0/3 工况 · 安全 0 违章", 18.5f, true, ColSky, 28);
            _s2StepTexts = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
                _s2StepTexts[i] = CreateBlockText(right.transform, "", 18.5f, false, ColWhite, 36);

            return wrap;
        }

        // --- STAGE 3: BIG, EFFORTLESSLY READABLE QUIZ STRIP (DIRECT FIX FOR IMAGE 3!) ---
        private GameObject BuildStage3Strip(Transform parent)
        {
            var col = new GameObject("Strip_Stage3", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            col.AddComponent<LayoutElement>().preferredHeight = 142;
            AddVLayout(col, 14, new RectOffset(0, 0, 2, 2));

            var topLine = CreateHRow(col.transform, 48, 12);
            topLine.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            _s3StripQuestion = CreateBlockText(topLine.transform, "", 23.5f, true, ColWhite, 46);
            _s3StripQuestion.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var detBtn = CreatePillButton(topLine.transform, "剖析 / 成绩 +", 18, ToggleDetails, out _s3DetailBtnText);
            detBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 158;
            var nextBtn = CreatePillButton(topLine.transform, "下一题 →", 18.5f, OnQuizNextClicked, out _s3NextBtnText);
            nextBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 156;
            SetPillButtonStyle(nextBtn, true, false);

            var optRow = CreateHRow(col.transform, 76, 14);
            _s3OptBtns = new Button[3];
            _s3OptTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                int cap = i;
                _s3OptBtns[i] = CreatePillButton(optRow.transform, "", 19.5f, () => SubmitQuizChoice(cap), out _s3OptTexts[i]);
            }

            return col;
        }

        private GameObject BuildStage3Details(Transform parent)
        {
            var wrap = CreateFullChild(parent, "Details_Stage3", 22, 16);
            var hl = wrap.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 20;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;

            var left = CreateSubCard(wrap.transform, 200);
            var lLe = left.GetComponent<LayoutElement>();
            lLe.preferredWidth = 640;
            lLe.flexibleWidth = 1.0f;
            CreateBlockText(left.transform, "安全与工艺原理解析", 19.5f, true, ColAmber, 30);
            _s3DetailExplain = CreateBlockText(left.transform, "", 19, false, ColWhite, 142);

            var right = CreateSubCard(wrap.transform, 200);
            var rLe = right.GetComponent<LayoutElement>();
            rLe.preferredWidth = 640;
            rLe.flexibleWidth = 1.0f;
            CreateBlockText(right.transform, "上岗安全准入评估单", 19.5f, true, ColMint, 30);
            _s3DetailScore = CreateBlockText(right.transform, "", 19, true, ColWhite, 142);

            return wrap;
        }

        // --- UI PRIMITIVE HELPERS ---
        private static GameObject CreateFullChild(Transform parent, string name, float padX, float padY)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
            return go;
        }

        private static VerticalLayoutGroup AddVLayout(GameObject go, float spacing, RectOffset pad)
        {
            var vl = go.AddComponent<VerticalLayoutGroup>();
            vl.padding = pad;
            vl.spacing = spacing;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            return vl;
        }

        private TextMeshProUGUI CreateMonitorChip(Transform parent, string text, Color col)
        {
            var chip = new GameObject("MonChip", typeof(RectTransform), typeof(UIImage));
            chip.transform.SetParent(parent, false);
            chip.GetComponent<UIImage>().color = new Color(0.06f, 0.11f, 0.19f, 0.92f);
            var lbl = CreateCenteredLabel(chip.transform, text, 17, true, col);
            lbl.enableWordWrapping = false;
            return lbl;
        }

        private GameObject CreateGlassPanel(Transform parent, string name, Sprite sp, Color fallbackCol,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UIImage));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<UIImage>();
            if (sp != null) { img.sprite = sp; img.type = UIImage.Type.Sliced; img.color = Color.white; }
            else img.color = fallbackCol;
            return go;
        }

        private GameObject CreateSubCard(Transform parent, float height)
        {
            var go = new GameObject("SubCard", typeof(RectTransform), typeof(UIImage));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var img = go.GetComponent<UIImage>();
            if (spSubcard != null) { img.sprite = spSubcard; img.type = UIImage.Type.Sliced; img.color = Color.white; }
            else img.color = ColSubcardBg;
            AddVLayout(go, 6, new RectOffset(14, 14, 9, 9));
            return go;
        }

        private GameObject CreateHRow(Transform parent, float height, float spacing)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var hl = go.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = spacing;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;
            hl.childAlignment = TextAnchor.MiddleCenter;
            return go;
        }

        private TextMeshProUGUI CreateBlockText(Transform parent, string text, float fontSize, bool bold, Color color, float preferredH)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = preferredH;
            var tmp = ConfigureCrispTmp(go.GetComponent<TextMeshProUGUI>(), text, fontSize, bold, color);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        private TextMeshProUGUI CreateCenteredLabel(Transform parent, string text, float fontSize, bool bold, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 4);
            rt.offsetMax = new Vector2(-12, -4);
            var tmp = ConfigureCrispTmp(go.GetComponent<TextMeshProUGUI>(), text, fontSize, bold, color);
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private TextMeshProUGUI ConfigureCrispTmp(TextMeshProUGUI tmp, string text, float fontSize, bool bold, Color color)
        {
            var chosenFont = (bold && uiFontBold != null) ? uiFontBold : uiFont;
            if (chosenFont != null) tmp.font = chosenFont;

            // Use razor-sharp custom cloned material (_FaceDilate = -0.025, _Sharpness = 0.42)
            // and NEVER stack synthetic FontStyles.Bold onto an already-Bold Chinese SDF font!
            var crispMat = (bold && _crispBoldMat != null) ? _crispBoldMat : _crispRegularMat;
            if (crispMat != null) tmp.fontSharedMaterial = crispMat;

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Normal; // Bold weight already comes from CNC_Sans_Bold_SDF!
            tmp.characterSpacing = 1.0f;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            tmp.extraPadding = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button CreatePillButton(Transform parent, string label, float fontSize, Action onClick, out TextMeshProUGUI textComp)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(UIImage), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<UIImage>();
            if (spPillIdle != null) { img.sprite = spPillIdle; img.type = UIImage.Type.Sliced; img.color = Color.white; }
            else img.color = ColIdleBtn;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            textComp = CreateCenteredLabel(go.transform, label, fontSize, true, ColWhite);
            return btn;
        }

        private void SetPillButtonStyle(Button btn, bool active, bool done)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UIImage>();
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (active)
            {
                if (spPillActiveSky != null) { img.sprite = spPillActiveSky; img.type = UIImage.Type.Sliced; img.color = Color.white; }
                else img.color = ColSky;
                if (txt != null) txt.color = ColWhite;
            }
            else if (done)
            {
                if (spPillGreen != null) { img.sprite = spPillGreen; img.type = UIImage.Type.Sliced; img.color = Color.white; }
                else img.color = new Color(0.10f, 0.44f, 0.34f, 0.92f);
                if (txt != null) txt.color = ColWhite;
            }
            else
            {
                if (spPillIdle != null) { img.sprite = spPillIdle; img.type = UIImage.Type.Sliced; img.color = Color.white; }
                else img.color = ColIdleBtn;
                if (txt != null) txt.color = ColSub;
            }
        }

        private static void SetCardSprite(UIImage img, Sprite sp, Color fallback)
        {
            if (img == null) return;
            if (sp != null) { img.sprite = sp; img.type = UIImage.Type.Sliced; img.color = Color.white; }
            else { img.sprite = null; img.color = fallback; }
        }

        private static void SetQuizOptionColor(Button btn, Sprite sp, Color fallback)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UIImage>();
            if (sp != null) { img.sprite = sp; img.type = UIImage.Type.Sliced; img.color = Color.white; }
            else img.color = fallback;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = ColWhite;
        }
    }
}
