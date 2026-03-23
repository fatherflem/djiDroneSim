using DroneSim.Drone.Flight;
using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using DroneSim.Drone.Training;
using DroneSim.Drone.UI;
using UnityEngine;

namespace DroneSim.Drone.Bootstrap
{
    public class VerticalSliceBootstrap : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private string dronePrefabResourcePath = "DroneTrainerDrone";
        [SerializeField] private string inputConfigResourcePath = "Configs/DroneInputConfig";
        [SerializeField] private string cineConfigResourcePath = "Configs/DroneModeCine";
        [SerializeField] private string normalConfigResourcePath = "Configs/DroneModeNormal";
        [SerializeField] private string sportConfigResourcePath = "Configs/DroneModeSport";

        [Header("Layout")]
        [SerializeField] private Vector3 droneSpawnPosition = new Vector3(0f, 1.25f, -4f);
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 4f, -8f);

        private void Start()
        {
            EnsurePhysicsSettings();
            CreateGround();
            CreateMarkers();
            EnsureLight();

            DroneInputConfig inputConfig = Resources.Load<DroneInputConfig>(inputConfigResourcePath);
            DroneFlightModeConfig cineConfig = Resources.Load<DroneFlightModeConfig>(cineConfigResourcePath);
            DroneFlightModeConfig normalConfig = Resources.Load<DroneFlightModeConfig>(normalConfigResourcePath);
            DroneFlightModeConfig sportConfig = Resources.Load<DroneFlightModeConfig>(sportConfigResourcePath);

            GameObject dronePrefab = Resources.Load<GameObject>(dronePrefabResourcePath);
            GameObject drone = dronePrefab != null
                ? Instantiate(dronePrefab, droneSpawnPosition, Quaternion.identity)
                : new GameObject("DroneRoot");

            if (dronePrefab == null)
            {
                drone.transform.position = droneSpawnPosition;
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
            inputReader.Initialize(inputConfig);

            DJIStyleFlightController controller = drone.GetComponent<DJIStyleFlightController>() ?? drone.AddComponent<DJIStyleFlightController>();
            controller.Initialize(inputReader, physicsBody, visualRig.TiltRoot, cineConfig, normalConfig, sportConfig);

            TelemetryRecorder telemetry = drone.GetComponent<TelemetryRecorder>() ?? drone.AddComponent<TelemetryRecorder>();
            telemetry.Initialize(physicsBody, controller);

            GameObject trainingObject = new GameObject("TrainingScenario");
            SimpleTrainingScenario scenario = trainingObject.AddComponent<SimpleTrainingScenario>();
            scenario.Initialize(physicsBody);

            GameObject hudObject = new GameObject("DebugHUD");
            DroneDebugHUD hud = hudObject.AddComponent<DroneDebugHUD>();
            hud.Initialize(inputReader, physicsBody, controller, scenario, telemetry);

            CreateFollowCamera(drone.transform);
        }

        private static void EnsurePhysicsSettings()
        {
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.color = new Color(0.21f, 0.26f, 0.22f);
                renderer.sharedMaterial = material;
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

        private void EnsureLight()
        {
            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void CreateFollowCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("TrainingCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.transform.position = target.position + cameraOffset;
            camera.transform.LookAt(target.position + Vector3.up * 1.25f);

            GameObject listenerObject = new GameObject("AudioListener");
            listenerObject.transform.SetParent(camera.transform, false);
            listenerObject.AddComponent<AudioListener>();

            camera.tag = "MainCamera";
            cameraObject.AddComponent<SimpleFollowCamera>().Initialize(target, cameraOffset);
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.color = color;
                renderer.sharedMaterial = material;
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.color = new Color(0.3f, 0.9f, 0.9f);
                renderer.sharedMaterial = material;
            }
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
