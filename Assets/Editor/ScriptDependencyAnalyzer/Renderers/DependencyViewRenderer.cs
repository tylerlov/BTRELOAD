using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ScriptAnalysis.Analyzers;
using ScriptAnalysis.Editor;

namespace ScriptAnalysis.Renderers
{
    public class DependencyViewRenderer
    {
        private Dictionary<string, Rect> nodePositions = new Dictionary<string, Rect>();
        private const float NODE_WIDTH = 150f;
        private const float NODE_HEIGHT = 30f;
        private const float VERTICAL_SPACING = 50f;
        private const float HORIZONTAL_SPACING = 200f;
        private const float GROUP_PADDING = 20f;

        public void Draw(ViewMode viewMode, Dictionary<string, ScriptMetadata> scriptMetadata, Rect graphArea)
        {
            // Clear previous positions
            nodePositions.Clear();

            // Begin drawing area
            GUI.BeginGroup(graphArea);

            // Add padding to prevent edge clipping
            float padding = 50f;
            Rect drawArea = new Rect(
                padding,
                padding,
                graphArea.width - (padding * 2),
                graphArea.height - (padding * 2)
            );

            // Draw the appropriate graph
            switch (viewMode)
            {
                case ViewMode.SystemOverview:
                    DrawSystemOverviewGraph(scriptMetadata, drawArea);
                    break;
                case ViewMode.LayeredArchitecture:
                    DrawLayeredArchitectureGraph(scriptMetadata, drawArea);
                    break;
                case ViewMode.ComponentGroups:
                    DrawComponentGroupsGraph(scriptMetadata, drawArea);
                    break;
            }

            DrawLegend(drawArea);

            GUI.EndGroup();
        }

        private Rect CalculateContentBounds(ViewMode viewMode, Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            switch (viewMode)
            {
                case ViewMode.ComponentGroups:
                    var groups = scriptMetadata.Values.GroupBy(m => GetComponentGroup(m)).Count();
                    float radius = (groups * (NODE_WIDTH + HORIZONTAL_SPACING)) / (2 * Mathf.PI);
                    return new Rect(0, 0, radius * 3, radius * 3);

                case ViewMode.LayeredArchitecture:
                    var layers = GetArchitecturalLayers(scriptMetadata);
                    float maxLayerWidth = layers.Max(l => l.Value.Count * (NODE_WIDTH + HORIZONTAL_SPACING));
                    float totalHeight = layers.Count * (NODE_HEIGHT + VERTICAL_SPACING * 3);
                    return new Rect(0, 0, maxLayerWidth + 200, totalHeight + 100);

                case ViewMode.SystemOverview:
                    var systems = scriptMetadata.Values.GroupBy(m => GetSystemType(m));
                    float maxSystemWidth = systems.Max(g => g.Count() * (NODE_WIDTH + HORIZONTAL_SPACING));
                    float totalSystemHeight = systems.Count() * (NODE_HEIGHT + VERTICAL_SPACING * 2);
                    return new Rect(0, 0, maxSystemWidth + 200, totalSystemHeight + 100);

                default:
                    return new Rect(0, 0, 2000, 2000);
            }
        }

        private void DrawSystemOverviewGraph(Dictionary<string, ScriptMetadata> scriptMetadata, Rect availableRect)
        {
            var systems = scriptMetadata.Values
                .GroupBy(m => GetSystemType(m))
                .OrderBy(g => g.Key)
                .ToList();

            float startY = availableRect.y + 50;
            float startX = availableRect.x + 50;
            float maxWidth = 0;
            nodePositions.Clear();

            // First pass to calculate total height and width
            foreach (var system in systems)
            {
                float groupWidth = NODE_WIDTH + 40;
                float groupHeight = (NODE_HEIGHT + 10) * system.Count() + 40;
                maxWidth = Mathf.Max(maxWidth, groupWidth);
            }

            // Draw systems
            for (int i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                float groupY = startY + (i * (VERTICAL_SPACING + NODE_HEIGHT * 2));

                // Draw system group box
                var groupRect = new Rect(
                    startX,
                    groupY,
                    maxWidth,
                    (NODE_HEIGHT + 10) * system.Count() + 40
                );

                EditorGUI.DrawRect(groupRect, new Color(0.2f, 0.2f, 0.2f, 0.1f));
                
                // Draw system label
                var labelRect = new Rect(groupRect.x + 10, groupY + 10, groupRect.width - 20, 20);
                EditorGUI.LabelField(labelRect, $"{system.Key} ({system.Count()})", EditorStyles.boldLabel);

                // Position nodes
                float nodeY = groupY + 40;
                foreach (var script in system)
                {
                    var nodeRect = new Rect(
                        startX + 20,
                        nodeY,
                        NODE_WIDTH,
                        NODE_HEIGHT
                    );
                    nodePositions[script.Name] = nodeRect;
                    nodeY += NODE_HEIGHT + 10;
                }
            }

            // Draw connections and nodes
            DrawConnectionsAndNodes(scriptMetadata);
        }

