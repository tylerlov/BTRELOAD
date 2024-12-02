using UnityEngine;

namespace Michsky.UI.Reach
{
    public static class ConditionalDebug
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool isDebugEnabled = true;
        #else
        private static bool isDebugEnabled = false;
        #endif

        public static void Log(string message)
        {
            if (isDebugEnabled)
                Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            if (isDebugEnabled)
                Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            if (isDebugEnabled)
                Debug.LogError(message);
        }
    }
}
