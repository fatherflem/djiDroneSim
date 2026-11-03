using System;
using System.Collections.Generic;

namespace DroneSim.Drone.Input.Calibration
{
    /// <summary>Stick axes in the same order DroneInputReader packs them (x roll, y pitch, z yaw, w throttle).</summary>
    public enum StickAxis
    {
        Roll = 0,
        Pitch = 1,
        Yaw = 2,
        Throttle = 3
    }

    public static class StickAxes
    {
        public const int Count = 4;

        public static readonly StickAxis[] All = { StickAxis.Roll, StickAxis.Pitch, StickAxis.Yaw, StickAxis.Throttle };

        public static string DisplayName(StickAxis axis)
        {
            switch (axis)
            {
                case StickAxis.Roll: return "Roll (right stick left/right)";
                case StickAxis.Pitch: return "Pitch (right stick up/down)";
                case StickAxis.Yaw: return "Yaw (left stick left/right)";
                default: return "Throttle (left stick up/down)";
            }
        }

        public static string ShortName(StickAxis axis)
        {
            switch (axis)
            {
                case StickAxis.Roll: return "Roll";
                case StickAxis.Pitch: return "Pitch";
                case StickAxis.Yaw: return "Yaw";
                default: return "Throttle";
            }
        }
    }

    /// <summary>
    /// Maps one raw device axis onto [-1, 1] using a measured center and the raw values seen at
    /// each end of travel. Corrects center offset, uneven travel and a reversed channel in one step.
    /// Pure C# so it can be unit-tested outside Unity.
    /// </summary>
    [Serializable]
    public class AxisCalibration
    {
        /// <summary>Smallest center-to-end distance (in raw units) treated as a real measurement.</summary>
        public const float MinimumSpan = 0.2f;

        public float center;
        public float rawAtPositive = 1f;
        public float rawAtNegative = -1f;

        public static AxisCalibration Identity()
        {
            return new AxisCalibration { center = 0f, rawAtPositive = 1f, rawAtNegative = -1f };
        }

        public AxisCalibration Clone()
        {
            return new AxisCalibration { center = center, rawAtPositive = rawAtPositive, rawAtNegative = rawAtNegative };
        }

        public bool IsValid
        {
            get
            {
                float positiveSpan = rawAtPositive - center;
                float negativeSpan = rawAtNegative - center;
                return IsFinite(center) && IsFinite(rawAtPositive) && IsFinite(rawAtNegative)
                    && Math.Abs(positiveSpan) >= MinimumSpan
                    && Math.Abs(negativeSpan) >= MinimumSpan
                    && Math.Sign(positiveSpan) == -Math.Sign(negativeSpan);
            }
        }

        /// <summary>True when the positive end reads as a lower raw value than center.</summary>
        public bool IsReversed => rawAtPositive < center;

        public float Apply(float raw)
        {
            if (!IsValid || !IsFinite(raw))
            {
                return Clamp(IsFinite(raw) ? raw : 0f);
            }

            float delta = raw - center;
            if (delta == 0f)
            {
                return 0f;
            }

            float positiveSpan = rawAtPositive - center;
            if (Math.Sign(delta) == Math.Sign(positiveSpan))
            {
                return Clamp(delta / positiveSpan);
            }

            float negativeSpan = rawAtNegative - center;
            return Clamp(-(delta / negativeSpan));
        }

        internal static float Clamp(float value)
        {
            return value < -1f ? -1f : value > 1f ? 1f : value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// A saved calibration for one physical controller, stored as JSON per machine. Output is in
    /// DroneInputReader's raw convention: the same sign the reader expects before it applies
    /// invertPitch/invertThrottle, so calibration never changes what those settings mean.
    /// </summary>
    [Serializable]
    public class ControllerCalibrationProfile
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string deviceName = string.Empty;
        public string createdUtc = string.Empty;
        public AxisCalibration roll = AxisCalibration.Identity();
        public AxisCalibration pitch = AxisCalibration.Identity();
        public AxisCalibration yaw = AxisCalibration.Identity();
        public AxisCalibration throttle = AxisCalibration.Identity();
        public bool throttleSelfCentering = true;
        public List<string> notes = new List<string>();

        public bool IsUsable
        {
            get
            {
                if (version != CurrentVersion)
                {
                    return false;
                }

                foreach (StickAxis axis in StickAxes.All)
                {
                    AxisCalibration calibration = Get(axis);
                    if (calibration == null || !calibration.IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public AxisCalibration Get(StickAxis axis)
        {
            switch (axis)
            {
                case StickAxis.Roll: return roll;
                case StickAxis.Pitch: return pitch;
                case StickAxis.Yaw: return yaw;
                default: return throttle;
            }
        }

        public void Set(StickAxis axis, AxisCalibration calibration)
        {
            switch (axis)
            {
                case StickAxis.Roll: roll = calibration; break;
                case StickAxis.Pitch: pitch = calibration; break;
                case StickAxis.Yaw: yaw = calibration; break;
                default: throttle = calibration; break;
            }
        }

        public float Apply(StickAxis axis, float raw)
        {
            AxisCalibration calibration = Get(axis);
            return calibration != null ? calibration.Apply(raw) : AxisCalibration.Clamp(raw);
        }
    }
}
