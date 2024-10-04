// Perfect Culling (C) 2023 Patrick König
//

using UnityEngine;

namespace Koenigz.PerfectCulling
{
    public static class PerfectCullingLogger
    {
        public static void Log(string message, GameObject context = null)
        {
            Debug.Log($"[PC] {message}", context);
        }
        
        public static void LogWarning(string message, GameObject context = null)
        {
            Debug.LogWarning($"[PC] {message}", context);
        }
        
        public static void LogError(string message, GameObject context = null)
        {
            Debug.LogError($"[PC] {message}", context);
        }
    }
}