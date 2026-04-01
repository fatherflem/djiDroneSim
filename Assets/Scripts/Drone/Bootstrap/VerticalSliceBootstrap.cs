using DroneSim.Drone.Camera;
using DroneSim.Drone.Flight;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using DroneSim.Drone.Benchmark;
using DroneSim.Drone.Training;
using DroneSim.Drone.UI;
using UnityEngine;
using UnityCamera = UnityEngine.Camera;

namespace DroneSim.Drone.Bootstrap
{
    /// <summary>
    /// Purpose: Optional runtime assembly helper for the vertical-slice scene.
    /// Does NOT: implement flight dynamics itself.
    /// Fits in sim: fallback wiring layer when scene-authored objects are missing.
    /// Depends on: scene-authored references first, then Resources/config assets as fallback.
    /// </summary>
    public class VerticalSliceBootstrap : MonoBehaviour
    {
        private enum BootstrapMode
        {
            Disabled = 0,
            FallbackOnly = 1,
            ForceRuntimeBuild = 2
        }

        [Header("Bootstrap mode")]
        [Tooltip("Disabled keeps the scene fully authored. FallbackOnly creates only missing objects. ForceRuntimeBuild always rebuilds everything at runtime.")]
        [SerializeField] private BootstrapMode bootstrapMode = BootstrapMode.FallbackOnly;

        [Header("Scene-authored references (preferred)")]
        [SerializeField] private GameObject sceneDrone;
        [SerializeField] private SimpleTrainingScenario sceneTrainingScenario;
        [SerializeField] private DroneDebugHUD sceneHud;
        [SerializeField] private RawJoystickDiagnosticsOverlay sceneJoystickDiagnostics;
        [SerializeField] private BenchmarkRunner sceneBenchmarkRunner;
        [SerializeField] private UnityCamera sceneCamera;
        [SerializeField] private DroneCameraModeController sceneCameraModeController;

        [Header("Resources")]
        [Tooltip("Resources path for drone prefab. If missing, a basic runtime drone object is created.")]
        [SerializeField] private string dronePrefabResourcePath = "DroneTrainerDrone";

        [Tooltip("Resources path for DroneInputConfig asset.")]
        [SerializeField] private string inputConfigResourcePath = "Configs/DroneInputConfig";

        [Tooltip("Resources path for Cine mode config asset.")]
        [SerializeField] private string cineConfigResourcePath = "Configs/DroneModeCine";

        [Tooltip("Resources path for Normal mode config asset.")]
        [SerializeField] private string normalConfigResourcePath = "Configs/DroneModeNormal";

        [Tooltip("Resources path for Sport mode config asset.")]
        [SerializeField] private string sportConfigResourcePath = "Configs/DroneModeSport";

        [Header("Initial layout")]
        [SerializeField] private Vector3 droneSpawnPosition = new Vector3(0f, 1.25f, -4f);
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 4f, -8f);

        private void Start()
        {
            if (bootstrapMode == BootstrapMode.Disabled)
            {
                return;
            }

            EnsurePhysicsSettings();

            if (bootstrapMode == BootstrapMode.ForceRuntimeBuild)
            {
                sceneDrone = null;
                sceneTrainingScenario = null;
                sceneHud = null;
                sceneJoystickDiagnostics = null;
                sceneBenchmarkRunner = null;
                sceneCamera = null;
                sceneCameraModeController = null;
            }

            DroneInputConfig inputConfig = Resources.Load<DroneInputConfig>(inputConfigResourcePath);
            DroneFlightModeConfig cineConfig = Resources.Load<DroneFlightModeConfig>(cineConfigResourcePath);
            DroneFlightModeConfig normalConfig = Resources.Load<DroneFlightModeConfig>(normalConfigResourcePath);
            DroneFlightModeConfig sportConfig = Resources.Load<DroneFlightModeConfig>(sportConfigResourcePath);

            GameObject drone = ResolveOrCreateDrone();
            if (drone == null)
            {
                Debug.LogError("VerticalSliceBootstrap could not find or create a drone.");
                return;
            }

            DroneVisualRig visualRig = drone.GetComponent<DroneVisualRig>() ?? drone.AddComponent<DroneVisualRig>();
            visualRig.EnsureVisuals();

            Rigidbody body = drone.GetComponent<Rigidbody>() ?? drone.AddComponent<Rigidbody>();
            BoxCollider collider = drone.GetComponent<BoxCollider>() ?? drone.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.7f, 0.18f, 0.7f);
            collider.center = Vector3.zero;

            DronePhysicsBody physicsBody = drone.GetComponent<DronePhysicsBody>() ?? drone.AddComponent<DronePhysicsBody>();
            physicsBody.Initialize(body);

            DroneInputReader inputReader = drone.GetComponent<DroneInputReader>() ?? drone.AddComponent<DroneInputReader>();
            if (inputReader.Config == null)
            {
                inputReader.Initialize(inputConfig);
            }

