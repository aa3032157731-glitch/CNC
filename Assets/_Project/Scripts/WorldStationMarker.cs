using TMPro;
using UnityEngine;
using UIImage = UnityEngine.UI.Image;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Floating 3D billboard station nameplate with distance-adaptive scaling and live status dot.
    /// </summary>
    public class WorldStationMarker : MonoBehaviour
    {
        public int stationIndex;
        public TextMeshProUGUI codeText;
        public TextMeshProUGUI nameText;
        public UIImage statusDot;
        public CanvasGroup canvasGroup;

        private Camera _cam;
        private Vector3 _baseScale = Vector3.one * 0.042f;

        private void Start()
        {
            _cam = Camera.main ?? (CameraRigManager.Instance != null ? CameraRigManager.Instance.mainCamera : null);
            _baseScale = transform.localScale;
        }

        private void LateUpdate()
        {
            var sim = SimulationState.Instance;
            if (canvasGroup != null && sim != null)
            {
                bool visible = sim.showWorldMarkers;
                canvasGroup.alpha = visible ? 1f : 0f;
                if (!visible) return;
            }

            if (_cam == null) _cam = Camera.main ?? (CameraRigManager.Instance != null ? CameraRigManager.Instance.mainCamera : null);
            if (_cam == null) return;

            // Billboard facing camera
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position, Vector3.up);

            // Distance-compensated readable scale
            float dist = Vector3.Distance(_cam.transform.position, transform.position);
            float factor = Mathf.Clamp(dist / 62f, 0.48f, 1.85f);
            transform.localScale = _baseScale * factor;

            // Fade smoothly if very close (< 10m overhead) or extremely far (> 220m)
            if (canvasGroup != null)
            {
                float farAlpha = Mathf.Clamp01(1f - (dist - 210f) / 100f);
                float nearAlpha = Mathf.Clamp01((dist - 7f) / 7f);
                canvasGroup.alpha = farAlpha * nearAlpha;
            }

            if (statusDot != null && sim != null && stationIndex >= 0 && stationIndex < sim.stationRunning.Length)
            {
                bool run = sim.isRunning && sim.stationRunning[stationIndex];
                bool sel = sim.selectedStationIndex == stationIndex;
                statusDot.color = sel
                    ? new Color(0f, 0.92f, 1f)
                    : (run ? new Color(0.16f, 0.92f, 0.48f) : new Color(0.98f, 0.68f, 0.14f));
            }
        }
    }
}
