using UnityEngine;
using Unity.XR.CoreUtils;

namespace DroneSim.VR
{
    public class AnchoredControllerPoseProvider : MonoBehaviour, IControllerPoseProvider
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Vector3 localOffset = new(0f, -0.33f, 0.42f);
        [SerializeField] private Vector3 localEulerOffset = new(18f, 0f, 0f);

        public bool TryGetPose(out Pose pose)
        {
            xrOrigin ??= FindFirstObjectByType<XROrigin>();
            Transform head = xrOrigin != null ? xrOrigin.Camera.transform : Camera.main != null ? Camera.main.transform : null;
            if (head == null)
            {
                pose = default;
                return false;
            }

            Vector3 position = head.TransformPoint(localOffset);
            Quaternion rotation = head.rotation * Quaternion.Euler(localEulerOffset);
            pose = new Pose(position, rotation);
            return true;
        }
    }
}
