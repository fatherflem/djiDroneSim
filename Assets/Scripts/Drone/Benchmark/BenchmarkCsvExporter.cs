using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DroneSim.Drone.Benchmark
{
    public static class BenchmarkCsvExporter
    {
        public readonly struct RunContext
        {
            public RunContext(string sessionId, string sessionDirectory, string runLabel, int runNumber)
            {
                SessionId = sessionId;
                SessionDirectory = sessionDirectory;
                RunLabel = runLabel;
                RunNumber = runNumber;
            }

            public string SessionId { get; }
            public string SessionDirectory { get; }
            public string RunLabel { get; }
            public int RunNumber { get; }
        }

        public static void Write(
            string outputPath,
            ManeuverDefinition maneuver,
            RunContext context,
            IReadOnlyList<BenchmarkTelemetryRecorder.BenchmarkSample> samples)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder csv = new StringBuilder(16384);
            csv.AppendLine(
                "session_id,session_dir,run_label,run_number,maneuver_name,protocol_category,protocol_order,maneuver_mode,maneuver_duration_s,sample_index,time_s,pos_x_m,pos_y_m,pos_z_m,vel_x_mps,vel_y_mps,vel_z_mps,horizontal_speed_mps,vertical_speed_mps,yaw_deg,pitch_deg,roll_deg,yaw_rate_degps,input_roll,input_pitch,input_throttle,input_yaw");

            string maneuverName = maneuver != null ? maneuver.maneuverName : "Unknown";
            string protocolCategory = maneuver != null ? maneuver.EffectiveProtocolCategory : "unknown";
            int protocolOrder = maneuver != null ? maneuver.protocolOrder : -1;
            string modeName = maneuver != null ? maneuver.flightMode.ToString() : "Unknown";
            float duration = maneuver != null ? maneuver.Duration : 0f;

            for (int i = 0; i < samples.Count; i++)
            {
                BenchmarkTelemetryRecorder.BenchmarkSample sample = samples[i];
                csv.Append(Escape(context.SessionId)).Append(',')
                    .Append(Escape(context.SessionDirectory)).Append(',')
                    .Append(Escape(context.RunLabel)).Append(',')
                    .Append(context.RunNumber).Append(',')
                    .Append(Escape(maneuverName)).Append(',')
                    .Append(protocolCategory).Append(',')
                    .Append(protocolOrder).Append(',')
                    .Append(modeName).Append(',')
                    .Append(F(duration)).Append(',')
                    .Append(sample.SampleIndex).Append(',')
                    .Append(F(sample.ElapsedTime)).Append(',')
                    .Append(F(sample.Position.x)).Append(',')
                    .Append(F(sample.Position.y)).Append(',')
                    .Append(F(sample.Position.z)).Append(',')
                    .Append(F(sample.Velocity.x)).Append(',')
                    .Append(F(sample.Velocity.y)).Append(',')
                    .Append(F(sample.Velocity.z)).Append(',')
                    .Append(F(sample.HorizontalSpeed)).Append(',')
                    .Append(F(sample.VerticalSpeed)).Append(',')
                    .Append(F(sample.YawDegrees)).Append(',')
                    .Append(F(sample.PitchDegrees)).Append(',')
                    .Append(F(sample.RollDegrees)).Append(',')
                    .Append(F(sample.YawRateDegPerSec)).Append(',')
                    .Append(F(sample.RollInput)).Append(',')
                    .Append(F(sample.PitchInput)).Append(',')
                    .Append(F(sample.ThrottleInput)).Append(',')
                    .Append(F(sample.YawInput))
                    .AppendLine();
            }

            File.WriteAllText(outputPath, csv.ToString());
        }

        private static string F(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains(',') || value.Contains('"'))
            {
                return '"' + value.Replace("\"", "\"\"") + '"';
            }

            return value;
        }
    }
}
