using DroneSim.Drone.Bootstrap;
using DroneSim.Drone.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneSim.Drone.Camera
{
    /// <summary>
    /// Purpose: Switches between camera view modes (Chase, FPV) and manages player-facing camera presentation.
    /// Does NOT: own feed texture lifetime or disable the onboard feed source.
    /// Fits in sim: top-level camera mode manager for the player's view.
    /// Depends on: SimpleFollowCamera (chase), DroneGimbalCameraRig + DroneVideoFeed (always-live onboard feed).
    /// </summary>
    public enum CameraMode
    {
        Chase = 0,
        FPV = 1
    }

    public class DroneCameraModeController : MonoBehaviour
    {
        [Header("Mode configuration")]
        [SerializeField] private CameraMode startupMode = CameraMode.Chase;

        [Header("Input (Unity Input System)")]
        [Tooltip("Optional input config source. If assigned, camera/gimbal bindings come from this asset.")]
        [SerializeField] private DroneInputConfig inputConfig;

        [Tooltip("Fallback toggle binding if inputConfig is not set.")]
        [SerializeField] private string cameraToggleBinding = "<Keyboard>/v";

        [Tooltip("Fallback gimbal down binding if inputConfig is not set.")]
        [SerializeField] private string gimbalTiltDownBinding = "<Keyboard>/leftBracket";

        [Tooltip("Fallback gimbal up binding if inputConfig is not set.")]
        [SerializeField] private string gimbalTiltUpBinding = "<Keyboard>/rightBracket";

        [Tooltip("Fallback gimbal reset binding if inputConfig is not set.")]
        [SerializeField] private string gimbalResetBinding = "<Keyboard>/backslash";

        [Tooltip("Pitch speed (degrees/second) while holding tilt input.")]
        [Min(1f)]
        [SerializeField] private float gimbalPitchRate = 50f;

        [Header("References")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private SimpleFollowCamera chaseCamera;
        [SerializeField] private DroneGimbalCameraRig gimbalRig;
        [SerializeField] private DroneVideoFeed videoFeed;

        [Header("FPV presentation")]
        [Tooltip("If true, copy onboard camera transform/FOV every LateUpdate when in FPV mode.")]
        [SerializeField] private bool syncFpvPresentationEachFrame = true;

        private InputAction cameraToggleAction;
        private InputAction gimbalTiltDownAction;
        private InputAction gimbalTiltUpAction;
        private InputAction gimbalResetAction;

        private CameraMode currentMode;

        public CameraMode CurrentMode => currentMode;
        public string CurrentModeLabel => currentMode.ToString();

        public void Initialize(
            UnityEngine.Camera mainCam,
            SimpleFollowCamera chase,
            DroneGimbalCameraRig gimbal,
            DroneVideoFeed feed,
            DroneInputConfig config = null)
        {
            mainCamera = mainCam;
            chaseCamera = chase;
            gimbalRig = gimbal;
            videoFeed = feed;
            inputConfig = config != null ? config : inputConfig;

            RebuildInputActions();
            ApplyMode(startupMode);
        }

        private void Awake()
        {
            mainCamera ??= UnityEngine.Camera.main;
            chaseCamera ??= FindFirstObjectByType<SimpleFollowCamera>();
            gimbalRig ??= FindFirstObjectByType<DroneGimbalCameraRig>();
            videoFeed ??= FindFirstObjectByType<DroneVideoFeed>();
            inputConfig ??= FindFirstObjectByType<DroneInputReader>()?.Config;
        }

        private void OnEnable()
        {
            RebuildInputActions();
        }

        private void Start()
        {
            if (mainCamera != null)
            {
                ApplyMode(startupMode);
            }
        }

        private void Update()
        {
            HandleModeToggleInput();
            HandleGimbalInput();
        }

        private void LateUpdate()
        {
            if (currentMode == CameraMode.FPV && syncFpvPresentationEachFrame)
            {
                SyncMainCameraToOnboardView();
            }
        }

        private void OnDisable()
        {
            DisposeInputActions();
        }

        public void ApplyMode(CameraMode mode)
        {
            currentMode = mode;

            if (videoFeed != null)
            {
                videoFeed.EnsureFeedIsLive();
            }

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
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }

            if (chaseCamera != null)
            {
                chaseCamera.enabled = true;
            }
        }

        private void ActivateFpvMode()
        {
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }

            if (chaseCamera != null)
            {
                chaseCamera.enabled = false;
            }

            SyncMainCameraToOnboardView();
        }

        private void SyncMainCameraToOnboardView()
        {
            if (mainCamera == null || gimbalRig == null || gimbalRig.OnboardCamera == null)
            {
                return;
            }

            UnityEngine.Camera onboard = gimbalRig.OnboardCamera;
            Transform onboardTransform = onboard.transform;

            mainCamera.transform.SetPositionAndRotation(onboardTransform.position, onboardTransform.rotation);
            mainCamera.fieldOfView = onboard.fieldOfView;
            mainCamera.nearClipPlane = onboard.nearClipPlane;
            mainCamera.farClipPlane = onboard.farClipPlane;
        }

        private void HandleModeToggleInput()
        {
            if (cameraToggleAction != null && cameraToggleAction.WasPressedThisFrame())
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

            float up = gimbalTiltUpAction?.ReadValue<float>() ?? 0f;
            float down = gimbalTiltDownAction?.ReadValue<float>() ?? 0f;
            float pitchDelta = (up - down) * gimbalPitchRate * Time.deltaTime;

            if (Mathf.Abs(pitchDelta) > 0.0001f)
            {
                gimbalRig.AdjustTargetPitch(pitchDelta);
            }

            if (gimbalResetAction != null && gimbalResetAction.WasPressedThisFrame())
            {
                gimbalRig.SetTargetPitch(0f);
            }
        }

        private void RebuildInputActions()
        {
            DisposeInputActions();

            cameraToggleAction = CreateButtonAction("CameraToggle", ResolveBinding(inputConfig?.cameraToggleBinding, cameraToggleBinding));
            gimbalTiltDownAction = CreateButtonAction("GimbalTiltDown", ResolveBinding(inputConfig?.gimbalTiltDownBinding, gimbalTiltDownBinding));
            gimbalTiltUpAction = CreateButtonAction("GimbalTiltUp", ResolveBinding(inputConfig?.gimbalTiltUpBinding, gimbalTiltUpBinding));
            gimbalResetAction = CreateButtonAction("GimbalReset", ResolveBinding(inputConfig?.gimbalResetBinding, gimbalResetBinding));
        }

        private static InputAction CreateButtonAction(string name, string binding)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                return null;
            }

            InputAction action = new InputAction(name, InputActionType.Button, binding);
            action.Enable();
            return action;
        }

        private static string ResolveBinding(string preferred, string fallback)
        {
            return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        }

        private void DisposeInputActions()
        {
            cameraToggleAction?.Dispose();
            gimbalTiltDownAction?.Dispose();
            gimbalTiltUpAction?.Dispose();
            gimbalResetAction?.Dispose();
            cameraToggleAction = null;
            gimbalTiltDownAction = null;
            gimbalTiltUpAction = null;
            gimbalResetAction = null;
        }
    }
}
