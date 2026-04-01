using UnityEngine;

namespace DroneSim.Drone.Bootstrap
{
    /// <summary>
    /// Prototype-only stand-in for the human operator and future VR rig anchors.
    /// Keeps an explicit hierarchy for where head/camera and controller objects should attach.
    /// </summary>
    public class VRUserPlaceholder : MonoBehaviour
    {
        [Header("Auto-build")]
        [SerializeField] private bool buildVisualPrimitivesOnAwake = true;

        [Header("Anchors")]
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform chestAnchor;
        [SerializeField] private Transform headAnchor;
        [SerializeField] private Transform vrCameraAnchor;
        [SerializeField] private Transform controllerAnchorLeft;
        [SerializeField] private Transform controllerAnchorRight;
        [SerializeField] private Transform controllerScreenAnchor;

        public Transform BodyRoot => bodyRoot;
        public Transform ChestAnchor => chestAnchor;
        public Transform HeadAnchor => headAnchor;
        public Transform VRCameraAnchor => vrCameraAnchor;
        public Transform ControllerAnchorLeft => controllerAnchorLeft;
        public Transform ControllerAnchorRight => controllerAnchorRight;
        public Transform ControllerScreenAnchor => controllerScreenAnchor;

        private void Awake()
        {
            if (buildVisualPrimitivesOnAwake)
            {
                EnsurePlaceholderHierarchy();
            }
        }

        public void EnsurePlaceholderHierarchy()
        {
            bodyRoot = EnsureChild(transform, "BodyRoot", new Vector3(0f, 0f, 0f));
            chestAnchor = EnsureChild(bodyRoot, "ChestAnchor", new Vector3(0f, 1.25f, 0f));
            headAnchor = EnsureChild(chestAnchor, "HeadAnchor", new Vector3(0f, 0.42f, 0f));
            vrCameraAnchor = EnsureChild(headAnchor, "VRCameraAnchor", new Vector3(0f, 0.02f, 0.09f));

            controllerAnchorLeft = EnsureChild(chestAnchor, "ControllerAnchor_Left", new Vector3(-0.18f, -0.24f, 0.34f));
            controllerAnchorRight = EnsureChild(chestAnchor, "ControllerAnchor_Right", new Vector3(0.18f, -0.24f, 0.34f));
            controllerScreenAnchor = EnsureChild(chestAnchor, "ControllerScreenAnchor", new Vector3(0f, -0.2f, 0.42f));

            EnsureVisual(bodyRoot, "TorsoVisual", PrimitiveType.Capsule, new Vector3(0f, 0.72f, 0f), new Vector3(0.5f, 0.72f, 0.28f), new Color(0.25f, 0.35f, 0.46f));
            EnsureVisual(chestAnchor, "ShoulderBarVisual", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.52f, 0.08f, 0.12f), new Color(0.2f, 0.28f, 0.4f));
            EnsureVisual(headAnchor, "HelmetVisual", PrimitiveType.Sphere, new Vector3(0f, 0.13f, 0f), new Vector3(0.28f, 0.28f, 0.28f), new Color(0.8f, 0.84f, 0.9f));
            EnsureVisual(vrCameraAnchor, "HeadsetVisual", PrimitiveType.Cube, new Vector3(0f, 0f, 0.07f), new Vector3(0.18f, 0.1f, 0.12f), new Color(0.1f, 0.1f, 0.12f));
            EnsureVisual(controllerAnchorLeft, "ControllerGripLeftVisual", PrimitiveType.Cube, Vector3.zero, new Vector3(0.08f, 0.08f, 0.14f), new Color(0.13f, 0.13f, 0.14f));
            EnsureVisual(controllerAnchorRight, "ControllerGripRightVisual", PrimitiveType.Cube, Vector3.zero, new Vector3(0.08f, 0.08f, 0.14f), new Color(0.13f, 0.13f, 0.14f));
            EnsureVisual(controllerScreenAnchor, "ControllerScreenMountVisual", PrimitiveType.Cube, Vector3.zero, new Vector3(0.42f, 0.13f, 0.08f), new Color(0.08f, 0.08f, 0.08f));
        }

        private static Transform EnsureChild(Transform parent, string childName, Vector3 localPosition)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                child = childObject.transform;
                child.SetParent(parent, false);
            }

            child.localPosition = localPosition;
            return child;
        }

        private static void EnsureVisual(Transform parent, string visualName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
        {
            Transform visual = parent.Find(visualName);
            if (visual == null)
            {
                GameObject primitiveObject = GameObject.CreatePrimitive(primitive);
                primitiveObject.name = visualName;
                primitiveObject.transform.SetParent(parent, false);
                Collider collider = primitiveObject.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }

                Renderer renderer = primitiveObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = renderer.sharedMaterial != null
                        ? new Material(renderer.sharedMaterial)
                        : new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
                    material.color = color;
                    renderer.sharedMaterial = material;
                }

                visual = primitiveObject.transform;
            }

            visual.localPosition = localPosition;
            visual.localScale = localScale;
        }
    }
}
