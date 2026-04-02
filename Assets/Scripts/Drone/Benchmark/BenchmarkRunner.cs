using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DroneSim.Drone.Flight;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using UnityEngine;
using LegacyInput = UnityEngine.Input;

namespace DroneSim.Drone.Benchmark
{
    /// <summary>
    /// Plays back scripted input maneuvers against the existing stabilized flight stack.
    /// Run structure: pre-roll neutral -> scripted input -> settle neutral.
    /// </summary>
    public class BenchmarkRunner : MonoBehaviour
    {
        private enum RunPhase
        {
            Idle,
            PreRoll,
            Input,
            Settle
        }

        [Serializable]
        private class SessionMetadata
        {
            public string type = "session_metadata";
            public string session_id;
            public string created_utc;
            public string export_directory;
            public string application_version;
            public float fixed_timestep_s;
            public string benchmark_area_origin;
            public string benchmark_spawn_offset;
            public bool benchmark_dedicated_area_enabled;
            public string maneuver_library;
            public BenchmarkSettingsSnapshot benchmark_settings;
            public ControllerSettingsSnapshot controller_settings;
            public List<ModeConfigSnapshot> mode_configs;
        }

        [Serializable]
        private class BenchmarkSettingsSnapshot
        {
            public bool auto_load_from_resources;
            public string resources_folder;
            public float default_pre_roll_s;
            public float default_settle_s;
            public bool run_protocol_in_order;
            public bool reset_mode_between_runs;
            public int maneuver_count;
            public int default_protocol_maneuver_count;
            public List<string> default_protocol_categories;
        }

        [Serializable]
        private class ControllerSettingsSnapshot
        {
            public float gravity_cancel_multiplier;
            public float global_forward_accel_limit;
            public float global_lateral_accel_limit;
            public float global_vertical_accel_limit;
            public float braking_input_deadband;
        }

        [Serializable]
        private class ModeConfigSnapshot
        {
            public string config_asset;
            public string mode;
            public float max_forward_speed;
            public float max_lateral_speed;
            public float forward_acceleration;
            public float lateral_acceleration;
            public float forward_stop_strength;
            public float lateral_stop_strength;
            public float max_climb_speed;
            public float max_descent_speed;
            public float vertical_acceleration;
            public float max_yaw_rate_degrees;
            public float yaw_catch_up_speed;
            public float tilt_limit_degrees;
            public float tilt_smoothing;
        }

        [Header("References")]
        [SerializeField] private DroneInputReader inputReader;
        [SerializeField] private DronePhysicsBody physicsBody;
        [SerializeField] private DJIStyleFlightController controller;
        [SerializeField] private BenchmarkEnvironmentController environmentController;

        [Header("Maneuver library")]
        [SerializeField] private List<ManeuverDefinition> maneuvers = new List<ManeuverDefinition>();
        [Min(0)] [SerializeField] private int selectedManeuverIndex;

        [Header("Run settings")]
        [SerializeField] private bool autoLoadFromResources = true;
        [SerializeField] private string resourcesFolder = "Benchmarks";
        [SerializeField] private bool autoStartOnPlay;
        [SerializeField] private KeyCode runManeuverKey = KeyCode.F8;
        [SerializeField] private KeyCode cycleManeuverKey = KeyCode.F7;
        [SerializeField] private KeyCode runFullProtocolKey = KeyCode.F9;
        [SerializeField] private string exportDirectoryName = "BenchmarkRuns";
        [SerializeField, Min(0f)] private float defaultPreRollDuration = 1.5f;
        [SerializeField, Min(0f)] private float defaultSettleDuration = 1.5f;
        [SerializeField] private bool runProtocolInOrder = true;
        [SerializeField] private bool resetRequestedModeToManeuver = true;

        [Header("Debug window")]
        [SerializeField] private bool showDebugWindow = true;
        [SerializeField] private bool startCollapsed;
        [SerializeField] private Rect defaultWindowRect = new Rect(16f, 312f, 460f, 170f);

        [Header("Debug gizmos")]
        [SerializeField] private bool showBenchmarkOriginGizmo = true;
        [SerializeField] private Color benchmarkOriginColor = new Color(0.2f, 0.95f, 1f, 0.85f);
        [SerializeField, Min(0.1f)] private float benchmarkOriginRadius = 0.5f;

