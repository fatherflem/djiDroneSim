using DroneSim.VR;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    public class HoverBoxDrillSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool vrMode;

        private void Start()
        {
            EnsureFieldLoader();

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

        private static void EnsureFieldLoader()
        {
            if (FindFirstObjectByType<DroneSim.Drone.Environment.FieldLoader>() != null)
            {
                return;
            }

            GameObject loaderObj = new GameObject("FieldLoader");
            var loader = loaderObj.AddComponent<DroneSim.Drone.Environment.FieldLoader>();
            var def = Resources.Load<DroneSim.Drone.Environment.FieldDefinition>("Environments/PlaceholderField");
            loader.SetField(def);
        }
    }
}
