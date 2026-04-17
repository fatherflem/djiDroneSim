using Unity.XR.CoreUtils;
using UnityEngine;

namespace DroneSim.VR
{
    /// <summary>
    /// Provides a chest-forward controller pose for stationary VR piloting.
    /// Uses XR head pose as source-of-truth, but can bias toward operator placeholder anchors
    /// for better in-sim readability and continuity with non-VR operator staging.
    /// </summary>
    public class AnchoredControllerPoseProvider : MonoBehaviour, IControllerPoseProvider
    {
        [Header("XR references")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Transform headTransformOverride;
        [SerializeField] private Transform chestAnchor;

        [Header("Base placement")]
        [SerializeField] private Vector3 localOffset = new(0f, -0.33f, 0.42f);
        [SerializeField] private Vector3 localEulerOffset = new(18f, 0f, 0f);

        [Header("VR readability tuning")]
        [Tooltip("Extra local positional bias used in VR for screen readability.")]
        [SerializeField] private Vector3 presentationPositionBias = new(0f, 0.02f, -0.02f);
        [Tooltip("Extra local rotation bias used in VR so users can read the controller screen when glancing down.")]
        [SerializeField] private Vector3 presentationEulerBias = new(8f, 0f, 0f);
        [Tooltip("How strongly chest anchor forward should stabilize yaw (0 = head only, 1 = chest only).")]
        [Range(0f, 1f)]
        [SerializeField] private float chestForwardInfluence = 0.6f;

        public bool TryGetPose(out Pose pose)
        {
            Transform head = ResolveHeadTransform();
            if (head == null)
            {
                pose = default;
                return false;
            }

            Vector3 finalOffset = localOffset + presentationPositionBias;
            Quaternion headRotation = head.rotation;
            Quaternion sourceRotation = headRotation;

            if (chestAnchor != null)
            {
                Vector3 blendedForward = Vector3.Slerp(head.forward, chestAnchor.forward, chestForwardInfluence);
                Vector3 blendedUp = Vector3.Slerp(head.up, chestAnchor.up, chestForwardInfluence);
                sourceRotation = Quaternion.LookRotation(blendedForward, blendedUp);
            }

            Vector3 position = head.TransformPoint(finalOffset);
            Quaternion rotation = sourceRotation * Quaternion.Euler(localEulerOffset + presentationEulerBias);
            pose = new Pose(position, rotation);
            return true;
        }

        public void Configure(XROrigin origin, Transform headOverride, Transform chest)
        {
            xrOrigin = origin;
            headTransformOverride = headOverride;
            chestAnchor = chest;
        }

        private Transform ResolveHeadTransform()
        {
            if (headTransformOverride != null)
            {
                return headTransformOverride;
            }

            xrOrigin ??= FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                return xrOrigin.Camera.transform;
            }

            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}
