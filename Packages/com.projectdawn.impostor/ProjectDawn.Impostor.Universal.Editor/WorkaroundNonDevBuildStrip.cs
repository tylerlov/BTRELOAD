using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using System;
using System.Reflection;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    /// <summary>
    /// This is workaround to force unity not to strip debug variants at non development build.
    /// It will still strip in case the UniversalRenderPipelineGlobalSettings has StripDebugVariants off.
    /// </summary>
    class WorkaroundNonDevBuildStrip : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 1; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            var typeName = "UnityEditor.Rendering.Universal.ShaderBuildPreprocessor";
            var type = Type.GetType(typeName);
            if (type == null)
            {
                Debug.Log($"WorkaroundNonDevBuildStrip failed to find type {typeName}");
                return;
            }

            var methodName = "GatherShaderFeatures";
            var methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                Debug.Log($"WorkaroundNonDevBuildStrip failed to find method {methodName}");
                return;
            }

            // Calls again GatherShaderFeatures(devBuild:false) overriding the initial call by URP
            methodInfo.Invoke(null, new object[] { true });
            Debug.Log("Applying workaround for non dev build strip!");
        }
    }
}