using DroneSim.Drone.Physics;
using DroneSim.VR;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    public class HoverBoxDrillSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool vrMode;

        private void Start()
        {
            DronePhysicsBody body = FindFirstObjectByType<DronePhysicsBody>();
            if (body == null && !vrMode)
            {
                body = FindFirstObjectByType<DronePhysicsBody>();
            }

            HoverBoxDrill drill = FindFirstObjectByType<HoverBoxDrill>();
            if (drill == null)
            {
                drill = new GameObject("HoverBoxDrill").AddComponent<HoverBoxDrill>();
            }

            if (vrMode)
            {
                if (FindFirstObjectByType<VRPilotBootstrap>() == null)
                {
                    new GameObject("VRPilotBootstrap").AddComponent<VRPilotBootstrap>();
                }

                if (FindFirstObjectByType<HoverBoxDrillVRUI>() == null)
                {
                    new GameObject("HoverBoxDrillVRUI").AddComponent<HoverBoxDrillVRUI>();
                }
            }
            else
            {
                if (FindFirstObjectByType<HoverBoxDrillDesktopUI>() == null)
                {
                    new GameObject("HoverBoxDrillDesktopUI").AddComponent<HoverBoxDrillDesktopUI>();
                }
            }
        }
    }
}
