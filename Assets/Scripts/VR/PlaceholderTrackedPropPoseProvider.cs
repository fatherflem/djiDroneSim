using UnityEngine;

namespace DroneSim.VR
{
    public class PlaceholderTrackedPropPoseProvider : MonoBehaviour, IControllerPoseProvider
    {
        [SerializeField] private Transform trackedPropTransform;
        [SerializeField] private Vector3 calibrationPositionOffset;
        [SerializeField] private Vector3 calibrationEulerOffset;

        public bool TryGetPose(out Pose pose)
        {
            if (trackedPropTransform == null)
            {
                pose = default;
                return false;
            }

            pose = new Pose(
                trackedPropTransform.TransformPoint(calibrationPositionOffset),
                trackedPropTransform.rotation * Quaternion.Euler(calibrationEulerOffset));
            return true;
        }
    }
}
