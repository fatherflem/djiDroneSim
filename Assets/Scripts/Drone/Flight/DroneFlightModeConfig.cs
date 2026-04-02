using DroneSim.Drone.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace DroneSim.Drone.Flight
{
    /// <summary>
    /// Purpose: Per-mode tuning profile (Cine/Normal/Sport) for speed, acceleration, yaw, and visual tilt.
    /// Does NOT: run control loops itself; values are consumed by DJIStyleFlightController.
    /// Fits in sim: mode-specific feel definition layer.
    /// Depends on: DroneMode enum and controller fields that read these serialized values.
    /// </summary>
    [CreateAssetMenu(menuName = "Drone Sim/Flight Mode Config", fileName = "DroneFlightModeConfig")]
    public class DroneFlightModeConfig : ScriptableObject
    {
        [Header("Mode")]
        [Tooltip("Which mode this config asset represents (Cine, Normal, or Sport).")]
        public DroneMode mode = DroneMode.Normal;

        [Header("Horizontal translation")]
        [Tooltip("Maximum forward speed in meters per second for this mode.")]
        [FormerlySerializedAs("maxHorizontalSpeed")]
        [Min(0.1f)] public float maxForwardSpeed = 6f;

        [Tooltip("Maximum lateral (side) speed in meters per second for this mode.")]
        [Min(0.1f)] public float maxLateralSpeed = 5f;

        [Tooltip("How quickly forward speed builds when pitch input is held.")]
        [FormerlySerializedAs("horizontalAcceleration")]
        [Min(0.1f)] public float forwardAcceleration = 7f;

        [Tooltip("How quickly lateral speed builds when roll input is held.")]
        [Min(0.1f)] public float lateralAcceleration = 6f;

        [Tooltip("How strongly the drone brakes in forward/back axis when sticks return near center.")]
        [FormerlySerializedAs("horizontalBrakeAcceleration")]
        [FormerlySerializedAs("horizontalStopStrength")]
        [Min(0.1f)] public float forwardStopStrength = 11f;

        [Tooltip("How strongly the drone brakes in lateral axis when sticks return near center.")]
        [Min(0.1f)] public float lateralStopStrength = 10f;

        [Tooltip("Multiplier applied only when commanding right lateral motion; <1 reduces rightward aggressiveness without affecting left.")]
        [Min(0.1f)] public float lateralRightSpeedMultiplier = 1f;

        [Tooltip("Multiplier applied only to rightward lateral acceleration authority while roll input is active.")]
        [Min(0.1f)] public float lateralRightAccelerationMultiplier = 1f;

        [Tooltip("Multiplier applied to lateral stop strength when neutralizing from rightward motion.")]
        [Min(0.1f)] public float lateralRightStopMultiplier = 1f;

        [Header("Vertical translation")]
        [Tooltip("Maximum climb speed in meters per second.")]
        [FormerlySerializedAs("maxVerticalSpeed")]
        [Min(0.1f)] public float maxClimbSpeed = 3f;

        [Tooltip("Maximum descent speed in meters per second.")]
        [Min(0.1f)] public float maxDescentSpeed = 3f;

        [Tooltip("How quickly vertical speed changes toward throttle target.")]
        [FormerlySerializedAs("verticalAcceleration")]
        [Min(0.1f)] public float verticalAcceleration = 6f;

        [Header("Yaw")]
        [Tooltip("Maximum yaw turn rate in degrees per second.")]
        [Min(1f)] public float maxYawRateDegrees = 80f;

        [FormerlySerializedAs("yawResponse")]
        [Tooltip("How quickly yaw rate catches up to stick command. Higher = snappier.")]
        [Min(0.1f)] public float yawCatchUpSpeed = 8f;

        [Tooltip("Multiplier on active yaw catch-up authority for right yaw input (positive rudder); <1 softens right-yaw onset accel without changing left.")]
        [Min(0.1f)] public float yawRightCatchUpMultiplier = 1f;

        [Tooltip("How quickly yaw rate damps toward zero when yaw input is near neutral.")]
        [Min(0.1f)] public float yawStopSpeed = 10f;

        [Tooltip("Directional command gain for right yaw input (positive rudder).")]
        [Min(0.1f)] public float yawRightCommandGain = 1f;

        [Tooltip("Directional command gain for left yaw input (negative rudder).")]
        [Min(0.1f)] public float yawLeftCommandGain = 1f;

        [Tooltip("Multiplier on neutral yaw braking while current yaw rate is rightward (positive). Kept as the only right-only yaw stop override.")]
        [Min(0.1f)] public float yawRightStopMultiplier = 1f;

        [Header("Visual attitude")]
        [Tooltip("Maximum visible pitch/roll tilt angle used by the visual rig.")]
        [Range(1f, 45f)] public float tiltLimitDegrees = 18f;

        [Tooltip("How quickly the visible drone mesh tilts toward commanded movement.")]
        [Min(0.1f)] public float tiltSmoothing = 8f;
    }
}
