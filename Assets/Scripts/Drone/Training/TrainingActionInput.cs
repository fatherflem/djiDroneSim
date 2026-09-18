using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneSim.Drone.Training
{
    /// <summary>Keyboard/development and physical-controller start/retry input shared by desktop and VR.</summary>
    public class TrainingActionInput : MonoBehaviour
    {
        [Tooltip("Optional Input System button path for a generic RC/joystick, for example <Joystick>/button3. Leave empty until the physical transmitter button has been identified.")]
        [SerializeField] private string genericJoystickButtonControlPath = string.Empty;

        private TrainingDrill drill;
        private InputAction startAction;
        private InputAction retryAction;

        public void Initialize(TrainingDrill trainingDrill, string genericJoystickButtonPath)
        {
            drill = trainingDrill;
            genericJoystickButtonControlPath = genericJoystickButtonPath;
            if (isActiveAndEnabled)
            {
                CreateActions();
            }
        }

        private void OnEnable()
        {
            CreateActions();
        }

        private void CreateActions()
        {
            startAction?.Dispose();
            retryAction?.Dispose();
            startAction = new InputAction("StartTraining", binding: "<Keyboard>/enter");
            startAction.AddBinding("<Keyboard>/space");
            startAction.AddBinding("<Gamepad>/buttonSouth");
            retryAction = new InputAction("RetryTraining", binding: "<Keyboard>/r");
            retryAction.AddBinding("<Gamepad>/buttonSouth");
            if (IsGenericJoystickButtonPath(genericJoystickButtonControlPath))
            {
                startAction.AddBinding(genericJoystickButtonControlPath);
                retryAction.AddBinding(genericJoystickButtonControlPath);
            }
            else if (!string.IsNullOrWhiteSpace(genericJoystickButtonControlPath))
            {
                Debug.LogWarning($"Ignoring invalid generic training action binding '{genericJoystickButtonControlPath}'. Expected a <Joystick>/button... control path.", this);
            }
            startAction.Enable();
            retryAction.Enable();
        }

        private static bool IsGenericJoystickButtonPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && path.StartsWith("<Joystick>/button", System.StringComparison.OrdinalIgnoreCase);
        }

        private void OnDisable()
        {
            startAction?.Dispose();
            retryAction?.Dispose();
        }

        private void Update()
        {
            if (drill == null) return;
            if (drill.State == DrillState.Instructions && startAction.WasPressedThisFrame())
            {
                drill.ContinueFromInstructions();
            }
            else if (drill.State == DrillState.Results && drill.CanRestart && retryAction.WasPressedThisFrame())
            {
                drill.RestartDrill();
            }
        }
    }
}
