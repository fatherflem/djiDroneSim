using UnityEngine;
using UnityEngine.Serialization;

namespace DroneSim.Drone.Input
{
    /// <summary>
    /// Purpose: Central ScriptableObject for pilot input bindings and filtering values.
    /// Does NOT: implement flight logic or physics; it only defines how raw input is read/filtered.
    /// Fits in sim: consumed by DroneInputReader to build the normalized DroneInputFrame.
    /// Depends on: Unity Input System binding path strings used by DroneInputReader InputActions.
    /// </summary>
    [CreateAssetMenu(menuName = "Drone Sim/Input Config", fileName = "DroneInputConfig")]
    public class DroneInputConfig : ScriptableObject
    {
        [Header("Primary axis bindings (radio/joystick)")]
        [Tooltip("Primary roll axis path. Example: <Joystick>/x for Mode 2 radios.")]
        public string rollBinding = "<Joystick>/x";

        [Tooltip("Primary pitch axis path. Example: <Joystick>/y for Mode 2 radios.")]
        public string pitchBinding = "<Joystick>/y";

        [Tooltip("Primary throttle axis path. Example: <Joystick>/z for Mode 2 radios.")]
        public string throttleBinding = "<Joystick>/z";

        [Tooltip("Primary yaw axis path. Example: <Joystick>/rx for Mode 2 radios.")]
        public string yawBinding = "<Joystick>/rx";

        [Header("Keyboard fallback bindings")]
        [Tooltip("Keyboard negative roll input (move left).")]
        public string rollFallbackNegative = "<Keyboard>/a";

        [Tooltip("Keyboard positive roll input (move right).")]
        public string rollFallbackPositive = "<Keyboard>/d";

        [Tooltip("Keyboard negative pitch input (move backward).")]
        public string pitchFallbackNegative = "<Keyboard>/s";

        [Tooltip("Keyboard positive pitch input (move forward).")]
        public string pitchFallbackPositive = "<Keyboard>/w";

        [Tooltip("Keyboard negative throttle input (descend).")]
        public string throttleFallbackNegative = "<Keyboard>/f";

        [Tooltip("Keyboard positive throttle input (climb).")]
        public string throttleFallbackPositive = "<Keyboard>/r";

        [Tooltip("Keyboard negative yaw input (turn left).")]
        public string yawFallbackNegative = "<Keyboard>/q";

        [Tooltip("Keyboard positive yaw input (turn right).")]
        public string yawFallbackPositive = "<Keyboard>/e";

        [Header("Gamepad fallback bindings")]
        [Tooltip("Gamepad roll axis fallback (typically left stick horizontal).")]
        public string rollGamepadBinding = "<Gamepad>/leftStick/x";

        [Tooltip("Gamepad pitch axis fallback (typically left stick vertical).")]
        public string pitchGamepadBinding = "<Gamepad>/leftStick/y";

        [Tooltip("Gamepad throttle positive input (typically right trigger).")]
        public string throttleGamepadPositive = "<Gamepad>/rightTrigger";

        [Tooltip("Gamepad throttle negative input (typically left trigger).")]
        public string throttleGamepadNegative = "<Gamepad>/leftTrigger";

        [Tooltip("Gamepad yaw axis fallback (typically right stick horizontal).")]
        public string yawGamepadBinding = "<Gamepad>/rightStick/x";

        [Header("Mode switch bindings")]
        [Tooltip("Button/key that requests Cine mode while flying.")]
        public string cineModeBinding = "<Keyboard>/1";

        [Tooltip("Button/key that requests Normal mode while flying.")]
        public string normalModeBinding = "<Keyboard>/2";

        [Tooltip("Button/key that requests Sport mode while flying.")]
        public string sportModeBinding = "<Keyboard>/3";

        [Header("Camera and gimbal bindings")]
        [Tooltip("Toggle between chase and FPV presentation modes.")]
        public string cameraToggleBinding = "<Keyboard>/v";

        [Tooltip("Hold to tilt gimbal downward.")]
        public string gimbalTiltDownBinding = "<Keyboard>/leftBracket";

        [Tooltip("Hold to tilt gimbal upward.")]
        public string gimbalTiltUpBinding = "<Keyboard>/rightBracket";

        [Tooltip("Recenter gimbal pitch to forward.")]
        public string gimbalResetBinding = "<Keyboard>/backslash";

        [Header("Input filtering and feel")]
        [FormerlySerializedAs("deadzone")]
        [Tooltip("Ignore tiny stick noise around center. Increase if your radio jitters at center.")]
        [Range(0f, 0.5f)]
        public float stickDeadzone = 0.15f;

        [FormerlySerializedAs("expo")]
        [Tooltip("Curves center-stick response. Higher values feel softer around center.")]
        [Range(0.1f, 2f)]
        public float stickExpo = 1.15f;

        [FormerlySerializedAs("inputResponse")]
        [Tooltip("Input smoothing speed. Higher values track stick changes faster.")]
        [Range(1f, 30f)]
        public float inputSmoothing = 12f;

        [Tooltip("Invert pitch axis. Commonly true for RC-style stick expectations.")]
        public bool invertPitch = true;

        [Tooltip("Invert throttle axis if your device reports climb/descent backwards.")]
        public bool invertThrottle = false;
    }
}