        private void DrawLayeredArchitectureGraph(Dictionary<string, ScriptMetadata> scriptMetadata, Rect availableRect)
        {
            var layers = GetArchitecturalLayers(scriptMetadata);
            float startY = availableRect.y + 50;
            float startX = availableRect.x + 50;
            nodePositions.Clear();

            // Calculate maximum width needed for any layer
            float maxLayerWidth = layers.Max(l => l.Value.Count * (NODE_WIDTH + HORIZONTAL_SPACING));

            // Draw layers
            float currentY = startY;
            foreach (var layer in layers)
            {
                // Draw layer background
                var layerRect = new Rect(
                    startX,
                    currentY,
                    maxLayerWidth + 100,
                    NODE_HEIGHT + 40
                );
                EditorGUI.DrawRect(layerRect, new Color(0.2f, 0.2f, 0.2f, 0.1f));

                // Draw layer label
                EditorGUI.LabelField(
                    new Rect(layerRect.x + 10, currentY + 10, 200, 20),
                    $"{layer.Key} Layer ({layer.Value.Count})",
                    EditorStyles.boldLabel
                );

                // Position nodes in layer
                float nodeX = startX + 50;
                for (int i = 0; i < layer.Value.Count; i++)
                {
                    var script = layer.Value[i];
                    var nodeRect = new Rect(
                        nodeX,
                        currentY + 40,
                        NODE_WIDTH,
                        NODE_HEIGHT
                    );
                    nodePositions[script.Name] = nodeRect;
                    nodeX += NODE_WIDTH + HORIZONTAL_SPACING;
                }

                currentY += NODE_HEIGHT + VERTICAL_SPACING + 40;
            }

            // Draw connections and nodes
            DrawConnectionsAndNodes(scriptMetadata);
        }

        private void DrawComponentGroupsGraph(Dictionary<string, ScriptMetadata> scriptMetadata, Rect graphArea)
        {
            var groups = scriptMetadata.Values
                .GroupBy(m => GetComponentGroup(m))
                .OrderByDescending(g => g.Count())
                .ToList();

            // Calculate optimal radius based on group count and available space
            float minDimension = Mathf.Min(graphArea.width, graphArea.height);
            float radius = minDimension * 0.35f;
            
            float centerX = graphArea.x + graphArea.width * 0.5f;
            float centerY = graphArea.y + graphArea.height * 0.5f;

            // Position groups in a circle with proper spacing
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                float angle = (2 * Mathf.PI * i / groups.Count) + (Mathf.PI / groups.Count);
                
                // Calculate group dimensions
                float groupHeight = (NODE_HEIGHT + 5) * group.Count() + 40;
                float groupWidth = NODE_WIDTH + 40;

                // Calculate group position with offset to prevent overlap
                float groupRadius = radius + (groupHeight * 0.5f); // Adjust radius based on group size
                float groupX = centerX + (radius * Mathf.Cos(angle));
                float groupY = centerY + (radius * Mathf.Sin(angle));

                // Create group rectangle
                var groupRect = new Rect(
                    groupX - groupWidth * 0.5f,
                    groupY - groupHeight * 0.5f,
                    groupWidth,
                    groupHeight
                );

                // Draw group background
                EditorGUI.DrawRect(groupRect, new Color(0.2f, 0.2f, 0.2f, 0.1f));
                
                // Draw group label
                var labelRect = new Rect(
                    groupRect.x,
                    groupRect.y + 10,
                    groupRect.width,
                    20
                );
                EditorGUI.LabelField(
                    labelRect,
                    $"{group.Key} ({group.Count()})",
                    new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter }
                );

                // Position nodes within group
                float nodeY = groupRect.y + 40;
                foreach (var script in group.OrderBy(s => s.Name))
                {
                    var nodeRect = new Rect(
                        groupRect.x + 20,
                        nodeY,
                        NODE_WIDTH - 40,
                        NODE_HEIGHT
                    );
                    nodePositions[script.Name] = nodeRect;
                    nodeY += NODE_HEIGHT + 5;
                }
            }

