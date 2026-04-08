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

        [Tooltip("Global forward-axis acceleration cap across all modes.")]
        [SerializeField] private float globalForwardAccelLimit = 8f;

        [Tooltip("Global lateral-axis acceleration cap across all modes.")]
        [SerializeField] private float globalLateralAccelLimit = 8f;

        [FormerlySerializedAs("maxVerticalAcceleration")]
        [Tooltip("Global vertical acceleration cap across all modes.")]
        [SerializeField] private float globalVerticalAccelLimit = 7f;

        [FormerlySerializedAs("brakingDeadband")]
        [Tooltip("Stick magnitude below this is treated as neutral for active horizontal braking.")]
        [SerializeField] private float brakingInputDeadband = 0.08f;

        [Tooltip("Jerk limit in m/s^3 on pilot-commanded acceleration. Caps how fast the commanded accel vector can change between FixedUpdate ticks, modelling real-drone onset delay and smoother brake ramps.")]
        [SerializeField] private float accelerationSlewRate = 8f;

        private float currentYawRate;
        private DroneMode activeMode = DroneMode.Normal;
        private Vector3 lastCommandedAcceleration;
        private Vector3 slewedPilotAcceleration;

        public DroneMode ActiveMode => activeMode;
        public DroneFlightModeConfig ActiveConfig => GetActiveConfig();
        public Vector3 LastCommandedAcceleration => lastCommandedAcceleration;
        public float GravityCancelMultiplier => gravityCancelMultiplier;
        public float GlobalForwardAccelLimit => globalForwardAccelLimit;
        public float GlobalLateralAccelLimit => globalLateralAccelLimit;
        public float GlobalVerticalAccelLimit => globalVerticalAccelLimit;
        public float BrakingInputDeadband => brakingInputDeadband;
        public DroneFlightModeConfig CineConfig => cineConfig;
        public DroneFlightModeConfig NormalConfig => normalConfig;
        public DroneFlightModeConfig SportConfig => sportConfig;

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



        public void ResetForBenchmark(DroneMode mode)
        {
            currentYawRate = 0f;
            lastCommandedAcceleration = Vector3.zero;
            slewedPilotAcceleration = Vector3.zero;
            activeMode = mode;

            if (visualTiltRoot != null)
            {
                visualTiltRoot.localRotation = Quaternion.identity;
            }
        }

        private void Reset() => AutoWireReferences();
        private void Awake() => AutoWireReferences();

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                AutoWireReferences();
            }
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

            float rightLateralSpeedScale = input.Roll >= 0f ? config.lateralRightSpeedMultiplier : 1f;
            float targetLateralSpeed = input.Roll * config.maxLateralSpeed * rightLateralSpeedScale;
            float targetForwardSpeed = input.Pitch * config.maxForwardSpeed;

            float lateralVelocityError = targetLateralSpeed - currentLocalHorizontal.x;
            float forwardVelocityError = targetForwardSpeed - currentLocalHorizontal.z;

            bool lateralNeutral = Mathf.Abs(input.Roll) < brakingInputDeadband;
            bool movingRightLaterally = currentLocalHorizontal.x >= 0f;
            float lateralAuthority = lateralNeutral
                ? config.lateralStopStrength * (movingRightLaterally ? config.lateralRightStopMultiplier : 1f)
                : config.lateralAcceleration * (input.Roll > 0f ? config.lateralRightAccelerationMultiplier : 1f);
            float forwardAuthority = Mathf.Abs(input.Pitch) < brakingInputDeadband
                ? config.forwardStopStrength
                : config.forwardAcceleration;

            float localAccelX = Mathf.Clamp(lateralVelocityError * lateralAuthority, -globalLateralAccelLimit, globalLateralAccelLimit);
            float localAccelZ = Mathf.Clamp(forwardVelocityError * forwardAuthority, -globalForwardAccelLimit, globalForwardAccelLimit);
            Vector3 worldAcceleration = transform.TransformDirection(new Vector3(localAccelX, 0f, localAccelZ));

            float targetVerticalSpeed = input.Throttle >= 0f
                ? input.Throttle * config.maxClimbSpeed
                : input.Throttle * config.maxDescentSpeed;
            float verticalError = targetVerticalSpeed - physicsBody.VerticalSpeed;
            float verticalAcceleration = Mathf.Clamp(verticalError * config.verticalAcceleration, -globalVerticalAccelLimit, globalVerticalAccelLimit);

            float normalizedYawInput = Mathf.Clamp(input.Yaw, -1f, 1f);
            float yawDirectionGain = normalizedYawInput >= 0f ? config.yawRightCommandGain : config.yawLeftCommandGain;
            float targetYawRate = normalizedYawInput * config.maxYawRateDegrees * yawDirectionGain;

            bool yawInputNeutral = Mathf.Abs(normalizedYawInput) < brakingInputDeadband;
            bool rightYawInputActive = normalizedYawInput > brakingInputDeadband;
            bool rightwardYawRate = currentYawRate >= 0f;
            float yawStopAuthority = config.yawStopSpeed * (rightwardYawRate ? config.yawRightStopMultiplier : 1f);

            float yawCatchUpAuthority = config.yawCatchUpSpeed * (rightYawInputActive ? config.yawRightCatchUpMultiplier : config.yawLeftCatchUpMultiplier);

            if (yawInputNeutral)
            {
                // Hard yaw stop — matches real DJI behavior (near-instant stop on stick release).
                // MoveTowards gives linear deceleration instead of exponential tail.
                float maxYawDecel = yawStopAuthority * Time.fixedDeltaTime * config.maxYawRateDegrees;
                currentYawRate = Mathf.MoveTowards(currentYawRate, 0f, maxYawDecel);
            }
            else
            {
                // Exponential catch-up for onset (preserves existing smooth ramp-up behavior).
                float yawBlend = 1f - Mathf.Exp(-yawCatchUpAuthority * Time.fixedDeltaTime);
                currentYawRate = Mathf.Lerp(currentYawRate, targetYawRate, yawBlend);
            }
            Vector3 gravityAssist = Vector3.up * (-UnityEngine.Physics.gravity.y * gravityCancelMultiplier);
            Vector3 desiredPilotAcceleration = worldAcceleration + Vector3.up * verticalAcceleration;
            float maxPilotAccelDelta = accelerationSlewRate * Time.fixedDeltaTime;
            slewedPilotAcceleration = Vector3.MoveTowards(slewedPilotAcceleration, desiredPilotAcceleration, maxPilotAccelDelta);
            lastCommandedAcceleration = slewedPilotAcceleration + gravityAssist;
            physicsBody.ApplyWorldAcceleration(lastCommandedAcceleration);
            physicsBody.ApplyYawStep(currentYawRate * Time.fixedDeltaTime);

            UpdateVisualTilt(config, slewedPilotAcceleration);
        }

        private void UpdateVisualTilt(DroneFlightModeConfig config, Vector3 commandedWorldAcceleration)
        {
            if (visualTiltRoot == null)
            {
                return;
            }

            Vector3 localCommand = Quaternion.Inverse(transform.rotation) * commandedWorldAcceleration;
            float pitchTilt = Mathf.Clamp(-localCommand.z * 4.2f, -config.tiltLimitDegrees, config.tiltLimitDegrees);
            float rollTilt = Mathf.Clamp(localCommand.x * 4.8f, -config.tiltLimitDegrees, config.tiltLimitDegrees);
            Quaternion targetTilt = Quaternion.Euler(pitchTilt, 0f, rollTilt);
            float blend = 1f - Mathf.Exp(-config.tiltSmoothing * Time.fixedDeltaTime);
            visualTiltRoot.localRotation = Quaternion.Slerp(visualTiltRoot.localRotation, targetTilt, blend);
        }

        private void AutoWireReferences()
        {
            inputReader ??= GetComponent<DroneInputReader>();
            physicsBody ??= GetComponent<DronePhysicsBody>();

            if (visualTiltRoot == null)
            {
                DroneVisualRig visualRig = GetComponent<DroneVisualRig>();
                if (visualRig != null)
                {
                    visualRig.EnsureVisuals();
                    visualTiltRoot = visualRig.TiltRoot;
                }
            }
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
