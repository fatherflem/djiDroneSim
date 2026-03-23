using DroneSim.Drone.Flight;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using DroneSim.Drone.Training;
using UnityEngine;

namespace DroneSim.Drone.UI
{
    public class DroneDebugHUD : MonoBehaviour
    {
        [SerializeField] private DroneInputReader inputReader;
        [SerializeField] private DronePhysicsBody physicsBody;
        [SerializeField] private DJIStyleFlightController flightController;
        [SerializeField] private SimpleTrainingScenario trainingScenario;
        [SerializeField] private TelemetryRecorder telemetryRecorder;

        private GUIStyle panelStyle;
        private GUIStyle labelStyle;

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

        private void OnGUI()
        {
            if (inputReader == null || physicsBody == null || flightController == null)
            {
                return;
            }

            EnsureStyles();
            DroneInputFrame input = inputReader.CurrentInput;
            Rect panelRect = new Rect(16f, 16f, 360f, 290f);
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
                GUILayout.Label($"Progress: {trainingScenario.Completion01 * 100f,6:F1}%", labelStyle);
                GUILayout.Label($"Hover Time: {trainingScenario.AccumulatedHoverTime,6:F1} s", labelStyle);
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
            panelStyle.normal.background = Texture2D.whiteTexture;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                richText = false
            };
            labelStyle.normal.textColor = Color.white;
        }
    }
}