            DrawConnectionsAndNodes(scriptMetadata);
        }

        private void DrawConnectionsAndNodes(Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            // Draw connections first (so they appear behind nodes)
            foreach (var script in scriptMetadata.Values)
            {
                foreach (var reference in script.References)
                {
                    if (nodePositions.TryGetValue(script.Name, out var sourceRect) &&
                        nodePositions.TryGetValue(reference.Key, out var targetRect))
                    {
                        DrawConnection(sourceRect, targetRect, GetComponentGroup(script));
                    }
                }
            }

            // Draw nodes on top
            foreach (var script in scriptMetadata.Values)
            {
                if (nodePositions.TryGetValue(script.Name, out var nodeRect))
                {
                    DrawNode(nodeRect, script);
                }
            }
        }

        private void DrawNode(Rect rect, ScriptMetadata script)
        {
            // Convert screen coordinates to GUI coordinates
            Vector2 mousePos = Event.current.mousePosition;
            mousePos = GUIUtility.GUIToScreenPoint(mousePos);
            mousePos = GUIUtility.ScreenToGUIPoint(mousePos);

            // Draw shadow
            EditorGUI.DrawRect(
                new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height),
                new Color(0, 0, 0, 0.3f)
            );

            // Draw node background
            Color nodeColor = script.IsMonoBehaviour ? new Color(0.2f, 0.6f, 0.2f) :
                             script.IsScriptableObject ? new Color(0.2f, 0.2f, 0.6f) :
                             new Color(0.4f, 0.4f, 0.4f);
            EditorGUI.DrawRect(rect, nodeColor);

            // Draw node border
            Handles.color = Color.white;
            Handles.DrawPolyLine(
                new Vector3(rect.x, rect.y),
                new Vector3(rect.x + rect.width, rect.y),
                new Vector3(rect.x + rect.width, rect.y + rect.height),
                new Vector3(rect.x, rect.y + rect.height),
                new Vector3(rect.x, rect.y)
            );

            // Draw label
            EditorGUI.LabelField(rect, script.Name, new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max((int)(rect.height * 0.4f), 9)
            });

            // Handle mouse interaction
            if (Event.current.type == EventType.MouseDown && rect.Contains(mousePos))
            {
                ShowScriptDetails(script);
                Event.current.Use();
            }
        }

        private void DrawConnection(Rect source, Rect target, string type)
        {
            Vector3 start = new Vector3(source.x + source.width, source.y + source.height / 2, 0);
            Vector3 end = new Vector3(target.x, target.y + target.height / 2, 0);

            Color lineColor = GetConnectionColor(type);
            Handles.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.6f); // More transparent lines

            // Calculate better curve control points
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            float controlPointDistance = distance * 0.4f;
            
            Vector3 startTangent = start + direction * controlPointDistance;
            Vector3 endTangent = end - direction * controlPointDistance;

            // Add slight vertical offset to control points to create smoother curves
            float verticalOffset = distance * 0.2f;
            startTangent += Vector3.up * verticalOffset;
            endTangent += Vector3.up * verticalOffset;

            // Draw curved line
            Handles.DrawBezier(
                start,
                end,
                startTangent,
                endTangent,
                lineColor,
                null,
                2f
            );

            // Draw arrow
            Vector3 arrowDirection = (end - endTangent).normalized;
            float arrowSize = 8f;
            Vector3 right = Quaternion.Euler(0, 0, 30) * -arrowDirection * arrowSize;
            Vector3 left = Quaternion.Euler(0, 0, -30) * -arrowDirection * arrowSize;

            Handles.DrawLine(end, end + right);
            Handles.DrawLine(end, end + left);
        }

        private Color GetConnectionColor(string type)
        {
            return type switch
            {
                "Managers" => new Color(1, 0.5f, 0),
                "Controllers" => new Color(0, 0.7f, 0.7f),
                "UI Components" => new Color(0.7f, 0, 0.7f),
                "ScriptableObjects" => new Color(0, 0.5f, 1),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        private void DrawLegend(Rect availableRect)
        {
            var legendRect = new Rect(
                availableRect.x + 10,
                availableRect.y + availableRect.height - 100,
                200,
                90
            );
            EditorGUI.DrawRect(legendRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            
            float y = legendRect.y + 5;
            EditorGUI.LabelField(new Rect(15, y, 190, 20), "Legend:", EditorStyles.boldLabel);
            y += 20;

            void DrawLegendItem(string label, Color color)
            {
                EditorGUI.DrawRect(new Rect(15, y, 15, 15), color);
                EditorGUI.LabelField(new Rect(35, y, 170, 15), label);
                y += 20;
            }

            DrawLegendItem("MonoBehaviour", new Color(0.2f, 0.6f, 0.2f));
            DrawLegendItem("ScriptableObject", new Color(0.2f, 0.2f, 0.6f));
            DrawLegendItem("Regular Class", new Color(0.4f, 0.4f, 0.4f));
        }

        private void ShowScriptDetails(ScriptMetadata script)
        {
            var message = new System.Text.StringBuilder();
            message.AppendLine($"Script: {script.Name}");
            message.AppendLine($"Type: {(script.IsMonoBehaviour ? "MonoBehaviour" : script.IsScriptableObject ? "ScriptableObject" : "Class")}");
            message.AppendLine($"Dependencies: {script.References.Count}");
            
            if (script.References.Any())
            {
                message.AppendLine("\nReferences:");
                foreach (var reference in script.References)
                {
                    message.AppendLine($"• {reference.Key} ({reference.Value} uses)");
                }
            }

            EditorUtility.DisplayDialog("Script Details", message.ToString(), "OK");
        }

        private void DrawSystemOverview(Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("System Overview", EditorStyles.boldLabel);

            var systems = scriptMetadata.Values
                .GroupBy(m => GetSystemType(m))
                .OrderBy(g => g.Key);

            foreach (var system in systems)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{system.Key} ({system.Count()} scripts)", EditorStyles.boldLabel);

                foreach (var script in system)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {script.Name}");
                    EditorGUILayout.LabelField($"Dependencies: {script.References.Count}", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLayeredArchitecture(Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Layered Architecture", EditorStyles.boldLabel);

            var layers = GetArchitecturalLayers(scriptMetadata);
            foreach (var layer in layers)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{layer.Key} Layer", EditorStyles.boldLabel);

                foreach (var script in layer.Value)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(script.Name);
                    EditorGUILayout.LabelField($"Deps: {script.References.Count}", GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentGroups(Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Component Groups", EditorStyles.boldLabel);

            var groups = scriptMetadata.Values
                .GroupBy(m => GetComponentGroup(m))
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{group.Key} ({group.Count()} components)", EditorStyles.boldLabel);

                foreach (var script in group.OrderBy(s => s.Name))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(script.Name);
                    if (script.IsMonoBehaviour)
                        EditorGUILayout.LabelField("[MB]", GUILayout.Width(40));
                    if (script.IsScriptableObject)
                        EditorGUILayout.LabelField("[SO]", GUILayout.Width(40));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
        }

        private string GetSystemType(ScriptMetadata metadata)
        {
            if (metadata.Name.EndsWith("System")) return "Core Systems";
            if (metadata.Name.EndsWith("Manager")) return "Managers";
            if (metadata.Name.EndsWith("Controller")) return "Controllers";
            if (metadata.IsScriptableObject) return "Data";
            if (metadata.Name.EndsWith("UI")) return "UI";
            return "Other";
        }

        private Dictionary<string, List<ScriptMetadata>> GetArchitecturalLayers(
            Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            var layers = new Dictionary<string, List<ScriptMetadata>>();
            
            // Presentation Layer
            layers["Presentation"] = scriptMetadata.Values
                .Where(m => m.Name.EndsWith("UI") || m.Name.EndsWith("View"))
                .ToList();

            // Business Logic Layer
            layers["Business"] = scriptMetadata.Values
                .Where(m => m.Name.EndsWith("Manager") || m.Name.EndsWith("Controller"))
                .ToList();

            // Data Layer
            layers["Data"] = scriptMetadata.Values
                .Where(m => m.IsScriptableObject || m.Name.EndsWith("Data"))
                .ToList();

            // Infrastructure Layer
            layers["Infrastructure"] = scriptMetadata.Values
                .Where(m => m.Name.EndsWith("Service") || m.Name.EndsWith("Provider"))
                .ToList();

            // Core Layer
            layers["Core"] = scriptMetadata.Values
                .Where(m => m.Name.EndsWith("System") || m.Name.EndsWith("Core"))
                .ToList();

            // Any remaining scripts go to Other
            var assignedScripts = layers.Values.SelectMany(l => l).ToList();
            layers["Other"] = scriptMetadata.Values
                .Where(m => !assignedScripts.Contains(m))
                .ToList();

            // Remove empty layers
            layers = layers.Where(l => l.Value.Any())
                .ToDictionary(l => l.Key, l => l.Value);

            return layers;
        }

        private string GetComponentGroup(ScriptMetadata metadata)
        {
            if (metadata.Name.EndsWith("Manager")) return "Managers";
            if (metadata.Name.EndsWith("Controller")) return "Controllers";
            if (metadata.IsScriptableObject) return "ScriptableObjects";
            if (metadata.Name.EndsWith("UI")) return "UI Components";
            if (metadata.IsMonoBehaviour) return "MonoBehaviours";
            if (metadata.Name.EndsWith("Service")) return "Services";
            if (metadata.Name.EndsWith("Provider")) return "Providers";
            if (metadata.Name.EndsWith("System")) return "Systems";
            return "Other";
        }

        private string GetLayerType(ScriptMetadata script, Dictionary<string, List<ScriptMetadata>> layers)
        {
            foreach (var layer in layers)
            {
                if (layer.Value.Contains(script))
                {
                    return layer.Key;
                }
            }
            return "Other";
        }
    }
} 