using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScriptAnalysis.Analyzers;

namespace ScriptAnalysis.Renderers
{
    public class DependencyMatrixRenderer
    {
        private bool showIndirectDependencies = false;
        private Vector2 matrixScroll;
        private float cellSize = 20f;
        private Dictionary<string, ScriptMetadata> scriptMetadata;
        
        private Color directDependencyColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        private Color reverseDependencyColor = new Color(0.2f, 0.8f, 0.4f, 0.8f);
        private Color mutualDependencyColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        public void Draw(Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            if (scriptMetadata.Count == 0) return;
            
            this.scriptMetadata = scriptMetadata;

            DrawControls();

            var scripts = GetOrderedScripts(scriptMetadata);
            float totalSize = scripts.Count * cellSize;
            
            matrixScroll = EditorGUILayout.BeginScrollView(matrixScroll);
            
            var matrixRect = GUILayoutUtility.GetRect(totalSize, totalSize);
            
            DrawGrid(matrixRect, scripts.Count);
            DrawLabels(matrixRect, scripts);
            DrawDependencies(matrixRect, scripts);
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawControls()
        {
            showIndirectDependencies = EditorGUILayout.Toggle(
                new GUIContent("Show Indirect Dependencies",
                "Show dependencies through intermediate scripts"),
                showIndirectDependencies);

            cellSize = EditorGUILayout.Slider(
                new GUIContent("Cell Size", "Adjust the size of matrix cells"),
                cellSize, 10f, 40f);
        }

        private void DrawGrid(Rect matrixRect, int scriptCount)
        {
            Handles.color = Color.gray;
            for (int i = 0; i <= scriptCount; i++)
            {
                float pos = i * cellSize;
                Handles.DrawLine(
                    new Vector2(matrixRect.x, matrixRect.y + pos),
                    new Vector2(matrixRect.x + matrixRect.width, matrixRect.y + pos)
                );
                Handles.DrawLine(
                    new Vector2(matrixRect.x + pos, matrixRect.y),
                    new Vector2(matrixRect.x + pos, matrixRect.y + matrixRect.height)
                );
            }
        }

        private void DrawLabels(Rect matrixRect, List<ScriptMetadata> scripts)
        {
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.alignment = TextAnchor.MiddleRight;

            // Draw row labels (left side)
            for (int i = 0; i < scripts.Count; i++)
            {
                var labelRect = new Rect(
                    matrixRect.x - 150, // Adjust this value to fit your labels
                    matrixRect.y + (i * cellSize),
                    145,
                    cellSize
                );
                GUI.Label(labelRect, scripts[i].Name, style);
            }

            // Draw column labels (top)
            style.alignment = TextAnchor.MiddleLeft;
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(-45, new Vector2(matrixRect.x, matrixRect.y));
            
            for (int i = 0; i < scripts.Count; i++)
            {
                var labelRect = new Rect(
                    matrixRect.x + (i * cellSize),
                    matrixRect.y - 20,
                    100,
                    20
                );
                GUI.Label(labelRect, scripts[i].Name, style);
            }
            
            GUI.matrix = matrix;
        }

        private void DrawDependencies(Rect matrixRect, List<ScriptMetadata> scripts)
        {
            for (int i = 0; i < scripts.Count; i++)
            {
                for (int j = 0; j < scripts.Count; j++)
                {
                    var source = scripts[i];
                    var target = scripts[j];
                    
                    if (HasDependency(source, target, showIndirectDependencies))
                    {
                        var cellRect = new Rect(
                            matrixRect.x + (j * cellSize),
                            matrixRect.y + (i * cellSize),
                            cellSize,
                            cellSize
                        );

                        var color = GetDependencyColor(source, target);
                        EditorGUI.DrawRect(cellRect, color);

                        if (cellRect.Contains(Event.current.mousePosition))
                        {
                            DrawTooltip(source, target);
                        }
                    }
                }
            }
        }

        private List<ScriptMetadata> GetOrderedScripts(Dictionary<string, ScriptMetadata> metadata)
        {
            return metadata.Values
                .OrderBy(m => m.Name)
                .ToList();
        }

        private bool HasDependency(ScriptMetadata source, ScriptMetadata target, bool includeIndirect)
        {
            if (source.Name == target.Name) return false;
            
            // Check direct dependency
            if (source.References.ContainsKey(target.Name))
                return true;

            // Check indirect dependencies if requested
            if (includeIndirect)
            {
                var visited = new HashSet<string>();
                return HasIndirectDependency(source, target.Name, visited);
            }

            return false;
        }

        private bool HasIndirectDependency(ScriptMetadata source, string targetName, HashSet<string> visited)
        {
            if (visited.Contains(source.Name)) return false;
            visited.Add(source.Name);

            foreach (var reference in source.References.Keys)
            {
                if (reference == targetName) return true;
                
                if (scriptMetadata.TryGetValue(reference, out var referencedScript))
                {
                    if (HasIndirectDependency(referencedScript, targetName, visited))
                        return true;
                }
            }

            return false;
        }

        private Color GetDependencyColor(ScriptMetadata source, ScriptMetadata target)
        {
            bool forward = source.References.ContainsKey(target.Name);
            bool reverse = target.References.ContainsKey(source.Name);

            if (forward && reverse) return mutualDependencyColor;
            if (forward) return directDependencyColor;
            if (reverse) return reverseDependencyColor;
            return Color.clear;
        }

        private void DrawTooltip(ScriptMetadata source, ScriptMetadata target)
        {
            var style = new GUIStyle(EditorStyles.helpBox);
            style.normal.textColor = Color.white;
            style.padding = new RectOffset(5, 5, 5, 5);

            var content = new StringBuilder();
            content.AppendLine($"{source.Name} → {target.Name}");

            if (source.References.TryGetValue(target.Name, out var refCount))
            {
                content.AppendLine($"References: {refCount}");
            }

            if (source.MethodCalls.Contains(target.Name))
            {
                content.AppendLine("Type: Method Call");
            }
            
            if (source.FieldReferences.Contains(target.Name))
            {
                content.AppendLine("Type: Field Reference");
            }

            var rect = new Rect(Event.current.mousePosition + new Vector2(10, 10), 
                new Vector2(200, style.CalcHeight(new GUIContent(content.ToString()), 200)));
                
            GUI.Label(rect, content.ToString(), style);
        }
    }
} 