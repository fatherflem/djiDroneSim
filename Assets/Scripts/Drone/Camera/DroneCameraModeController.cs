using DroneSim.Drone.Bootstrap;
using UnityEngine;

namespace DroneSim.Drone.Camera
{
    /// <summary>
    /// Purpose: Switches between camera view modes (Chase, FPV) and manages which camera is active.
    /// Does NOT: control gimbal orientation or own the feed texture.
    /// Fits in sim: top-level camera mode manager for the player's view.
    /// Depends on: SimpleFollowCamera (chase), DroneGimbalCameraRig + DroneVideoFeed (FPV).
    /// </summary>
    public enum CameraMode
    {
        Chase = 0,
        FPV = 1
        // Future modes: Orbit, FreeLook, VRControllerScreen, etc.
    }

    public class DroneCameraModeController : MonoBehaviour
    {
        [Header("Mode configuration")]
        [Tooltip("Camera mode active on startup.")]
        [SerializeField] private CameraMode startupMode = CameraMode.Chase;

        [Tooltip("Key to toggle between Chase and FPV modes.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.V;

        [Header("FPV rendering")]
        [Tooltip("If true, FPV mode renders the onboard camera directly to screen (lower latency). " +
                 "If false, FPV mode blits the feed RenderTexture to screen (always available for VR screen too).")]
        [SerializeField] private bool fpvDirectRender = true;

        [Header("References")]
        [Tooltip("The main scene camera used for chase mode.")]
        [SerializeField] private UnityEngine.Camera mainCamera;

        [Tooltip("The chase camera controller on the main camera.")]
        [SerializeField] private SimpleFollowCamera chaseCamera;

        [Tooltip("The drone's gimbal camera rig.")]
        [SerializeField] private DroneGimbalCameraRig gimbalRig;

        [Tooltip("The drone video feed manager.")]
        [SerializeField] private DroneVideoFeed videoFeed;

        [Header("Gimbal input (temporary keyboard fallback)")]
        [Tooltip("Key to tilt gimbal down.")]
        [SerializeField] private KeyCode gimbalDownKey = KeyCode.LeftBracket;

        [Tooltip("Key to tilt gimbal up.")]
        [SerializeField] private KeyCode gimbalUpKey = KeyCode.RightBracket;

        [Tooltip("Key to reset gimbal pitch to 0 degrees (forward).")]
        [SerializeField] private KeyCode gimbalResetKey = KeyCode.Backslash;

        [Tooltip("Gimbal pitch rate when using keyboard input (degrees per second).")]
        [Min(1f)]
        [SerializeField] private float gimbalKeyboardRate = 40f;

        [Tooltip("Gimbal pitch rate when using mouse scroll wheel (degrees per scroll notch).")]
        [Min(0.1f)]
        [SerializeField] private float gimbalScrollRate = 5f;

        private CameraMode currentMode;

        /// <summary>Current active camera mode. UI/HUD can read this.</summary>
        public CameraMode CurrentMode => currentMode;

        /// <summary>Human-readable label for the current mode.</summary>
        public string CurrentModeLabel => currentMode.ToString();

        /// <summary>
        /// Initialize with all required references.
        /// </summary>
        public void Initialize(
            UnityEngine.Camera mainCam,
            SimpleFollowCamera chase,
            DroneGimbalCameraRig gimbal,
            DroneVideoFeed feed)
        {
            mainCamera = mainCam;
            chaseCamera = chase;
            gimbalRig = gimbal;
            videoFeed = feed;
            ApplyMode(startupMode);
        }

        private void Awake()
        {
            mainCamera ??= UnityEngine.Camera.main;
            chaseCamera ??= FindFirstObjectByType<SimpleFollowCamera>();
            gimbalRig ??= FindFirstObjectByType<DroneGimbalCameraRig>();
            videoFeed ??= FindFirstObjectByType<DroneVideoFeed>();
        }

        private void Start()
        {
            if (currentMode == 0 && mainCamera != null)
            {
                ApplyMode(startupMode);
            }
        }

        private void Update()
        {
            HandleModeToggle();
            HandleGimbalInput();
        }

        private void HandleModeToggle()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                CameraMode next = currentMode == CameraMode.Chase ? CameraMode.FPV : CameraMode.Chase;
                ApplyMode(next);
            }
        }

        private void HandleGimbalInput()
        {
            if (gimbalRig == null)
            {
                return;
            }

            // Keyboard: bracket keys for pitch control
            float keyDelta = 0f;
            if (UnityEngine.Input.GetKey(gimbalDownKey))
            {
                keyDelta -= gimbalKeyboardRate * Time.deltaTime;
            }
            if (UnityEngine.Input.GetKey(gimbalUpKey))
            {
                keyDelta += gimbalKeyboardRate * Time.deltaTime;
            }

            if (Mathf.Abs(keyDelta) > 0.001f)
            {
                gimbalRig.AdjustTargetPitch(keyDelta);
            }

            // Mouse scroll wheel for gimbal pitch
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                gimbalRig.AdjustTargetPitch(-scroll * gimbalScrollRate);
            }

            // Reset key
            if (UnityEngine.Input.GetKeyDown(gimbalResetKey))
            {
                gimbalRig.SetTargetPitch(0f);
            }
        }

        /// <summary>
        /// Switch to a specific camera mode.
        /// Can be called from UI, input, or external code.
        /// </summary>
        public void ApplyMode(CameraMode mode)
        {
            currentMode = mode;

            switch (mode)
            {
                case CameraMode.Chase:
                    ActivateChaseMode();
                    break;
                case CameraMode.FPV:
                    ActivateFpvMode();
                    break;
            }
        }

        private void ActivateChaseMode()
        {
            // Enable chase camera and main camera.
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }
            if (chaseCamera != null)
            {
                chaseCamera.enabled = true;
            }

            // The onboard camera renders to the feed texture (for VR screen / PiP use).
            if (videoFeed != null)
            {
                videoFeed.RebindCameraToFeed();
            }
            if (gimbalRig != null && gimbalRig.OnboardCamera != null)
            {
                gimbalRig.OnboardCamera.enabled = true;
            }
        }

        private void ActivateFpvMode()
        {
            if (fpvDirectRender)
            {
                // Direct render: disable chase, use onboard camera as the player's view directly.
                if (chaseCamera != null)
                {
                    chaseCamera.enabled = false;
                }
                if (mainCamera != null)
                {
                    mainCamera.enabled = false;
                }

                if (videoFeed != null)
                {
                    videoFeed.UnbindCameraFromFeed();
                }
                if (gimbalRig != null && gimbalRig.OnboardCamera != null)
                {
                    gimbalRig.OnboardCamera.targetTexture = null;
                    gimbalRig.OnboardCamera.enabled = true;
                }
            }
            else
            {
                // Blit mode: onboard camera renders to texture, then we blit to screen.
                // This keeps the feed texture always available for VR controller screen.
                if (chaseCamera != null)
                {
                    chaseCamera.enabled = false;
                }
                if (mainCamera != null)
                {
                    mainCamera.enabled = false;
                }

                if (videoFeed != null)
                {
                    videoFeed.RebindCameraToFeed();
                }
                if (gimbalRig != null && gimbalRig.OnboardCamera != null)
                {
                    gimbalRig.OnboardCamera.enabled = true;
                }
            }
        }

        /// <summary>
        /// Blit the feed texture to screen when in non-direct FPV mode.
        /// </summary>
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (currentMode == CameraMode.FPV && !fpvDirectRender
                && videoFeed != null && videoFeed.FeedTexture != null)
            {
                Graphics.Blit(videoFeed.FeedTexture, destination);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}
