using UnityEngine;
using System.Runtime.CompilerServices;

public static class ConditionalDebug
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static DebugSettings settings;
        
        private static bool IsLoggingEnabled
        {
            get
            {
                if (settings == null)
                {
                    settings = Resources.Load<DebugSettings>("DebugSettings");
                }
                return settings != null && settings.isLoggingEnabled;
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, [CallerFilePath] string sourceFilePath = "")
        {
            if (IsLoggingEnabled)
            {
                Debug.Log($"[{System.IO.Path.GetFileNameWithoutExtension(sourceFilePath)}] {message}");
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message, [CallerFilePath] string sourceFilePath = "")
        {
            if (IsLoggingEnabled)
            {
                Debug.LogWarning($"[{System.IO.Path.GetFileNameWithoutExtension(sourceFilePath)}] {message}");
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(object message, [CallerFilePath] string sourceFilePath = "")
        {
            if (IsLoggingEnabled)
            {
                Debug.LogError($"[{System.IO.Path.GetFileNameWithoutExtension(sourceFilePath)}] {message}");
            }
        }
    #else
        public static void Log(object message, [CallerFilePath] string sourceFilePath = "") { }
        public static void LogWarning(object message, [CallerFilePath] string sourceFilePath = "") { }
        public static void LogError(object message, [CallerFilePath] string sourceFilePath = "") { }
    #endif
}
