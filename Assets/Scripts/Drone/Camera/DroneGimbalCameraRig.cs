using UnityEngine;

namespace DroneSim.Drone.Camera
{
    /// <summary>
    /// Purpose: Drone-mounted onboard camera with gimbal-style pitch control and horizon stabilization.
    /// Does NOT: decide how the camera feed is presented (full-screen, VR screen, etc.).
    /// Fits in sim: represents the physical DJI-style gimbal camera mounted on the drone body.
    /// Depends on: being a child of the drone GameObject; driven by external gimbal pitch input.
    /// </summary>
    public class DroneGimbalCameraRig : MonoBehaviour
    {
        [Header("Camera setup")]
        [Tooltip("The Unity Camera used as the drone's onboard camera. Auto-created if null.")]
        [SerializeField] private UnityEngine.Camera onboardCamera;

        [Tooltip("Local position offset of the camera relative to the drone body center.")]
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, -0.04f, 0.35f);

        [Tooltip("Field of view for the onboard camera (degrees).")]
        [Range(30f, 120f)]
        [SerializeField] private float fieldOfView = 84f;

        [Header("Gimbal pitch")]
        [Tooltip("Minimum pitch angle (negative = looking down). Typical DJI range: -90.")]
        [SerializeField] private float minPitchDegrees = -90f;

        [Tooltip("Maximum pitch angle (positive = looking up). Typical DJI range: +10 to +30.")]
        [SerializeField] private float maxPitchDegrees = 10f;

        [Tooltip("Speed at which gimbal pitch moves toward the target angle (degrees per second).")]
        [Min(1f)]
        [SerializeField] private float pitchSpeed = 60f;

        [Tooltip("Smoothing factor for gimbal pitch movement. Higher = snappier.")]
        [Min(0.1f)]
        [SerializeField] private float pitchSmoothing = 8f;

        [Header("Stabilization")]
        [Tooltip("How much drone body roll is removed from the camera view. 1 = fully stabilized horizon, 0 = body-locked.")]
        [Range(0f, 1f)]
        [SerializeField] private float rollStabilization = 0.85f;

        [Tooltip("How much drone body pitch is removed from the camera view (independent of gimbal pitch input). 1 = fully stabilized, 0 = body-locked.")]
        [Range(0f, 1f)]
        [SerializeField] private float pitchStabilization = 0.9f;

        // Gimbal state
        private float targetPitchDegrees;
        private float currentPitchDegrees;
        private Transform gimbalPivot;

        /// <summary>Current gimbal pitch angle in degrees (negative = looking down).</summary>
        public float CurrentPitchDegrees => currentPitchDegrees;

        /// <summary>Target gimbal pitch angle in degrees.</summary>
        public float TargetPitchDegrees => targetPitchDegrees;

        /// <summary>The onboard Unity Camera component.</summary>
        public UnityEngine.Camera OnboardCamera => onboardCamera;

        /// <summary>The gimbal pivot transform for external inspection.</summary>
        public Transform GimbalPivot => gimbalPivot;

        /// <summary>
        /// Set the desired gimbal pitch angle directly (clamped to configured range).
        /// Use this for absolute positioning (e.g., from a dial or slider).
        /// </summary>
        public void SetTargetPitch(float degrees)
        {
            targetPitchDegrees = Mathf.Clamp(degrees, minPitchDegrees, maxPitchDegrees);
        }

        /// <summary>
        /// Adjust the target gimbal pitch by a delta (negative = tilt down, positive = tilt up).
        /// Use this for incremental input (e.g., keyboard hold, mouse wheel, RC wheel).
        /// </summary>
        public void AdjustTargetPitch(float deltaDegrees)
        {
            targetPitchDegrees = Mathf.Clamp(targetPitchDegrees + deltaDegrees, minPitchDegrees, maxPitchDegrees);
        }

