using System;
using System.Collections.Generic;
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

        private readonly BenchmarkTelemetryRecorder recorder = new BenchmarkTelemetryRecorder();
        private ManeuverDefinition activeManeuver;
        private float runElapsedTime;
        private bool isRunning;
        private int runCounter;

        public bool IsRunning => isRunning;
        public string CurrentManeuverName => GetSelectedManeuver() != null ? GetSelectedManeuver().maneuverName : "None";
        public int SelectedManeuverIndex => selectedManeuverIndex;

        public void Initialize(DroneInputReader reader, DronePhysicsBody body, DJIStyleFlightController flightController)
        {
            inputReader = reader;
            physicsBody = body;
            controller = flightController;
        }

        private void Awake()
        {
            if (inputReader == null || physicsBody == null || controller == null)
            {
                inputReader ??= FindFirstObjectByType<DroneInputReader>();
                physicsBody ??= FindFirstObjectByType<DronePhysicsBody>();
                controller ??= FindFirstObjectByType<DJIStyleFlightController>();
            }

            if (autoLoadFromResources && maneuvers.Count == 0)
            {
                LoadManeuversFromResources();
            }
        }

        private void Start()
        {
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
            Rect panel = new Rect(16f, 370f, 540f, 90f);
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(panel);
            GUILayout.Label($"Benchmark: {CurrentManeuverName} (#{selectedManeuverIndex + 1}/{Mathf.Max(1, maneuvers.Count)})");
            GUILayout.Label($"Run Key: {runManeuverKey} | Cycle Key: {cycleManeuverKey}");
            GUILayout.Label(isRunning ? $"Status: Running ({runElapsedTime:F2}s/{activeManeuver.Duration:F2}s)" : "Status: Idle (manual input active)");
            GUILayout.EndArea();
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

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string runLabel = $"{timestamp}_run{runCounter:000}";
            runCounter++;

            string outputDir = System.IO.Path.Combine(Application.persistentDataPath, exportDirectoryName);
            recorder.ExportCsv(outputDir, runLabel, activeManeuver);

            Debug.Log($"BenchmarkRunner finished '{activeManeuver.maneuverName}'. CSV saved to {outputDir}");
            activeManeuver = null;
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

        private void ResetDroneState(ManeuverDefinition maneuver)
        {
            Rigidbody body = physicsBody.Body;
            body.position = maneuver.initialPosition;
            body.rotation = Quaternion.Euler(0f, maneuver.initialYawDegrees, 0f);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
            body.WakeUp();
        }
    }
}
