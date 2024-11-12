using UnityEngine;

public class LogManager : MonoBehaviour
{
  public static LogManager Instance { get; private set; }

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
      InitializeLogging();
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void InitializeLogging()
  {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
      Debug.unityLogger.logEnabled = true;
      Debug.unityLogger.filterLogType = LogType.Log | LogType.Warning | LogType.Error;
      Debug.Log("Logging Initialized - Development Mode");
    #else
      // Completely disable all logging in release
      Debug.unityLogger.logEnabled = false;
      // Disable stack traces for all log types
      Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
      Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
      Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
      Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
    #endif
  }
} 