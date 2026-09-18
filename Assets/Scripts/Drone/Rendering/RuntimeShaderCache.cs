using UnityEngine;

namespace DroneSim.Drone.Rendering
{
    [CreateAssetMenu(menuName = "Drone Sim/Rendering/Runtime Shader Cache", fileName = "RuntimeShaderCache")]
    public class RuntimeShaderCache : ScriptableObject
    {
        private const string ResourcePath = "Configs/RuntimeShaderCache";
        private const string UrpLitShaderPath = "Universal Render Pipeline/Lit";
        private const string UrpUnlitShaderPath = "Universal Render Pipeline/Unlit";

        [SerializeField] private Shader litShader;
        [SerializeField] private Shader unlitShader;

        private static RuntimeShaderCache cachedInstance;
        private static Shader cachedLitFallback;
        private static Shader cachedUnlitFallback;
        private static bool loggedMissingLit;
        private static bool loggedMissingUnlit;

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

                cachedLitFallback ??= Shader.Find(UrpLitShaderPath);
                LogIfShaderMissing(cachedLitFallback, UrpLitShaderPath, ref loggedMissingLit);
                return cachedLitFallback;
            }
        }

        public static Shader UnlitShader
        {
            get
            {
                if (cachedInstance == null) cachedInstance = Resources.Load<RuntimeShaderCache>(ResourcePath);
                if (cachedInstance != null && cachedInstance.unlitShader != null) return cachedInstance.unlitShader;

                cachedUnlitFallback ??= Shader.Find(UrpUnlitShaderPath);
                LogIfShaderMissing(cachedUnlitFallback, UrpUnlitShaderPath, ref loggedMissingUnlit);
                return cachedUnlitFallback;
            }
        }

        private void OnEnable()
        {
            // Shader references from render-pipeline packages can be lost during an editor upgrade.
            // Resolve once when this Resources asset loads so runtime users share the cached result.
            litShader ??= Shader.Find(UrpLitShaderPath);
            unlitShader ??= Shader.Find(UrpUnlitShaderPath);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Shader previousLit = litShader;
            Shader previousUnlit = unlitShader;
            litShader ??= Shader.Find(UrpLitShaderPath);
            unlitShader ??= Shader.Find(UrpUnlitShaderPath);
            if (litShader != previousLit || unlitShader != previousUnlit)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        private static void LogIfShaderMissing(Shader shader, string shaderPath, ref bool alreadyLogged)
        {
            if (shader != null || alreadyLogged) return;
            alreadyLogged = true;
            Debug.LogWarning($"RuntimeShaderCache: Unable to resolve shader '{shaderPath}'. Materials may render pink.");
        }
    }
}
