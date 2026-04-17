using DroneSim.Drone.Bootstrap;
using DroneSim.Drone.Camera;
using DroneSim.Drone.Flight;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using DroneSim.Drone.Training;
using Unity.XR.CoreUtils;
using DroneSim.Drone.Rendering;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace DroneSim.VR
{
    /// <summary>
    /// Scoped VR bootstrap for a stationary pilot workflow.
    /// Preserves existing drone/controller/feed architecture and bridges it to XR Origin.
    /// </summary>
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
        [SerializeField] private bool ensureMinimalTestEnvironment = true;
        [SerializeField] private bool logBootstrapDiagnostics = true;

        [Header("VR pilot tuning")]
        [SerializeField] private float operatorStandingHeightMeters = 1.67f;
        [SerializeField] private bool alignXrOriginToOperatorPlaceholder = true;

        private void Start()
        {
            VRUserPlaceholder vrUserPlaceholder = EnsureOperatorPlaceholder();
            XROrigin origin = SetupXR(vrUserPlaceholder);

            if (ensureMinimalTestEnvironment)
            {
                EnsureMinimalTestEnvironment();
            }

            GameObject drone = GetOrSpawnDrone();
            DroneInputReader inputReader = EnsureFlightStack(drone);
            DroneVideoFeed feed = EnsureDroneCameraFeed(drone);
            EnsureTrainingScenario(drone.GetComponent<DronePhysicsBody>());
            BuildVirtualController(inputReader, feed, vrUserPlaceholder, origin);
        }

        private VRUserPlaceholder EnsureOperatorPlaceholder()
        {
            VRUserPlaceholder placeholder = FindFirstObjectByType<VRUserPlaceholder>();
            if (placeholder == null)
            {
                GameObject placeholderRoot = new("VRUserPlaceholder");
                placeholder = placeholderRoot.AddComponent<VRUserPlaceholder>();
            }

            placeholder.transform.SetPositionAndRotation(pilotRigPosition, Quaternion.Euler(pilotRigEuler));
            placeholder.EnsurePlaceholderHierarchy();
            return placeholder;
        }

        private XROrigin SetupXR(VRUserPlaceholder placeholder)
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
                cam.gameObject.AddComponent<AudioListener>();
                origin.Camera = cam;
            }

            if (origin.Camera != null && origin.Camera.GetComponent<TrackedPoseDriver>() == null)
            {
                origin.Camera.gameObject.AddComponent<TrackedPoseDriver>();
            }

            if (origin.CameraFloorOffsetObject != null)
            {
                Vector3 offset = origin.CameraFloorOffsetObject.transform.localPosition;
                offset.y = Mathf.Max(1.0f, operatorStandingHeightMeters);
                origin.CameraFloorOffsetObject.transform.localPosition = offset;
            }

            if (alignXrOriginToOperatorPlaceholder && placeholder != null)
            {
                Transform desiredHead = placeholder.VRCameraAnchor != null ? placeholder.VRCameraAnchor : placeholder.HeadAnchor;
                Transform xrHead = origin.Camera != null ? origin.Camera.transform : null;
                if (desiredHead != null && xrHead != null)
                {
                    Vector3 headDelta = desiredHead.position - xrHead.position;
                    origin.transform.position += headDelta;
                }

                origin.transform.rotation = placeholder.transform.rotation;
            }

            return origin;
        }

        private GameObject GetOrSpawnDrone()
        {
            DronePhysicsBody existingPhysicsBody = FindFirstObjectByType<DronePhysicsBody>();
            if (existingPhysicsBody != null)
            {
                return existingPhysicsBody.gameObject;
            }

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

        private void BuildVirtualController(
            DroneInputReader input,
            DroneVideoFeed feed,
            VRUserPlaceholder placeholder,
            XROrigin origin)
        {
            VirtualRCControllerRig rig = FindFirstObjectByType<VirtualRCControllerRig>();
            GameObject rigObj = rig != null ? rig.gameObject : new GameObject("VirtualDJIRC");
            rig ??= rigObj.AddComponent<VirtualRCControllerRig>();

            AnchoredControllerPoseProvider anchored = rigObj.GetComponent<AnchoredControllerPoseProvider>()
                                                    ?? rigObj.AddComponent<AnchoredControllerPoseProvider>();
            PlaceholderTrackedPropPoseProvider tracked = rigObj.GetComponent<PlaceholderTrackedPropPoseProvider>()
                                                           ?? rigObj.AddComponent<PlaceholderTrackedPropPoseProvider>();
            tracked.SetTrackedPropTransform(placeholder != null ? placeholder.ControllerPropAnchor : null);
            anchored.Configure(
                origin,
                origin != null && origin.Camera != null ? origin.Camera.transform : null,
                placeholder != null ? placeholder.ChestAnchor : null);

            rig.InjectPoseProviders(anchored, tracked);

            VirtualRCInputBridge inputBridge = rigObj.GetComponent<VirtualRCInputBridge>()
                                              ?? rigObj.AddComponent<VirtualRCInputBridge>();
            DroneScreenFeedBridge screenBridge = rigObj.GetComponent<DroneScreenFeedBridge>()
                                               ?? rigObj.AddComponent<DroneScreenFeedBridge>();

            inputBridge.SetInputReader(input);
            screenBridge.SetVideoFeed(feed);
        }

        private void EnsureMinimalTestEnvironment()
        {
            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject lightRoot = new("VR Scene Light");
                Light light = lightRoot.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                lightRoot.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            if (GameObject.Find("VR_TestFloor") == null)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "VR_TestFloor";
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(3f, 1f, 3f);
                Renderer renderer = floor.GetComponent<Renderer>();
                renderer.material = new Material(RuntimeShaderCache.LitShader ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color"))
                {
                    color = new Color(0.22f, 0.22f, 0.24f)
                };
            }

            if (logBootstrapDiagnostics)
            {
                Debug.Log("VRPilotBootstrap ready: stationary XR pilot + operator-aligned RC/feed bridge initialized.");
            }
        }
    }
}
