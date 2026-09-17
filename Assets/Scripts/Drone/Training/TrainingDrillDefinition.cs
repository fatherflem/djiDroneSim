using UnityEngine;

namespace DroneSim.Drone.Training
{
    /// <summary>
    /// Authorable student-facing metadata and common timing rules for a training drill.
    /// Flight handling and drill-specific geometry deliberately do not belong here.
    /// </summary>
    [CreateAssetMenu(menuName = "Drone Sim/Training/Drill Definition", fileName = "TrainingDrillDefinition")]
    public class TrainingDrillDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string drillId = "training-drill";
        public string displayName = "Training Drill";

        [TextArea(3, 8)]
        public string instructions = "Follow the on-screen instructions.";

        [Header("Lifecycle")]
        [Min(0f)] public float countdownSeconds = 3f;
        [Tooltip("Zero disables the common time limit. A drill can still fail for its own reasons.")]
        [Min(0f)] public float timeLimitSeconds;
        public bool allowRestart = true;
    }
}
