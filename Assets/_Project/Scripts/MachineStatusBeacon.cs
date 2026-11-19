using UnityEngine;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Controls a 3D industrial 3-color signal tower light mounted at a station.
    /// Green = running, Amber = standby/paused, Cyan pulse = focused/inspecting.
    /// </summary>
    public class MachineStatusBeacon : MonoBehaviour
    {
        public int stationIndex;
        public MeshRenderer greenLens;
        public MeshRenderer amberLens;
        public MeshRenderer redLens;

        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            var sim = SimulationState.Instance;
            if (sim == null) return;

            bool globalRun = sim.isRunning;
            bool stRun = stationIndex >= 0 && stationIndex < sim.stationRunning.Length && sim.stationRunning[stationIndex];
            bool selected = sim.selectedStationIndex == stationIndex;
            float pulse = 0.75f + 0.35f * Mathf.Sin(Time.unscaledTime * 5.0f);

            if (globalRun && stRun)
            {
                Color g = selected ? new Color(0f, 1.0f, 1.0f) : new Color(0.12f, 0.95f, 0.45f);
                SetLens(greenLens, g, g * (selected ? 3.2f * pulse : 2.1f));
                SetLens(amberLens, new Color(0.22f, 0.18f, 0.08f), Color.black);
                SetLens(redLens, new Color(0.22f, 0.08f, 0.08f), Color.black);
            }
            else
            {
                Color a = new Color(1.0f, 0.65f, 0.05f);
                SetLens(greenLens, new Color(0.08f, 0.2f, 0.12f), Color.black);
                SetLens(amberLens, a, a * (2.4f * pulse));
                SetLens(redLens, new Color(0.22f, 0.08f, 0.08f), Color.black);
            }
        }

        private void SetLens(MeshRenderer mr, Color baseCol, Color emission)
        {
            if (mr == null) return;
            mr.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, baseCol);
            _mpb.SetColor(EmissionColorId, emission);
            mr.SetPropertyBlock(_mpb);
        }
    }
}
