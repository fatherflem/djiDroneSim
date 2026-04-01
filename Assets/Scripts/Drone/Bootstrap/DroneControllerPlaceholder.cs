using DroneSim.Drone.Camera;
using UnityEngine;

namespace DroneSim.Drone.Bootstrap
{
    /// <summary>
    /// Procedural handheld controller prop mounted to VRUserPlaceholder anchors.
    /// Represents a DJI RC 2-like integrated-screen layout (without logos/trademark details).
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
        [SerializeField] private Vector3 bodySize = new Vector3(0.35f, 0.055f, 0.145f);
        [SerializeField] private Vector3 shoulderSize = new Vector3(0.29f, 0.05f, 0.085f);
        [SerializeField] private Vector3 gripSize = new Vector3(0.1f, 0.13f, 0.12f);
        [SerializeField] private Vector3 screenBezelSize = new Vector3(0.245f, 0.108f, 0.024f);
        [SerializeField] private Vector3 screenSurfaceSize = new Vector3(0.205f, 0.085f, 1f);

        [Header("Placement")]
        [SerializeField] private Vector3 localPosition = Vector3.zero;
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

            body = EnsureVisual(transform, "Body", PrimitiveType.Cube, new Vector3(0f, -0.003f, -0.004f), bodySize, new Color(0.11f, 0.11f, 0.12f));
            _ = EnsureVisual(transform, "UpperShoulder", PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.018f), shoulderSize, new Color(0.12f, 0.12f, 0.13f));
            leftGrip = EnsureVisual(transform, "LeftGrip", PrimitiveType.Cube, new Vector3(-0.152f, -0.043f, -0.002f), gripSize, new Color(0.09f, 0.09f, 0.1f));
            rightGrip = EnsureVisual(transform, "RightGrip", PrimitiveType.Cube, new Vector3(0.152f, -0.043f, -0.002f), gripSize, new Color(0.09f, 0.09f, 0.1f));

            _ = EnsureVisual(transform, "LeftStickBase", PrimitiveType.Cylinder, new Vector3(-0.104f, 0.028f, -0.027f), new Vector3(0.036f, 0.007f, 0.036f), new Color(0.17f, 0.17f, 0.17f));
            _ = EnsureVisual(transform, "RightStickBase", PrimitiveType.Cylinder, new Vector3(0.104f, 0.028f, -0.027f), new Vector3(0.036f, 0.007f, 0.036f), new Color(0.17f, 0.17f, 0.17f));
            _ = EnsureVisual(transform, "LeftStickVisual", PrimitiveType.Sphere, new Vector3(-0.104f, 0.04f, -0.027f), new Vector3(0.022f, 0.016f, 0.022f), new Color(0.2f, 0.2f, 0.2f));
            _ = EnsureVisual(transform, "RightStickVisual", PrimitiveType.Sphere, new Vector3(0.104f, 0.04f, -0.027f), new Vector3(0.022f, 0.016f, 0.022f), new Color(0.2f, 0.2f, 0.2f));

            _ = EnsureVisual(transform, "TopEdgeBar", PrimitiveType.Cube, new Vector3(0f, 0.054f, 0.052f), new Vector3(0.26f, 0.014f, 0.018f), new Color(0.15f, 0.15f, 0.16f));
            _ = EnsureVisual(transform, "TopDialLeft", PrimitiveType.Cylinder, new Vector3(-0.118f, 0.059f, 0.052f), new Vector3(0.014f, 0.006f, 0.014f), new Color(0.19f, 0.19f, 0.2f));
            _ = EnsureVisual(transform, "TopDialRight", PrimitiveType.Cylinder, new Vector3(0.118f, 0.059f, 0.052f), new Vector3(0.014f, 0.006f, 0.014f), new Color(0.19f, 0.19f, 0.2f));
            _ = EnsureVisual(transform, "TopToggleLeft", PrimitiveType.Cube, new Vector3(-0.048f, 0.058f, 0.053f), new Vector3(0.022f, 0.01f, 0.012f), new Color(0.18f, 0.18f, 0.18f));
            _ = EnsureVisual(transform, "TopToggleRight", PrimitiveType.Cube, new Vector3(0.048f, 0.058f, 0.053f), new Vector3(0.022f, 0.01f, 0.012f), new Color(0.18f, 0.18f, 0.18f));

            screenBezel = EnsureVisual(transform, "ScreenBezel", PrimitiveType.Cube, new Vector3(0f, 0.07f, 0.006f), screenBezelSize, new Color(0.04f, 0.04f, 0.05f));
            _ = EnsureVisual(screenBezel, "ScreenFrameInset", PrimitiveType.Cube, new Vector3(0f, 0f, -0.002f), new Vector3(0.222f, 0.092f, 0.02f), new Color(0.025f, 0.025f, 0.03f));
            screenSurface = EnsureVisual(screenBezel, "ScreenSurface", PrimitiveType.Quad, new Vector3(0f, 0f, -0.013f), screenSurfaceSize, Color.black);
            screenSurface.localRotation = Quaternion.Euler(0f, 180f, 0f);

            feedDisplaySurface = screenSurface.GetComponent<DroneFeedDisplaySurface>();
            if (feedDisplaySurface == null)
            {
                feedDisplaySurface = screenSurface.gameObject.AddComponent<DroneFeedDisplaySurface>();
            }

            if (screenAnchor != null)
            {
                screenAnchor.SetParent(screenBezel, false);
                screenAnchor.localPosition = new Vector3(0f, 0f, -0.013f);
                screenAnchor.localRotation = Quaternion.identity;
            }

            if (leftHandAnchor != null)
            {
                leftHandAnchor.SetParent(leftGrip, false);
                leftHandAnchor.localPosition = Vector3.zero;
            }

            if (rightHandAnchor != null)
            {
                rightHandAnchor.SetParent(rightGrip, false);
                rightHandAnchor.localPosition = Vector3.zero;
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
