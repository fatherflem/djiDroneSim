using System.Collections.Generic;
using DroneSim.Drone.Flight;
using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    /// <summary>
    /// Purpose: Captures lightweight flight telemetry samples at a fixed interval.
    /// Does NOT: persist to disk or provide replay/export features yet.
    /// Fits in sim: observability layer for HUD, tuning comparison, and future analytics.
    /// Depends on: DronePhysicsBody state and DJIStyleFlightController active mode.
    /// </summary>
    public class TelemetryRecorder : MonoBehaviour
    {
        public struct TelemetrySample
        {
            public float Time;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Yaw;
            public DroneSim.Drone.Input.DroneMode Mode;
        }

        [Header("References")]
        [Tooltip("Physics source for position/velocity/yaw state.")]
        [SerializeField] private DronePhysicsBody physicsBody;

        [Tooltip("Controller source for active mode metadata.")]
        [SerializeField] private DJIStyleFlightController controller;

        [Header("Sampling")]
        [Tooltip("How often telemetry samples are captured (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float sampleInterval = 0.1f;

        [Tooltip("Maximum number of samples kept in memory (oldest are dropped first).")]
        [Min(1)]
        [SerializeField] private int maxSamples = 1200;

        private readonly List<TelemetrySample> samples = new List<TelemetrySample>();
        private float sampleTimer;

        public IReadOnlyList<TelemetrySample> Samples => samples;
        public TelemetrySample? LatestSample => samples.Count > 0 ? samples[^1] : null;

        public void Initialize(DronePhysicsBody body, DJIStyleFlightController flightController)
        {
            physicsBody = body;
            controller = flightController;
        }


        private void Reset()
        {
            physicsBody = GetComponent<DronePhysicsBody>() ?? FindFirstObjectByType<DronePhysicsBody>();
            controller = GetComponent<DJIStyleFlightController>() ?? FindFirstObjectByType<DJIStyleFlightController>();
        }

        private void Awake()
        {
            physicsBody ??= GetComponent<DronePhysicsBody>() ?? FindFirstObjectByType<DronePhysicsBody>();
            controller ??= GetComponent<DJIStyleFlightController>() ?? FindFirstObjectByType<DJIStyleFlightController>();
        }

        private void Update()
        {
            if (physicsBody == null || controller == null)
            {
                return;
            }

            sampleTimer += Time.deltaTime;
            if (sampleTimer < sampleInterval)
            {
                return;
            }

            sampleTimer = 0f;
            RecordSample();
        }

        private void RecordSample()
        {
            if (samples.Count >= maxSamples)
            {
                samples.RemoveAt(0);
            }

            samples.Add(new TelemetrySample
            {
                Time = Time.time,
                Position = physicsBody.transform.position,
                Velocity = physicsBody.Velocity,
                Yaw = physicsBody.YawDegrees,
                Mode = controller.ActiveMode
            });
        }
    }
}
