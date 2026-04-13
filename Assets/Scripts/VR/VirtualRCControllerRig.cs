using UnityEngine;

namespace DroneSim.VR
{
    public class VirtualRCControllerRig : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour fallbackPoseProvider;
        [SerializeField] private MonoBehaviour trackedPoseProvider;
        [SerializeField] private float poseLerpSpeed = 18f;

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
            Pose targetPose;
            if (!TryGetTargetPose(out targetPose))
            {
                return;
            }

            float t = 1f - Mathf.Exp(-poseLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPose.position, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPose.rotation, t);
        }

        private bool TryGetTargetPose(out Pose pose)
        {
            if (trackedProvider != null && trackedProvider.TryGetPose(out pose))
            {
                return true;
            }

            if (fallbackProvider != null && fallbackProvider.TryGetPose(out pose))
            {
                return true;
            }

            pose = default;
            return false;
        }

        private void BuildIfNeeded()
        {
            if (bodyRoot != null && leftStick != null && rightStick != null && screenRenderer != null)
            {
                return;
            }

            bodyRoot = BuildBody("RC_Body", new Vector3(0.42f, 0.035f, 0.18f), new Vector3(0f, 0f, 0f), new Color(0.13f, 0.13f, 0.14f));
            Transform top = BuildBody("RC_Top", new Vector3(0.34f, 0.025f, 0.11f), new Vector3(0f, 0.03f, -0.01f), new Color(0.19f, 0.19f, 0.2f));

            leftStick = BuildStick("LeftStick", new Vector3(-0.12f, 0.026f, 0.02f));
            rightStick = BuildStick("RightStick", new Vector3(0.12f, 0.026f, 0.02f));

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "RC_Screen";
            screen.transform.SetParent(top, false);
            screen.transform.localScale = new Vector3(0.19f, 0.008f, 0.085f);
            screen.transform.localPosition = new Vector3(0f, 0.013f, -0.005f);
            screenRenderer = screen.GetComponent<Renderer>();
            Object.Destroy(screen.GetComponent<Collider>());
            screenRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = Color.black
            };
        }

        private Transform BuildBody(string name, Vector3 scale, Vector3 localPosition, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.localScale = scale;
            part.transform.localPosition = localPosition;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            Object.Destroy(part.GetComponent<Collider>());
            return part.transform;
        }

        private Transform BuildStick(string name, Vector3 localPosition)
        {
            GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stick.name = name;
            stick.transform.SetParent(bodyRoot, false);
            stick.transform.localScale = new Vector3(0.015f, 0.012f, 0.015f);
            stick.transform.localPosition = localPosition;
            Object.Destroy(stick.GetComponent<Collider>());
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
