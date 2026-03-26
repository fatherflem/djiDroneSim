using System;
using System.Collections.Generic;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Benchmark
{
    /// <summary>
    /// Captures dense telemetry for scripted benchmark runs.
    /// </summary>
    public class BenchmarkTelemetryRecorder
    {
        public struct BenchmarkSample
        {
            public float ElapsedTime;
            public Vector3 Position;
            public Vector3 Velocity;
            public float HorizontalSpeed;
            public float VerticalSpeed;
            public float YawDegrees;
            public float YawRateDegPerSec;
            public float RollInput;
            public float PitchInput;
            public float ThrottleInput;
            public float YawInput;
            public DroneMode Mode;
        }

        private readonly List<BenchmarkSample> samples = new List<BenchmarkSample>(2048);
        private float previousYaw;
        private bool hasPreviousYaw;

        public IReadOnlyList<BenchmarkSample> Samples => samples;

        public void BeginRun()
        {
            samples.Clear();
            hasPreviousYaw = false;
        }

        public void Record(float elapsedTime, DronePhysicsBody body, BenchmarkInputFrame inputFrame)
        {
            Vector3 velocity = body.Velocity;
            float yaw = body.YawDegrees;
            float yawRate = 0f;

            if (hasPreviousYaw)
            {
                float yawDelta = Mathf.DeltaAngle(previousYaw, yaw);
                yawRate = Mathf.Abs(Time.fixedDeltaTime) > 0.0001f ? yawDelta / Time.fixedDeltaTime : 0f;
            }

            previousYaw = yaw;
            hasPreviousYaw = true;

            samples.Add(new BenchmarkSample
            {
                ElapsedTime = elapsedTime,
                Position = body.transform.position,
                Velocity = velocity,
                HorizontalSpeed = body.HorizontalVelocity.magnitude,
                VerticalSpeed = body.VerticalSpeed,
                YawDegrees = yaw,
                YawRateDegPerSec = yawRate,
                RollInput = inputFrame.Roll,
                PitchInput = inputFrame.Pitch,
                ThrottleInput = inputFrame.Throttle,
                YawInput = inputFrame.Yaw,
                Mode = inputFrame.Mode
            });
        }

        public void ExportCsv(string directoryPath, string runFileLabel, ManeuverDefinition maneuver)
        {
            string safeManeuver = MakeSafeFilename(maneuver != null ? maneuver.maneuverName : "UnknownManeuver");
            string safeLabel = MakeSafeFilename(runFileLabel);
            string filePath = System.IO.Path.Combine(directoryPath, $"benchmark_{safeManeuver}_{safeLabel}.csv");
            BenchmarkCsvExporter.Write(filePath, maneuver, samples);
        }

        private static string MakeSafeFilename(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "run";
            }

            string cleaned = raw.Trim();
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                cleaned = cleaned.Replace(invalid, '_');
            }

            return cleaned.Replace(' ', '_');
        }
    }
}
