using System;
using System.Collections.Generic;
using System.IO;
using DroneSim.Drone.Benchmark;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneSim.EditorTools.Benchmark
{
    /// <summary>
    /// Runs the default benchmark protocol (the same one F9 starts) without a person at the keyboard,
    /// then copies the session folder out of persistentDataPath into the repo.
    ///
    /// Command line (close the project in the Editor first):
    ///   Unity -batchmode -projectPath . -executeMethod DroneSim.EditorTools.Benchmark.HeadlessBenchmarkRunner.RunFromCommandLine
    ///         [-benchmarkScene Assets/Scenes/DroneTrainingVerticalSlice.unity]
    ///         [-benchmarkOutput BenchmarkRuns] [-benchmarkTimeout 900] [-benchmarkFailOnError]
    ///         -logFile -
    /// Do not pass -quit: the run takes place over many editor frames, and this class exits the editor itself.
    /// Tools/run_headless_benchmark.py wraps this, zips the result, and runs the acceptance gate.
    ///
    /// Exit codes: 0 success, 1 protocol or export failure, 2 timeout, 3 setup error.
    ///
    /// Entering play mode reloads the script domain (project setting), which wipes static state,
    /// so run state is kept in SessionState and picked back up by the [InitializeOnLoad] constructor.
    /// </summary>
    [InitializeOnLoad]
    public static class HeadlessBenchmarkRunner
    {
        public const string DefaultScenePath = "Assets/Scenes/DroneTrainingVerticalSlice.unity";
        public const string ResultFileName = "headless_benchmark_result.json";

        private const string StateKey = "DroneSim.HeadlessBenchmark.State";
        private const string LogPrefix = "[HeadlessBenchmark]";
        private const int FramesToWaitBeforeStart = 10;
        private const float SecondsToWaitForRunner = 30f;
        private const double SecondsToWaitForPlayMode = 120.0;

        private enum Phase
        {
            EnteringPlayMode,
            WaitingForRunner,
            Running,
            ExitingPlayMode,
            Done
        }

        [Serializable]
        private class RunState
        {
            public bool active;
            public bool exitEditorWhenDone;
            public bool failOnError;
            public string scenePath;
            public string outputDirectory;
            public float timeoutSeconds;
            public long startedUtcTicks;
            public string phase;
            public int expectedRuns;
            public string sessionId;
            public string sessionDirectory;
            public int exitCode;
            public string failureReason;
            public List<string> errors = new List<string>();
        }

        [Serializable]
        private class RunResult
        {
            public string status;
            public int exit_code;
            public string failure_reason;
            public string session_id;
            public string source_session_directory;
            public string output_session_directory;
            public int expected_runs;
            public int csv_count;
            public double elapsed_seconds;
            public string scene;
            public List<string> errors;
        }

        // Play-mode-only scratch state. Safe as statics: no domain reload happens mid play session.
        private static BenchmarkRunner runner;
        private static int playStartFrame = -1;
        private static float waitingSinceRealtime = -1f;

        static HeadlessBenchmarkRunner()
        {
            RunState state = LoadState();
            if (state != null && state.active)
            {
                Hook();
            }
        }

        // ------------------------------------------------------------------ entry points

        public static void RunFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            RunState state = new RunState
            {
                active = true,
                exitEditorWhenDone = true,
                failOnError = HasFlag(args, "-benchmarkFailOnError"),
                scenePath = GetArg(args, "-benchmarkScene", DefaultScenePath),
                outputDirectory = ResolveOutputDirectory(GetArg(args, "-benchmarkOutput", "BenchmarkRuns")),
                timeoutSeconds = ParseFloat(GetArg(args, "-benchmarkTimeout", "900"), 900f),
            };
            Begin(state);
        }

        [MenuItem("DroneSim/Benchmarks/Run Default Protocol (unattended)")]
        private static void RunFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"{LogPrefix} Exit play mode before starting an unattended benchmark.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Begin(new RunState
            {
                active = true,
                exitEditorWhenDone = false,
                scenePath = DefaultScenePath,
                outputDirectory = ResolveOutputDirectory("BenchmarkRuns"),
                timeoutSeconds = 900f,
            });
        }

        [MenuItem("DroneSim/Benchmarks/Run Default Protocol (unattended)", true)]
        private static bool RunFromMenuValidate() => !IsActive();

        [MenuItem("DroneSim/Benchmarks/Cancel Unattended Run")]
        private static void CancelFromMenu()
        {
            RunState state = LoadState();
            if (state != null && state.active)
            {
                Fail(state, 1, "Cancelled from menu.");
            }
        }

        [MenuItem("DroneSim/Benchmarks/Cancel Unattended Run", true)]
        private static bool CancelFromMenuValidate() => IsActive();

        // ------------------------------------------------------------------ lifecycle

        private static void Begin(RunState state)
        {
            state.startedUtcTicks = DateTime.UtcNow.Ticks;
            state.phase = Phase.EnteringPlayMode.ToString();

            if (!File.Exists(Path.Combine(ProjectRoot(), state.scenePath)))
            {
                SaveState(state);
                Fail(state, 3, $"Scene not found: {state.scenePath}");
                return;
            }

            try
            {
                Directory.CreateDirectory(state.outputDirectory);
                EditorSceneManager.OpenScene(state.scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                SaveState(state);
                Fail(state, 3, $"Setup failed: {ex.Message}");
                return;
            }

            Debug.Log($"{LogPrefix} Starting. Scene={state.scenePath} Output={state.outputDirectory} Timeout={state.timeoutSeconds}s");
            SaveState(state);
            Hook();
            EditorApplication.EnterPlaymode();
        }

        private static void Hook()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static void Unhook()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
        }

        private static void Tick()
        {
            RunState state = LoadState();
            if (state == null || !state.active)
            {
                Unhook();
                return;
            }

            if (ElapsedSeconds(state) > state.timeoutSeconds)
            {
                Fail(state, 2, $"Timed out after {state.timeoutSeconds}s in phase {state.phase}.");
                return;
            }

            Phase phase = ParsePhase(state.phase);
            switch (phase)
            {
                case Phase.EnteringPlayMode:
                    if (EditorApplication.isPlaying)
                    {
                        playStartFrame = Time.frameCount;
                        waitingSinceRealtime = Time.realtimeSinceStartup;
                        SetPhase(state, Phase.WaitingForRunner);
                    }
                    else if (!EditorApplication.isPlayingOrWillChangePlaymode && ElapsedSeconds(state) > SecondsToWaitForPlayMode)
                    {
                        // Play mode was refused (usually compile errors) or never started.
                        Fail(state, 3, "Editor did not enter play mode. Check the log for compile errors.");
                    }
                    break;

                case Phase.WaitingForRunner:
                    TickWaitingForRunner(state);
                    break;

                case Phase.Running:
                    TickRunning(state);
                    break;

                case Phase.ExitingPlayMode:
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        Package(state);
                    }
                    break;
            }
        }

        private static void TickWaitingForRunner(RunState state)
        {
            if (!EditorApplication.isPlaying)
            {
                Fail(state, 1, "Play mode ended before the protocol started.");
                return;
            }

            if (playStartFrame < 0)
            {
                // Domain reloaded after we entered play mode; restart the wait.
                playStartFrame = Time.frameCount;
                waitingSinceRealtime = Time.realtimeSinceStartup;
            }

            if (runner == null)
            {
                runner = UnityEngine.Object.FindFirstObjectByType<BenchmarkRunner>();
            }

            bool settled = Time.frameCount - playStartFrame >= FramesToWaitBeforeStart;

            if (runner == null || !runner.IsSessionInitialized || !settled)
            {
                if (Time.realtimeSinceStartup - waitingSinceRealtime > SecondsToWaitForRunner)
                {
                    Fail(state, 3, runner == null
                        ? "No BenchmarkRunner found in the scene after play mode started."
                        : "BenchmarkRunner never initialized a session.");
                }

                return;
            }

            int expected = runner.DefaultProtocolManeuverCount;
            if (expected == 0)
            {
                Fail(state, 3, "BenchmarkRunner has no default protocol maneuvers (Resources/Benchmarks).");
                return;
            }

            state.expectedRuns = expected;
            state.sessionId = runner.SessionId;
            state.sessionDirectory = runner.SessionDirectoryPath;
            SaveState(state);

            Debug.Log($"{LogPrefix} Running default protocol: {expected} maneuvers, session {state.sessionId}.");
            runner.StartFullProtocol();
            SetPhase(state, Phase.Running);
        }

        private static void TickRunning(RunState state)
        {
            if (!EditorApplication.isPlaying || runner == null)
            {
                Fail(state, 1, "Play mode ended or BenchmarkRunner was destroyed mid-protocol.");
                return;
            }

            if (runner.IsRunning || runner.IsProtocolRunActive)
            {
                return;
            }

            Debug.Log($"{LogPrefix} Protocol finished after {runner.CompletedRunCount} runs ({ElapsedSeconds(state):F1}s).");
            runner = null;
            SetPhase(state, Phase.ExitingPlayMode);
            EditorApplication.ExitPlaymode();
        }

        private static void Package(RunState state)
        {
            string source = state.sessionDirectory;
            if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            {
                Fail(state, 1, $"Session directory missing: {source}");
                return;
            }

            string destination = Path.Combine(state.outputDirectory, state.sessionId);
            int csvCount;
            try
            {
                CopyDirectory(source, destination);
                csvCount = Directory.GetFiles(destination, "*.csv", SearchOption.AllDirectories).Length;
            }
            catch (Exception ex)
            {
                Fail(state, 1, $"Copying session failed: {ex.Message}");
                return;
            }

            if (csvCount != state.expectedRuns)
            {
                Fail(state, 1, $"Expected {state.expectedRuns} run CSVs but found {csvCount} in {destination}.");
                return;
            }

            if (state.failOnError && state.errors.Count > 0)
            {
                Fail(state, 1, $"{state.errors.Count} error(s) were logged during the run (-benchmarkFailOnError).");
                return;
            }

            Finish(state, 0, "success", null, destination, csvCount);
        }

        // ------------------------------------------------------------------ completion

        private static void Fail(RunState state, int exitCode, string reason)
        {
            Debug.LogError($"{LogPrefix} FAILED: {reason}");
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            int csvCount = 0;
            string destination = null;
            if (!string.IsNullOrEmpty(state.sessionId) && !string.IsNullOrEmpty(state.outputDirectory))
            {
                destination = Path.Combine(state.outputDirectory, state.sessionId);
                if (Directory.Exists(destination))
                {
                    csvCount = Directory.GetFiles(destination, "*.csv", SearchOption.AllDirectories).Length;
                }
            }

            Finish(state, exitCode, "failed", reason, destination, csvCount);
        }

        private static void Finish(RunState state, int exitCode, string status, string reason, string destination, int csvCount)
        {
            state.active = false;
            state.exitCode = exitCode;
            state.failureReason = reason;
            state.phase = Phase.Done.ToString();
            SaveState(state);
            Unhook();

            RunResult result = new RunResult
            {
                status = status,
                exit_code = exitCode,
                failure_reason = reason,
                session_id = state.sessionId,
                source_session_directory = state.sessionDirectory,
                output_session_directory = destination,
                expected_runs = state.expectedRuns,
                csv_count = csvCount,
                elapsed_seconds = Math.Round(ElapsedSeconds(state), 2),
                scene = state.scenePath,
                errors = state.errors,
            };

            try
            {
                Directory.CreateDirectory(state.outputDirectory);
                File.WriteAllText(Path.Combine(state.outputDirectory, ResultFileName), JsonUtility.ToJson(result, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Could not write result file: {ex.Message}");
            }

            if (exitCode == 0)
            {
                Debug.Log($"{LogPrefix} SUCCESS: {csvCount} runs written to {destination}");
            }

            SessionState.EraseString(StateKey);

            if (state.exitEditorWhenDone)
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)
            {
                return;
            }

            if (condition != null && condition.StartsWith(LogPrefix, StringComparison.Ordinal))
            {
                return;
            }

            RunState state = LoadState();
            if (state == null || !state.active)
            {
                return;
            }

            if (state.errors.Count < 50)
            {
                state.errors.Add($"{type}: {condition}");
                SaveState(state);
            }

            // An exception during the protocol means the data cannot be trusted.
            if (type == LogType.Exception && ParsePhase(state.phase) == Phase.Running)
            {
                Fail(state, 1, $"Exception during protocol: {condition}");
            }
        }

        // ------------------------------------------------------------------ helpers

        private static bool IsActive()
        {
            RunState state = LoadState();
            return state != null && state.active;
        }

        private static RunState LoadState()
        {
            string json = SessionState.GetString(StateKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<RunState>(json);
            }
            catch (ArgumentException)
            {
                SessionState.EraseString(StateKey);
                return null;
            }
        }

        private static void SaveState(RunState state)
        {
            SessionState.SetString(StateKey, JsonUtility.ToJson(state));
        }

        private static void SetPhase(RunState state, Phase phase)
        {
            state.phase = phase.ToString();
            SaveState(state);
        }

        private static Phase ParsePhase(string value)
        {
            return Enum.TryParse(value, out Phase phase) ? phase : Phase.Done;
        }

        private static double ElapsedSeconds(RunState state)
        {
            return (DateTime.UtcNow - new DateTime(state.startedUtcTicks, DateTimeKind.Utc)).TotalSeconds;
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string ResolveOutputDirectory(string value)
        {
            return Path.IsPathRooted(value) ? value : Path.GetFullPath(Path.Combine(ProjectRoot(), value));
        }

        private static string GetArg(string[] args, string name, string fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }

        private static bool HasFlag(string[] args, string name)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static float ParseFloat(string value, float fallback)
        {
            return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            foreach (string directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }
}
