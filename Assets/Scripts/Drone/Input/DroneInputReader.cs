using System;
using System.Collections.Generic;
using DroneSim.Drone.Input.Calibration;
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

    /// <summary>
    /// Purpose: Reads device input through Unity Input System and exposes a normalized input frame.
    /// Does NOT: decide drone physics, mode tuning, or stabilization behavior.
    /// Fits in sim: first live runtime stage before controller logic.
    /// Depends on: DroneInputConfig binding/filter fields and Unity InputAction runtime.
    /// </summary>
    public class DroneInputReader : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Input profile that defines axis bindings, deadzone, expo, and smoothing.")]
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
        private bool useExternalInput;
        private DroneInputFrame externalInput;
        private ControllerCalibrationProfile calibration;

        public DroneInputConfig Config => config;
        public DroneInputFrame CurrentInput => currentInput;
        public bool UseExternalInput => useExternalInput;

        /// <summary>Calibration applied to physical-joystick axes only. Null means raw values are used as-is.</summary>
        public ControllerCalibrationProfile Calibration => calibration;

        public void SetCalibration(ControllerCalibrationProfile profile)
        {
            calibration = profile != null && profile.IsUsable ? profile : null;
        }

        public void Initialize(DroneInputConfig inputConfig)
        {
            config = inputConfig;
            RebuildActions();
        }

        public void SetExternalInputEnabled(bool enabled)
        {
            useExternalInput = enabled;
            smoothedAxes = Vector4.zero;
        }

        public void SetExternalInputFrame(DroneInputFrame frame)
        {
            externalInput = frame;
        }

        public void ResetForBenchmark(DroneMode mode)
        {
            smoothedAxes = Vector4.zero;
            currentInput = new DroneInputFrame
            {
                Roll = 0f,
                Pitch = 0f,
                Yaw = 0f,
                Throttle = 0f,
                RequestedMode = mode
            };
            externalInput = currentInput;
            useExternalInput = false;
        }

        private void OnEnable() => RebuildActions();

        private void OnDisable() => DisableActions();

        private void Update()
        {
            if (useExternalInput)
            {
                currentInput = externalInput;
                return;
            }

            if (config == null || rollAction == null)
            {
                return;
            }

            Vector4 rawAxes = new Vector4(
                ReadAxis(rollAction, StickAxis.Roll),
                ReadAxis(pitchAction, StickAxis.Pitch) * (config.invertPitch ? -1f : 1f),
                ReadAxis(yawAction, StickAxis.Yaw),
                ReadThrottle());

            float smoothing = 1f - Mathf.Exp(-config.inputSmoothing * Time.unscaledDeltaTime);
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
            float primary = ReadAxis(throttleAction, StickAxis.Throttle) * (config.invertThrottle ? -1f : 1f);
            return Mathf.Clamp(primary, -1f, 1f);
        }

        private float ReadAxis(InputAction action, StickAxis axis)
        {
            if (action == null)
            {
                return 0f;
            }

            float value = action.ReadValue<float>();
            if (calibration != null && action.activeControl != null && action.activeControl.device is Joystick)
            {
                // Correct center offset, uneven travel and reversed channels before deadzone/expo.
                value = calibration.Apply(axis, value);
            }

            float magnitude = Mathf.Abs(value);
            if (magnitude <= config.stickDeadzone)
            {
                return 0f;
            }

            float normalized = Mathf.InverseLerp(config.stickDeadzone, 1f, magnitude);
            float curved = Mathf.Pow(normalized, config.stickExpo);
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
            AddBindingWithJoystickAxisCompatibility(action, primaryBinding);
            AddBindingWithJoystickAxisCompatibility(action, gamepadBinding);
            action.AddCompositeBinding("1DAxis")
                .With("negative", negativeBinding)
                .With("positive", positiveBinding);
            action.Enable();
            return action;
        }

        private void AddBindingWithJoystickAxisCompatibility(InputAction action, string binding)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                return;
            }

            foreach (string expanded in ExpandJoystickAxisBinding(binding))
            {
                action.AddBinding(expanded);
            }
        }

        /// <summary>
        /// Returns the binding plus its alternate joystick axis spelling (<Joystick>/x vs <Joystick>/stick/x),
        /// since radios report the same axis under either path. Shared with ControllerSetupService.
        /// </summary>
        public static IEnumerable<string> ExpandJoystickAxisBinding(string binding)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                yield break;
            }

            yield return binding;
            if (TryGetAlternateJoystickAxisBinding(binding, out string alternateBinding))
            {
                yield return alternateBinding;
            }
        }

        private static bool TryGetAlternateJoystickAxisBinding(string binding, out string alternateBinding)
        {
            alternateBinding = null;

            if (binding.IndexOf("Joystick", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (binding.EndsWith("/stick/x", StringComparison.OrdinalIgnoreCase))
            {
                alternateBinding = binding.Substring(0, binding.Length - "/stick/x".Length) + "/x";
                return true;
            }

            if (binding.EndsWith("/stick/y", StringComparison.OrdinalIgnoreCase))
            {
                alternateBinding = binding.Substring(0, binding.Length - "/stick/y".Length) + "/y";
                return true;
            }

            if (binding.EndsWith("/x", StringComparison.OrdinalIgnoreCase))
            {
                alternateBinding = binding.Substring(0, binding.Length - "/x".Length) + "/stick/x";
                return true;
            }

            if (binding.EndsWith("/y", StringComparison.OrdinalIgnoreCase))
            {
                alternateBinding = binding.Substring(0, binding.Length - "/y".Length) + "/stick/y";
                return true;
            }

            return false;
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
