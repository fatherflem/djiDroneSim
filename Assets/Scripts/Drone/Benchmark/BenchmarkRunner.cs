using System;
using System.Collections.Generic;
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
    /// </summary>
    public class BenchmarkRunner : MonoBehaviour
    {
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
        [SerializeField] private string exportDirectoryName = "BenchmarkRuns";

        [Header("Debug window")]
        [SerializeField] private bool showDebugWindow = true;
        [SerializeField] private bool startCollapsed;
        [SerializeField] private Rect defaultWindowRect = new Rect(16f, 312f, 430f, 110f);

        private readonly BenchmarkTelemetryRecorder recorder = new BenchmarkTelemetryRecorder();
        private ManeuverDefinition activeManeuver;
        private float runElapsedTime;
        private bool isRunning;
        private int runCounter;
        private Rect windowRect;
        private bool isCollapsed;
        private string sessionId;
        private string sessionDirectoryPath;

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
        }

        private void FixedUpdate()
        {
            if (!isRunning || activeManeuver == null || inputReader == null || physicsBody == null)
            {
                return;
            }

            runElapsedTime += Time.fixedDeltaTime;
            BenchmarkInputFrame benchmarkInput = activeManeuver.Evaluate(runElapsedTime);
            inputReader.SetExternalInputFrame(benchmarkInput.ToDroneInputFrame());
            recorder.Record(runElapsedTime, physicsBody, benchmarkInput);

            if (runElapsedTime >= activeManeuver.Duration)
            {
                StopRun();
            }
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
                GUILayout.Label($"Run Key: {runManeuverKey} | Cycle Key: {cycleManeuverKey}");
                GUILayout.Label(isRunning ? $"Status: Running ({runElapsedTime:F2}s/{activeManeuver.Duration:F2}s)" : "Status: Idle (manual input active)");
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 34f, DroneSim.Drone.UI.DebugWindowLayoutUtility.HeaderHeight));
        }

        public void StartSelectedManeuver()
        {
            ManeuverDefinition maneuver = GetSelectedManeuver();
            if (maneuver == null || inputReader == null || physicsBody == null)
            {
                Debug.LogWarning("BenchmarkRunner could not start maneuver. Missing setup or maneuver list.");
                return;
            }

            activeManeuver = maneuver;
            runElapsedTime = 0f;
            isRunning = true;

            environmentController?.SetBenchmarkIsolationActive(true);
            ResetDroneState(activeManeuver);
            recorder.BeginRun();
            inputReader.SetExternalInputEnabled(true);
            inputReader.SetExternalInputFrame(activeManeuver.Evaluate(0f).ToDroneInputFrame());

            Debug.Log($"BenchmarkRunner started '{activeManeuver.maneuverName}' in {activeManeuver.flightMode} mode.");
        }

        public void StopRun()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            inputReader.SetExternalInputEnabled(false);
            environmentController?.SetBenchmarkIsolationActive(false);

            EnsureSessionInitialized();

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            int runNumber = runCounter + 1;
            string runLabel = $"{timestamp}_run{runNumber:000}";
            runCounter++;

            BenchmarkCsvExporter.RunContext context = new BenchmarkCsvExporter.RunContext(sessionId, sessionDirectoryPath, runLabel, runNumber);
            recorder.ExportCsv(sessionDirectoryPath, context, activeManeuver);
            WriteRunManifestEntry(context, activeManeuver);

            Debug.Log($"BenchmarkRunner finished '{activeManeuver.maneuverName}'. CSV saved to {sessionDirectoryPath}");
            activeManeuver = null;
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

            StringBuilder sb = new StringBuilder(256);
            sb.Append('{')
                .Append("\"type\":\"session_metadata\",")
                .Append("\"session_id\":\"").Append(sessionId).Append("\",")
                .Append("\"created_utc\":\"").Append(DateTime.UtcNow.ToString("O")).Append("\",")
                .Append("\"export_directory\":\"").Append(sessionDirectoryPath.Replace("\\", "\\\\")).Append("\"")
                .Append('}');
            File.WriteAllText(manifestPath, sb.ToString() + "\n");
        }

        private void WriteRunManifestEntry(BenchmarkCsvExporter.RunContext context, ManeuverDefinition maneuver)
        {
            string manifestPath = Path.Combine(sessionDirectoryPath, "session_manifest.jsonl");
            StringBuilder sb = new StringBuilder(384);
            sb.Append('{')
                .Append("\"type\":\"run\",")
                .Append("\"session_id\":\"").Append(context.SessionId).Append("\",")
                .Append("\"run_number\":").Append(context.RunNumber).Append(',')
                .Append("\"run_label\":\"").Append(context.RunLabel).Append("\",")
                .Append("\"maneuver_name\":\"").Append(maneuver != null ? maneuver.maneuverName : "Unknown").Append("\",")
                .Append("\"protocol_category\":\"").Append(maneuver != null ? maneuver.EffectiveProtocolCategory : "unknown").Append("\",")
                .Append("\"mode\":\"").Append(maneuver != null ? maneuver.flightMode.ToString() : "Unknown").Append("\",")
                .Append("\"duration_s\":").Append(maneuver != null ? maneuver.Duration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "0")
                .Append('}');
            File.AppendAllText(manifestPath, sb.ToString() + "\n");
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
        }

        private void ResetDroneState(ManeuverDefinition maneuver)
        {
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
