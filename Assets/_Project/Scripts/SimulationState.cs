using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.DigitalTwin
{
    public struct EventLogEntry
    {
        public string timestamp;
        public string level; // INFO, WARN, OK
        public string message;
    }

    /// <summary>
    /// Central reactive simulation & digital twin telemetry state hub.
    /// Synchronizes global speed/pause with URP shaders, legacy scripts, and HUD dashboards.
    /// </summary>
    public class SimulationState : MonoBehaviour
    {
        public static SimulationState Instance { get; private set; }

        [Header("Catalog")]
        public WorkshopCatalog catalog;

        [Header("Runtime Simulation Control")]
        public bool isRunning = true;
        public float simSpeed = 1.0f;
        public float simTime { get; private set; }

        [Header("Mode & Selection")]
        public AppMode currentMode = AppMode.Dashboard;
        public int selectedStationIndex = -1;
        public bool showWorldMarkers = true;
        public bool roofCutawayEnabled = false;

        // Per-station states
        public bool[] stationRunning = new bool[9];
        public bool[] stationLearned = new bool[9];
        public bool[] stationQuizPassed = new bool[9];

        // Live KPIs
        public float totalOutputKg { get; private set; } = 1842.5f;
        public int qualifiedCount { get; private set; } = 1246;
        public int unqualifiedCount { get; private set; } = 31;
        public float passRatePct => (qualifiedCount + unqualifiedCount) > 0
            ? (qualifiedCount * 100f / (qualifiedCount + unqualifiedCount))
            : 100f;
        public float oeePct { get; private set; } = 91.4f;
        public int occupiedWarehouseSlots { get; private set; } = 11;
        public bool[] warehouseSlotStates = new bool[16];

        public readonly List<EventLogEntry> eventLogs = new List<EventLogEntry>(32);

        // Events
        public event Action<AppMode> OnModeChanged;
        public event Action<int> OnStationSelected;
        public event Action OnSimulationChanged;
        public event Action OnTelemetryTick;
        public event Action OnLogsChanged;

        private float _telemetryTimer;
        private float _productionTimer;
        private static readonly int SimTimeShaderId = Shader.PropertyToID("_CNC_SimTime");

        public List<StationInfo> Stations
        {
            get
            {
                if (catalog != null && catalog.stations != null && catalog.stations.Count > 0)
                    return catalog.stations;
                if (_fallbackStations == null)
                    _fallbackStations = WorkshopCatalog.CreateDefaultStations();
                return _fallbackStations;
            }
        }
        private List<StationInfo> _fallbackStations;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentMode = AppLaunchContext.InitialMode;
            selectedStationIndex = AppLaunchContext.InitialStationIndex;

            for (int i = 0; i < 9; i++)
                stationRunning[i] = true;

            // Initialize warehouse 4x4 occupancy matrix realistically
            for (int i = 0; i < 16; i++)
                warehouseSlotStates[i] = i < occupiedWarehouseSlots;

            PushLog("OK", "数字孪生引擎初始化完成，URP 高画质渲染已就绪");
            PushLog("INFO", "全厂 9 大纺纱工段与立体仓储已上线联机");
            PushLog("INFO", "AGV 自动搬运编队（6台）进入巡航待命状态");
        }

        private void Update()
        {
            float dt = isRunning ? Time.deltaTime * simSpeed : 0f;
            simTime += dt;
            Shader.SetGlobalFloat(SimTimeShaderId, simTime);

            // Sync with legacy grabber static flag if running
            int runningCount = 0;
            for (int i = 0; i < stationRunning.Length; i++)
                if (stationRunning[i]) runningCount++;

            if (isRunning && runningCount > 0)
            {
                _productionTimer += dt;
                if (_productionTimer >= 1.5f)
                {
                    _productionTimer = 0f;
                    float ratio = runningCount / 9f;
                    totalOutputKg += UnityEngine.Random.Range(0.45f, 0.85f) * ratio;

                    // Simulated inspection throughput synced with factory run rate
                    if (stationRunning[8])
                    {
                        bool qual = UnityEngine.Random.value <= 0.965f;
                        RecordInspectionResult(qual, false);
                    }
                }
            }

            _telemetryTimer += Time.unscaledDeltaTime;
            if (_telemetryTimer >= 0.5f)
            {
                _telemetryTimer = 0f;
                float activeRatio = isRunning ? (runningCount / 9f) : 0f;
                float targetOee = activeRatio * (89.5f + Mathf.Sin(Time.unscaledTime * 0.4f) * 2.4f);
                oeePct = Mathf.Lerp(oeePct, targetOee, 0.35f);
                OnTelemetryTick?.Invoke();
            }
        }

        public void ToggleGlobalRun()
        {
            isRunning = !isRunning;
            PushLog(isRunning ? "OK" : "WARN", isRunning ? "全厂仿真已恢复运行" : "全厂仿真已暂停");
            OnSimulationChanged?.Invoke();
        }

        public void SetSimSpeed(float speed)
        {
            simSpeed = Mathf.Clamp(speed, 0.25f, 4f);
            PushLog("INFO", $"仿真倍速切换为 {simSpeed:0.#}x");
            OnSimulationChanged?.Invoke();
        }

        public void ResetSimulation()
        {
            isRunning = true;
            simSpeed = 1.0f;
            totalOutputKg = 1842.5f;
            qualifiedCount = 1246;
            unqualifiedCount = 31;
            occupiedWarehouseSlots = 11;
            for (int i = 0; i < 9; i++) stationRunning[i] = true;
            for (int i = 0; i < 16; i++) warehouseSlotStates[i] = i < occupiedWarehouseSlots;
            PushLog("OK", "产线仿真参数与统计指标已复位");
            OnSimulationChanged?.Invoke();
            OnTelemetryTick?.Invoke();
        }

        public void ToggleStationRun(int idx)
        {
            if (idx < 0 || idx >= stationRunning.Length) return;
            stationRunning[idx] = !stationRunning[idx];
            var st = Stations[idx];
            PushLog(stationRunning[idx] ? "OK" : "WARN",
                $"[{st.code} {st.shortName}] 远程{(stationRunning[idx] ? "启动运行" : "停机待命")}");
            OnSimulationChanged?.Invoke();
            OnTelemetryTick?.Invoke();
        }

        public void SelectStation(int idx)
        {
            if (idx < -1 || idx >= Stations.Count) return;
            selectedStationIndex = idx;
            if (idx >= 0)
            {
                stationLearned[idx] = true;
                var st = Stations[idx];
                PushLog("INFO", $"聚焦查看工位 [{st.code} {st.name}]");
            }
            OnStationSelected?.Invoke(idx);
        }

        public void SetMode(AppMode mode)
        {
            if (currentMode == mode) return;
            currentMode = mode;
            PushLog("INFO", mode == AppMode.Dashboard ? "切换至【数字孪生监控大屏模式】" : "切换至【教学实训工艺导览模式】");
            OnModeChanged?.Invoke(mode);
        }

        public void MarkQuizPassed(int idx)
        {
            if (idx < 0 || idx >= stationQuizPassed.Length) return;
            stationQuizPassed[idx] = true;
            stationLearned[idx] = true;
            PushLog("OK", $"完成 [{Stations[idx].code} {Stations[idx].shortName}] 工艺随堂考核");
            OnTelemetryTick?.Invoke();
        }

        public void RecordInspectionResult(bool qualified, bool logWarn = true)
        {
            if (qualified)
            {
                qualifiedCount++;
                // Periodically cycle warehouse slots between 10 and 16 to show live inbound/outbound
                if (qualifiedCount % 12 == 0)
                {
                    int slot = UnityEngine.Random.Range(0, 16);
                    warehouseSlotStates[slot] = !warehouseSlotStates[slot];
                    int occ = 0;
                    for (int i = 0; i < 16; i++) if (warehouseSlotStates[i]) occ++;
                    if (occ < 8) { warehouseSlotStates[slot] = true; occ++; }
                    occupiedWarehouseSlots = occ;
                }
            }
            else
            {
                unqualifiedCount++;
                if (logWarn || unqualifiedCount % 3 == 0)
                    PushLog("WARN", "ST-09 视觉检测拦截 1 件不良纱筒，已自动分流至剔除线");
            }
        }

        public void SetWarehouseSlotCount(int count)
        {
            occupiedWarehouseSlots = Mathf.Clamp(count, 0, 16);
            for (int i = 0; i < 16; i++)
                warehouseSlotStates[i] = i < occupiedWarehouseSlots;
            OnTelemetryTick?.Invoke();
        }

        public void GetLiveTelemetry(int idx, out float rpm, out float tempC, out float powerKw, out float loadPct)
        {
            var st = Stations[Mathf.Clamp(idx, 0, Stations.Count - 1)];
            bool active = isRunning && stationRunning[idx];
            if (!active)
            {
                rpm = 0f;
                tempC = 26.4f + (idx % 3) * 0.6f;
                powerKw = st.nominalPowerKw * 0.06f;
                loadPct = 0f;
                return;
            }
            float wave = Mathf.Sin(Time.unscaledTime * 1.1f + idx * 1.37f);
            float wave2 = Mathf.Cos(Time.unscaledTime * 0.7f + idx * 2.1f);
            rpm = st.nominalRpm * (0.985f + 0.025f * wave) * Mathf.Clamp(simSpeed, 0.5f, 1.5f);
            tempC = st.nominalTempC + 1.4f * wave2;
            powerKw = st.nominalPowerKw * (0.94f + 0.08f * wave);
            loadPct = Mathf.Clamp(st.nominalLoadPct + 4.5f * wave2, 20f, 98f);
        }

        public int GetLearnedCount()
        {
            int c = 0;
            for (int i = 0; i < stationLearned.Length; i++) if (stationLearned[i]) c++;
            return c;
        }

        public int GetQuizPassedCount()
        {
            int c = 0;
            for (int i = 0; i < stationQuizPassed.Length; i++) if (stationQuizPassed[i]) c++;
            return c;
        }

        public void PushLog(string level, string message)
        {
            eventLogs.Insert(0, new EventLogEntry
            {
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                level = level,
                message = message
            });
            if (eventLogs.Count > 24)
                eventLogs.RemoveAt(eventLogs.Count - 1);
            OnLogsChanged?.Invoke();
        }
    }
}
