using System;

namespace DroneSim.Drone.Training
{
    /// <summary>Small, serializable result contract suitable for UI and future local persistence.</summary>
    [Serializable]
    public class TrainingResult
    {
        public string drillId;
        public string drillName;
        public bool succeeded;
        public float elapsedSeconds;
        public float score;
        public string summary;
    }
}
