using System;
using System.Collections.Generic;
using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    public class HoverBoxDrill : TrainingDrill
    {
        private const int PhysicalWaypointCount = 4;
        private const int LogicalWaypointVisitCount = 5;

        [Header("References")]
        [SerializeField] private DronePhysicsBody droneBody;

        [Header("Waypoint Geometry")]
        [SerializeField] private float waypointRadius = 0.5f;
        [SerializeField] private float waypointHeight = 1.5f;
        [SerializeField] private float waypointAltitude = 2f;
        [SerializeField] private float boxSize = 5f;

        [Header("Hold Requirements")]
        [SerializeField] private float requiredHoldSeconds = 2f;
        [SerializeField] private float maxHorizontalSpeed = 0.3f;
        [SerializeField] private float maxVerticalSpeed = 0.3f;
        [SerializeField] private float maxYawRate = 10f;

        [Header("Safety Envelope")]
        [SerializeField] private float safetyEnvelopeSize = 12f;
        [SerializeField] private float safetyAltitudeMin = 0.5f;
        [SerializeField] private float safetyAltitudeMax = 8f;

        private readonly Vector3[] basePath = new Vector3[LogicalWaypointVisitCount];
        private readonly List<WaypointMarker> markers = new();
        private float holdTimer;
        private float previousYaw;
        private bool hasPreviousYaw;
        private float outOfBoundsSeconds;
        private int interruptedHolds;

        public int ActiveWaypointIndex { get; private set; }
        public int CompletedWaypoints { get; private set; }
        public bool IsComplete => State == DrillState.Completed || State == DrillState.Results;
        public bool IsHolding { get; private set; }
        public bool IsOutOfBounds { get; private set; }
        public float HoldTimer => holdTimer;
        public float RequiredHoldSeconds => requiredHoldSeconds;
        public float HorizontalSpeed { get; private set; }
        public float VerticalSpeed { get; private set; }
        public float YawRateDegPerSec { get; private set; }
        public bool IsStable => HorizontalSpeed <= maxHorizontalSpeed && VerticalSpeed <= maxVerticalSpeed && YawRateDegPerSec <= maxYawRate;
        public string ActiveWaypointLetter => GetWaypointLetterForVisit(ActiveWaypointIndex);
        public Vector3 LastOutOfBoundsPosition { get; private set; }
        public float OutOfBoundsSeconds => outOfBoundsSeconds;
        public int InterruptedHolds => interruptedHolds;

        private void Awake()
        {
            droneBody ??= FindFirstObjectByType<DronePhysicsBody>();
            ApplyFieldOverrides();
            BuildPath();
            BuildMarkers();
        }

        private void ApplyFieldOverrides()
        {
            var loader = DroneSim.Drone.Environment.FieldLoader.Active;
            if (loader == null) return;

            waypointAltitude = loader.RecommendedAltitude + loader.GroundY;
            safetyEnvelopeSize = Mathf.Max(loader.OperatingAreaSize.x, loader.OperatingAreaSize.y);
            safetyAltitudeMin = loader.GroundY + 0.5f;
            safetyAltitudeMax = loader.GroundY + loader.MaxAltitude;
        }

        protected override void UpdateRunning(float deltaTime)
        {
            if (droneBody == null)
            {
                return;
            }

            UpdateMetrics();
            UpdateBounds(deltaTime);
            EvaluateActiveWaypoint(deltaTime);
            RefreshMarkerVisuals();
        }

        protected override void ResetDrill()
        {
            ActiveWaypointIndex = 0;
            CompletedWaypoints = 0;
            holdTimer = 0f;
            IsHolding = false;
            IsOutOfBounds = false;
            HorizontalSpeed = 0f;
            VerticalSpeed = 0f;
            YawRateDegPerSec = 0f;
            hasPreviousYaw = false;
            outOfBoundsSeconds = 0f;
            interruptedHolds = 0;
            RefreshMarkerVisuals();
        }

        protected override void OnRunStarted()
        {
            ResetDrill();
        }

        protected override TrainingResult BuildResult(bool succeeded, string summary)
        {
            TrainingResult result = base.BuildResult(succeeded, summary);
            // Completion is worth most of the score; boundary time and broken holds provide
            // understandable coaching deductions without changing the flight or pass criteria.
            float rawScore = 100f - outOfBoundsSeconds * 2f - interruptedHolds * 2f;
            result.score = succeeded && !float.IsNaN(rawScore) && !float.IsInfinity(rawScore)
                ? Mathf.Clamp(rawScore, 0f, 100f)
                : 0f;
            result.summary = $"{summary} {CompletedWaypoints}/5 waypoints, {outOfBoundsSeconds:F1}s out of bounds, {interruptedHolds} interrupted holds.";
            result.metrics.Add(new TrainingMetric("waypointsCompleted", "Waypoints completed", CompletedWaypoints));
            result.metrics.Add(new TrainingMetric("outOfBoundsSeconds", "Time out of bounds", outOfBoundsSeconds, "s"));
            result.metrics.Add(new TrainingMetric("interruptedHolds", "Interrupted holds", interruptedHolds));
            return result;
        }

        public Vector3 GetWaypoint(int i) => basePath[Mathf.Clamp(i, 0, basePath.Length - 1)];

        private void BuildPath()
        {
            basePath[0] = new Vector3(0f, waypointAltitude, 0f);
            basePath[1] = new Vector3(boxSize, waypointAltitude, 0f);
            basePath[2] = new Vector3(boxSize, waypointAltitude, boxSize);
            basePath[3] = new Vector3(0f, waypointAltitude, boxSize);
            basePath[4] = basePath[0];
        }

        private void BuildMarkers()
        {
            markers.Clear();
            for (int i = 0; i < PhysicalWaypointCount; i++)
            {
                GameObject go = new($"Waypoint_{i}");
                go.transform.SetParent(transform, false);
                go.transform.position = basePath[i];
                WaypointMarker marker = go.AddComponent<WaypointMarker>();
                marker.Configure(waypointRadius, waypointHeight);
                markers.Add(marker);
            }

            RefreshMarkerVisuals();
        }

        private void UpdateMetrics()
        {
            Vector3 velocity = droneBody.Velocity;
            HorizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            VerticalSpeed = Mathf.Abs(velocity.y);

            float yaw = droneBody.transform.eulerAngles.y;
            if (hasPreviousYaw)
            {
                YawRateDegPerSec = Mathf.Abs(Mathf.DeltaAngle(previousYaw, yaw)) / Mathf.Max(Time.deltaTime, 0.0001f);
            }
            previousYaw = yaw;
            hasPreviousYaw = true;
        }

        private void UpdateBounds(float deltaTime)
        {
            Vector3 pos = droneBody.transform.position;
            float half = safetyEnvelopeSize * 0.5f;
            bool inside = pos.x >= -half && pos.x <= half && pos.z >= -half && pos.z <= half && pos.y >= safetyAltitudeMin && pos.y <= safetyAltitudeMax;
            if (!inside)
            {
                LastOutOfBoundsPosition = pos;
            }
            IsOutOfBounds = !inside;
            if (IsOutOfBounds)
            {
                outOfBoundsSeconds += deltaTime;
            }
        }

        private void EvaluateActiveWaypoint(float deltaTime)
        {
            Vector3 center = basePath[ActiveWaypointIndex];
            Vector3 pos = droneBody.transform.position;
            float halfHeight = waypointHeight * 0.5f;
            bool insideHorizontal = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(center.x, center.z)) <= waypointRadius;
            bool insideVertical = Mathf.Abs(pos.y - center.y) <= halfHeight;
            bool valid = insideHorizontal && insideVertical && IsStable;

            if (valid)
            {
                IsHolding = true;
                holdTimer += deltaTime;
                if (holdTimer >= requiredHoldSeconds)
                {
                    CompletedWaypoints++;
                    ActiveWaypointIndex++;
                    holdTimer = 0f;
                    IsHolding = false;
                    if (CompletedWaypoints >= LogicalWaypointVisitCount)
                    {
                        CompleteDrill("Hover Box completed.");
                    }
                }
            }
            else
            {
                if (holdTimer > 0f && ActiveWaypointIndex < basePath.Length)
                {
                    markers[GetPhysicalMarkerIndexForVisit(ActiveWaypointIndex)].FlashFailed();
                    interruptedHolds++;
                }
                holdTimer = 0f;
                IsHolding = false;
            }
        }

        private void RefreshMarkerVisuals()
        {
            int activePhysicalMarker = IsComplete
                ? -1
                : GetPhysicalMarkerIndexForVisit(ActiveWaypointIndex);

            for (int i = 0; i < markers.Count; i++)
            {
                // Active wins over completed when the final logical visit returns to A.
                if (i == activePhysicalMarker)
                {
                    markers[i].SetActive(IsHolding ? holdTimer / requiredHoldSeconds : 0f, IsHolding);
                }
                else if (HasCompletedVisitForPhysicalMarker(i))
                {
                    markers[i].SetCompleted();
                }
                else
                {
                    markers[i].SetFuture();
                }
            }
        }
        private static int GetPhysicalMarkerIndexForVisit(int logicalVisitIndex)
        {
            return Mathf.Clamp(logicalVisitIndex, 0, LogicalWaypointVisitCount - 1) % PhysicalWaypointCount;
        }

        private static string GetWaypointLetterForVisit(int logicalVisitIndex)
        {
            return "ABCD"[GetPhysicalMarkerIndexForVisit(logicalVisitIndex)].ToString();
        }

        private bool HasCompletedVisitForPhysicalMarker(int physicalMarkerIndex)
        {
            for (int visit = 0; visit < CompletedWaypoints; visit++)
            {
                if (GetPhysicalMarkerIndexForVisit(visit) == physicalMarkerIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
