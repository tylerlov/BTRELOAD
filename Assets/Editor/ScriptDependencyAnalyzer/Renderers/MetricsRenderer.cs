using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ScriptAnalysis.Analyzers;

namespace ScriptAnalysis.Renderers
{
    public class MetricsRenderer
    {
        private Vector2 metricsScroll;

        public void Draw(Dictionary<string, CodeMetrics> metrics)
        {
            if (!metrics.Any()) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Code Metrics", EditorStyles.boldLabel);

            DrawMetricsSummary(metrics);

            metricsScroll = EditorGUILayout.BeginScrollView(metricsScroll);
            DrawDetailedMetrics(metrics);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawMetricsSummary(Dictionary<string, CodeMetrics> metrics)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            float avgComplexity = (float)metrics.Values.Average(m => m.CyclomaticComplexity);
            float avgMaintainability = (float)metrics.Values.Average(m => m.MaintenanceIndex);
            
            EditorGUILayout.BeginHorizontal();
            DrawMetricGauge("Avg Complexity", avgComplexity, 10, 20, 30);
            DrawMetricGauge("Maintainability", avgMaintainability, 20, 50, 80);
            EditorGUILayout.EndHorizontal();

            // Pattern usage summary
            var patterns = metrics.Values
                .SelectMany(m => m.DesignPatterns)
                .GroupBy(p => p)
                .OrderByDescending(g => g.Count());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Design Patterns Used:", EditorStyles.boldLabel);
            foreach (var pattern in patterns.Take(5))
            {
                EditorGUILayout.LabelField($"• {pattern.Key}: {pattern.Count()} uses");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailedMetrics(Dictionary<string, CodeMetrics> metrics)
        {
            foreach (var kvp in metrics.OrderByDescending(m => m.Value.CyclomaticComplexity))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Cyclomatic Complexity: {kvp.Value.CyclomaticComplexity}");
                EditorGUILayout.LabelField($"Lines of Code: {kvp.Value.LinesOfCode}");
                EditorGUILayout.LabelField($"Methods: {kvp.Value.MethodCount}");
                EditorGUILayout.LabelField($"Dependencies: {kvp.Value.DependencyCount}");
                
                if (kvp.Value.DesignPatterns.Any())
                {
                    EditorGUILayout.LabelField("Patterns:", 
                        string.Join(", ", kvp.Value.DesignPatterns));
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        private void DrawMetricGauge(string label, float value, float low, float medium, float high)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            EditorGUILayout.LabelField(label);
            
            Rect r = GUILayoutUtility.GetRect(140, 20);
            Color color = value < low ? Color.green :
                          value < medium ? Color.yellow :
                          value < high ? new Color(1, 0.5f, 0) : 
                          Color.red;
                          
            EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));
            r.width *= Mathf.Min(value / high, 1);
            EditorGUI.DrawRect(r, color);
            
            EditorGUI.LabelField(r, $"{value:F1}", new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
            
            EditorGUILayout.EndVertical();
        }
    }
} 