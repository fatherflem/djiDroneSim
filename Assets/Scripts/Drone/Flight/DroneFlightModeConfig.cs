using DroneSim.Drone.Input;
using UnityEngine;

namespace DroneSim.Drone.Flight
{
    [CreateAssetMenu(menuName = "Drone Sim/Flight Mode Config", fileName = "DroneFlightModeConfig")]
    public class DroneFlightModeConfig : ScriptableObject
    {
        public DroneMode mode = DroneMode.Normal;

        [Header("Translation")]
        [Min(0.1f)] public float maxHorizontalSpeed = 6f;
        [Min(0.1f)] public float horizontalAcceleration = 7f;
        [Min(0.1f)] public float horizontalBrakeAcceleration = 11f;
        [Min(0.1f)] public float maxVerticalSpeed = 3f;
        [Min(0.1f)] public float verticalAcceleration = 6f;

        [Header("Yaw")]
        [Min(1f)] public float maxYawRateDegrees = 80f;
        [Min(0.1f)] public float yawResponse = 8f;

        [Header("Attitude")]
        [Range(1f, 45f)] public float tiltLimitDegrees = 18f;
        [Min(0.1f)] public float tiltSmoothing = 8f;
    }
}
