using System;
using System.Collections.Generic;
using DroneSim.Drone.Input;
using UnityEngine;

namespace DroneSim.Drone.Benchmark
{
    [CreateAssetMenu(menuName = "DroneSim/Benchmark/Maneuver Definition", fileName = "ManeuverDefinition")]
    public class ManeuverDefinition : ScriptableObject
    {
        [Serializable]
        public struct InputSegment
        {
            [Min(0.01f)] public float duration;
            [Range(-1f, 1f)] public float roll;
            [Range(-1f, 1f)] public float pitch;
            [Range(-1f, 1f)] public float throttle;
            [Range(-1f, 1f)] public float yaw;
        }

        [Header("Metadata")]
        public string maneuverName = "New Maneuver";
        [TextArea] public string description = "";
        public DroneMode flightMode = DroneMode.Normal;
        [Tooltip("Protocol category used for sim-vs-real alignment (hover_hold, forward_step, lateral_right, lateral_left, climb, descent, yaw_right, yaw_left).")]
        public string protocolCategory = "";
        [Tooltip("Optional protocol order index for human-readable benchmark sequencing.")]
        public int protocolOrder = -1;

        [Header("Initial state")]
        [Tooltip("World-space starting position before maneuver playback.")]
        public Vector3 initialPosition = new Vector3(0f, 2f, 0f);

        [Tooltip("Initial world yaw in degrees before maneuver playback.")]
        public float initialYawDegrees;



        [Header("Benchmark timing")]
        [Tooltip("If enabled, this maneuver overrides runner default neutral pre-roll duration.")]
        public bool overridePreRollDuration;

        [Min(0f)]
        [Tooltip("Neutral pre-roll duration used when override is enabled.")]
        public float preRollDuration = 1.5f;

        [Tooltip("If enabled, this maneuver overrides runner default post-input settle duration.")]
        public bool overrideSettleDuration;

        [Min(0f)]
        [Tooltip("Neutral settle duration used when override is enabled.")]
        public float settleDuration = 1.5f;

        [Header("Input sequence")]
        public List<InputSegment> segments = new List<InputSegment>();

        public string EffectiveProtocolCategory
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(protocolCategory))
                {
                    return protocolCategory.Trim().ToLowerInvariant();
                }

                string normalized = maneuverName.Trim().ToLowerInvariant().Replace(" ", "_");
                if (normalized.Contains("hover")) return "hover_hold";
                if (normalized.Contains("forward")) return "forward_step";
                if (normalized.Contains("lateral")) return "lateral_right";
                if (normalized.Contains("vertical")) return "climb";
                if (normalized.Contains("yaw")) return "yaw_right";
                return normalized;
            }
        }

        public bool HasCustomPreRollDuration => overridePreRollDuration;
        public bool HasCustomSettleDuration => overrideSettleDuration;

        public float Duration
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < segments.Count; i++)
                {
                    total += Mathf.Max(0f, segments[i].duration);
                }

                return total;
            }
        }

        public BenchmarkInputFrame Evaluate(float elapsed)
        {
            float timeCursor = 0f;
            InputSegment activeSegment = default;
            bool hasSegment = false;

            for (int i = 0; i < segments.Count; i++)
            {
                InputSegment segment = segments[i];
                float clampedDuration = Mathf.Max(0f, segment.duration);
                if (elapsed <= timeCursor + clampedDuration)
                {
                    activeSegment = segment;
                    hasSegment = true;
                    break;
                }

                timeCursor += clampedDuration;
                activeSegment = segment;
                hasSegment = true;
            }

            return new BenchmarkInputFrame
            {
                Time = Mathf.Max(0f, elapsed),
                Roll = hasSegment ? activeSegment.roll : 0f,
                Pitch = hasSegment ? activeSegment.pitch : 0f,
                Throttle = hasSegment ? activeSegment.throttle : 0f,
                Yaw = hasSegment ? activeSegment.yaw : 0f,
                Mode = flightMode
            };
        }
    }
}
