using DroneSim.Drone.Camera;
using DroneSim.Drone.Flight;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using DroneSim.Drone.Training;
using UnityEngine;

namespace DroneSim.Drone.UI
{
    /// <summary>
    /// Purpose: On-screen debug overlay for live pilot input, flight state, and training progress.
    /// Does NOT: provide production UI/UX or menu interaction.
    /// Fits in sim: quick iteration aid for tuning and classroom demos.
    /// Depends on: input/controller/physics/training/telemetry components supplied by bootstrap.
    /// </summary>
    public class DroneDebugHUD : MonoBehaviour
    {
        [Header("Data sources")]
        [Tooltip("Input source for current normalized stick values.")]
        [SerializeField] private DroneInputReader inputReader;

        [Tooltip("Physics source for speed, altitude, and yaw values.")]
        [SerializeField] private DronePhysicsBody physicsBody;

        [Tooltip("Controller source for active mode and commanded acceleration.")]
        [SerializeField] private DJIStyleFlightController flightController;

        [Tooltip("Optional training scenario source for hover drill progress.")]
        [SerializeField] private SimpleTrainingScenario trainingScenario;

        [Tooltip("Optional telemetry source for sample count display.")]
        [SerializeField] private TelemetryRecorder telemetryRecorder;

        [Header("Camera (optional)")]
        [Tooltip("Camera mode controller for displaying current view mode.")]
        [SerializeField] private DroneCameraModeController cameraModeController;

        [Tooltip("Gimbal camera rig for displaying current gimbal pitch.")]
        [SerializeField] private DroneGimbalCameraRig gimbalRig;

        [Header("Window")]
        [Tooltip("Show this debug window at startup.")]
        [SerializeField] private bool startVisible = true;

        [Tooltip("Start with the window collapsed to title bar only.")]
        [SerializeField] private bool startCollapsed;

        [Tooltip("Default window rect used at startup and by Reset Layout.")]
        [SerializeField] private Rect defaultWindowRect = new Rect(16f, 16f, 380f, 380f);

        private const int WindowId = 1301;

        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private Texture2D panelBackground;
        private Rect windowRect;
        private bool isVisible;
        private bool isCollapsed;

        public void Initialize(
            DroneInputReader reader,
            DronePhysicsBody body,
            DJIStyleFlightController controller,
            SimpleTrainingScenario scenario,
            TelemetryRecorder recorder)
        {
            inputReader = reader;
            physicsBody = body;
            flightController = controller;
            trainingScenario = scenario;
            telemetryRecorder = recorder;
        }

        public void InitializeCamera(DroneCameraModeController modeController, DroneGimbalCameraRig gimbal)
        {
            cameraModeController = modeController;
            gimbalRig = gimbal;
        }

        public void SetWindowVisibility(bool visible)
        {
            isVisible = visible;
        }

        public void ResetWindowLayout()
        {
            windowRect = defaultWindowRect;
            isCollapsed = startCollapsed;
        }

        private void Reset()
        {
            inputReader = FindFirstObjectByType<DroneInputReader>();
            physicsBody = FindFirstObjectByType<DronePhysicsBody>();
            flightController = FindFirstObjectByType<DJIStyleFlightController>();
            trainingScenario = FindFirstObjectByType<SimpleTrainingScenario>();
            telemetryRecorder = FindFirstObjectByType<TelemetryRecorder>();
        }

        private void Awake()
        {
            if (inputReader == null || physicsBody == null || flightController == null)
            {
                Reset();
            }

            isVisible = startVisible;
            isCollapsed = startCollapsed;
            windowRect = defaultWindowRect;
        }

        private void OnGUI()
        {
            if (!isVisible || inputReader == null || physicsBody == null || flightController == null)
            {
                return;
            }

            EnsureStyles();
            windowRect = DebugWindowLayoutUtility.ClampToScreen(windowRect);
            float targetHeight = isCollapsed ? DebugWindowLayoutUtility.HeaderHeight + 6f : defaultWindowRect.height;
            windowRect.height = Mathf.Max(DebugWindowLayoutUtility.HeaderHeight + 6f, targetHeight);
            windowRect = GUI.Window(WindowId, windowRect, DrawWindowContents, GUIContent.none, panelStyle);
            windowRect = DebugWindowLayoutUtility.ClampToScreen(windowRect);
        }

