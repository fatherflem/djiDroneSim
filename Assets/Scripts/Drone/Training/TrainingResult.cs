using System;
using System.Collections.Generic;

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
        public List<TrainingMetric> metrics = new();

        public bool TryGetMetric(string key, out double value)
        {
            TrainingMetric metric = metrics.Find(item => item.key == key);
            value = metric != null ? metric.value : 0d;
            return metric != null;
        }
    }

    [Serializable]
    public class TrainingMetric
    {
        public string key;
        public string displayLabel;
        public double value;
        public string unit;

        public TrainingMetric(string key, string displayLabel, double value, string unit = "")
        {
            this.key = key;
            this.displayLabel = displayLabel;
            this.value = value;
            this.unit = unit;
        }
    }
}