            DJIStyleFlightController controller = drone.GetComponent<DJIStyleFlightController>() ?? drone.AddComponent<DJIStyleFlightController>();
            controller.Initialize(inputReader, physicsBody, visualRig.TiltRoot, cineConfig, normalConfig, sportConfig);

            TelemetryRecorder telemetry = drone.GetComponent<TelemetryRecorder>() ?? drone.AddComponent<TelemetryRecorder>();
            telemetry.Initialize(physicsBody, controller);

            SimpleTrainingScenario scenario = ResolveOrCreateTrainingScenario(physicsBody);
            DroneDebugHUD hud = ResolveOrCreateHud(inputReader, physicsBody, controller, scenario, telemetry);
            _ = hud;
            ResolveOrCreateJoystickDiagnostics();
            ResolveOrCreateBenchmarkRunner(inputReader, physicsBody, controller);

            EnsureGround();
            EnsureMarkers();
            EnsureLight();
            EnsureFollowCamera(drone.transform);
            EnsureDroneCameraSystem(drone, hud);
        }

        private GameObject ResolveOrCreateDrone()
        {
            if (sceneDrone != null)
            {
                return sceneDrone;
            }

            DJIStyleFlightController authoredController = FindFirstObjectByType<DJIStyleFlightController>();
            if (authoredController != null)
            {
                sceneDrone = authoredController.gameObject;
                return sceneDrone;
            }

            GameObject dronePrefab = Resources.Load<GameObject>(dronePrefabResourcePath);
            GameObject drone = dronePrefab != null
                ? Instantiate(dronePrefab, droneSpawnPosition, Quaternion.identity)
                : new GameObject("DroneRoot");

            if (dronePrefab == null)
            {
                drone.transform.position = droneSpawnPosition;
            }

            sceneDrone = drone;
            return drone;
        }

        private SimpleTrainingScenario ResolveOrCreateTrainingScenario(DronePhysicsBody physicsBody)
        {
            if (sceneTrainingScenario == null)
            {
                sceneTrainingScenario = FindFirstObjectByType<SimpleTrainingScenario>();
            }

            if (sceneTrainingScenario == null)
            {
                GameObject trainingObject = new GameObject("TrainingScenario");
                sceneTrainingScenario = trainingObject.AddComponent<SimpleTrainingScenario>();
            }

            sceneTrainingScenario.Initialize(physicsBody);
            return sceneTrainingScenario;
        }

        private DroneDebugHUD ResolveOrCreateHud(
            DroneInputReader inputReader,
            DronePhysicsBody physicsBody,
            DJIStyleFlightController controller,
            SimpleTrainingScenario scenario,
            TelemetryRecorder telemetry)
        {
            if (sceneHud == null)
            {
                sceneHud = FindFirstObjectByType<DroneDebugHUD>();
            }

            if (sceneHud == null)
            {
                GameObject hudObject = new GameObject("DebugHUD");
                sceneHud = hudObject.AddComponent<DroneDebugHUD>();
            }

            sceneHud.Initialize(inputReader, physicsBody, controller, scenario, telemetry);
            return sceneHud;
        }


        private RawJoystickDiagnosticsOverlay ResolveOrCreateJoystickDiagnostics()
        {
            if (sceneJoystickDiagnostics == null)
            {
                sceneJoystickDiagnostics = FindFirstObjectByType<RawJoystickDiagnosticsOverlay>();
            }

            if (sceneJoystickDiagnostics == null)
            {
                GameObject diagnosticsObject = new GameObject("RawJoystickDiagnostics");
                sceneJoystickDiagnostics = diagnosticsObject.AddComponent<RawJoystickDiagnosticsOverlay>();
            }

            return sceneJoystickDiagnostics;
        }

        private BenchmarkRunner ResolveOrCreateBenchmarkRunner(
            DroneInputReader inputReader,
            DronePhysicsBody physicsBody,
            DJIStyleFlightController controller)
        {
            if (sceneBenchmarkRunner == null)
            {
                sceneBenchmarkRunner = FindFirstObjectByType<BenchmarkRunner>();
            }

            if (sceneBenchmarkRunner == null)
            {
                GameObject benchmarkObject = new GameObject("BenchmarkRunner");
                sceneBenchmarkRunner = benchmarkObject.AddComponent<BenchmarkRunner>();
            }

            sceneBenchmarkRunner.Initialize(inputReader, physicsBody, controller);
            return sceneBenchmarkRunner;
        }

        private void EnsureDroneCameraSystem(GameObject drone, DroneDebugHUD hud)
        {
            // 1. Gimbal camera rig — the physical onboard camera with gimbal pitch control.
            DroneGimbalCameraRig gimbalRig = drone.GetComponentInChildren<DroneGimbalCameraRig>();
            if (gimbalRig == null)
            {
                GameObject rigObject = new GameObject("GimbalCameraRig");
                gimbalRig = rigObject.AddComponent<DroneGimbalCameraRig>();
                gimbalRig.Initialize(drone.transform);
            }

            // 2. Video feed — manages the RenderTexture that captures the onboard camera output.
            DroneVideoFeed videoFeed = FindFirstObjectByType<DroneVideoFeed>();
            if (videoFeed == null)
            {
                GameObject feedObject = new GameObject("DroneVideoFeed");
                videoFeed = feedObject.AddComponent<DroneVideoFeed>();
            }
            videoFeed.Initialize(gimbalRig);

            // 3. Camera mode controller — switches between Chase and FPV views.
            if (sceneCameraModeController == null)
            {
                sceneCameraModeController = FindFirstObjectByType<DroneCameraModeController>();
            }
            if (sceneCameraModeController == null)
            {
                GameObject modeObject = new GameObject("CameraModeController");
                sceneCameraModeController = modeObject.AddComponent<DroneCameraModeController>();
            }

            SimpleFollowCamera followCam = sceneCamera != null
                ? sceneCamera.GetComponent<SimpleFollowCamera>()
                : FindFirstObjectByType<SimpleFollowCamera>();

            sceneCameraModeController.Initialize(sceneCamera, followCam, gimbalRig, videoFeed, inputConfig);

            // 4. Wire camera references into the debug HUD for mode/gimbal display.
            if (hud != null)
            {
                hud.InitializeCamera(sceneCameraModeController, gimbalRig);
            }
        }

        private static void EnsurePhysicsSettings()
        {
            UnityEngine.Physics.gravity = new Vector3(0f, -9.81f, 0f);
        }

        private void EnsureGround()
        {
            if (GameObject.Find("Ground") != null)
            {
                return;
            }

            CreateGround();
        }

        private void EnsureMarkers()
        {
            if (GameObject.Find("HoverBoxEdge") != null || GameObject.Find("Marker") != null)
            {
                return;
            }

            CreateMarkers();
        }

        private void EnsureLight()
        {
            if (FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void EnsureFollowCamera(Transform target)
        {
            if (sceneCamera == null)
            {
                sceneCamera = UnityCamera.main;
            }

            if (sceneCamera == null)
            {
                GameObject cameraObject = new GameObject("TrainingCamera");
                sceneCamera = cameraObject.AddComponent<UnityCamera>();
                sceneCamera.clearFlags = CameraClearFlags.Skybox;
                sceneCamera.tag = "MainCamera";

                GameObject listenerObject = new GameObject("AudioListener");
                listenerObject.transform.SetParent(sceneCamera.transform, false);
                listenerObject.AddComponent<AudioListener>();
            }

            if (sceneCamera.GetComponent<SimpleFollowCamera>() == null)
            {
                sceneCamera.gameObject.AddComponent<SimpleFollowCamera>();
            }

            sceneCamera.transform.position = target.position + cameraOffset;
            sceneCamera.transform.LookAt(target.position + Vector3.up * 1.25f);
            sceneCamera.GetComponent<SimpleFollowCamera>().Initialize(target, cameraOffset);
        }

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateCompatibleMaterial(renderer, new Color(0.21f, 0.26f, 0.22f));
            }
        }

        private void CreateMarkers()
        {
            CreateMarker(new Vector3(-3f, 1f, 0f), new Color(0.95f, 0.4f, 0.25f));
            CreateMarker(new Vector3(3f, 1f, 0f), new Color(0.25f, 0.7f, 0.95f));
            CreateMarker(new Vector3(0f, 1f, 3f), new Color(0.95f, 0.85f, 0.25f));
            CreateMarker(new Vector3(0f, 1f, -3f), new Color(0.4f, 0.95f, 0.55f));

            CreateHoverBoxEdge(new Vector3(-1.5f, 0.02f, 0f), new Vector3(0.1f, 0.04f, 3f));
            CreateHoverBoxEdge(new Vector3(1.5f, 0.02f, 0f), new Vector3(0.1f, 0.04f, 3f));
            CreateHoverBoxEdge(new Vector3(0f, 0.02f, -1.5f), new Vector3(3f, 0.04f, 0.1f));
            CreateHoverBoxEdge(new Vector3(0f, 0.02f, 1.5f), new Vector3(3f, 0.04f, 0.1f));
        }

        private void CreateMarker(Vector3 position, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Marker";
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(0.18f, 1f, 0.18f);
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateCompatibleMaterial(renderer, color);
            }
        }

        private void CreateHoverBoxEdge(Vector3 position, Vector3 scale)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "HoverBoxEdge";
            edge.transform.position = position;
            edge.transform.localScale = scale;
            Renderer renderer = edge.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateCompatibleMaterial(renderer, new Color(0.3f, 0.9f, 0.9f));
            }
        }

        private static Material CreateCompatibleMaterial(Renderer renderer, Color color)
        {
            Material baseMaterial = renderer.sharedMaterial;
            Material material = baseMaterial != null
                ? new Material(baseMaterial)
                : new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));

            material.color = color;
            return material;
        }
    }

    public class SimpleFollowCamera : MonoBehaviour
    {
        private Transform target;
        private Vector3 offset;

        public void Initialize(Transform followTarget, Vector3 followOffset)
        {
            target = followTarget;
            offset = followOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, target.position + offset, 1f - Mathf.Exp(-4f * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }
    }
}
