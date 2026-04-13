using DroneSim.Drone.Input;
using UnityEngine;

namespace DroneSim.VR
{
    public class VirtualRCInputBridge : MonoBehaviour
    {
        [SerializeField] private DroneInputReader inputReader;
        [SerializeField] private VirtualRCControllerRig controllerRig;
        [SerializeField] private float maxStickAngleDegrees = 18f;

        public void SetInputReader(DroneInputReader reader)
        {
            inputReader = reader;
        }

        private void Awake()
        {
            inputReader ??= FindFirstObjectByType<DroneInputReader>();
            controllerRig ??= GetComponent<VirtualRCControllerRig>();
        }

        private void LateUpdate()
        {
            if (inputReader == null || controllerRig == null)
            {
                return;
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
