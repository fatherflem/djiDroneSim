using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Flight
{
    public class DJIStyleFlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DroneInputReader inputReader;
        [SerializeField] private DronePhysicsBody physicsBody;
        [SerializeField] private Transform visualTiltRoot;
        [SerializeField] private DroneFlightModeConfig cineConfig;
        [SerializeField] private DroneFlightModeConfig normalConfig;
        [SerializeField] private DroneFlightModeConfig sportConfig;

        [Header("Assist")]
        [SerializeField] private float hoverGravityCompensationMultiplier = 1f;
        [SerializeField] private float maxHorizontalAcceleration = 12f;
        [SerializeField] private float maxVerticalAcceleration = 10f;
        [SerializeField] private float brakingDeadband = 0.08f;

        private float currentYawRate;
        private DroneMode activeMode = DroneMode.Normal;
        private Vector3 lastCommandedAcceleration;

        public DroneMode ActiveMode => activeMode;
        public DroneFlightModeConfig ActiveConfig => GetActiveConfig();
        public Vector3 LastCommandedAcceleration => lastCommandedAcceleration;

        public void Initialize(
            DroneInputReader reader,
            DronePhysicsBody body,
            Transform tiltRoot,
            DroneFlightModeConfig cine,
            DroneFlightModeConfig normal,
            DroneFlightModeConfig sport)
        {
            inputReader = reader;
            physicsBody = body;
            visualTiltRoot = tiltRoot;
            cineConfig = cine;
            normalConfig = normal;
            sportConfig = sport;
        }

        private void FixedUpdate()
        {
            if (inputReader == null || physicsBody == null || ActiveConfig == null)
            {
                return;
            }

            DroneInputFrame input = inputReader.CurrentInput;
            activeMode = input.RequestedMode;
            DroneFlightModeConfig config = ActiveConfig;

            Vector3 currentHorizontalVelocity = physicsBody.HorizontalVelocity;
            Vector3 currentLocalHorizontal = Quaternion.Inverse(transform.rotation) * currentHorizontalVelocity;
            Vector2 desiredLocalVelocity = new Vector2(input.Roll, input.Pitch) * config.maxHorizontalSpeed;
            Vector2 currentLocalVelocity = new Vector2(currentLocalHorizontal.x, currentLocalHorizontal.z);
            Vector2 velocityError = desiredLocalVelocity - currentLocalVelocity;

            float horizontalAuthority = desiredLocalVelocity.sqrMagnitude < brakingDeadband * brakingDeadband
                ? config.horizontalBrakeAcceleration
                : config.horizontalAcceleration;

            Vector2 localAcceleration = Vector2.ClampMagnitude(velocityError * horizontalAuthority, Mathf.Min(horizontalAuthority, maxHorizontalAcceleration));
            Vector3 worldAcceleration = transform.TransformDirection(new Vector3(localAcceleration.x, 0f, localAcceleration.y));

            float targetVerticalSpeed = input.Throttle * config.maxVerticalSpeed;
            float verticalError = targetVerticalSpeed - physicsBody.VerticalSpeed;
            float verticalAcceleration = Mathf.Clamp(verticalError * config.verticalAcceleration, -maxVerticalAcceleration, maxVerticalAcceleration);

            float targetYawRate = input.Yaw * config.maxYawRateDegrees;
            float yawBlend = 1f - Mathf.Exp(-config.yawResponse * Time.fixedDeltaTime);
            currentYawRate = Mathf.Lerp(currentYawRate, targetYawRate, yawBlend);

            Vector3 gravityAssist = Vector3.up * (-UnityEngine.Physics.gravity.y * hoverGravityCompensationMultiplier);
            lastCommandedAcceleration = worldAcceleration + gravityAssist + Vector3.up * verticalAcceleration;
            physicsBody.ApplyWorldAcceleration(lastCommandedAcceleration);
            physicsBody.ApplyYawStep(currentYawRate * Time.fixedDeltaTime);

            UpdateVisualTilt(config, worldAcceleration);
        }

        private void UpdateVisualTilt(DroneFlightModeConfig config, Vector3 commandedWorldAcceleration)
        {
            if (visualTiltRoot == null)
            {
                return;
            }

            Vector3 localCommand = Quaternion.Inverse(transform.rotation) * commandedWorldAcceleration;
            float pitchTilt = Mathf.Clamp(-localCommand.z * 2.2f, -config.tiltLimitDegrees, config.tiltLimitDegrees);
            float rollTilt = Mathf.Clamp(localCommand.x * 2.2f, -config.tiltLimitDegrees, config.tiltLimitDegrees);
            Quaternion targetTilt = Quaternion.Euler(pitchTilt, 0f, rollTilt);
            float blend = 1f - Mathf.Exp(-config.tiltSmoothing * Time.fixedDeltaTime);
            visualTiltRoot.localRotation = Quaternion.Slerp(visualTiltRoot.localRotation, targetTilt, blend);
        }

        private DroneFlightModeConfig GetActiveConfig()
        {
            return activeMode switch
            {
                DroneMode.Cine => cineConfig,
                DroneMode.Sport => sportConfig,
                _ => normalConfig
            };
        }
    }
}
