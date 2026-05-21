using DroneSim.Drone.Rendering;
using UnityEngine;

namespace DroneSim.VR
{
    public class VirtualRCControllerRig : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour fallbackPoseProvider;
        [SerializeField] private MonoBehaviour trackedPoseProvider;
        [SerializeField] private float poseLerpSpeed = 18f;

        [Header("Real DJI RC 2 Dimensions (meters)")]
        [SerializeField] private Vector3 bodySize = new(0.155f, 0.042f, 0.068f);
        [SerializeField] private Vector3 topShellSize = new(0.13f, 0.02f, 0.05f);
        [SerializeField] private Vector3 topShellOffset = new(0f, 0.027f, -0.003f);
        [SerializeField] private Vector2 screenActiveArea = new(0.115f, 0.072f);
        [SerializeField] private float screenThickness = 0.004f;
        [SerializeField] private Vector3 screenOffset = new(0f, 0.011f, -0.002f);
        [SerializeField] private float stickBaseDiameter = 0.008f;
        [SerializeField] private float stickHeight = 0.018f;
        [SerializeField] private float stickSpacing = 0.078f;
        [SerializeField] private float stickForwardOffset = 0.014f;
        [SerializeField] private Vector3 antennaSize = new(0.02f, 0.008f, 0.008f);

        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform leftStick;
        [SerializeField] private Transform rightStick;
        [SerializeField] private Renderer screenRenderer;

        private IControllerPoseProvider fallbackProvider;
        private IControllerPoseProvider trackedProvider;

        public Transform LeftStick => leftStick;
        public Transform RightStick => rightStick;
        public Renderer ScreenRenderer => screenRenderer;

        private void Awake()
        {
            BuildIfNeeded();
            fallbackProvider = fallbackPoseProvider as IControllerPoseProvider;
            trackedProvider = trackedPoseProvider as IControllerPoseProvider;
        }

        private void LateUpdate()
        {
            if (!TryGetTargetPose(out Pose targetPose)) return;
            float t = 1f - Mathf.Exp(-poseLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPose.position, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPose.rotation, t);
        }

        private bool TryGetTargetPose(out Pose pose)
        {
            if (trackedProvider != null && trackedProvider.TryGetPose(out pose)) return true;
            if (fallbackProvider != null && fallbackProvider.TryGetPose(out pose)) return true;
            pose = default;
            return false;
        }

        private void BuildIfNeeded()
        {
            if (bodyRoot != null && leftStick != null && rightStick != null && screenRenderer != null) return;

            bodyRoot = BuildBody("RC_Body", bodySize, Vector3.zero, new Color(0.13f, 0.13f, 0.14f));
            Transform top = BuildBody("RC_Top", topShellSize, topShellOffset, new Color(0.19f, 0.19f, 0.2f));

            float side = stickSpacing * 0.5f;
            float y = bodySize.y * 0.5f + stickHeight * 0.5f;
            leftStick = BuildStick("LeftStick", new Vector3(-side, y, stickForwardOffset));
            rightStick = BuildStick("RightStick", new Vector3(side, y, stickForwardOffset));

            BuildBody("LeftAntenna", antennaSize, new Vector3(-bodySize.x * 0.38f, bodySize.y * 0.5f + antennaSize.y * 0.5f, -bodySize.z * 0.47f), new Color(0.15f,0.15f,0.15f));
            BuildBody("RightAntenna", antennaSize, new Vector3(bodySize.x * 0.38f, bodySize.y * 0.5f + antennaSize.y * 0.5f, -bodySize.z * 0.47f), new Color(0.15f,0.15f,0.15f));

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "RC_Screen";
            screen.transform.SetParent(top, false);
            screen.transform.localScale = new Vector3(screenActiveArea.x, screenThickness, screenActiveArea.y);
            screen.transform.localPosition = screenOffset;
            screenRenderer = screen.GetComponent<Renderer>();
            Destroy(screen.GetComponent<Collider>());
            screenRenderer.material = new Material(RuntimeShaderCache.LitShader ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { color = Color.black };
        }

        private Transform BuildBody(string name, Vector3 scale, Vector3 localPosition, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.localScale = scale;
            part.transform.localPosition = localPosition;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = new Material(RuntimeShaderCache.LitShader ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { color = color };
            Destroy(part.GetComponent<Collider>());
            return part.transform;
        }

        private Transform BuildStick(string name, Vector3 localPosition)
        {
            GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stick.name = name;
            stick.transform.SetParent(bodyRoot, false);
            stick.transform.localScale = new Vector3(stickBaseDiameter * 0.5f, stickHeight * 0.5f, stickBaseDiameter * 0.5f);
            stick.transform.localPosition = localPosition;
            Destroy(stick.GetComponent<Collider>());
            return stick.transform;
        }

        public void InjectPoseProviders(MonoBehaviour fallback, MonoBehaviour tracked)
        {
            fallbackPoseProvider = fallback;
            trackedPoseProvider = tracked;
            fallbackProvider = fallback as IControllerPoseProvider;
            trackedProvider = tracked as IControllerPoseProvider;
        }
    }
}
