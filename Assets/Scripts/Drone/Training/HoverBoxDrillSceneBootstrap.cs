using DroneSim.VR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using DroneSim.Drone.Physics;

namespace DroneSim.Drone.Training
{
    [DefaultExecutionOrder(100)]
    public class HoverBoxDrillSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool vrMode;
        [SerializeField] private Vector3 aircraftStartPosition = new(0f, 1.25f, -4f);
        [SerializeField] private Vector3 aircraftStartEuler;

        private void Start()
        {
            EnsureFieldLoader();

            HoverBoxDrill drill = FindFirstObjectByType<HoverBoxDrill>();
            if (drill == null)
            {
                drill = new GameObject("HoverBoxDrill").AddComponent<HoverBoxDrill>();
            }

            var definition = Resources.Load<TrainingDrillDefinition>("Training/HoverBoxDrillDefinition");
            if (definition == null)
            {
                Debug.LogError("Hover Box cannot start: Resources/Training/HoverBoxDrillDefinition is missing.", this);
                enabled = false;
                return;
            }
            drill.Configure(definition);

            DronePhysicsBody drone = FindFirstObjectByType<DronePhysicsBody>();
            if (drone == null || drone.Body == null)
            {
                Debug.LogError("Hover Box cannot start: no initialized DronePhysicsBody was created by the scene bootstrap.", this);
                enabled = false;
                return;
            }

            TrainingAircraftReset reset = drill.GetComponent<TrainingAircraftReset>() ?? drill.gameObject.AddComponent<TrainingAircraftReset>();
            if (!reset.Initialize(drill, drone, aircraftStartPosition, Quaternion.Euler(aircraftStartEuler)))
            {
                enabled = false;
                return;
            }

            TrainingActionInput actionInput = drill.GetComponent<TrainingActionInput>() ?? drill.gameObject.AddComponent<TrainingActionInput>();
            actionInput.Initialize(drill);

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
                EnsureEventSystem();
                if (FindFirstObjectByType<HoverBoxDrillDesktopUI>() == null)
                {
                    new GameObject("HoverBoxDrillDesktopUI").AddComponent<HoverBoxDrillDesktopUI>();
                }
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null) Destroy(legacy);
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                InputSystemUIInputModule module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
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
