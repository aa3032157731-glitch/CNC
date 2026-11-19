using System.Collections.Generic;
using UnityEngine;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Animates workshop AGVs, inspection robotic arms, and floor holographic rings
    /// synchronized with SimulationState speed and per-station running toggles.
    /// </summary>
    public class WorkshopAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class AgvTrackItem
        {
            public Transform agvTransform;
            public Vector3 homePos;
            public Quaternion homeRot;
            public float phaseOffset;
            public float patrolLength = 26f;
            public float laneHalfWidth = 3.5f;
        }

        public List<AgvTrackItem> agvItems = new List<AgvTrackItem>();
        public List<Transform> inspectionArms = new List<Transform>();
        public List<Transform> floorRingPads = new List<Transform>();

        private readonly List<Quaternion> _armHomeRots = new List<Quaternion>();
        private float _agvTime;
        private float _armTime;

        private void Start()
        {
            for (int i = 0; i < inspectionArms.Count; i++)
                _armHomeRots.Add(inspectionArms[i] != null ? inspectionArms[i].localRotation : Quaternion.identity);
        }

        private void Update()
        {
            var sim = SimulationState.Instance;
            float speed = (sim != null && sim.isRunning) ? sim.simSpeed : 0f;
            bool warehouseRun = sim == null || sim.stationRunning[8];

            // 1. AGV Smooth Patrol along AGV Corridor (Z ≈ 266 in Workshop 1)
            if (speed > 0f && warehouseRun)
                _agvTime += Time.deltaTime * speed;

            for (int i = 0; i < agvItems.Count; i++)
            {
                var item = agvItems[i];
                if (item.agvTransform == null) continue;
                // Smooth sinusoidal shuttle along Z/X corridor without colliding with neighbor AGVs
                float cycle = _agvTime * 0.35f + item.phaseOffset;
                float offsetZ = Mathf.Sin(cycle) * item.patrolLength * 0.5f;
                float dir = Mathf.Cos(cycle);
                Vector3 pos = item.homePos + new Vector3(0f, 0f, offsetZ);
                item.agvTransform.position = pos;
                // Gentle orientation facing travel direction
                if (Mathf.Abs(dir) > 0.05f)
                {
                    Quaternion targetRot = item.homeRot * Quaternion.Euler(0f, dir > 0f ? 0f : 180f, 0f);
                    item.agvTransform.rotation = Quaternion.Slerp(item.agvTransform.rotation, targetRot, Time.deltaTime * 4f);
                }
            }

            // 2. Inspection Robotic Arms pick-and-place sweep
            if (speed > 0f && warehouseRun)
                _armTime += Time.deltaTime * speed;

            for (int i = 0; i < inspectionArms.Count; i++)
            {
                if (inspectionArms[i] == null) continue;
                float angle = Mathf.Sin(_armTime * 1.4f + i * 1.1f) * 28f;
                inspectionArms[i].localRotation = _armHomeRots[i] * Quaternion.Euler(0f, angle, 0f);
            }

            // 3. Subtle rotation on holographic station floor pads
            for (int i = 0; i < floorRingPads.Count; i++)
            {
                if (floorRingPads[i] != null)
                    floorRingPads[i].Rotate(Vector3.up, 18f * Time.unscaledDeltaTime, Space.World);
            }
        }
    }
}
