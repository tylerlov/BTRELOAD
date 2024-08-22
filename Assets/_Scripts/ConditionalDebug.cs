using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class ConditionalDebug
{
    private static DebugSettings settings;
    private static StreamWriter logFileWriter;

    public static bool IsLoggingEnabled
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

    private static void EnsureLogFileWriter()
    {
        if (settings.logToFile && logFileWriter == null)
        {
            logFileWriter = new StreamWriter(settings.logFilePath, true);
        }
        else if (!settings.logToFile && logFileWriter != null)
        {
            logFileWriter.Close();
            logFileWriter = null;
        }
    }

    private static void LogToFile(string message)
    {
        EnsureLogFileWriter();
        if (logFileWriter != null)
        {
            logFileWriter.WriteLine($"{System.DateTime.Now}: {message}");
            logFileWriter.Flush();
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(
        object message,
        DebugSettings.LogLevel level = DebugSettings.LogLevel.Info,
        [CallerFilePath] string sourceFilePath = ""
    )
    {
        LogInternal(message, level, sourceFilePath);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message, [CallerFilePath] string sourceFilePath = "")
    {
        LogInternal(message, DebugSettings.LogLevel.Warning, sourceFilePath);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message, [CallerFilePath] string sourceFilePath = "")
    {
        LogInternal(message, DebugSettings.LogLevel.Error, sourceFilePath);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogVerbose(object message, [CallerFilePath] string sourceFilePath = "")
    {
        LogInternal(message, DebugSettings.LogLevel.Verbose, sourceFilePath);
    }

    private static void LogInternal(
        object message,
        DebugSettings.LogLevel level,
        string sourceFilePath
    )
    {
        if (!IsLoggingEnabled)
            return;

        string className = Path.GetFileNameWithoutExtension(sourceFilePath);
        var classLogSetting = settings.GetClassLogSetting(className);

        if (classLogSetting.isEnabled && classLogSetting.logLevel.HasFlag(level))
        {
            string logMessage = $"[{className}] {message}";
            if (settings.includeStackTrace)
            {
                logMessage += "\n" + System.Environment.StackTrace;
            }

            switch (level)
            {
                case DebugSettings.LogLevel.Error:
                    Debug.LogError(logMessage);
                    break;
                case DebugSettings.LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                default:
                    Debug.Log(logMessage);
                    break;
            }

            if (settings.logToFile)
            {
                LogToFile(logMessage);
            }
        }
    }

    public static void LogProjectileHit(
        bool isPlayerShot,
        bool hitEnemy,
        string additionalInfo = ""
    )
    {
        if (IsLoggingEnabled)
        {
            string message = isPlayerShot
                ? (hitEnemy ? "Player projectile hit enemy" : "Player projectile missed")
                : (hitEnemy ? "Enemy projectile hit player" : "Enemy projectile missed");

            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" - {additionalInfo}";
            }

            Debug.Log($"[ProjectileHit] {message}");
            LogToFile($"[ProjectileHit] {message}");
        }
    }
}
