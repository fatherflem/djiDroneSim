using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneSim.Drone.Input
{
    public struct DroneInputFrame
    {
        public float Roll;
        public float Pitch;
        public float Yaw;
        public float Throttle;
        public DroneMode RequestedMode;
    }

    public class DroneInputReader : MonoBehaviour
    {
        [SerializeField] private DroneInputConfig config;

        private InputAction rollAction;
        private InputAction pitchAction;
        private InputAction throttleAction;
        private InputAction yawAction;
        private InputAction cineModeAction;
        private InputAction normalModeAction;
        private InputAction sportModeAction;

        private DroneInputFrame currentInput;
        private Vector4 smoothedAxes;

        public DroneInputConfig Config => config;
        public DroneInputFrame CurrentInput => currentInput;

        public void Initialize(DroneInputConfig inputConfig)
        {
            config = inputConfig;
            RebuildActions();
        }

        private void OnEnable()
        {
            RebuildActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void Update()
        {
            if (config == null || rollAction == null)
            {
                return;
            }

            Vector4 rawAxes = new Vector4(
                ReadAxis(rollAction),
                ReadAxis(pitchAction) * (config.invertPitch ? -1f : 1f),
                ReadAxis(yawAction),
                ReadThrottle());

            float smoothing = 1f - Mathf.Exp(-config.inputResponse * Time.unscaledDeltaTime);
            smoothedAxes = Vector4.Lerp(smoothedAxes, rawAxes, smoothing);

            currentInput.Roll = smoothedAxes.x;
            currentInput.Pitch = smoothedAxes.y;
            currentInput.Yaw = smoothedAxes.z;
            currentInput.Throttle = smoothedAxes.w;

            if (cineModeAction != null && cineModeAction.WasPressedThisFrame())
            {
                currentInput.RequestedMode = DroneMode.Cine;
            }
            else if (normalModeAction != null && normalModeAction.WasPressedThisFrame())
            {
                currentInput.RequestedMode = DroneMode.Normal;
            }
            else if (sportModeAction != null && sportModeAction.WasPressedThisFrame())
            {
                currentInput.RequestedMode = DroneMode.Sport;
            }
        }

        private float ReadThrottle()
        {
            float primary = ReadAxis(throttleAction) * (config.invertThrottle ? -1f : 1f);
            return Mathf.Clamp(primary, -1f, 1f);
        }

        private float ReadAxis(InputAction action)
        {
            if (action == null)
            {
                return 0f;
            }

            float value = action.ReadValue<float>();
            float magnitude = Mathf.Abs(value);
            if (magnitude <= config.deadzone)
            {
                return 0f;
            }

            float normalized = Mathf.InverseLerp(config.deadzone, 1f, magnitude);
            float curved = Mathf.Pow(normalized, config.expo);
            return curved * Mathf.Sign(value);
        }

        private void RebuildActions()
        {
            DisableActions();

            if (config == null)
            {
                return;
            }

            rollAction = CreateAxisAction("Roll", config.rollBinding, config.rollGamepadBinding, config.rollFallbackNegative, config.rollFallbackPositive);
            pitchAction = CreateAxisAction("Pitch", config.pitchBinding, config.pitchGamepadBinding, config.pitchFallbackNegative, config.pitchFallbackPositive);
            throttleAction = CreateThrottleAction();
            yawAction = CreateAxisAction("Yaw", config.yawBinding, config.yawGamepadBinding, config.yawFallbackNegative, config.yawFallbackPositive);

            cineModeAction = CreateButtonAction("ModeCine", config.cineModeBinding);
            normalModeAction = CreateButtonAction("ModeNormal", config.normalModeBinding);
            sportModeAction = CreateButtonAction("ModeSport", config.sportModeBinding);

            currentInput.RequestedMode = DroneMode.Normal;
        }

        private InputAction CreateAxisAction(string name, string primaryBinding, string gamepadBinding, string negativeBinding, string positiveBinding)
        {
            InputAction action = new InputAction(name, InputActionType.Value);
            action.AddBinding(primaryBinding);
            action.AddBinding(gamepadBinding);
            action.AddCompositeBinding("1DAxis")
                .With("negative", negativeBinding)
                .With("positive", positiveBinding);
            action.Enable();
            return action;
        }

        private InputAction CreateThrottleAction()
        {
            InputAction action = new InputAction("Throttle", InputActionType.Value);
            action.AddBinding(config.throttleBinding);
            action.AddCompositeBinding("1DAxis")
                .With("negative", config.throttleFallbackNegative)
                .With("positive", config.throttleFallbackPositive);
            action.AddCompositeBinding("1DAxis")
                .With("negative", config.throttleGamepadNegative)
                .With("positive", config.throttleGamepadPositive);
            action.Enable();
            return action;
        }

        private InputAction CreateButtonAction(string name, string binding)
        {
            InputAction action = new InputAction(name, InputActionType.Button, binding);
            action.Enable();
            return action;
        }

        private void DisableActions()
        {
            rollAction?.Dispose();
            pitchAction?.Dispose();
            throttleAction?.Dispose();
            yawAction?.Dispose();
            cineModeAction?.Dispose();
            normalModeAction?.Dispose();
            sportModeAction?.Dispose();
        }
    }
}
