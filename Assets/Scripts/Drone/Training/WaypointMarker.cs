using UnityEngine;

namespace DroneSim.Drone.Training
{
    public class WaypointMarker : MonoBehaviour
    {
        private Renderer cylinderRenderer;
        private Transform ring;
        private Material mat;
        private Material ringMaterial;
        private Vector3 ringFullScale;
        private float failFlashUntil;

        public void Configure(float radius, float height)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            Destroy(cylinder.GetComponent<Collider>());

            cylinderRenderer = cylinder.GetComponent<Renderer>();
            Shader markerShader = DroneSim.Drone.Rendering.RuntimeShaderCache.UnlitShader
                ?? Shader.Find("Unlit/Color")
                ?? DroneSim.Drone.Rendering.RuntimeShaderCache.LitShader;
            mat = new Material(markerShader);
            ConfigureTransparentMaterial(mat);
            mat.color = new Color(0.55f, 0.65f, 0.75f, 0.38f);
            cylinderRenderer.material = mat;

            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "HoldRing";
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, -height * 0.5f + 0.02f, 0f);
            ringObj.transform.localScale = new Vector3(radius * 2.2f, 0.01f, radius * 2.2f);
            Destroy(ringObj.GetComponent<Collider>());
            ring = ringObj.transform;
            ringFullScale = ring.localScale;
            Renderer ringRenderer = ringObj.GetComponent<Renderer>();
            ringMaterial = new Material(markerShader);
            ConfigureTransparentMaterial(ringMaterial);
            ringMaterial.color = new Color(1f, 0.85f, 0f, 0.95f);
            ringRenderer.material = ringMaterial;
            ringObj.SetActive(false);
        }

        public void SetActive(float progress01, bool holding)
        {
            if (Time.time < failFlashUntil)
            {
                return;
            }

            Color c = Color.yellow;
            if (holding)
            {
                c = Color.Lerp(Color.yellow, Color.green, Mathf.Clamp01(progress01));
                c *= 1.1f + Mathf.PingPong(Time.time * 2f, 0.3f);
            }

            SetColor(c, holding ? 0.82f : 0.72f);
            if (ring != null)
            {
                ring.gameObject.SetActive(true);
                float scale = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(progress01));
                ring.localScale = new Vector3(ringFullScale.x, ringFullScale.y, ringFullScale.z * scale);
                if (ringMaterial != null) ringMaterial.color = holding
                    ? new Color(0.2f, 1f, 0.25f, 0.98f)
                    : new Color(1f, 0.85f, 0f, 0.98f);
            }
        }

        public void SetCompleted()
        {
            if (Time.time < failFlashUntil)
            {
                return;
            }

            SetColor(new Color(0.15f, 0.85f, 0.25f), 0.48f);
            if (ring != null) ring.gameObject.SetActive(false);
        }

        public void SetFuture()
        {
            if (Time.time < failFlashUntil)
            {
                return;
            }

            SetColor(new Color(0.5f, 0.65f, 0.8f), 0.34f);
            if (ring != null) ring.gameObject.SetActive(false);
        }

        public void FlashFailed()
        {
            failFlashUntil = Time.time + 0.2f;
            SetColor(new Color(1f, 0.05f, 0.02f), 0.9f);
            if (ring != null) ring.gameObject.SetActive(false);
        }

        private void SetColor(Color color, float alpha)
        {
            if (mat == null) return;
            color.a = alpha;
            mat.color = color;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            // URP transparency uses _Surface/blend state; _Mode belongs to the old Standard shader.
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
