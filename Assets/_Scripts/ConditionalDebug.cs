using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConditionalDebug
{
    public static bool IsLoggingEnabled;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        if (IsLoggingEnabled)
        {
            Debug.Log(message);
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
        if (IsLoggingEnabled)
        {
            Debug.LogWarning(message);
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
        if (IsLoggingEnabled)
        {
            Debug.LogError(message);
        }
    }
    // ...add more for other Debug methods you use
}
