using DroneSim.Drone.Camera;
using UnityEngine;

namespace DroneSim.Drone.Bootstrap
{
    /// <summary>
    /// Prototype handheld controller prop mounted to VRUserPlaceholder anchors.
    /// Represents where a future DJI-style controller asset and VR hand/controller rig will attach.
    /// The controller screen surface hosts DroneFeedDisplaySurface to show the always-live onboard feed.
    /// </summary>
    public class DroneControllerPlaceholder : MonoBehaviour
    {
        [Header("Auto-build")]
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool showControllerVisual = true;

        [Header("Anchor references")]
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;
        [SerializeField] private Transform screenAnchor;

        [Header("Controller shape")]
        [SerializeField] private Vector3 bodySize = new Vector3(0.36f, 0.06f, 0.14f);
        [SerializeField] private Vector3 gripSize = new Vector3(0.09f, 0.12f, 0.12f);
        [SerializeField] private Vector3 screenBezelSize = new Vector3(0.24f, 0.11f, 0.02f);
        [SerializeField] private Vector3 screenSurfaceSize = new Vector3(0.2f, 0.085f, 1f);

        [Header("Placement")]
        [SerializeField] private Vector3 localPosition = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 localEuler = new Vector3(18f, 180f, 0f);

        [Header("References (runtime-built)")]
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftGrip;
        [SerializeField] private Transform rightGrip;
        [SerializeField] private Transform screenBezel;
        [SerializeField] private Transform screenSurface;
        [SerializeField] private DroneFeedDisplaySurface feedDisplaySurface;

        public DroneFeedDisplaySurface FeedDisplaySurface => feedDisplaySurface;
        public Transform ScreenSurface => screenSurface;

        private void Awake()
        {
            if (buildOnAwake)
            {
                Rebuild();
            }
        }

        public void Initialize(Transform leftAnchor, Transform rightAnchor, Transform mountedScreenAnchor)
        {
            leftHandAnchor = leftAnchor;
            rightHandAnchor = rightAnchor;
            screenAnchor = mountedScreenAnchor;
            Rebuild();
        }

        public void SetVideoFeed(DroneVideoFeed videoFeed)
        {
            if (feedDisplaySurface == null)
            {
                return;
            }

            feedDisplaySurface.SetVideoFeed(videoFeed);
        }

        public void Rebuild()
        {
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEuler);

            body = EnsureVisual(transform, "Body", PrimitiveType.Cube, Vector3.zero, bodySize, new Color(0.1f, 0.1f, 0.11f));
            leftGrip = EnsureVisual(transform, "LeftGrip", PrimitiveType.Cube, new Vector3(-0.15f, -0.03f, 0f), gripSize, new Color(0.08f, 0.08f, 0.09f));
            rightGrip = EnsureVisual(transform, "RightGrip", PrimitiveType.Cube, new Vector3(0.15f, -0.03f, 0f), gripSize, new Color(0.08f, 0.08f, 0.09f));

            _ = EnsureVisual(transform, "LeftStickZone", PrimitiveType.Cylinder, new Vector3(-0.1f, 0.034f, -0.005f), new Vector3(0.03f, 0.008f, 0.03f), new Color(0.16f, 0.16f, 0.16f));
            _ = EnsureVisual(transform, "RightStickZone", PrimitiveType.Cylinder, new Vector3(0.1f, 0.034f, -0.005f), new Vector3(0.03f, 0.008f, 0.03f), new Color(0.16f, 0.16f, 0.16f));
            _ = EnsureVisual(transform, "LeftStickVisual", PrimitiveType.Sphere, new Vector3(-0.1f, 0.046f, -0.005f), new Vector3(0.025f, 0.015f, 0.025f), new Color(0.19f, 0.19f, 0.19f));
            _ = EnsureVisual(transform, "RightStickVisual", PrimitiveType.Sphere, new Vector3(0.1f, 0.046f, -0.005f), new Vector3(0.025f, 0.015f, 0.025f), new Color(0.19f, 0.19f, 0.19f));
            _ = EnsureVisual(transform, "TopAntennaLeft", PrimitiveType.Cube, new Vector3(-0.1f, 0.058f, 0.06f), new Vector3(0.02f, 0.04f, 0.02f), new Color(0.2f, 0.2f, 0.2f));
            _ = EnsureVisual(transform, "TopAntennaRight", PrimitiveType.Cube, new Vector3(0.1f, 0.058f, 0.06f), new Vector3(0.02f, 0.04f, 0.02f), new Color(0.2f, 0.2f, 0.2f));

            screenBezel = EnsureVisual(transform, "ScreenBezel", PrimitiveType.Cube, new Vector3(0f, 0.078f, 0.012f), screenBezelSize, new Color(0.05f, 0.05f, 0.05f));
            screenSurface = EnsureVisual(screenBezel, "ScreenSurface", PrimitiveType.Quad, new Vector3(0f, 0f, -0.011f), screenSurfaceSize, Color.black);
            screenSurface.localRotation = Quaternion.Euler(0f, 180f, 0f);

            feedDisplaySurface = screenSurface.GetComponent<DroneFeedDisplaySurface>();
            if (feedDisplaySurface == null)
            {
                feedDisplaySurface = screenSurface.gameObject.AddComponent<DroneFeedDisplaySurface>();
            }

            if (screenAnchor != null)
            {
                screenAnchor.SetParent(screenBezel, false);
                screenAnchor.localPosition = new Vector3(0f, 0f, -0.011f);
                screenAnchor.localRotation = Quaternion.identity;
            }

            if (leftHandAnchor != null)
            {
                leftHandAnchor.SetParent(leftGrip, false);
                leftHandAnchor.localPosition = new Vector3(0f, 0f, 0f);
            }

            if (rightHandAnchor != null)
            {
                rightHandAnchor.SetParent(rightGrip, false);
                rightHandAnchor.localPosition = new Vector3(0f, 0f, 0f);
            }

            SetVisualEnabled(showControllerVisual);
        }

        private void SetVisualEnabled(bool enabled)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.enabled = enabled;
            }
        }

        private static Transform EnsureVisual(Transform parent, string visualName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
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
            return visual;
        }
    }
}
