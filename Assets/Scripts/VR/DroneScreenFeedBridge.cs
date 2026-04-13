using DroneSim.Drone.Camera;
using UnityEngine;

namespace DroneSim.VR
{
    public class DroneScreenFeedBridge : MonoBehaviour
    {
        [SerializeField] private VirtualRCControllerRig controllerRig;
        [SerializeField] private DroneVideoFeed videoFeed;
        [SerializeField] private float feedDiscoveryRetrySeconds = 0.5f;
        [SerializeField] private float warningAfterSeconds = 5f;

        private DroneFeedDisplaySurface displaySurface;
        private float nextDiscoveryTime;
        private bool hasWarnedMissingFeed;

        public void SetVideoFeed(DroneVideoFeed feed)
        {
            videoFeed = feed;
            if (displaySurface != null && videoFeed != null)
            {
                displaySurface.SetVideoFeed(videoFeed);
            }
        }

        private void Awake()
        {
            controllerRig ??= GetComponent<VirtualRCControllerRig>();
            videoFeed ??= FindFirstObjectByType<DroneVideoFeed>();
            nextDiscoveryTime = Time.unscaledTime;

            if (controllerRig != null && controllerRig.ScreenRenderer != null)
            {
                displaySurface = controllerRig.ScreenRenderer.gameObject.GetComponent<DroneFeedDisplaySurface>();
                if (displaySurface == null)
                {
                    displaySurface = controllerRig.ScreenRenderer.gameObject.AddComponent<DroneFeedDisplaySurface>();
                }
            }

            if (displaySurface != null && videoFeed != null)
            {
                displaySurface.SetVideoFeed(videoFeed);
            }
        }

        private void LateUpdate()
        {
            if (displaySurface == null && controllerRig != null && controllerRig.ScreenRenderer != null)
            {
                displaySurface = controllerRig.ScreenRenderer.gameObject.GetComponent<DroneFeedDisplaySurface>()
                                 ?? controllerRig.ScreenRenderer.gameObject.AddComponent<DroneFeedDisplaySurface>();
            }

            if (videoFeed == null && Time.unscaledTime >= nextDiscoveryTime)
            {
                videoFeed = FindFirstObjectByType<DroneVideoFeed>();
                nextDiscoveryTime = Time.unscaledTime + Mathf.Max(0.1f, feedDiscoveryRetrySeconds);
                if (videoFeed != null)
                {
                    displaySurface?.SetVideoFeed(videoFeed);
                    hasWarnedMissingFeed = false;
                }
                else if (!hasWarnedMissingFeed && Time.unscaledTime >= warningAfterSeconds)
                {
                    hasWarnedMissingFeed = true;
                    Debug.LogWarning(
                        "DroneScreenFeedBridge: no DroneVideoFeed found. " +
                        "The RC screen will stay dark until a DroneVideoFeed exists and is enabled.");
                }
            }
        }
    }
}
