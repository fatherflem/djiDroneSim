using UnityEngine;

namespace DroneSim.Drone.Environment
{
    public class FieldLoader : MonoBehaviour
    {
        [Tooltip("The field to load when this scene starts. If null, a runtime placeholder is generated.")]
        [SerializeField] private FieldDefinition field;

        [Tooltip("Parent transform for the instantiated field content. If null, a child object is created.")]
        [SerializeField] private Transform fieldRoot;

        private GameObject instantiatedField;
        private static FieldLoader activeLoader;
        private bool hasLoaded;

        public FieldDefinition ActiveField => field;
        public float GroundY => field != null ? field.groundY : 0f;
        public float RecommendedAltitude => field != null ? field.recommendedDrillAltitude : 2f;
        public Vector2 OperatingAreaSize => field != null ? field.operatingAreaSize : new Vector2(12f, 12f);
        public float MaxAltitude => field != null ? field.maxAltitude : 8f;
        public string FieldName => field != null ? field.fieldName : "Runtime Placeholder";

        public static FieldLoader Active => activeLoader;

        private void Awake()
        {
            activeLoader = this;

            if (fieldRoot == null)
            {
                GameObject rootObj = new GameObject("FieldContent");
                rootObj.transform.SetParent(transform, false);
                fieldRoot = rootObj.transform;
            }
        }

        private void Start()
        {
            EnsureLoaded();
        }

        private void OnDestroy()
        {
            if (activeLoader == this) activeLoader = null;
        }

        public void SetField(FieldDefinition def)
        {
            field = def;
            if (hasLoaded)
            {
                ReloadField();
            }
        }

        private void EnsureLoaded()
        {
            if (hasLoaded) return;
            LoadField();
            hasLoaded = true;
        }

        private void ReloadField()
        {
            if (instantiatedField != null)
            {
                Destroy(instantiatedField);
            }

            LoadField();
        }

        private void LoadField()
        {
            if (field != null && field.fieldPrefab != null)
            {
                instantiatedField = Instantiate(field.fieldPrefab, fieldRoot);
                instantiatedField.name = field.fieldName;
            }
            else
            {
                instantiatedField = BuildRuntimePlaceholder(fieldRoot);
            }
        }

        private static GameObject BuildRuntimePlaceholder(Transform parent)
        {
            GameObject root = new GameObject("PlaceholderField");
            root.transform.SetParent(parent, false);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            Renderer gRenderer = ground.GetComponent<Renderer>();
            gRenderer.material = new Material(DroneSim.Drone.Rendering.RuntimeShaderCache.LitShader ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { color = new Color(0.3f, 0.45f, 0.2f) };

            float half = 6f;
            Vector3[] corners = {
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(half, 0f, half),
                new Vector3(-half, 0f, half)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"BoundaryPost_{i}";
                post.transform.SetParent(root.transform, false);
                post.transform.localPosition = corners[i] + new Vector3(0f, 0.5f, 0f);
                post.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
                Destroy(post.GetComponent<Collider>());
                Renderer postRenderer = post.GetComponent<Renderer>();
                postRenderer.material = new Material(DroneSim.Drone.Rendering.RuntimeShaderCache.LitShader ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { color = new Color(0.9f, 0.6f, 0.1f) };
            }

            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObj = new GameObject("FieldSun");
                lightObj.transform.SetParent(root.transform, false);
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                light.color = Color.white;
            }

            return root;
        }
    }
}
