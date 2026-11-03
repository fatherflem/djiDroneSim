using DroneSim.Drone.Input;
using DroneSim.Drone.Input.Calibration;
using UnityEngine;

namespace DroneSim.VR
{
    public class VirtualRCInputBridge : MonoBehaviour
    {
        [SerializeField] private DroneInputReader inputReader;
        [SerializeField] private VirtualRCControllerRig controllerRig;
        [SerializeField] private float maxStickAngleDegrees = 18f;
        [SerializeField] private float inputDiscoveryRetrySeconds = 0.5f;

        private float nextDiscoveryTime;
        private ControllerSetupService setupService;

        public void SetInputReader(DroneInputReader reader)
        {
            inputReader = reader;
        }

        private void Awake()
        {
            inputReader ??= FindFirstObjectByType<DroneInputReader>();
            controllerRig ??= GetComponent<VirtualRCControllerRig>();
            nextDiscoveryTime = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            if (inputReader == null && Time.unscaledTime >= nextDiscoveryTime)
            {
                inputReader = FindFirstObjectByType<DroneInputReader>();
                nextDiscoveryTime = Time.unscaledTime + Mathf.Max(0.1f, inputDiscoveryRetrySeconds);
            }

            if (inputReader == null || controllerRig == null)
            {
                return;
            }

            if (ControllerSetupService.IsCheckRunning)
            {
                // The drone is held at neutral during the controller check, so mirror the physical
                // sticks directly; otherwise the virtual sticks would sit still while the pilot moves.
                if (setupService == null)
                {
                    setupService = FindFirstObjectByType<ControllerSetupService>();
                }

                if (setupService != null)
                {
                    Vector2 left = setupService.GetStickPreview(true);
                    Vector2 right = setupService.GetStickPreview(false);
                    ApplyStick(controllerRig.LeftStick, left.x, left.y);
                    ApplyStick(controllerRig.RightStick, right.x, right.y);
                    return;
                }
            }

            DroneInputFrame input = inputReader.CurrentInput;
            ApplyStick(controllerRig.LeftStick, input.Yaw, input.Throttle);
            ApplyStick(controllerRig.RightStick, input.Roll, input.Pitch);
        }

        private void ApplyStick(Transform stick, float x, float y)
        {
            if (stick == null)
            {
                return;
            }

            float pitch = Mathf.Clamp(y, -1f, 1f) * maxStickAngleDegrees;
            float roll = Mathf.Clamp(-x, -1f, 1f) * maxStickAngleDegrees;
            stick.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }
    }
}
