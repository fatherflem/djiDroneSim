using DroneSim.Drone.Input;

namespace DroneSim.Drone.Benchmark
{
    /// <summary>
    /// Flattened benchmark input sample used by playback and telemetry logging.
    /// </summary>
    public struct BenchmarkInputFrame
    {
        public float Time;
        public float Roll;
        public float Pitch;
        public float Throttle;
        public float Yaw;
        public DroneMode Mode;

        public static BenchmarkInputFrame Neutral(DroneMode mode)
        {
            return new BenchmarkInputFrame
            {
                Time = 0f,
                Roll = 0f,
                Pitch = 0f,
                Throttle = 0f,
                Yaw = 0f,
                Mode = mode
            };
        }

        public DroneInputFrame ToDroneInputFrame()
        {
            return new DroneInputFrame
            {
                Roll = Roll,
                Pitch = Pitch,
                Throttle = Throttle,
                Yaw = Yaw,
                RequestedMode = Mode
            };
        }
    }
}
