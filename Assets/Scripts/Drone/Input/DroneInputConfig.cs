using UnityEngine;

namespace DroneSim.Drone.Input
{
    [CreateAssetMenu(menuName = "Drone Sim/Input Config", fileName = "DroneInputConfig")]
    public class DroneInputConfig : ScriptableObject
    {
        [Header("Axis bindings")]
        public string rollBinding = "<Joystick>/x";
        public string pitchBinding = "<Joystick>/y";
        public string throttleBinding = "<Joystick>/z";
        public string yawBinding = "<Joystick>/rx";

        [Header("Fallback bindings")]
        public string rollFallbackNegative = "<Keyboard>/a";
        public string rollFallbackPositive = "<Keyboard>/d";
        public string pitchFallbackNegative = "<Keyboard>/s";
        public string pitchFallbackPositive = "<Keyboard>/w";
        public string throttleFallbackNegative = "<Keyboard>/f";
        public string throttleFallbackPositive = "<Keyboard>/r";
        public string yawFallbackNegative = "<Keyboard>/q";
        public string yawFallbackPositive = "<Keyboard>/e";

        [Header("Gamepad fallback")]
        public string rollGamepadBinding = "<Gamepad>/leftStick/x";
        public string pitchGamepadBinding = "<Gamepad>/leftStick/y";
        public string throttleGamepadPositive = "<Gamepad>/rightTrigger";
        public string throttleGamepadNegative = "<Gamepad>/leftTrigger";
        public string yawGamepadBinding = "<Gamepad>/rightStick/x";

        [Header("Mode bindings")]
        public string cineModeBinding = "<Keyboard>/1";
        public string normalModeBinding = "<Keyboard>/2";
        public string sportModeBinding = "<Keyboard>/3";

        [Header("Filtering")]
        [Range(0f, 0.5f)] public float deadzone = 0.15f;
        [Range(0.1f, 2f)] public float expo = 1.15f;
        [Range(1f, 30f)] public float inputResponse = 12f;
        public bool invertPitch = true;
        public bool invertThrottle = false;
    }
}
