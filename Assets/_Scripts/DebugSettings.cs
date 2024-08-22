using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableDictionary<TKey, TValue>
    : Dictionary<TKey, TValue>,
        ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys.Count != values.Count)
            throw new System.Exception(
                $"there are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable."
            );

        for (int i = 0; i < keys.Count; i++)
            this.Add(keys[i], values[i]);
    }
}

[CreateAssetMenu(fileName = "DebugSettings", menuName = "Debug/Settings", order = 1)]
public class DebugSettings : ScriptableObject
{
    [System.Flags]
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 4,
        Verbose = 8,
    }

    public bool isLoggingEnabled = false;
    public LogLevel globalLogLevel = LogLevel.Error | LogLevel.Warning | LogLevel.Info;
    public bool includeStackTrace = false;
    public bool logToFile = false;
    public string logFilePath = "Logs/debug_log.txt";
    public int maxLogFileSizeKB = 1024; // 1MB
    public bool showPerformanceMetrics = false;
    public bool showVisualDebugging = false;
    public string globalClockName = "Test";
    public float debugTimeScale = 1f;

    [System.Serializable]
    public class ClassLogSetting
    {
        public bool isEnabled;
        public LogLevel logLevel;

        public ClassLogSetting()
        {
            this.isEnabled = true;
            this.logLevel = LogLevel.Error | LogLevel.Warning | LogLevel.Info;
        }
    }

    public SerializableDictionary<string, ClassLogSetting> classLogSettings =
        new SerializableDictionary<string, ClassLogSetting>();

    public ClassLogSetting GetClassLogSetting(string className)
    {
        if (!classLogSettings.TryGetValue(className, out ClassLogSetting setting))
        {
            setting = new ClassLogSetting();
            classLogSettings[className] = setting;
        }
        return setting;
    }

    [System.Serializable]
    public class ProjectileHitStats
    {
        public int totalPlayerProjectilesShot;
        public int playerProjectileHits;
        public int playerProjectilesMissed;
        public int playerProjectilesExpired;
        public Dictionary<string, int> missedHitObjects = new Dictionary<string, int>();
        public float hitRate =>
            totalPlayerProjectilesShot > 0
                ? (float)playerProjectileHits / totalPlayerProjectilesShot * 100f
                : 0f;
    }

    public ProjectileHitStats projectileHitStats = new ProjectileHitStats();

    public void LogProjectileHit(bool isPlayerShot, bool hitEnemy, string hitObjectTag = null)
    {
        if (isPlayerShot)
        {
            projectileHitStats.totalPlayerProjectilesShot++;
            if (hitEnemy)
            {
                projectileHitStats.playerProjectileHits++;
            }
            else
            {
                projectileHitStats.playerProjectilesMissed++;
                if (!string.IsNullOrEmpty(hitObjectTag))
                {
                    if (!projectileHitStats.missedHitObjects.ContainsKey(hitObjectTag))
                    {
                        projectileHitStats.missedHitObjects[hitObjectTag] = 0;
                    }
                    projectileHitStats.missedHitObjects[hitObjectTag]++;
                }
            }
        }
    }

    public void LogProjectileExpired(bool isPlayerShot)
    {
        if (isPlayerShot)
        {
            projectileHitStats.playerProjectilesExpired++;
        }
    }
}
