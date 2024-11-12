using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ScriptAnalysis.Analyzers;

namespace ScriptAnalysis.Renderers
{
    public class CodeInsightRenderer
    {
        private Vector2 insightScroll;
        private InsightSeverity minimumSeverity = InsightSeverity.Low;
        private Dictionary<string, bool> expandedScripts = new Dictionary<string, bool>();

        public void Draw(List<CodeInsight> insights)
        {
            if (!insights.Any()) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Code Insights", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            minimumSeverity = (InsightSeverity)EditorGUILayout.EnumPopup(
                "Show Severity:", 
                minimumSeverity
            );

            var filteredInsights = insights.Where(s => s.Severity == minimumSeverity);

            insightScroll = EditorGUILayout.BeginScrollView(insightScroll);

            if (filteredInsights.Any())
            {
                DrawSeverityGroup(minimumSeverity, filteredInsights);
            }
            else
            {
                EditorGUILayout.HelpBox($"No insights found with {minimumSeverity} severity.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSeverityGroup(InsightSeverity severity, IEnumerable<CodeInsight> insights)
        {
            EditorGUILayout.Space();
            
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = GetSeverityColor(severity);
            EditorGUILayout.LabelField($"{severity} Insights:", style);

            foreach (var insight in insights)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField(insight.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(insight.Description);
                
                EditorGUILayout.LabelField("Affected Scripts:", EditorStyles.miniLabel);
                foreach (var script in insight.AffectedScripts)
                {
                    // Ensure the script has an entry in expandedScripts
                    if (!expandedScripts.ContainsKey(script))
                    {
                        expandedScripts[script] = false;
                    }

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Foldout for script
                    expandedScripts[script] = EditorGUILayout.Foldout(
                        expandedScripts[script], 
                        $"• {script}",
                        true
                    );
                    
                    // Open in IDE button
                    if (GUILayout.Button("Open in IDE", GUILayout.Width(80)))
                    {
                        OpenScriptInIDE(script);
                    }
                    
                    EditorGUILayout.EndHorizontal();

                    // Show fix recommendation if expanded
                    if (expandedScripts[script])
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        var recommendationStyle = new GUIStyle(EditorStyles.label);
                        recommendationStyle.wordWrap = true;
                        recommendationStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                        EditorGUILayout.LabelField("How to fix:", EditorStyles.miniBoldLabel);
                        EditorGUILayout.LabelField(insight.Recommendation, recommendationStyle);
                        EditorGUILayout.EndVertical();
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        private void OpenScriptInIDE(string scriptName)
        {
            // Find the script asset
            string[] guids = AssetDatabase.FindAssets($"t:Script {scriptName}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }
            else
            {
                Debug.LogWarning($"Could not find script: {scriptName}");
            }
        }

        private Color GetSeverityColor(InsightSeverity severity)
        {
            return severity switch
            {
                InsightSeverity.Critical => Color.red,
                InsightSeverity.High => new Color(1, 0.5f, 0),
                InsightSeverity.Medium => Color.yellow,
                _ => Color.green
            };
        }
    }
} 