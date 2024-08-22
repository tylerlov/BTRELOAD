using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditorInternal;

public class DebugSettingsWindow : EditorWindow
{
    private DebugSettings settings;
    private Vector2 scrollPosition;
    private ReorderableList reorderableList;
    private List<string> allClassNames;
    private string searchString = "";
    private Vector2 projectileStatsScrollPosition;

    [MenuItem("Tools/Debug Settings")]
    public static void ShowWindow()
    {
        GetWindow<DebugSettingsWindow>("Debug Settings");
    }

    private void OnEnable()
    {
        settings = Resources.Load<DebugSettings>("DebugSettings");
        if (settings == null)
        {
            settings = CreateInstance<DebugSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Resources/DebugSettings.asset");
            AssetDatabase.SaveAssets();
        }

        RefreshClassNames();
        InitializeReorderableList();
    }

    private void RefreshClassNames()
    {
        allClassNames = GetScriptClassNames();
        if (reorderableList != null)
        {
            reorderableList.list = settings.classLogSettings.Keys.Intersect(allClassNames).ToList();
        }
    }

    private void InitializeReorderableList()
    {
        reorderableList = new ReorderableList(settings.classLogSettings.Keys.ToList(), typeof(string), true, true, true, true);
        reorderableList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Class-specific Log Settings");
        reorderableList.drawElementCallback = DrawListItems;
        reorderableList.onAddDropdownCallback = AddDropdownCallback;
        reorderableList.onRemoveCallback = (ReorderableList l) =>
        {
            string key = (string)l.list[l.index];
            settings.classLogSettings.Remove(key);
            l.list.RemoveAt(l.index);
        };
    }

    private void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
    {
        string key = (string)reorderableList.list[index];
        var classSetting = settings.classLogSettings[key];

        rect.y += 2;
        rect.height = EditorGUIUtility.singleLineHeight;

        float toggleWidth = 20f;
        float nameWidth = rect.width * 0.4f;
        float levelWidth = rect.width - nameWidth - toggleWidth - 10f;

        classSetting.isEnabled = EditorGUI.Toggle(new Rect(rect.x, rect.y, toggleWidth, rect.height), classSetting.isEnabled);
        EditorGUI.LabelField(new Rect(rect.x + toggleWidth, rect.y, nameWidth, rect.height), key);
        classSetting.logLevel = (DebugSettings.LogLevel)EditorGUI.EnumFlagsField(new Rect(rect.x + toggleWidth + nameWidth + 5f, rect.y, levelWidth, rect.height), classSetting.logLevel);
    }

    private void AddDropdownCallback(Rect buttonRect, ReorderableList list)
    {
        var menu = new GenericMenu();
        foreach (var className in allClassNames.Where(c => !settings.classLogSettings.ContainsKey(c)))
        {
            menu.AddItem(new GUIContent(className), false, () =>
            {
                settings.classLogSettings[className] = new DebugSettings.ClassLogSetting();
                list.list = settings.classLogSettings.Keys.ToList();
            });
        }
        menu.ShowAsContext();
    }

    private void OnGUI()
    {
        if (settings == null)
        {
            EditorGUILayout.LabelField("Error: DebugSettings not found.");
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUI.BeginChangeCheck();

        settings.isLoggingEnabled = EditorGUILayout.Toggle("Enable Logging", settings.isLoggingEnabled);

        EditorGUI.indentLevel++;
        if (settings.isLoggingEnabled)
        {
            settings.globalLogLevel = (DebugSettings.LogLevel)EditorGUILayout.EnumFlagsField("Global Log Levels", settings.globalLogLevel);
            settings.includeStackTrace = EditorGUILayout.Toggle("Include Stack Trace", settings.includeStackTrace);
            settings.logToFile = EditorGUILayout.Toggle("Log to File", settings.logToFile);

            if (settings.logToFile)
            {
                settings.logFilePath = EditorGUILayout.TextField("Log File Path", settings.logFilePath);
                settings.maxLogFileSizeKB = EditorGUILayout.IntField("Max Log File Size (KB)", settings.maxLogFileSizeKB);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Log File"))
                {
                    if (File.Exists(settings.logFilePath))
                    {
                        EditorUtility.OpenWithDefaultApp(settings.logFilePath);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Log file does not exist.", "OK");
                    }
                }
                if (GUILayout.Button("Clear Logs"))
                {
                    if (File.Exists(settings.logFilePath))
                    {
                        File.Delete(settings.logFilePath);
                        EditorUtility.DisplayDialog("Success", "Log file cleared.", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Log file does not exist.", "OK");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            searchString = EditorGUILayout.TextField("Search Classes", searchString);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshClassNames();
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(searchString))
            {
                reorderableList.list = settings.classLogSettings.Keys.Where(k => k.ToLower().Contains(searchString.ToLower())).ToList();
            }
            else
            {
                reorderableList.list = settings.classLogSettings.Keys.ToList();
            }

            reorderableList.DoLayoutList();
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        settings.showPerformanceMetrics = EditorGUILayout.Toggle("Show Performance Metrics", settings.showPerformanceMetrics);
        settings.showVisualDebugging = EditorGUILayout.Toggle("Show Visual Debugging", settings.showVisualDebugging);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Chronos Time Control", EditorStyles.boldLabel);
        settings.globalClockName = EditorGUILayout.TextField("Global Clock Name", settings.globalClockName);
        settings.debugTimeScale = EditorGUILayout.Slider("Debug Time Scale", settings.debugTimeScale, 0.1f, 10f);
        
        if (GUILayout.Button("Reset Time Scale"))
        {
            settings.debugTimeScale = 1f;
            EditorUtility.SetDirty(settings);
        }

        DrawProjectileHitStats();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawProjectileHitStats()
    {
        EditorGUILayout.LabelField("Projectile Hit Statistics", EditorStyles.boldLabel);
        
        projectileStatsScrollPosition = EditorGUILayout.BeginScrollView(projectileStatsScrollPosition);

        EditorGUILayout.LabelField($"Total Player Projectiles Shot: {settings.projectileHitStats.totalPlayerProjectilesShot}");
        EditorGUILayout.LabelField($"Player Projectile Hits: {settings.projectileHitStats.playerProjectileHits}");
        EditorGUILayout.LabelField($"Player Projectiles Missed: {settings.projectileHitStats.playerProjectilesMissed}");
        EditorGUILayout.LabelField($"Player Projectiles Expired: {settings.projectileHitStats.playerProjectilesExpired}");
        EditorGUILayout.LabelField($"Hit Rate: {settings.projectileHitStats.hitRate:F2}%");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Missed Hit Objects:", EditorStyles.boldLabel);
        foreach (var kvp in settings.projectileHitStats.missedHitObjects)
        {
            EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}");
        }

        if (GUILayout.Button("Reset Projectile Stats"))
        {
            settings.projectileHitStats = new DebugSettings.ProjectileHitStats();
            EditorUtility.SetDirty(settings);
        }

        EditorGUILayout.EndScrollView();
    }

    private List<string> GetScriptClassNames()
    {
        List<string> classNames = new List<string>();
        string scriptsPath = "Assets/_Scripts";
        
        if (Directory.Exists(scriptsPath))
        {
            string[] scriptFiles = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
            foreach (string scriptFile in scriptFiles)
            {
                string className = Path.GetFileNameWithoutExtension(scriptFile);
                classNames.Add(className);
            }
        }
        else
        {
            Debug.LogWarning("_Scripts folder not found.");
        }

        return classNames;
    }
}