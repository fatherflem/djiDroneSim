using DroneSim.Drone.Camera;
using DroneSim.Drone.Flight;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using DroneSim.Drone.Training;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace DroneSim.VR
{
    public class VRPilotBootstrap : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private string dronePrefabResourcePath = "DroneTrainerDrone";
        [SerializeField] private string inputConfigResourcePath = "Configs/DroneInputConfig";
        [SerializeField] private string cineConfigResourcePath = "Configs/DroneModeCine";
        [SerializeField] private string normalConfigResourcePath = "Configs/DroneModeNormal";
        [SerializeField] private string sportConfigResourcePath = "Configs/DroneModeSport";

        [Header("Layout")]
        [SerializeField] private Vector3 droneSpawnPosition = new(0f, 1.25f, -4f);
        [SerializeField] private Vector3 pilotRigPosition = new(-2.8f, 0f, 1.2f);
        [SerializeField] private Vector3 pilotRigEuler = new(0f, 42f, 0f);

        private void Start()
        {
            SetupXR();
            GameObject drone = SpawnDrone();
            DroneInputReader inputReader = EnsureFlightStack(drone);
            DroneVideoFeed feed = EnsureDroneCameraFeed(drone);
            EnsureTrainingScenario(drone.GetComponent<DronePhysicsBody>());
            BuildVirtualController(inputReader, feed);
        }

        private void SetupXR()
        {
            XROrigin origin = FindFirstObjectByType<XROrigin>();
            if (origin == null)
            {
                GameObject xrRoot = new("XR Origin");
                xrRoot.transform.SetPositionAndRotation(pilotRigPosition, Quaternion.Euler(pilotRigEuler));
                origin = xrRoot.AddComponent<XROrigin>();
                Camera cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.transform.SetParent(xrRoot.transform, false);
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 2000f;
                origin.Camera = cam;
            }

            InputTracking.disablePositionalTracking = false;
        }

        private GameObject SpawnDrone()
        {
            GameObject prefab = Resources.Load<GameObject>(dronePrefabResourcePath);
            return prefab != null
                ? Instantiate(prefab, droneSpawnPosition, Quaternion.identity)
                : new GameObject("DroneTrainerDrone");
        }

        private DroneInputReader EnsureFlightStack(GameObject drone)
        {
            DroneInputConfig inputConfig = Resources.Load<DroneInputConfig>(inputConfigResourcePath);
            DroneFlightModeConfig cine = Resources.Load<DroneFlightModeConfig>(cineConfigResourcePath);
            DroneFlightModeConfig normal = Resources.Load<DroneFlightModeConfig>(normalConfigResourcePath);
            DroneFlightModeConfig sport = Resources.Load<DroneFlightModeConfig>(sportConfigResourcePath);

            DroneVisualRig visualRig = drone.GetComponent<DroneVisualRig>() ?? drone.AddComponent<DroneVisualRig>();
            visualRig.EnsureVisuals();

            Rigidbody body = drone.GetComponent<Rigidbody>() ?? drone.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            DronePhysicsBody physics = drone.GetComponent<DronePhysicsBody>() ?? drone.AddComponent<DronePhysicsBody>();
            physics.Initialize(body);

            DroneInputReader input = drone.GetComponent<DroneInputReader>() ?? drone.AddComponent<DroneInputReader>();
            if (input.Config == null)
            {
                input.Initialize(inputConfig);
            }

            DJIStyleFlightController controller = drone.GetComponent<DJIStyleFlightController>() ?? drone.AddComponent<DJIStyleFlightController>();
            controller.Initialize(input, physics, visualRig.TiltRoot, cine, normal, sport);

            return input;
        }

        private DroneVideoFeed EnsureDroneCameraFeed(GameObject drone)
        {
            DroneGimbalCameraRig rig = drone.GetComponentInChildren<DroneGimbalCameraRig>();
            if (rig == null)
            {
                GameObject rigRoot = new("DroneCameraRig");
                rigRoot.transform.SetParent(drone.transform, false);
                rig = rigRoot.AddComponent<DroneGimbalCameraRig>();
            }

            rig.Initialize(drone.transform);

            DroneVideoFeed feed = rig.GetComponent<DroneVideoFeed>() ?? rig.gameObject.AddComponent<DroneVideoFeed>();
            feed.Initialize(rig);
            return feed;
        }

        private void EnsureTrainingScenario(DronePhysicsBody physicsBody)
        {
            SimpleTrainingScenario scenario = FindFirstObjectByType<SimpleTrainingScenario>();
            if (scenario == null)
            {
                scenario = new GameObject("TrainingScenario").AddComponent<SimpleTrainingScenario>();
            }

            scenario.Initialize(physicsBody);
        }

        private void BuildVirtualController(DroneInputReader input, DroneVideoFeed feed)
        {
            GameObject rigObj = new("VirtualDJIRC");
            VirtualRCControllerRig rig = rigObj.AddComponent<VirtualRCControllerRig>();
            AnchoredControllerPoseProvider anchored = rigObj.AddComponent<AnchoredControllerPoseProvider>();
            PlaceholderTrackedPropPoseProvider tracked = rigObj.AddComponent<PlaceholderTrackedPropPoseProvider>();
            rig.InjectPoseProviders(anchored, tracked);

            VirtualRCInputBridge inputBridge = rigObj.AddComponent<VirtualRCInputBridge>();
            DroneScreenFeedBridge screenBridge = rigObj.AddComponent<DroneScreenFeedBridge>();

            inputBridge.SetInputReader(input);
            screenBridge.SetVideoFeed(feed);
        }
    }
}