        private readonly BenchmarkTelemetryRecorder recorder = new BenchmarkTelemetryRecorder();
        private readonly Queue<int> queuedProtocolIndices = new Queue<int>();

        private ManeuverDefinition activeManeuver;
        private float runElapsedTime;
        private float phaseElapsedTime;
        private RunPhase runPhase = RunPhase.Idle;
        private bool isRunning;
        private bool isProtocolRunActive;
        private int runCounter;
        private Rect windowRect;
        private bool isCollapsed;
        private string sessionId;
        private string sessionDirectoryPath;
        private float activePreRollDuration;
        private float activeSettleDuration;
        private string activeRunSource = "manual";

        public bool IsRunning => isRunning;
        public string CurrentManeuverName => GetSelectedManeuver() != null ? GetSelectedManeuver().maneuverName : "None";
        public int SelectedManeuverIndex => selectedManeuverIndex;

        public void Initialize(DroneInputReader reader, DronePhysicsBody body, DJIStyleFlightController flightController, BenchmarkEnvironmentController benchmarkEnvironmentController = null)
        {
            inputReader = reader;
            physicsBody = body;
            controller = flightController;
            environmentController = benchmarkEnvironmentController ?? environmentController;
        }

        private void Awake()
        {
            if (inputReader == null || physicsBody == null || controller == null)
            {
                inputReader ??= FindFirstObjectByType<DroneInputReader>();
                physicsBody ??= FindFirstObjectByType<DronePhysicsBody>();
                controller ??= FindFirstObjectByType<DJIStyleFlightController>();
            }

            environmentController ??= FindFirstObjectByType<BenchmarkEnvironmentController>();

            if (autoLoadFromResources && maneuvers.Count == 0)
            {
                LoadManeuversFromResources();
            }

            windowRect = defaultWindowRect;
            isCollapsed = startCollapsed;
        }

        private void Start()
        {
            EnsureSessionInitialized();

            if (autoStartOnPlay)
            {
                StartSelectedManeuver();
            }
        }

        private void Update()
        {
            if (LegacyInput.GetKeyDown(cycleManeuverKey))
            {
                SelectNextManeuver();
            }

            if (LegacyInput.GetKeyDown(runManeuverKey))
            {
                if (isRunning)
                {
                    StopRun();
                }
                else
                {
                    StartSelectedManeuver();
                }
            }

            if (LegacyInput.GetKeyDown(runFullProtocolKey) && !isRunning)
            {
                StartFullProtocol();
            }
        }

        private void FixedUpdate()
        {
            if (!isRunning || activeManeuver == null || inputReader == null || physicsBody == null)
            {
                return;
            }

            runElapsedTime += Time.fixedDeltaTime;
            phaseElapsedTime += Time.fixedDeltaTime;

            BenchmarkInputFrame benchmarkInput = EvaluateInputForCurrentPhase();
            inputReader.SetExternalInputFrame(benchmarkInput.ToDroneInputFrame());
            recorder.Record(runElapsedTime, physicsBody, benchmarkInput, runPhase.ToString().ToLowerInvariant());

            AdvanceRunPhaseIfNeeded();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showBenchmarkOriginGizmo)
            {
                return;
            }

            ManeuverDefinition maneuver = GetSelectedManeuver();
            if (maneuver == null)
            {
                return;
            }

            Vector3 origin = environmentController != null
                ? environmentController.GetBenchmarkSpawnPosition(maneuver.initialPosition)
                : maneuver.initialPosition;

            Gizmos.color = benchmarkOriginColor;
            Gizmos.DrawSphere(origin, benchmarkOriginRadius);

            Vector3 heading = Quaternion.Euler(0f, maneuver.initialYawDegrees, 0f) * Vector3.forward;
            Gizmos.DrawLine(origin, origin + heading * 2f);
        }

        private void OnGUI()
        {
            if (!showDebugWindow)
            {
                return;
            }

            windowRect = DroneSim.Drone.UI.DebugWindowLayoutUtility.ClampToScreen(windowRect);
            float targetHeight = isCollapsed ? DroneSim.Drone.UI.DebugWindowLayoutUtility.HeaderHeight + 6f : defaultWindowRect.height;
            windowRect.height = Mathf.Max(DroneSim.Drone.UI.DebugWindowLayoutUtility.HeaderHeight + 6f, targetHeight);
            windowRect = GUI.Window(1304, windowRect, DrawDebugWindow, "");
            windowRect = DroneSim.Drone.UI.DebugWindowLayoutUtility.ClampToScreen(windowRect);
        }

