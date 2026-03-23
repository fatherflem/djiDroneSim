using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    public class SimpleTrainingScenario : MonoBehaviour
    {
        [SerializeField] private DronePhysicsBody droneBody;
        [SerializeField] private Vector3 hoverBoxCenter = new Vector3(0f, 2f, 0f);
        [SerializeField] private Vector2 hoverBoxSize = new Vector2(3f, 3f);
        [SerializeField] private float targetAltitude = 2f;
        [SerializeField] private float altitudeTolerance = 0.5f;
        [SerializeField] private float requiredHoverTime = 15f;
        [SerializeField] private float idealSpeedThreshold = 0.75f;

        private float accumulatedHoverTime;
        private bool completed;

        public float AccumulatedHoverTime => accumulatedHoverTime;
        public float Completion01 => Mathf.Clamp01(accumulatedHoverTime / requiredHoverTime);
        public bool Completed => completed;
        public Vector3 HoverBoxCenter => hoverBoxCenter;
        public Vector2 HoverBoxSize => hoverBoxSize;
        public float TargetAltitude => targetAltitude;
        public bool IsDroneInsideBox { get; private set; }

        public void Initialize(DronePhysicsBody body)
        {
            droneBody = body;
        }

        private void Update()
        {
            if (droneBody == null)
            {
                return;
            }

            Vector3 position = droneBody.transform.position;
            Vector3 relative = position - hoverBoxCenter;
            bool insideHorizontal = Mathf.Abs(relative.x) <= hoverBoxSize.x * 0.5f && Mathf.Abs(relative.z) <= hoverBoxSize.y * 0.5f;
            bool insideVertical = Mathf.Abs(position.y - targetAltitude) <= altitudeTolerance;
            bool lowSpeed = droneBody.HorizontalVelocity.magnitude <= idealSpeedThreshold;
            IsDroneInsideBox = insideHorizontal && insideVertical;

            if (IsDroneInsideBox && lowSpeed)
            {
                accumulatedHoverTime += Time.deltaTime;
                completed = accumulatedHoverTime >= requiredHoverTime;
            }
        }
    }
}
