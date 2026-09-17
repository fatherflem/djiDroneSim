namespace DroneSim.Drone.Training
{
    /// <summary>
    /// Shared lifecycle states used by every student training drill.
    /// Presentation code may choose to combine terminal states, but drill logic should not invent
    /// its own parallel set of booleans.
    /// </summary>
    public enum DrillState
    {
        NotStarted,
        Instructions,
        Countdown,
        Running,
        Completed,
        Failed,
        Results
    }
}
