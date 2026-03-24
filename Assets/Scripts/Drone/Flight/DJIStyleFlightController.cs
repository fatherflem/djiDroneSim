using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using UnityEngine;
using UnityEngine.Serialization;

namespace DroneSim.Drone.Flight
{
    /// <summary>
    /// Purpose: DJI-style stabilized flight controller that maps pilot intent to acceleration and yaw commands.
    /// Does NOT: simulate individual motors/props or acro-rate body dynamics.
    /// Fits in sim: central flight behavior layer between input and Rigidbody physics.
    /// Depends on: DroneInputReader for pilot intent, DroneFlightModeConfig for tuning, DronePhysicsBody for force/rotation application.
    /// </summary>
    public class DJIStyleFlightController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Live pilot input reader.")]
        [SerializeField] private DroneInputReader inputReader;

        [Tooltip("Physics wrapper that applies acceleration and yaw to the Rigidbody.")]
        [SerializeField] private DronePhysicsBody physicsBody;

        [Tooltip("Visual-only tilt root used to pitch/roll the mesh for feedback.")]
        [SerializeField] private Transform visualTiltRoot;

        [Tooltip("Tuning profile for Cine mode.")]
        [SerializeField] private DroneFlightModeConfig cineConfig;

        [Tooltip("Tuning profile for Normal mode.")]
        [SerializeField] private DroneFlightModeConfig normalConfig;

        [Tooltip("Tuning profile for Sport mode.")]
        [SerializeField] private DroneFlightModeConfig sportConfig;

        [Header("Global assist limits")]
        [FormerlySerializedAs("hoverGravityCompensationMultiplier")]
        [Tooltip("Upward gravity cancellation multiplier. 1.0 means full gravity counter-force baseline.")]
        [SerializeField] private float gravityCancelMultiplier = 1f;

        [FormerlySerializedAs("maxHorizontalAcceleration")]
        [Tooltip("Global horizontal acceleration cap across all modes.")]
        [SerializeField] private float globalHorizontalAccelLimit = 12f;

        [FormerlySerializedAs("maxVerticalAcceleration")]
        [Tooltip("Global vertical acceleration cap across all modes.")]
        [SerializeField] private float globalVerticalAccelLimit = 10f;

        [FormerlySerializedAs("brakingDeadband")]
        [Tooltip("Stick magnitude below this is treated as neutral for active horizontal braking.")]
        [SerializeField] private float brakingInputDeadband = 0.08f;

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

            float horizontalAuthority = desiredLocalVelocity.sqrMagnitude < brakingInputDeadband * brakingInputDeadband
                ? config.horizontalStopStrength
                : config.horizontalAcceleration;

            Vector2 localAcceleration = Vector2.ClampMagnitude(
                velocityError * horizontalAuthority,
                Mathf.Min(horizontalAuthority, globalHorizontalAccelLimit));
            Vector3 worldAcceleration = transform.TransformDirection(new Vector3(localAcceleration.x, 0f, localAcceleration.y));

            float targetVerticalSpeed = input.Throttle * config.maxVerticalSpeed;
            float verticalError = targetVerticalSpeed - physicsBody.VerticalSpeed;
            float verticalAcceleration = Mathf.Clamp(
                verticalError * config.verticalAcceleration,
                -globalVerticalAccelLimit,
                globalVerticalAccelLimit);

            float targetYawRate = input.Yaw * config.maxYawRateDegrees;
            float yawBlend = 1f - Mathf.Exp(-config.yawCatchUpSpeed * Time.fixedDeltaTime);
            currentYawRate = Mathf.Lerp(currentYawRate, targetYawRate, yawBlend);

            Vector3 gravityAssist = Vector3.up * (-UnityEngine.Physics.gravity.y * gravityCancelMultiplier);
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