        private void DrawWindowContents(int _)
        {
            GUILayout.BeginVertical();
            DrawWindowHeader("Drone Debug HUD");

            if (!isCollapsed)
            {
                DroneInputFrame input = inputReader.CurrentInput;
                GUILayout.Space(3f);
                GUILayout.Label($"Mode: {flightController.ActiveMode}", labelStyle);
                GUILayout.Label($"Roll: {input.Roll,6:F2}   Pitch: {input.Pitch,6:F2}", labelStyle);
                GUILayout.Label($"Throttle: {input.Throttle,6:F2}   Yaw: {input.Yaw,6:F2}", labelStyle);
                GUILayout.Space(6f);
                GUILayout.Label($"Altitude: {physicsBody.Altitude,6:F2} m", labelStyle);
                GUILayout.Label($"Horizontal Speed: {physicsBody.HorizontalVelocity.magnitude,6:F2} m/s", labelStyle);
                GUILayout.Label($"Vertical Speed: {physicsBody.VerticalSpeed,6:F2} m/s", labelStyle);
                GUILayout.Label($"Yaw: {physicsBody.YawDegrees,6:F1} deg", labelStyle);
                GUILayout.Space(6f);
                GUILayout.Label($"Cmd Accel: {flightController.LastCommandedAcceleration.magnitude,6:F2} m/s²", labelStyle);

                if (cameraModeController != null || gimbalRig != null)
                {
                    GUILayout.Space(6f);
                    if (cameraModeController != null)
                    {
                        GUILayout.Label($"Camera: {cameraModeController.CurrentModeLabel}  [V toggle]", labelStyle);
                    }
                    if (gimbalRig != null)
                    {
                        GUILayout.Label($"Gimbal Pitch: {gimbalRig.CurrentPitchDegrees,6:F1} deg  [[ ] keys]", labelStyle);
                    }
                }

                if (trainingScenario != null)
                {
                    GUILayout.Space(6f);
                    GUILayout.Label("Hover Box Drill", labelStyle);
                    GUILayout.Label($"Inside Box: {trainingScenario.IsDroneInsideBox}", labelStyle);
                    GUILayout.Label($"Target Altitude: {trainingScenario.TargetAltitude,6:F2} m", labelStyle);
                    GUILayout.Label($"Progress: {trainingScenario.Completion01 * 100f,6:F1}%", labelStyle);
                    GUILayout.Label($"Hover Time: {trainingScenario.AccumulatedHoverTime,6:F1} s", labelStyle);
                    GUILayout.Label($"Completed: {trainingScenario.Completed}", labelStyle);
                }

                if (telemetryRecorder != null)
                {
                    GUILayout.Space(6f);
                    GUILayout.Label($"Telemetry Samples: {telemetryRecorder.Samples.Count}", labelStyle);
                }
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 34f, DebugWindowLayoutUtility.HeaderHeight));
        }

        private void DrawWindowHeader(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle, GUILayout.Height(DebugWindowLayoutUtility.HeaderHeight));
            string buttonLabel = isCollapsed ? "+" : "-";
            if (GUILayout.Button(buttonLabel, GUILayout.Width(26f), GUILayout.Height(20f)))
            {
                isCollapsed = !isCollapsed;
            }
            GUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(10, 10, 6, 10)
            };
            panelBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            panelBackground.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.09f, 0.92f));
            panelBackground.Apply();
            panelStyle.normal.background = panelBackground;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                richText = false
            };
            labelStyle.normal.textColor = new Color(0.92f, 0.95f, 0.98f);
        }

        private void OnDestroy()
        {
            if (panelBackground != null)
            {
                Destroy(panelBackground);
            }
        }
    }
}
