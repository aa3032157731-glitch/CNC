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
            _cam = Camera.main;
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

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            // Billboard facing camera
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position, Vector3.up);

            // Distance-compensated readable scale
            float dist = Vector3.Distance(_cam.transform.position, transform.position);
            float factor = Mathf.Clamp(dist / 55f, 0.55f, 2.6f);
            transform.localScale = _baseScale * factor;

            // Fade smoothly if extremely far (> 280m) or behind camera
            if (canvasGroup != null)
            {
                float alpha = Mathf.Clamp01(1f - (dist - 220f) / 100f);
                canvasGroup.alpha = alpha;
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
