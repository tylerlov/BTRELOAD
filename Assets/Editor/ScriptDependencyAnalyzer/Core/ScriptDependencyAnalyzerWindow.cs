using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ScriptAnalysis.Analyzers;
using ScriptAnalysis.Renderers;
using ScriptAnalysis.Exporters;
using ScriptAnalysis.Core;

namespace ScriptAnalysis.Editor
{
    public class ScriptDependencyAnalyzerWindow : EditorWindow
    {
        public DependencyAnalyzer DependencyAnalyzer { get; private set; } = new DependencyAnalyzer();
        private CodeMetricsAnalyzer codeMetricsAnalyzer;
        private DependencyMatrixRenderer matrixRenderer;
        private DependencyViewRenderer viewRenderer;
        private MetricsRenderer metricsRenderer;
        private CodeInsightRenderer insightRenderer;
        private ExportManager exportManager;
        
        private Vector2 scrollPosition;
        private bool isAnalyzing = false;
        private float analysisProgress = 0f;
        private ViewMode currentView = ViewMode.DependencyMatrix;
        private string targetFolder = "Assets";
        private Object targetFolderObject;

        private enum Tab
        {
            Dependencies,
            Metrics,
            CodeInsights,
            Settings
        }
        private Tab currentTab = Tab.Dependencies;

        private Vector2 settingsScroll;
        private bool showMethodSettings;
        private bool showClassSettings;
        private bool showDependencySettings;
        private bool showUnitySettings;
        private bool showGeneralSettings;
        private AnalysisSettings settings = new AnalysisSettings();

