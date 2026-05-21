using System;
using System.Collections.Generic;
using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    public class HoverBoxDrill : MonoBehaviour
    {
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

        private readonly Vector3[] basePath = new Vector3[5];
        private readonly List<WaypointMarker> markers = new();
        private float holdTimer;
        private float previousYaw;
        private bool hasPreviousYaw;

        public int ActiveWaypointIndex { get; private set; }
        public int CompletedWaypoints { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsHolding { get; private set; }
        public bool IsOutOfBounds { get; private set; }
        public float HoldTimer => holdTimer;
        public float RequiredHoldSeconds => requiredHoldSeconds;
        public float HorizontalSpeed { get; private set; }
        public float VerticalSpeed { get; private set; }
        public float YawRateDegPerSec { get; private set; }
        public string ActiveWaypointLetter => "ABCD"[Mathf.Min(ActiveWaypointIndex, 3)].ToString();
        public Vector3 LastOutOfBoundsPosition { get; private set; }

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

        private void Update()
        {
            if (droneBody == null || IsComplete)
            {
                return;
            }

            UpdateMetrics();
            UpdateBounds();
            EvaluateActiveWaypoint();
            RefreshMarkerVisuals();
        }

        public void RestartDrill()
        {
            ActiveWaypointIndex = 0;
            CompletedWaypoints = 0;
            IsComplete = false;
            holdTimer = 0f;
            IsHolding = false;
            hasPreviousYaw = false;
            RefreshMarkerVisuals();
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
            for (int i = 0; i < basePath.Length; i++)
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

        private void UpdateBounds()
        {
            Vector3 pos = droneBody.transform.position;
            float half = safetyEnvelopeSize * 0.5f;
            bool inside = pos.x >= -half && pos.x <= half && pos.z >= -half && pos.z <= half && pos.y >= safetyAltitudeMin && pos.y <= safetyAltitudeMax;
            if (!inside)
            {
                LastOutOfBoundsPosition = pos;
            }
            IsOutOfBounds = !inside;
        }

        private void EvaluateActiveWaypoint()
        {
            Vector3 center = basePath[ActiveWaypointIndex];
            Vector3 pos = droneBody.transform.position;
            float halfHeight = waypointHeight * 0.5f;
            bool insideHorizontal = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(center.x, center.z)) <= waypointRadius;
            bool insideVertical = Mathf.Abs(pos.y - center.y) <= halfHeight;
            bool stable = HorizontalSpeed <= maxHorizontalSpeed && VerticalSpeed <= maxVerticalSpeed && YawRateDegPerSec <= maxYawRate;
            bool valid = insideHorizontal && insideVertical && stable;

            if (valid)
            {
                IsHolding = true;
                holdTimer += Time.deltaTime;
                if (holdTimer >= requiredHoldSeconds)
                {
                    CompletedWaypoints++;
                    ActiveWaypointIndex++;
                    holdTimer = 0f;
                    IsHolding = false;
                    if (CompletedWaypoints >= 5)
                    {
                        IsComplete = true;
                    }
                }
            }
            else
            {
                if (holdTimer > 0f && ActiveWaypointIndex < markers.Count)
                {
                    markers[ActiveWaypointIndex].FlashFailed();
                }
                holdTimer = 0f;
                IsHolding = false;
            }
        }

        private void RefreshMarkerVisuals()
        {
            for (int i = 0; i < markers.Count; i++)
            {
                if (i < CompletedWaypoints)
                {
                    markers[i].SetCompleted();
                }
                else if (i == ActiveWaypointIndex && !IsComplete)
                {
                    markers[i].SetActive(IsHolding ? holdTimer / requiredHoldSeconds : 0f, IsHolding);
                }
                else
                {
                    markers[i].SetFuture();
                }
            }
        }
    }
}
