using UnityEngine;

namespace DroneSim.VR
{
    public interface IControllerPoseProvider
    {
        bool TryGetPose(out Pose pose);
    }
}
