using UnityEngine;

namespace DroneSim.Drone.Camera
{
    /// <summary>
    /// Purpose: Manages the always-live RenderTexture output from the drone's onboard camera.
    /// Does NOT: decide where/how the feed is displayed.
    /// Fits in sim: single source-of-truth for drone camera feed texture lifetime + binding.
    /// Depends on: DroneGimbalCameraRig providing the onboard Camera component.
    /// </summary>
    public class DroneVideoFeed : MonoBehaviour
    {
        [Header("Feed configuration")]
        [Min(64)]
        [SerializeField] private int feedWidth = 1280;

        [Min(64)]
        [SerializeField] private int feedHeight = 720;

        [SerializeField] private int depthBits = 24;
        [SerializeField] private int antiAliasing = 2;

        [Header("References")]
        [SerializeField] private DroneGimbalCameraRig gimbalRig;

        private RenderTexture feedTexture;

        public RenderTexture FeedTexture => feedTexture;
        public DroneGimbalCameraRig GimbalRig => gimbalRig;
        public bool IsActive => feedTexture != null && gimbalRig != null && gimbalRig.OnboardCamera != null;
        public int FeedWidth => feedTexture != null ? feedTexture.width : feedWidth;
        public int FeedHeight => feedTexture != null ? feedTexture.height : feedHeight;
        public bool IsCameraBoundToFeed =>
            gimbalRig != null
            && gimbalRig.OnboardCamera != null
            && feedTexture != null
            && gimbalRig.OnboardCamera.targetTexture == feedTexture;
        public bool IsFeedTextureCreated => feedTexture != null && feedTexture.IsCreated();

        public void Initialize(DroneGimbalCameraRig rig)
        {
            gimbalRig = rig;
            EnsureFeedIsLive();
        }

        private void Awake()
        {
            gimbalRig ??= GetComponentInParent<DroneGimbalCameraRig>()
                          ?? FindFirstObjectByType<DroneGimbalCameraRig>();
        }

        private void Start()
        {
            EnsureFeedIsLive();
        }

        private void LateUpdate()
        {
            // Defensive: keep feed alive if another script changed camera.targetTexture.
            EnsureFeedIsLive();
        }

        public void SetResolution(int width, int height)
        {
            feedWidth = Mathf.Max(64, width);
            feedHeight = Mathf.Max(64, height);
            ReleaseFeedTexture();
            EnsureFeedIsLive();
        }

        /// <summary>
        /// Guarantees that the onboard camera continuously renders into the persistent feed texture.
        /// Safe to call repeatedly.
        /// </summary>
        public void EnsureFeedIsLive()
        {
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

            feedTexture = new RenderTexture(feedWidth, feedHeight, depthBits)
            {
                antiAliasing = antiAliasing,
                name = "DroneVideoFeed_RT"
            };
            feedTexture.Create();
        }

        private void BindCameraToFeed()
        {
            if (gimbalRig == null || gimbalRig.OnboardCamera == null || feedTexture == null)
            {
                return;
            }

            if (gimbalRig.OnboardCamera.targetTexture != feedTexture)
            {
                gimbalRig.OnboardCamera.targetTexture = feedTexture;
            }

            if (!gimbalRig.OnboardCamera.enabled)
            {
                gimbalRig.OnboardCamera.enabled = true;
            }
        }

        private void ReleaseFeedTexture()
        {
            if (feedTexture == null)
            {
                return;
            }

            if (gimbalRig != null && gimbalRig.OnboardCamera != null
                && gimbalRig.OnboardCamera.targetTexture == feedTexture)
            {
                gimbalRig.OnboardCamera.targetTexture = null;
            }

            feedTexture.Release();
            Destroy(feedTexture);
            feedTexture = null;
        }

        private void OnDestroy()
        {
            ReleaseFeedTexture();
        }
    }
}
