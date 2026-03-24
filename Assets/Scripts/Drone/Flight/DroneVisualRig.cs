using UnityEngine;

namespace DroneSim.Drone.Flight
{
    /// <summary>
    /// Purpose: Ensures a lightweight visual drone model exists, including a tilt root for attitude feedback.
    /// Does NOT: control flight or physics.
    /// Fits in sim: visual representation layer driven indirectly by controller tilt updates.
    /// Depends on: DJIStyleFlightController using TiltRoot for local visual pitch/roll.
    /// </summary>
    public class DroneVisualRig : MonoBehaviour
    {
        [Header("Visual rig")]
        [Tooltip("Child transform that receives visual pitch/roll tilt from the controller.")]
        [SerializeField] private Transform tiltRoot;

        [Tooltip("If true, placeholder visuals are auto-built on Awake when no mesh exists.")]
        [SerializeField] private bool buildOnAwake = true;

        public Transform TiltRoot => tiltRoot != null ? tiltRoot : transform;

        private void Awake()
        {
            if (buildOnAwake)
            {
                EnsureVisuals();
            }
        }

        public void EnsureVisuals()
        {
            if (tiltRoot == null)
            {
                GameObject tiltObject = new GameObject("VisualTiltRoot");
                tiltObject.transform.SetParent(transform, false);
                tiltRoot = tiltObject.transform;
            }

            if (tiltRoot.childCount > 0)
            {
                return;
            }

            CreatePart("Body", tiltRoot, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.12f, 0.55f), new Color(0.18f, 0.2f, 0.22f));
            CreatePart("CameraNose", tiltRoot, PrimitiveType.Cube, new Vector3(0f, -0.05f, 0.34f), new Vector3(0.18f, 0.12f, 0.18f), new Color(0.05f, 0.05f, 0.06f));

            CreateArm(new Vector3(0.42f, 0f, 0.42f));
            CreateArm(new Vector3(-0.42f, 0f, 0.42f));
            CreateArm(new Vector3(0.42f, 0f, -0.42f));
            CreateArm(new Vector3(-0.42f, 0f, -0.42f));
        }

        private void CreateArm(Vector3 position)
        {
            GameObject arm = CreatePart("Arm", tiltRoot, PrimitiveType.Cylinder, position * 0.5f, new Vector3(0.06f, 0.25f, 0.06f), new Color(0.36f, 0.36f, 0.38f));
            arm.transform.LookAt(tiltRoot.position + position);
            CreatePart("Rotor", tiltRoot, PrimitiveType.Cylinder, position, new Vector3(0.18f, 0.01f, 0.18f), new Color(0.1f, 0.1f, 0.1f));
        }

        private GameObject CreatePart(string name, Transform parent, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material baseMaterial = renderer.sharedMaterial;
                Material material = baseMaterial != null
                    ? new Material(baseMaterial)
                    : new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            return part;
        }
    }
}
