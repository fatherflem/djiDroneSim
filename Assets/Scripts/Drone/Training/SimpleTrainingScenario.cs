using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    /// <summary>
    /// Purpose: Implements a basic hover-box training drill with time-based completion.
    /// Does NOT: control flight, alter inputs, or perform advanced scoring analytics.
    /// Fits in sim: training feedback layer consumed by HUD and instructor-facing tooling.
    /// Depends on: DronePhysicsBody for position/speed information.
    /// </summary>
    public class SimpleTrainingScenario : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Drone physics source used to read position and speed.")]
        [SerializeField] private DronePhysicsBody droneBody;

        [Header("Hover drill target")]
        [Tooltip("Center of the horizontal training box where the pilot should hold position.")]
        [SerializeField] private Vector3 hoverBoxCenter = new Vector3(0f, 2f, 0f);

        [Tooltip("Horizontal box size (X width, Y depth in world X/Z plane).")]
        [SerializeField] private Vector2 hoverBoxSize = new Vector2(3f, 3f);

        [Tooltip("Target altitude for the hover drill.")]
        [SerializeField] private float targetAltitude = 2f;

        [Tooltip("Allowed altitude error around target altitude.")]
        [Min(0f)]
        [SerializeField] private float altitudeTolerance = 0.5f;

        [Tooltip("Seconds of stable hover required to complete the drill.")]
        [Min(0.1f)]
        [SerializeField] private float requiredHoverTime = 15f;

        [Tooltip("Maximum horizontal speed allowed while counting hover time.")]
        [Min(0f)]
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
