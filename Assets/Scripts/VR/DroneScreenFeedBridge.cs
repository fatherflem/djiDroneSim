using DroneSim.Drone.Camera;
using UnityEngine;

namespace DroneSim.VR
{
    public class DroneScreenFeedBridge : MonoBehaviour
    {
        [SerializeField] private VirtualRCControllerRig controllerRig;
        [SerializeField] private DroneVideoFeed videoFeed;

        private DroneFeedDisplaySurface displaySurface;

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
            if (videoFeed == null)
            {
                videoFeed = FindFirstObjectByType<DroneVideoFeed>();
                if (videoFeed != null)
                {
                    displaySurface?.SetVideoFeed(videoFeed);
                }
            }
        }
    }
}
