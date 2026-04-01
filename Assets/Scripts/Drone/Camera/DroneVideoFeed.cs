using UnityEngine;

namespace DroneSim.Drone.Camera
{
    /// <summary>
    /// Purpose: Manages the RenderTexture output from the drone's onboard camera.
    /// Does NOT: decide where/how the feed is displayed (that's DroneFeedDisplaySurface or the mode controller).
    /// Fits in sim: single source-of-truth for the drone camera feed texture.
    /// Depends on: DroneGimbalCameraRig providing the onboard Camera component.
    ///
    /// --- VR CONTROLLER SCREEN HOOKUP ---
    /// To display this feed on a VR controller screen:
    ///   1. Access DroneVideoFeed.FeedTexture (the live RenderTexture).
    ///   2. Assign it to the controller screen mesh's material: material.mainTexture = feed.FeedTexture;
    ///   3. Or use DroneFeedDisplaySurface component on the screen mesh for automatic binding.
    ///   4. The feed renders every frame regardless of the active camera mode,
    ///      so the VR controller screen always shows the live drone camera view.
    /// </summary>
    public class DroneVideoFeed : MonoBehaviour
    {
        [Header("Feed configuration")]
        [Tooltip("Width of the feed render texture in pixels.")]
        [Min(64)]
        [SerializeField] private int feedWidth = 1280;

        [Tooltip("Height of the feed render texture in pixels.")]
        [Min(64)]
        [SerializeField] private int feedHeight = 720;

        [Tooltip("Depth buffer bits for the render texture. 16 or 24 recommended.")]
        [SerializeField] private int depthBits = 24;

        [Tooltip("Anti-aliasing sample count for the feed texture. 1 = none, 2/4/8 for MSAA.")]
        [SerializeField] private int antiAliasing = 2;

        [Header("References")]
        [Tooltip("The gimbal camera rig that provides the onboard camera.")]
        [SerializeField] private DroneGimbalCameraRig gimbalRig;

        private RenderTexture feedTexture;

        /// <summary>
        /// The live RenderTexture containing the drone camera feed.
        /// Assign this to any material, RawImage, or UI surface to display the feed.
        /// Returns null if the feed has not been initialized.
        /// </summary>
        public RenderTexture FeedTexture => feedTexture;

        /// <summary>The gimbal camera rig driving this feed.</summary>
        public DroneGimbalCameraRig GimbalRig => gimbalRig;

        /// <summary>Whether the feed is currently active and rendering.</summary>
        public bool IsActive => feedTexture != null && gimbalRig != null && gimbalRig.OnboardCamera != null;

        /// <summary>
        /// Initialize with an explicit gimbal rig reference.
        /// </summary>
        public void Initialize(DroneGimbalCameraRig rig)
        {
            gimbalRig = rig;
            EnsureFeedTexture();
            BindCameraToFeed();
        }

        private void Awake()
        {
            gimbalRig ??= GetComponentInParent<DroneGimbalCameraRig>()
                          ?? FindFirstObjectByType<DroneGimbalCameraRig>();
        }

        private void Start()
        {
            EnsureFeedTexture();
            BindCameraToFeed();
        }

        /// <summary>
        /// Recreate the feed texture at a new resolution.
        /// Useful if display requirements change at runtime.
        /// </summary>
        public void SetResolution(int width, int height)
        {
            feedWidth = Mathf.Max(64, width);
            feedHeight = Mathf.Max(64, height);
            ReleaseFeedTexture();
            EnsureFeedTexture();
            BindCameraToFeed();
        }

        private void EnsureFeedTexture()
        {
            if (feedTexture != null && feedTexture.width == feedWidth && feedTexture.height == feedHeight)
            {
                return;
            }

            ReleaseFeedTexture();

            feedTexture = new RenderTexture(feedWidth, feedHeight, depthBits);
            feedTexture.antiAliasing = antiAliasing;
            feedTexture.name = "DroneVideoFeed_RT";
            feedTexture.Create();
        }

        private void BindCameraToFeed()
        {
            if (gimbalRig == null || gimbalRig.OnboardCamera == null || feedTexture == null)
            {
                return;
            }

            // The onboard camera always renders to this texture.
            // When in FPV mode, the mode controller blits this to screen or enables the camera directly.
            // When in chase mode, this texture still updates for VR controller screen / picture-in-picture use.
            gimbalRig.OnboardCamera.targetTexture = feedTexture;
            gimbalRig.OnboardCamera.enabled = true;
        }

        /// <summary>
        /// Temporarily unbind the camera from the render texture so it can render directly to screen.
        /// Used by the mode controller for full-screen FPV with no blit overhead.
        /// Call BindCameraToFeed() to restore texture rendering.
        /// </summary>
        public void UnbindCameraFromFeed()
        {
            if (gimbalRig != null && gimbalRig.OnboardCamera != null)
            {
                gimbalRig.OnboardCamera.targetTexture = null;
            }
        }

        /// <summary>
        /// Re-bind the camera to the feed texture after a direct-render period.
        /// </summary>
        public void RebindCameraToFeed()
        {
            BindCameraToFeed();
        }

        private void ReleaseFeedTexture()
        {
            if (feedTexture != null)
            {
                if (gimbalRig != null && gimbalRig.OnboardCamera != null
                    && gimbalRig.OnboardCamera.targetTexture == feedTexture)
                {
                    gimbalRig.OnboardCamera.targetTexture = null;
                }

                feedTexture.Release();
                Destroy(feedTexture);
                feedTexture = null;
            }
        }

        private void OnDestroy()
        {
            ReleaseFeedTexture();
        }
    }
}