        private void DrawDebugWindow(int _)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Benchmark Runner", GUILayout.Height(DroneSim.Drone.UI.DebugWindowLayoutUtility.HeaderHeight));
            if (GUILayout.Button(isCollapsed ? "+" : "-", GUILayout.Width(26f), GUILayout.Height(20f)))
            {
                isCollapsed = !isCollapsed;
            }
            GUILayout.EndHorizontal();

            if (!isCollapsed)
            {
                GUILayout.Label($"Benchmark: {CurrentManeuverName} (#{selectedManeuverIndex + 1}/{Mathf.Max(1, maneuvers.Count)})");
                GUILayout.Label($"Run Key: {runManeuverKey} | Cycle Key: {cycleManeuverKey} | Full Protocol Key: {runFullProtocolKey}");

                if (isRunning)
                {
                    string status = $"Status: Running {runPhase} ({phaseElapsedTime:F2}s phase, {runElapsedTime:F2}s total)";
                    if (runPhase == RunPhase.Input)
                    {
                        status += $" / Input {activeManeuver.Duration:F2}s";
                    }
                    GUILayout.Label(status);
                }
                else
                {
                    GUILayout.Label("Status: Idle (manual input active)");
                }

                GUILayout.Label($"Durations => Pre-roll: {GetManeuverPreRollDuration(GetSelectedManeuver()):F2}s, Input: {GetSelectedManeuver()?.Duration ?? 0f:F2}s, Settle: {GetManeuverSettleDuration(GetSelectedManeuver()):F2}s");
                GUILayout.Label($"Session: {sessionId ?? "(pending)"}, Runs: {runCounter}, Protocol Queue: {queuedProtocolIndices.Count}");
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 34f, DroneSim.Drone.UI.DebugWindowLayoutUtility.HeaderHeight));
        }

        public void StartSelectedManeuver()
        {
            ManeuverDefinition maneuver = GetSelectedManeuver();
            StartManeuver(maneuver, false);
        }

        public void StartFullProtocol()
        {
            List<int> indices = BuildProtocolOrder();
            if (indices.Count == 0)
            {
                Debug.LogWarning("BenchmarkRunner cannot start protocol because no protocol maneuvers are configured.");
                return;
            }

            queuedProtocolIndices.Clear();
            for (int i = 0; i < indices.Count; i++)
            {
                queuedProtocolIndices.Enqueue(indices[i]);
            }

            isProtocolRunActive = queuedProtocolIndices.Count > 0;
            StartNextQueuedManeuver();
        }

        public void StopRun()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            runPhase = RunPhase.Idle;
            inputReader.SetExternalInputEnabled(false);
            environmentController?.SetBenchmarkIsolationActive(false);

            EnsureSessionInitialized();

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            int runNumber = runCounter + 1;
            string runLabel = $"{timestamp}_run{runNumber:000}";
            runCounter++;

            BenchmarkCsvExporter.RunContext context = new BenchmarkCsvExporter.RunContext(
                sessionId,
                sessionDirectoryPath,
                runLabel,
                runNumber,
                activePreRollDuration,
                activeManeuver != null ? activeManeuver.Duration : 0f,
                activeSettleDuration);
            recorder.ExportCsv(sessionDirectoryPath, context, activeManeuver);
            WriteRunManifestEntry(context, activeManeuver);

            Debug.Log($"BenchmarkRunner finished '{activeManeuver.maneuverName}'. CSV saved to {sessionDirectoryPath}");
            activeManeuver = null;

            if (isProtocolRunActive)
            {
                StartNextQueuedManeuver();
            }
        }

        private void StartManeuver(ManeuverDefinition maneuver, bool fromProtocolQueue)
        {
            if (maneuver == null || inputReader == null || physicsBody == null || controller == null)
            {
                Debug.LogWarning("BenchmarkRunner could not start maneuver. Missing setup or maneuver list.");
                isProtocolRunActive = false;
                queuedProtocolIndices.Clear();
                return;
            }

            activeManeuver = maneuver;
            runElapsedTime = 0f;
            phaseElapsedTime = 0f;
            runPhase = RunPhase.PreRoll;
            isRunning = true;

            activePreRollDuration = GetManeuverPreRollDuration(activeManeuver);
            activeSettleDuration = GetManeuverSettleDuration(activeManeuver);

            environmentController?.SetBenchmarkIsolationActive(true);
            ResetDroneState(activeManeuver);

            recorder.BeginRun();
            inputReader.SetExternalInputEnabled(true);
            BenchmarkInputFrame neutralFrame = BenchmarkInputFrame.Neutral(activeManeuver.flightMode);
            inputReader.SetExternalInputFrame(neutralFrame.ToDroneInputFrame());
            activeRunSource = fromProtocolQueue ? "full_protocol" : "manual";

            string launchKind = fromProtocolQueue ? "protocol" : "manual";
            Debug.Log($"BenchmarkRunner started '{activeManeuver.maneuverName}' ({launchKind}) with pre-roll {activePreRollDuration:F2}s, input {activeManeuver.Duration:F2}s, settle {activeSettleDuration:F2}s in {activeManeuver.flightMode} mode.");
        }

        private void StartNextQueuedManeuver()
        {
            if (queuedProtocolIndices.Count == 0)
            {
                isProtocolRunActive = false;
                return;
            }

            int nextIndex = queuedProtocolIndices.Dequeue();
            selectedManeuverIndex = Mathf.Clamp(nextIndex, 0, Mathf.Max(0, maneuvers.Count - 1));
            StartManeuver(GetSelectedManeuver(), true);
        }

        private BenchmarkInputFrame EvaluateInputForCurrentPhase()
        {
            if (runPhase == RunPhase.Input)
            {
                return activeManeuver.Evaluate(phaseElapsedTime);
            }

            return BenchmarkInputFrame.Neutral(activeManeuver.flightMode);
        }

        private void AdvanceRunPhaseIfNeeded()
        {
            switch (runPhase)
            {
                case RunPhase.PreRoll:
                    if (phaseElapsedTime >= activePreRollDuration)
                    {
                        runPhase = RunPhase.Input;
                        phaseElapsedTime = 0f;
                    }
                    break;
                case RunPhase.Input:
                    if (phaseElapsedTime >= activeManeuver.Duration)
                    {
                        runPhase = RunPhase.Settle;
                        phaseElapsedTime = 0f;
                    }
                    break;
                case RunPhase.Settle:
                    if (phaseElapsedTime >= activeSettleDuration)
                    {
                        StopRun();
                    }
                    break;
            }
        }

        private float GetManeuverPreRollDuration(ManeuverDefinition maneuver)
        {
            return maneuver != null && maneuver.HasCustomPreRollDuration
                ? maneuver.preRollDuration
                : defaultPreRollDuration;
        }

        private float GetManeuverSettleDuration(ManeuverDefinition maneuver)
        {
            return maneuver != null && maneuver.HasCustomSettleDuration
                ? maneuver.settleDuration
                : defaultSettleDuration;
        }

        private List<int> BuildProtocolOrder()
        {
            List<int> indices = new List<int>(maneuvers.Count);
            for (int i = 0; i < maneuvers.Count; i++)
            {
                if (maneuvers[i] != null)
                {
                    if (maneuvers[i].IsIncludedInDefaultProtocol)
                    {
                        indices.Add(i);
                    }
                }
            }

            if (!runProtocolInOrder)
            {
                return indices;
            }

            indices.Sort((a, b) =>
            {
                ManeuverDefinition left = maneuvers[a];
                ManeuverDefinition right = maneuvers[b];
                int leftOrder = left != null && left.protocolOrder >= 0 ? left.protocolOrder : int.MaxValue;
                int rightOrder = right != null && right.protocolOrder >= 0 ? right.protocolOrder : int.MaxValue;
                int orderCompare = leftOrder.CompareTo(rightOrder);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }

                return string.Compare(left != null ? left.maneuverName : string.Empty, right != null ? right.maneuverName : string.Empty, StringComparison.Ordinal);
            });

            return indices;
        }

        private void EnsureSessionInitialized()
        {
            if (!string.IsNullOrEmpty(sessionDirectoryPath))
            {
                return;
            }

            string sessionTimestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            sessionId = $"session_{sessionTimestamp}";
            string root = Path.Combine(Application.persistentDataPath, exportDirectoryName);
            sessionDirectoryPath = Path.Combine(root, sessionId);
            Directory.CreateDirectory(sessionDirectoryPath);
            WriteSessionManifestHeader();
        }

        private void WriteSessionManifestHeader()
        {
            string manifestPath = Path.Combine(sessionDirectoryPath, "session_manifest.jsonl");
            if (File.Exists(manifestPath))
            {
                return;
            }

            SessionMetadata metadata = new SessionMetadata
            {
                session_id = sessionId,
                created_utc = DateTime.UtcNow.ToString("O"),
                export_directory = sessionDirectoryPath,
                application_version = Application.version,
                fixed_timestep_s = Time.fixedDeltaTime,
                benchmark_area_origin = GetBenchmarkOriginString(),
                benchmark_spawn_offset = environmentController != null ? environmentController.BenchmarkSpawnOffset.ToString("F3") : Vector3.zero.ToString("F3"),
                benchmark_dedicated_area_enabled = environmentController != null && environmentController.UseDedicatedBenchmarkArea,
                maneuver_library = resourcesFolder,
                benchmark_settings = new BenchmarkSettingsSnapshot
                {
                    auto_load_from_resources = autoLoadFromResources,
                    resources_folder = resourcesFolder,
                    default_pre_roll_s = defaultPreRollDuration,
                    default_settle_s = defaultSettleDuration,
                    run_protocol_in_order = runProtocolInOrder,
                    reset_mode_between_runs = resetRequestedModeToManeuver,
                    maneuver_count = maneuvers.Count,
                    default_protocol_maneuver_count = GetDefaultProtocolManeuvers().Count,
                    default_protocol_categories = BuildDefaultProtocolCategoryList()
                },
                controller_settings = new ControllerSettingsSnapshot
                {
                    gravity_cancel_multiplier = controller.GravityCancelMultiplier,
                    global_forward_accel_limit = controller.GlobalForwardAccelLimit,
                    global_lateral_accel_limit = controller.GlobalLateralAccelLimit,
                    global_vertical_accel_limit = controller.GlobalVerticalAccelLimit,
                    braking_input_deadband = controller.BrakingInputDeadband
                },
                mode_configs = BuildModeConfigSnapshots()
            };

            string metadataJson = JsonUtility.ToJson(metadata);
            File.WriteAllText(manifestPath, metadataJson + "\n");
        }

        private string GetBenchmarkOriginString()
        {
            ManeuverDefinition selected = GetSelectedManeuver();
            if (selected == null)
            {
                return Vector3.zero.ToString("F3");
            }

            Vector3 origin = environmentController != null
                ? environmentController.GetBenchmarkSpawnPosition(selected.initialPosition)
                : selected.initialPosition;
            return origin.ToString("F3");
        }

        private List<ModeConfigSnapshot> BuildModeConfigSnapshots()
        {
            List<ModeConfigSnapshot> snapshots = new List<ModeConfigSnapshot>();
            AddModeConfigSnapshot(controller.CineConfig, snapshots);
            AddModeConfigSnapshot(controller.NormalConfig, snapshots);
            AddModeConfigSnapshot(controller.SportConfig, snapshots);
            return snapshots;
        }

        private void AddModeConfigSnapshot(DroneFlightModeConfig config, List<ModeConfigSnapshot> snapshots)
        {
            if (config == null)
            {
                return;
            }

            snapshots.Add(new ModeConfigSnapshot
            {
                config_asset = config.name,
                mode = config.mode.ToString(),
                max_forward_speed = config.maxForwardSpeed,
                max_lateral_speed = config.maxLateralSpeed,
                forward_acceleration = config.forwardAcceleration,
                lateral_acceleration = config.lateralAcceleration,
                forward_stop_strength = config.forwardStopStrength,
                lateral_stop_strength = config.lateralStopStrength,
                max_climb_speed = config.maxClimbSpeed,
                max_descent_speed = config.maxDescentSpeed,
                vertical_acceleration = config.verticalAcceleration,
                max_yaw_rate_degrees = config.maxYawRateDegrees,
                yaw_catch_up_speed = config.yawCatchUpSpeed,
                tilt_limit_degrees = config.tiltLimitDegrees,
                tilt_smoothing = config.tiltSmoothing
            });
        }

        private void WriteRunManifestEntry(BenchmarkCsvExporter.RunContext context, ManeuverDefinition maneuver)
        {
            string manifestPath = Path.Combine(sessionDirectoryPath, "session_manifest.jsonl");
            StringBuilder sb = new StringBuilder(512);
            sb.Append('{')
                .Append("\"type\":\"run\",")
                .Append("\"session_id\":\"").Append(context.SessionId).Append("\",")
                .Append("\"run_number\":").Append(context.RunNumber).Append(',')
                .Append("\"run_label\":\"").Append(context.RunLabel).Append("\",")
                .Append("\"maneuver_name\":\"").Append(maneuver != null ? maneuver.maneuverName : "Unknown").Append("\",")
                .Append("\"protocol_category\":\"").Append(maneuver != null ? maneuver.EffectiveProtocolCategory : "unknown").Append("\",")
                .Append("\"protocol_order\":").Append(maneuver != null ? maneuver.protocolOrder : -1).Append(',')
                .Append("\"mode\":\"").Append(maneuver != null ? maneuver.flightMode.ToString() : "Unknown").Append("\",")
                .Append("\"run_source\":\"").Append(activeRunSource).Append("\",")
                .Append("\"pre_roll_s\":").Append(GetManeuverPreRollDuration(maneuver).ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append("\"input_duration_s\":").Append(maneuver != null ? maneuver.Duration.ToString("0.###", CultureInfo.InvariantCulture) : "0").Append(',')
                .Append("\"settle_duration_s\":").Append(GetManeuverSettleDuration(maneuver).ToString("0.###", CultureInfo.InvariantCulture))
                .Append('}');
            File.AppendAllText(manifestPath, sb.ToString() + "\n");
        }

        private List<ManeuverDefinition> GetDefaultProtocolManeuvers()
        {
            List<ManeuverDefinition> protocol = new List<ManeuverDefinition>();
            List<int> protocolIndices = BuildProtocolOrder();
            for (int i = 0; i < protocolIndices.Count; i++)
            {
                ManeuverDefinition maneuver = maneuvers[protocolIndices[i]];
                if (maneuver != null)
                {
                    protocol.Add(maneuver);
                }
            }

            return protocol;
        }

        private List<string> BuildDefaultProtocolCategoryList()
        {
            List<ManeuverDefinition> protocol = GetDefaultProtocolManeuvers();
            List<string> categories = new List<string>(protocol.Count);
            for (int i = 0; i < protocol.Count; i++)
            {
                categories.Add(protocol[i].EffectiveProtocolCategory);
            }

            return categories;
        }

        public void SelectNextManeuver()
        {
            if (maneuvers.Count == 0)
            {
                return;
            }

            selectedManeuverIndex = (selectedManeuverIndex + 1) % maneuvers.Count;
        }

        private ManeuverDefinition GetSelectedManeuver()
        {
            if (maneuvers.Count == 0)
            {
                return null;
            }

            selectedManeuverIndex = Mathf.Clamp(selectedManeuverIndex, 0, maneuvers.Count - 1);
            return maneuvers[selectedManeuverIndex];
        }

        private void LoadManeuversFromResources()
        {
            ManeuverDefinition[] loaded = Resources.LoadAll<ManeuverDefinition>(resourcesFolder);
            maneuvers.Clear();
            maneuvers.AddRange(loaded);
            maneuvers.Sort((a, b) => string.Compare(a.maneuverName, b.maneuverName, StringComparison.Ordinal));
        }

        private void OnDisable()
        {
            if (environmentController != null)
            {
                environmentController.SetBenchmarkIsolationActive(false);
            }

            if (inputReader != null)
            {
                inputReader.SetExternalInputEnabled(false);
            }
        }

        private void ResetDroneState(ManeuverDefinition maneuver)
        {
            inputReader.ResetForBenchmark(resetRequestedModeToManeuver ? maneuver.flightMode : inputReader.CurrentInput.RequestedMode);
            controller.ResetForBenchmark(maneuver.flightMode);

            Rigidbody body = physicsBody.Body;
            Vector3 benchmarkStartPosition = environmentController != null
                ? environmentController.GetBenchmarkSpawnPosition(maneuver.initialPosition)
                : maneuver.initialPosition;
            body.position = benchmarkStartPosition;
            body.rotation = Quaternion.Euler(0f, maneuver.initialYawDegrees, 0f);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
            body.WakeUp();
        }
    }
}
