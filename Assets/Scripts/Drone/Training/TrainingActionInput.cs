using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneSim.Drone.Training
{
    /// <summary>Keyboard/development and physical-controller start/retry input shared by desktop and VR.</summary>
    public class TrainingActionInput : MonoBehaviour
    {
        private TrainingDrill drill;
        private InputAction startAction;
        private InputAction retryAction;

        public void Initialize(TrainingDrill trainingDrill) => drill = trainingDrill;

        private void OnEnable()
        {
            startAction = new InputAction("StartTraining", binding: "<Keyboard>/enter");
            startAction.AddBinding("<Keyboard>/space");
            startAction.AddBinding("<Gamepad>/buttonSouth");
            retryAction = new InputAction("RetryTraining", binding: "<Keyboard>/r");
            retryAction.AddBinding("<Gamepad>/buttonSouth");
            startAction.Enable();
            retryAction.Enable();
        }

        private void OnDisable()
        {
            startAction?.Dispose();
            retryAction?.Dispose();
        }

        private void Update()
        {
            if (drill == null) return;
            if (drill.State == DrillState.Instructions && startAction.WasPressedThisFrame())
            {
                drill.ContinueFromInstructions();
            }
            else if (drill.State == DrillState.Results && drill.CanRestart && retryAction.WasPressedThisFrame())
            {
                drill.RestartDrill();
            }
        }
    }
}
