using UnityEngine;

namespace DroneSim.Drone.Rendering
{
    [CreateAssetMenu(menuName = "Drone Sim/Rendering/Runtime Shader Cache", fileName = "RuntimeShaderCache")]
    public class RuntimeShaderCache : ScriptableObject
    {
        private const string ResourcePath = "Configs/RuntimeShaderCache";
        private const string UrpLitShaderPath = "Universal Render Pipeline/Lit";

        [SerializeField] private Shader litShader;

        private static RuntimeShaderCache cachedInstance;

        public static Shader LitShader
        {
            get
            {
                if (cachedInstance == null)
                {
                    cachedInstance = Resources.Load<RuntimeShaderCache>(ResourcePath);
                }

                if (cachedInstance != null && cachedInstance.litShader != null)
                {
                    return cachedInstance.litShader;
                }

                Shader fallback = Shader.Find(UrpLitShaderPath);
                if (fallback == null)
                {
                    Debug.LogWarning($"RuntimeShaderCache: Unable to resolve shader '{UrpLitShaderPath}'. Materials may render pink.");
                    return null;
                }

                Debug.LogWarning("RuntimeShaderCache: Missing RuntimeShaderCache asset or litShader reference. Falling back to Shader.Find().");
                return fallback;
            }
        }
    }
}
