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

        [Header("Translation")]
        [Tooltip("Maximum forward/sideways speed in meters per second for this mode.")]
        [Min(0.1f)] public float maxHorizontalSpeed = 6f;

        [Tooltip("How quickly horizontal speed builds when stick input is held.")]
        [Min(0.1f)] public float horizontalAcceleration = 7f;

        [FormerlySerializedAs("horizontalBrakeAcceleration")]
        [Tooltip("How strongly the drone brakes horizontally when sticks return near center.")]
        [Min(0.1f)] public float horizontalStopStrength = 11f;

        [Tooltip("Maximum climb/descent speed in meters per second.")]
        [Min(0.1f)] public float maxVerticalSpeed = 3f;

        [Tooltip("How quickly vertical speed changes toward throttle target.")]
        [Min(0.1f)] public float verticalAcceleration = 6f;

        [Header("Yaw")]
        [Tooltip("Maximum yaw turn rate in degrees per second.")]
        [Min(1f)] public float maxYawRateDegrees = 80f;

        [FormerlySerializedAs("yawResponse")]
        [Tooltip("How quickly yaw rate catches up to stick command. Higher = snappier.")]
        [Min(0.1f)] public float yawCatchUpSpeed = 8f;

        [Header("Visual attitude")]
        [Tooltip("Maximum visible pitch/roll tilt angle used by the visual rig.")]
        [Range(1f, 45f)] public float tiltLimitDegrees = 18f;

        [Tooltip("How quickly the visible drone mesh tilts toward commanded movement.")]
        [Min(0.1f)] public float tiltSmoothing = 8f;
    }
}
