using UnityEngine;

namespace DroneSim.Drone.Physics
{
    /// <summary>
    /// Purpose: Thin Rigidbody wrapper for applying controller commands and exposing readable flight state.
    /// Does NOT: choose control targets or mode behavior.
    /// Fits in sim: physics plant layer used by controller, training, telemetry, and HUD.
    /// Depends on: Unity Rigidbody configured for upright stabilized drone behavior.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DronePhysicsBody : MonoBehaviour
    {
        [Header("Physics reference")]
        [Tooltip("Rigidbody used as the simulated drone body.")]
        [SerializeField] private Rigidbody body;

        public Rigidbody Body => body;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public Vector3 HorizontalVelocity => Vector3.ProjectOnPlane(Velocity, Vector3.up);
        public float VerticalSpeed => Velocity.y;
        public float Altitude => transform.position.y;
        public float YawDegrees => transform.eulerAngles.y;

        public void Initialize(Rigidbody rigidbody)
        {
            body = rigidbody;
            ConfigureBody();
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
            ConfigureBody();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            ConfigureBody();
        }

        public void ApplyWorldAcceleration(Vector3 acceleration)
        {
            body.AddForce(acceleration, ForceMode.Acceleration);
        }

        public void ApplyYawStep(float deltaDegrees)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, deltaDegrees, 0f);
            body.MoveRotation(body.rotation * yawRotation);
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            body.useGravity = true;
            body.mass = 1.6f;
            body.linearDamping = 0.4f;
            body.angularDamping = 5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}
