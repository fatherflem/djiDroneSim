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

        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private Texture2D panelBackground;

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
        }

        private void OnGUI()
        {
            if (inputReader == null || physicsBody == null || flightController == null)
            {
                return;
            }

            EnsureStyles();
            DroneInputFrame input = inputReader.CurrentInput;
            Rect panelRect = new Rect(16f, 16f, 380f, 340f);
            GUILayout.BeginArea(panelRect, GUIContent.none, panelStyle);
            GUILayout.Label("DJI-Style Drone Debug HUD", labelStyle);
            GUILayout.Space(6f);
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

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box);
            panelBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            panelBackground.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.09f, 0.9f));
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
