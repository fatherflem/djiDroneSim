using System.Collections.Generic;
using DroneSim.Drone.Flight;
using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Training
{
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

        [SerializeField] private DronePhysicsBody physicsBody;
        [SerializeField] private DJIStyleFlightController controller;
        [SerializeField] private float sampleInterval = 0.1f;
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
