using UnityEngine;

namespace DroneSim.Drone.Training
{
    public class WaypointMarker : MonoBehaviour
    {
        private Renderer cylinderRenderer;
        private Transform ring;
        private Material mat;
        private float failFlashUntil;

        public void Configure(float radius, float height)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            Destroy(cylinder.GetComponent<Collider>());

            cylinderRenderer = cylinder.GetComponent<Renderer>();
            mat = new Material(DroneSim.Drone.Rendering.RuntimeShaderCache.LitShader ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
            mat.SetFloat("_Mode", 3f);
            mat.color = new Color(0.7f, 0.7f, 0.7f, 0.2f);
            cylinderRenderer.material = mat;

            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "HoldRing";
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, -height * 0.5f + 0.02f, 0f);
            ringObj.transform.localScale = new Vector3(radius * 2.2f, 0.01f, radius * 2.2f);
            Destroy(ringObj.GetComponent<Collider>());
            ring = ringObj.transform;
            Renderer ringRenderer = ringObj.GetComponent<Renderer>();
            ringRenderer.material = new Material(DroneSim.Drone.Rendering.RuntimeShaderCache.LitShader ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color")) { color = new Color(1f, 1f, 0f, 0.2f) };
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

            SetColor(c, 0.3f);
            if (ring != null)
            {
                ring.gameObject.SetActive(true);
                float scale = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(progress01));
                ring.localScale = new Vector3(ring.localScale.x, ring.localScale.y, ring.localScale.z * scale);
            }
        }

        public void SetCompleted()
        {
            if (Time.time < failFlashUntil)
            {
                return;
            }

            SetColor(new Color(0.2f, 0.7f, 0.2f), 0.2f);
            if (ring != null) ring.gameObject.SetActive(false);
        }

        public void SetFuture()
        {
            if (Time.time < failFlashUntil)
            {
                return;
            }

            SetColor(new Color(0.5f, 0.5f, 0.5f), 0.15f);
            if (ring != null) ring.gameObject.SetActive(false);
        }

        public void FlashFailed()
        {
            failFlashUntil = Time.time + 0.2f;
            SetColor(Color.red, 0.5f);
            if (ring != null) ring.gameObject.SetActive(false);
        }

        private void SetColor(Color color, float alpha)
        {
            if (mat == null) return;
            color.a = alpha;
            mat.color = color;
        }
    }
}
