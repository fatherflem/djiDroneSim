using DroneSim.Drone.Input;
using DroneSim.Drone.Physics;
using UnityEngine;

namespace DroneSim.Drone.Training
{
    /// <summary>Owns the physical start pose and input lock for a training attempt.</summary>
    public class TrainingAircraftReset : MonoBehaviour
    {
        private TrainingDrill drill;
        private DroneInputReader input;
        private Rigidbody body;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private bool unlockedIsKinematic;
        private CollisionDetectionMode unlockedCollisionDetectionMode;
        private bool initialized;

        public bool Initialize(TrainingDrill trainingDrill, DronePhysicsBody physicsBody, Vector3 position, Quaternion rotation)
        {
            if (trainingDrill == null || physicsBody == null || physicsBody.Body == null)
            {
                Debug.LogError("TrainingAircraftReset requires a drill and an initialized drone Rigidbody.", this);
                return false;
            }

            if (initialized && drill != null)
            {
                drill.StateChanged -= OnStateChanged;
            }

            drill = trainingDrill;
            body = physicsBody.Body;
            input = physicsBody.GetComponent<DroneInputReader>();
            startPosition = position;
            startRotation = rotation;
            unlockedIsKinematic = body.isKinematic;
            unlockedCollisionDetectionMode = body.collisionDetectionMode;
            drill.StateChanged += OnStateChanged;
            initialized = true;
            EnterLockedState(true);
            return true;
        }

        private void OnDestroy()
        {
            if (drill != null) drill.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(DrillState previous, DrillState next)
        {
            if (next == DrillState.Instructions || next == DrillState.Countdown)
            {
                EnterLockedState(true);
            }
            else if (next == DrillState.Running)
            {
                EnterRunningState();
            }
            else if (next == DrillState.Results)
            {
                EnterLockedState(false);
            }
        }

        public void ResetAircraft()
        {
            if (body == null) return;

            body.position = startPosition;
            body.rotation = startRotation;
            UnityEngine.Physics.SyncTransforms();
        }

        private void EnterLockedState(bool resetPose)
        {
            if (input != null)
            {
                input.SetExternalInputFrame(default);
                input.SetExternalInputEnabled(true);
            }

            // Velocity setters are only valid while this body is dynamic in Unity 6.3.
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            if (body.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic)
            {
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
            body.isKinematic = true;
            if (resetPose) ResetAircraft();
        }

        private void EnterRunningState()
        {
            // Keep input neutral until pose and the complete dynamic Rigidbody state are restored.
            if (input != null)
            {
                input.SetExternalInputFrame(default);
                input.SetExternalInputEnabled(true);
            }

            ResetAircraft();
            body.isKinematic = unlockedIsKinematic;
            body.collisionDetectionMode = unlockedCollisionDetectionMode;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
            if (input != null) input.SetExternalInputEnabled(false);
        }
    }
}
