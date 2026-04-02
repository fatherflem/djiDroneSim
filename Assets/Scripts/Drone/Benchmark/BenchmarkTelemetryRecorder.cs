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
            public int SampleIndex;
            public float ElapsedTime;
            public string Phase;
            public Vector3 Position;
            public Vector3 Velocity;
            public float ForwardSpeed;
            public float LateralSpeed;
            public float VerticalSpeed;
            public float HorizontalSpeed;
            public float YawDegrees;
            public float PitchDegrees;
            public float RollDegrees;
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

        public void Record(float elapsedTime, DronePhysicsBody body, BenchmarkInputFrame inputFrame, string phase)
        {
            Vector3 velocity = body.Velocity;
            Vector3 localVelocity = Quaternion.Inverse(body.transform.rotation) * velocity;
            Vector3 euler = body.transform.rotation.eulerAngles;
            float pitch = NormalizeSignedAngle(euler.x);
            float yaw = NormalizeSignedAngle(euler.y);
            float roll = NormalizeSignedAngle(euler.z);
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
                SampleIndex = samples.Count,
                ElapsedTime = elapsedTime,
                Phase = string.IsNullOrWhiteSpace(phase) ? "unknown" : phase,
                Position = body.transform.position,
                Velocity = velocity,
                ForwardSpeed = localVelocity.z,
                LateralSpeed = localVelocity.x,
                VerticalSpeed = body.VerticalSpeed,
                HorizontalSpeed = body.HorizontalVelocity.magnitude,
                YawDegrees = yaw,
                PitchDegrees = pitch,
                RollDegrees = roll,
                YawRateDegPerSec = yawRate,
                RollInput = inputFrame.Roll,
                PitchInput = inputFrame.Pitch,
                ThrottleInput = inputFrame.Throttle,
                YawInput = inputFrame.Yaw,
                Mode = inputFrame.Mode
            });
        }

        public void ExportCsv(string directoryPath, BenchmarkCsvExporter.RunContext context, ManeuverDefinition maneuver)
        {
            string safeManeuver = MakeSafeFilename(maneuver != null ? maneuver.maneuverName : "UnknownManeuver");
            string safeCategory = MakeSafeFilename(maneuver != null ? maneuver.EffectiveProtocolCategory : "unknown");
            string safeMode = MakeSafeFilename(maneuver != null ? maneuver.flightMode.ToString() : "UnknownMode");
            string safeLabel = MakeSafeFilename(context.RunLabel);
            string filePath = System.IO.Path.Combine(
                directoryPath,
                $"run_{context.RunNumber:000}_{safeCategory}_{safeManeuver}_{safeMode}_{safeLabel}.csv");
            BenchmarkCsvExporter.Write(filePath, maneuver, context, samples);
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

        private static float NormalizeSignedAngle(float value)
        {
            return Mathf.DeltaAngle(0f, value);
        }
    }
}