        [MenuItem("Tools/Script Dependency Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<ScriptDependencyAnalyzerWindow>("Script Analysis");
        }

        private void OnEnable()
        {
            DependencyAnalyzer = new DependencyAnalyzer();
            DependencyAnalyzer.OnAnalysisProgress += UpdateProgress;
            
            matrixRenderer = new DependencyMatrixRenderer();
            viewRenderer = new DependencyViewRenderer();
            metricsRenderer = new MetricsRenderer();
            insightRenderer = new CodeInsightRenderer();
            codeMetricsAnalyzer = new CodeMetricsAnalyzer(DependencyAnalyzer);
            exportManager = new ExportManager(DependencyAnalyzer);

            if (!string.IsNullOrEmpty(targetFolder))
            {
                targetFolderObject = AssetDatabase.LoadAssetAtPath<Object>(targetFolder);
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space();

            if (isAnalyzing)
            {
                DrawAnalysisProgress();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawMainContent();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Folder selection
            EditorGUILayout.LabelField("Target Folder:", GUILayout.Width(80));
            GUI.enabled = false;
            EditorGUILayout.TextField(targetFolder, GUILayout.ExpandWidth(true));
            GUI.enabled = true;
            
            if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                string newFolder = EditorUtility.OpenFolderPanel("Select Target Folder", targetFolder, "");
                if (!string.IsNullOrEmpty(newFolder))
                {
                    if (newFolder.StartsWith(Application.dataPath))
                    {
                        targetFolder = "Assets" + newFolder.Substring(Application.dataPath.Length);
                        targetFolderObject = AssetDatabase.LoadAssetAtPath<Object>(targetFolder);
                    }
                }
            }

            GUI.enabled = !isAnalyzing && !string.IsNullOrEmpty(targetFolder);
            if (GUILayout.Button("Analyze", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                AnalyzeDependencies();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            
            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton);
            tabStyle.fixedHeight = 30;
            tabStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Toggle(currentTab == Tab.Dependencies, "Dependencies", tabStyle))
                currentTab = Tab.Dependencies;
            
            if (GUILayout.Toggle(currentTab == Tab.Metrics, "Metrics", tabStyle))
                currentTab = Tab.Metrics;
            
            if (GUILayout.Toggle(currentTab == Tab.CodeInsights, "Code Insights", tabStyle))
                currentTab = Tab.CodeInsights;
            
            if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", tabStyle))
                currentTab = Tab.Settings;

            EditorGUILayout.EndVertical();
        }

        private void DrawMainContent()
        {
            EditorGUILayout.BeginVertical();

            if (DependencyAnalyzer.ScriptMetadata.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No scripts analyzed yet. Select a folder and click 'Analyze' to begin.",
                    MessageType.Info
                );
                EditorGUILayout.EndVertical();
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case Tab.Dependencies:
                    DrawDependenciesTab();
                    break;
                case Tab.Metrics:
                    metricsRenderer.Draw(codeMetricsAnalyzer.CodeMetrics);
                    break;
                case Tab.CodeInsights:
                    insightRenderer.Draw(codeMetricsAnalyzer.CodeInsights);
                    break;
                case Tab.Settings:
                    DrawSettingsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDependenciesTab()
        {
            // Draw view mode selector
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            currentView = (ViewMode)EditorGUILayout.EnumPopup(currentView, EditorStyles.toolbarPopup, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            // Draw the selected view
            if (currentView == ViewMode.DependencyMatrix)
            {
                matrixRenderer.Draw(DependencyAnalyzer.ScriptMetadata);
            }
            else
            {
                // Calculate available height for graph
                float availableHeight = position.height - 150f; // Account for toolbar and padding
                
                // Begin scroll view with both scrollbars
                Rect scrollViewRect = GUILayoutUtility.GetRect(position.width - 20, availableHeight);
                scrollPosition = GUI.BeginScrollView(
                    scrollViewRect,
                    scrollPosition,
                    new Rect(0, 0, 2000, 2000) // Fixed content size
                );

                // Draw the graph in the scrollable area
                viewRenderer.Draw(currentView, DependencyAnalyzer.ScriptMetadata, new Rect(0, 0, 2000, 2000));

                GUI.EndScrollView();
            }
        }

        private void DrawSettingsTab()
        {
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
            exportManager.DrawExportOptions();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Analysis Settings", EditorStyles.boldLabel);
            
            // Method Settings
            showMethodSettings = EditorGUILayout.Foldout(showMethodSettings, "Method Analysis Settings", true);
            if (showMethodSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUI.BeginChangeCheck();
                int newLargeMethodThreshold = EditorGUILayout.IntField(
                    new GUIContent("Large Method Line Threshold", 
                        "Methods with more lines than this will be flagged"),
                    settings.LargeMethodLineThreshold);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Change Large Method Threshold");
                    settings.LargeMethodLineThreshold = newLargeMethodThreshold;
                }

                EditorGUI.BeginChangeCheck();
                int newHighComplexity = EditorGUILayout.IntField(
                    new GUIContent("High Complexity Threshold", 
                        "Methods with higher cyclomatic complexity will be flagged as Critical"),
                    settings.HighComplexityThreshold);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Change High Complexity Threshold");
                    settings.HighComplexityThreshold = newHighComplexity;
                }

                EditorGUI.BeginChangeCheck();
                int newModerateComplexity = EditorGUILayout.IntField(
                    new GUIContent("Moderate Complexity Threshold", 
                        "Methods with higher cyclomatic complexity will be flagged as Medium"),
                    settings.ModerateComplexityThreshold);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Change Moderate Complexity Threshold");
                    settings.ModerateComplexityThreshold = newModerateComplexity;
                }

                EditorGUI.indentLevel--;
            }

            // Class Settings
            showClassSettings = EditorGUILayout.Foldout(showClassSettings, "Class Analysis Settings", true);
            if (showClassSettings)
            {
                EditorGUI.indentLevel++;
                settings.GodClassMethodCount = EditorGUILayout.IntField(
                    new GUIContent("God Class Method Threshold", 
                        "Classes with more methods than this will be considered in God Class detection"),
                    settings.GodClassMethodCount);
                
                settings.GodClassDependencyCount = EditorGUILayout.IntField(
                    new GUIContent("God Class Dependency Threshold", 
                        "Classes with more dependencies than this will be considered in God Class detection"),
                    settings.GodClassDependencyCount);
                
                settings.GodClassLineCount = EditorGUILayout.IntField(
                    new GUIContent("God Class Line Threshold", 
                        "Classes with more lines than this will be considered in God Class detection"),
                    settings.GodClassLineCount);
                EditorGUI.indentLevel--;
            }

            // Dependency Settings
            showDependencySettings = EditorGUILayout.Foldout(showDependencySettings, "Dependency Analysis Settings", true);
            if (showDependencySettings)
            {
                EditorGUI.indentLevel++;
                settings.HighDependencyCount = EditorGUILayout.IntField(
                    new GUIContent("High Dependency Threshold", 
                        "Classes with more outgoing dependencies than this will be flagged"),
                    settings.HighDependencyCount);
                
                settings.HighIncomingDependencyCount = EditorGUILayout.IntField(
                    new GUIContent("High Incoming Dependency Threshold", 
                        "Classes with more incoming dependencies than this will be flagged"),
                    settings.HighIncomingDependencyCount);
                
                settings.UnstableDependencyThreshold = EditorGUILayout.IntField(
                    new GUIContent("Unstable Dependency Threshold", 
                        "Number of unstable dependencies allowed before flagging"),
                    settings.UnstableDependencyThreshold);
                EditorGUI.indentLevel--;
            }

            // Unity-Specific Settings
            showUnitySettings = EditorGUILayout.Foldout(showUnitySettings, "Unity-Specific Settings", true);
            if (showUnitySettings)
            {
                EditorGUI.indentLevel++;
                settings.UpdateMethodComplexityThreshold = EditorGUILayout.IntField(
                    new GUIContent("Update Method Complexity Threshold", 
                        "Update methods with higher complexity than this will be flagged"),
                    settings.UpdateMethodComplexityThreshold);
                
                settings.UpdateMethodLineThreshold = EditorGUILayout.IntField(
                    new GUIContent("Update Method Line Threshold", 
                        "Update methods with more lines than this will be flagged"),
                    settings.UpdateMethodLineThreshold);
                EditorGUI.indentLevel--;
            }

            // General Analysis Settings
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Analysis Settings", true);
            if (showGeneralSettings)
            {
                EditorGUI.indentLevel++;
                settings.DetectCircularDependencies = EditorGUILayout.Toggle(
                    new GUIContent("Detect Circular Dependencies", 
                        "Check for circular dependencies between classes"),
                    settings.DetectCircularDependencies);
                
                settings.DetectLayerViolations = EditorGUILayout.Toggle(
                    new GUIContent("Detect Layer Violations", 
                        "Check for architectural layer violations"),
                    settings.DetectLayerViolations);
                
                settings.DetectNamingIssues = EditorGUILayout.Toggle(
                    new GUIContent("Detect Naming Issues", 
                        "Check for naming convention violations"),
                    settings.DetectNamingIssues);
                
                settings.DetectPatternIssues = EditorGUILayout.Toggle(
                    new GUIContent("Detect Pattern Issues", 
                        "Check for pattern-related issues and opportunities"),
                    settings.DetectPatternIssues);
                
                settings.DetectUnitySpecificIssues = EditorGUILayout.Toggle(
                    new GUIContent("Detect Unity-Specific Issues", 
                        "Check for Unity-specific best practice violations"),
                    settings.DetectUnitySpecificIssues);
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                Undo.RecordObject(this, "Reset Analysis Settings");
                settings = new AnalysisSettings();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawAnalysisProgress()
        {
            EditorGUILayout.HelpBox("Analyzing dependencies...", MessageType.Info);
            EditorGUI.ProgressBar(
                GUILayoutUtility.GetRect(10, 20),
                analysisProgress,
                $"Analyzed {(int)(analysisProgress * 100)}%"
            );
        }

        private void AnalyzeDependencies()
        {
            if (string.IsNullOrEmpty(targetFolder))
            {
                EditorUtility.DisplayDialog("Error", "Please select a target folder first.", "OK");
                return;
            }

            isAnalyzing = true;
            analysisProgress = 0f;
            
            EditorApplication.delayCall += () =>
            {
                DependencyAnalyzer.AnalyzeDependencies(targetFolder);
                codeMetricsAnalyzer.UpdateSettings(settings);
                codeMetricsAnalyzer.AnalyzeMetrics(DependencyAnalyzer.ScriptMetadata);
                isAnalyzing = false;
                Repaint();
            };
        }

        private void UpdateProgress(float progress)
        {
            analysisProgress = progress;
            Repaint();
        }

        private void OnDisable()
        {
            if (DependencyAnalyzer != null)
            {
                DependencyAnalyzer.OnAnalysisProgress -= UpdateProgress;
            }
        }
    }
} 