        /// <summary>
        /// Initialize the rig with an explicit drone parent transform.
        /// Called by bootstrap; also safe to call from inspector-wired setups.
        /// </summary>
        public void Initialize(Transform droneRoot)
        {
            transform.SetParent(droneRoot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            EnsureGimbalStructure();
        }

        private void Awake()
        {
            EnsureGimbalStructure();
        }

        private void LateUpdate()
        {
            if (gimbalPivot == null)
            {
                return;
            }

            UpdateGimbalPitch();
            ApplyStabilizedOrientation();
            SyncCameraSettings();
        }

        private void EnsureGimbalStructure()
        {
            // Create gimbal pivot as an intermediate transform between drone body and camera.
            // This mirrors how a real 2/3-axis gimbal has independent rotation axes.
            if (gimbalPivot == null)
            {
                Transform existing = transform.Find("GimbalPivot");
                if (existing != null)
                {
                    gimbalPivot = existing;
                }
                else
                {
                    GameObject pivotObject = new GameObject("GimbalPivot");
                    pivotObject.transform.SetParent(transform, false);
                    pivotObject.transform.localPosition = cameraLocalOffset;
                    gimbalPivot = pivotObject.transform;
                }
            }

            if (onboardCamera == null)
            {
                onboardCamera = gimbalPivot.GetComponentInChildren<UnityEngine.Camera>();
            }

            if (onboardCamera == null)
            {
                GameObject cameraObject = new GameObject("DroneOnboardCamera");
                cameraObject.transform.SetParent(gimbalPivot, false);
                cameraObject.transform.localPosition = Vector3.zero;
                cameraObject.transform.localRotation = Quaternion.identity;
                onboardCamera = cameraObject.AddComponent<UnityEngine.Camera>();
                onboardCamera.nearClipPlane = 0.1f;
                onboardCamera.farClipPlane = 1500f;
                onboardCamera.fieldOfView = fieldOfView;
                // Start disabled; the mode controller decides when this camera renders.
                onboardCamera.enabled = false;
            }
        }

        private void UpdateGimbalPitch()
        {
            // Smooth approach to target pitch using framerate-independent exponential blend.
            currentPitchDegrees = Mathf.Lerp(
                currentPitchDegrees,
                targetPitchDegrees,
                1f - Mathf.Exp(-pitchSmoothing * Time.deltaTime));

            // Also allow direct speed-limited movement for large jumps.
            float maxStep = pitchSpeed * Time.deltaTime;
            float diff = targetPitchDegrees - currentPitchDegrees;
            if (Mathf.Abs(diff) > maxStep)
            {
                currentPitchDegrees += Mathf.Sign(diff) * maxStep;
            }
        }

        private void ApplyStabilizedOrientation()
        {
            if (gimbalPivot == null || transform.parent == null)
            {
                return;
            }

            // Start from the drone's world yaw only (strip body pitch and roll).
            Transform droneBody = transform.parent;
            float droneYaw = droneBody.eulerAngles.y;

            // Read drone body pitch and roll for partial stabilization.
            float bodyPitch = WrapAngle(droneBody.eulerAngles.x);
            float bodyRoll = WrapAngle(droneBody.eulerAngles.z);

            // Stabilize: remove a configurable portion of body pitch/roll from the camera.
            float cameraRoll = bodyRoll * (1f - rollStabilization);
            float cameraPitch = currentPitchDegrees + bodyPitch * (1f - pitchStabilization);

            // The rig transform itself stays at drone position, but we set gimbal world rotation
            // to decouple it from body attitude.
            gimbalPivot.rotation = Quaternion.Euler(cameraPitch, droneYaw, cameraRoll);
            gimbalPivot.position = droneBody.TransformPoint(cameraLocalOffset);
        }

        private void SyncCameraSettings()
        {
            if (onboardCamera != null)
            {
                onboardCamera.fieldOfView = fieldOfView;
            }
        }

        /// <summary>Wrap angle to -180..+180 range.</summary>
        private static float WrapAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
