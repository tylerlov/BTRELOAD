using UnityEngine;
using System.Diagnostics;

namespace Michsky.UI.Reach
{
    public static class ConditionalDebug
    {
        // Control whether debug messages are enabled
        private static bool debugEnabled = false;

        public static void SetDebugEnabled(bool enabled)
        {
            debugEnabled = enabled;
        }

        public static bool IsDebugEnabled()
        {
            return debugEnabled;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            if (!debugEnabled) return;
            UnityEngine.Debug.Log($"[Reach UI] {message}");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
            if (!debugEnabled) return;
            UnityEngine.Debug.LogWarning($"[Reach UI] {message}");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message)
        {
            if (!debugEnabled) return;
            UnityEngine.Debug.LogError($"[Reach UI] {message}");
        }
    }
}
