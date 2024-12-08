using UnityEngine;

namespace OccaSoftware.LSPP.Runtime
{
    [AddComponentMenu("OccaSoftware/LSPP/Override Light Settings")]
    [ExecuteAlways]
    public class OverrideLightSettings : MonoBehaviour
    {
        [SerializeField, ColorUsage(false, true)]
        private Color lightColor = Color.white;

        private void OnDisable()
        {
            Shader.SetGlobalFloat(ShaderIds._OverrideLightSettings, 0.0f);
        }

        void Update()
        {
            Shader.SetGlobalFloat(ShaderIds._OverrideLightSettings, 1.0f);
            Shader.SetGlobalVector(ShaderIds._OverrideLightDirection, transform.forward);
            Shader.SetGlobalColor(ShaderIds._OverrideLightColor, lightColor);
        }

        private static class ShaderIds
        {
            public static int _OverrideLightSettings = Shader.PropertyToID("_OverrideLightSettings");
            public static int _OverrideLightDirection = Shader.PropertyToID("_OverrideLightDirection");
            public static int _OverrideLightColor = Shader.PropertyToID("_OverrideLightColor");
        }
    }
}
