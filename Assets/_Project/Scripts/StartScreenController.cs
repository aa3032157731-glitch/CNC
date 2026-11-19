using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Modernized Industrial Digital Twin Start Screen controller for Play.unity.
    /// Supports direct launch into Dashboard or Teaching Tour mode, Desktop Quality Settings,
    /// and smooth asynchronous scene loading with progress bar.
    /// </summary>
    public class StartScreenController : MonoBehaviour
    {
        [Header("Animated Background")]
        public RectTransform heroBackground;

        [Header("Action Buttons")]
        public Button btnEnterDashboard;
        public Button btnEnterTeaching;
        public Button btnOpenSettings;
        public Button btnQuit;

        [Header("Style A Quality Pill Sprites")]
        public Sprite spPillActiveWhite;
        public Sprite spPillIdle;

        [Header("Settings Modal")]
        public GameObject settingsModal;
        public Button btnQualityMed;
        public Button btnQualityHigh;
        public Button btnQualityUltra;
        public Slider sensitivitySlider;
        public TextMeshProUGUI sensitivityValueText;
        public Button btnCloseSettings;

        [Header("Async Loading Overlay")]
        public GameObject loadingOverlay;
        public UIImage loadingBarFill;
        public TextMeshProUGUI loadingPercentText;
        public TextMeshProUGUI loadingTipText;

        private bool _isLoading;
        private readonly string[] _loadingTips =
        {
            "正在加载 URP 高画质渲染管线与工业 PBR 材质库...",
            "正在初始化全厂 9 大纺纱工艺工段与数字孪生状态引擎...",
            "提示：进入车间后可按 F1 切换鸟瞰全景，F2 切换第一人称漫游，F4 开启屋顶透视。"
        };

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (btnEnterDashboard != null) btnEnterDashboard.onClick.AddListener(() => LaunchWorkshop(AppMode.Dashboard));
            if (btnEnterTeaching != null) btnEnterTeaching.onClick.AddListener(() => LaunchWorkshop(AppMode.TeachingTour));
            if (btnOpenSettings != null) btnOpenSettings.onClick.AddListener(() => SetSettingsVisible(true));
            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(() => SetSettingsVisible(false));
            if (btnQuit != null) btnQuit.onClick.AddListener(QuitApplication);

            if (btnQualityMed != null) btnQualityMed.onClick.AddListener(() => ApplyQuality(2));
            if (btnQualityHigh != null) btnQualityHigh.onClick.AddListener(() => ApplyQuality(4));
            if (btnQualityUltra != null) btnQualityUltra.onClick.AddListener(() => ApplyQuality(5));

            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = AppLaunchContext.MouseSensitivity;
                sensitivitySlider.onValueChanged.AddListener(v =>
                {
                    AppLaunchContext.MouseSensitivity = v;
                    if (sensitivityValueText != null) sensitivityValueText.text = $"{v:0.0}";
                });
                if (sensitivityValueText != null) sensitivityValueText.text = $"{AppLaunchContext.MouseSensitivity:0.0}";
            }

            SetSettingsVisible(false);
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
            RefreshQualityButtonHighlights();
        }

        private void Update()
        {
            // Subtle slow Ken Burns breathe animation on hero background
            if (heroBackground != null)
            {
                float s = 1.04f + 0.03f * Mathf.Sin(Time.unscaledTime * 0.18f);
                heroBackground.localScale = new Vector3(s, s, 1f);
            }
        }

        private void LaunchWorkshop(AppMode mode)
        {
            if (_isLoading) return;
            _isLoading = true;
            AppLaunchContext.InitialMode = mode;
            AppLaunchContext.InitialCameraMode = mode == AppMode.TeachingTour
                ? CameraViewMode.StationFocus
                : CameraViewMode.FirstPersonWalk;
            AppLaunchContext.InitialStationIndex = mode == AppMode.TeachingTour ? 0 : -1;
            StartCoroutine(LoadSceneRoutine(1));
        }

        private IEnumerator LoadSceneRoutine(int buildIndex)
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(true);
            if (loadingTipText != null) loadingTipText.text = _loadingTips[0];

            AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
            op.allowSceneActivation = false;

            float displayed = 0f;
            float elapsed = 0f;
            while (!op.isDone)
            {
                elapsed += Time.unscaledDeltaTime;
                float target = Mathf.Clamp01(op.progress / 0.9f);
                displayed = Mathf.MoveTowards(displayed, target, Time.unscaledDeltaTime * 1.4f);

                if (loadingBarFill != null) loadingBarFill.fillAmount = displayed;
                if (loadingPercentText != null) loadingPercentText.text = $"{Mathf.RoundToInt(displayed * 100f)}%";
                if (loadingTipText != null)
                {
                    int tipIdx = Mathf.Clamp((int)(displayed * _loadingTips.Length), 0, _loadingTips.Length - 1);
                    loadingTipText.text = _loadingTips[tipIdx];
                }

                if (displayed >= 0.99f && op.progress >= 0.9f && elapsed >= 0.45f)
                {
                    op.allowSceneActivation = true;
                }
                yield return null;
            }
        }

        private void SetSettingsVisible(bool visible)
        {
            if (settingsModal != null) settingsModal.SetActive(visible);
        }

        private void ApplyQuality(int levelIndex)
        {
            int clamped = Mathf.Clamp(levelIndex, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(clamped, true);
            RefreshQualityButtonHighlights();
        }

        private void RefreshQualityButtonHighlights()
        {
            int cur = QualitySettings.GetQualityLevel();
            SetBtnStyle(btnQualityMed, cur <= 2);
            SetBtnStyle(btnQualityHigh, cur == 3 || cur == 4);
            SetBtnStyle(btnQualityUltra, cur >= 5);
        }

        private void SetBtnStyle(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UIImage>();
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (img != null)
            {
                if (active && spPillActiveWhite != null) img.sprite = spPillActiveWhite;
                else if (!active && spPillIdle != null) img.sprite = spPillIdle;
                img.color = Color.white;
            }
            if (txt != null)
            {
                txt.color = active ? new Color(0.06f, 0.09f, 0.16f, 1f) : new Color(0.92f, 0.95f, 1.0f, 0.85f);
            }
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
