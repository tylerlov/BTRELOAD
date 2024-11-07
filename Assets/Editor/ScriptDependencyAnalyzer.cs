using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Text;

public class ScriptDependencyAnalyzer : EditorWindow
{
  private string targetFolder = "Assets/_Scripts";
  private Dictionary<string, ScriptMetadata> scriptMetadata = new Dictionary<string, ScriptMetadata>();
  private Vector2 scrollPosition;
  
  // Matrix view state
  private bool showIndirectDependencies = false;
  private Vector2 matrixScroll;
  private float cellSize = 20f;
  
  // Colors for dependency visualization
  private Color directDependencyColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);  // Blue
  private Color reverseDependencyColor = new Color(0.2f, 0.8f, 0.4f, 0.8f); // Green
  private Color mutualDependencyColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);  // Red
  
  private enum ViewMode
  {
    DependencyMatrix,    // DSM view
    LayeredArchitecture, // Layer-based view
    ComponentGroups,     // Grouped by component type
    SystemOverview,      // High-level system view
    CircularDependencies,  // Dependency cycles
    GameSystems,         // Shows relationships between major game systems
    MonoBehaviours,      // Shows MonoBehaviour hierarchy and dependencies
    ScriptableObjects,   // Shows ScriptableObject data flow
    Managers,           // Shows manager/controller relationships
  }

  private ViewMode currentView = ViewMode.DependencyMatrix;

  private enum GroupingStrategy
  {
    None,
    Namespace,
    Folder,
    Inheritance,
    References
  }
  
  private GroupingStrategy groupingStrategy = GroupingStrategy.References;
  private bool showMethodCalls = true;
  private bool showFieldReferences = true;
  private float minReferenceWeight = 1;
  
  private enum ArchitecturalPattern
  {
    MVC,            // Model-View-Controller pattern
    Service,        // Service-oriented architecture
    DataAccess,     // Data access layer
    UI,             // User interface components
    GameLogic,      // Core game logic
    Utility         // Utility/Helper components
  }

  private class ArchitecturalGroup
  {
    public string Name;
    public HashSet<string> Scripts = new HashSet<string>();
    public List<string> Dependencies = new List<string>();
    public ArchitecturalPattern Pattern;
  }

  private class ScriptMetadata
  {
    public string Name;
    public string Namespace;
    public string FolderPath;
    public bool IsMonoBehaviour;
    public bool IsScriptableObject;
    public HashSet<string> BaseTypes = new HashSet<string>();
    public HashSet<string> Interfaces = new HashSet<string>();
    public Dictionary<string, int> References = new Dictionary<string, int>(); // script -> reference count
    public HashSet<string> MethodCalls = new HashSet<string>();
    public HashSet<string> FieldReferences = new HashSet<string>();
  }

  [MenuItem("Tools/Script Dependency Analyzer")]
  public static void ShowWindow()
  {
    GetWindow<ScriptDependencyAnalyzer>("Dependency Analyzer");
  }

  private void OnGUI()
  {
    EditorGUILayout.BeginHorizontal();
    
    // Controls panel
    EditorGUILayout.BeginVertical(GUILayout.Width(250));
    DrawControls();
    EditorGUILayout.EndVertical();

    // Main view with scrolling
    EditorGUILayout.BeginVertical();
    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
    
    switch (currentView)
    {
      case ViewMode.DependencyMatrix:
        DrawDependencyMatrix();
        break;
      case ViewMode.LayeredArchitecture:
        DrawLayeredView();
        break;
      case ViewMode.ComponentGroups:
        DrawComponentGroups();
        break;
      case ViewMode.SystemOverview:
        DrawSystemOverview();
        break;
      case ViewMode.CircularDependencies:
        DrawCircularDependencies();
        break;
      case ViewMode.GameSystems:
        DrawGameSystems();
        break;
      case ViewMode.MonoBehaviours:
        DrawMonoBehaviours();
        break;
      case ViewMode.ScriptableObjects:
        DrawScriptableObjects();
        break;
      case ViewMode.Managers:
        DrawManagers();
        break;
    }
    
    EditorGUILayout.EndScrollView();
    EditorGUILayout.EndVertical();

    EditorGUILayout.EndHorizontal();

    // Export button at the bottom
    if (GUILayout.Button("Export Graph"))
    {
      ExportForGraphViz();
    }
    if (GUILayout.Button("Export Analysis Report"))
    {
      ExportAnalysisReport();
    }
  }

  private void DrawControls()
  {
    currentView = (ViewMode)EditorGUILayout.EnumPopup(
      new GUIContent("View Mode", 
      "DependencyMatrix: Detailed dependency matrix\n" +
      "LayeredArchitecture: Layer-based organization\n" +
      "ComponentGroups: Grouped by component type\n" +
      "SystemOverview: High-level system dependencies\n" +
      "CircularDependencies: Show dependency cycles\n" +
      "GameSystems: Shows relationships between major game systems\n" +
      "MonoBehaviours: Shows MonoBehaviour hierarchy and dependencies\n" +
      "ScriptableObjects: Shows ScriptableObject data flow\n" +
      "Managers: Shows manager/controller relationships"),
      currentView);

    showIndirectDependencies = EditorGUILayout.Toggle(
      new GUIContent("Show Indirect Dependencies", 
      "Show dependencies through intermediate scripts"),
      showIndirectDependencies);

    cellSize = EditorGUILayout.Slider(
      new GUIContent("Cell Size", "Adjust the size of matrix cells"),
      cellSize, 10f, 40f);

    if (GUILayout.Button("Analyze Dependencies"))
    {
      AnalyzeDependencies();
    }

    // Show analysis results
    if (scriptMetadata.Count > 0)
    {
      EditorGUILayout.Space(10);
      DrawAnalysisResults();
    }
  }

  private void DrawAnalysisResults()
  {
    EditorGUILayout.LabelField("Analysis Results", EditorStyles.boldLabel);
    
    var cycles = FindCircularDependencies();
    if (cycles.Count > 0)
    {
      EditorGUILayout.HelpBox(
        $"Found {cycles.Count} circular dependencies\n" +
        "Click 'CircularDependencies' view to see details", 
        MessageType.Warning);
    }

    var highlyConnected = FindHighlyConnectedScripts();
    if (highlyConnected.Any())
    {
      EditorGUILayout.HelpBox(
        $"Found {highlyConnected.Count} highly coupled scripts\n" +
        "Consider breaking these into smaller components",
        MessageType.Info);
    }

    var architecturalGroups = DetectArchitecturalGroups();
    EditorGUILayout.HelpBox(
      $"Detected {architecturalGroups.Count} architectural groups\n" +
      "View details in 'ComponentGroups' view",
      MessageType.Info);
  }

  private void DrawSystemOverview()
  {
    var groups = DetectArchitecturalGroups();
    
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("System Architecture Overview", EditorStyles.boldLabel);
    
    foreach (var group in groups)
    {
      EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
      
      EditorGUILayout.LabelField($"{group.Name}", EditorStyles.boldLabel);
      EditorGUILayout.LabelField($"Scripts: {group.Scripts.Count}", GUILayout.Width(100));
      EditorGUILayout.LabelField($"Pattern: {group.Pattern}", GUILayout.Width(150));
      
      EditorGUILayout.EndHorizontal();
      
      if (group.Dependencies.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Dependencies:", EditorStyles.boldLabel);
        foreach (var dep in group.Dependencies)
        {
          EditorGUILayout.LabelField($"→ {dep}");
        }
        EditorGUI.indentLevel--;
      }
      
      EditorGUILayout.Space(5);
    }
    
    EditorGUILayout.EndVertical();
  }

  private void DrawDependencyMatrix()
  {
    if (scriptMetadata.Count == 0) return;

    var scripts = GetOrderedScripts();
    float totalSize = scripts.Count * cellSize;
    
    matrixScroll = EditorGUILayout.BeginScrollView(matrixScroll);
    
    var matrixRect = GUILayoutUtility.GetRect(totalSize, totalSize);
    
    // Draw grid
    Handles.color = Color.gray;
    for (int i = 0; i <= scripts.Count; i++)
    {
      float pos = i * cellSize;
      Handles.DrawLine(
        new Vector2(matrixRect.x, matrixRect.y + pos),
        new Vector2(matrixRect.x + totalSize, matrixRect.y + pos)
      );
      Handles.DrawLine(
        new Vector2(matrixRect.x + pos, matrixRect.y),
        new Vector2(matrixRect.x + pos, matrixRect.y + totalSize)
      );
    }

    // Draw dependencies
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

          // Show tooltip on hover
          if (cellRect.Contains(Event.current.mousePosition))
          {
            var tooltip = GetDependencyTooltip(source, target);
            GUI.Label(new Rect(Event.current.mousePosition, new Vector2(200, 40)), tooltip);
          }
        }
      }
    }

    EditorGUILayout.EndScrollView();
  }

  private Color GetDependencyColor(ScriptMetadata source, ScriptMetadata target)
  {
    bool forward = HasDirectDependency(source, target);
    bool reverse = HasDirectDependency(target, source);

    if (forward && reverse) return mutualDependencyColor;
    if (forward) return directDependencyColor;
    if (reverse) return reverseDependencyColor;
    return Color.clear;
  }

  private List<ScriptMetadata> GetOrderedScripts()
  {
    switch (currentView)
    {
      case ViewMode.LayeredArchitecture:
        return OrderByLayers();
      case ViewMode.ComponentGroups:
        return OrderByComponents();
      default:
        return scriptMetadata.Values.ToList();
    }
  }

  private void AnalyzeDependencies()
  {
    scriptMetadata.Clear();
    string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { targetFolder });
    
    // First pass: Collect basic metadata
    foreach (string guid in scriptGuids)
    {
      string path = AssetDatabase.GUIDToAssetPath(guid);
      AnalyzeScript(path);
    }
    
    // Second pass: Analyze references between scripts
    foreach (var metadata in scriptMetadata.Values)
    {
      AnalyzeReferences(metadata);
    }
  }
  
  private void AnalyzeScript(string path)
  {
    string content = File.ReadAllText(path);
    string scriptName = Path.GetFileNameWithoutExtension(path);
    
    var metadata = new ScriptMetadata
    {
      Name = scriptName,
      FolderPath = Path.GetDirectoryName(path),
      IsMonoBehaviour = content.Contains(": MonoBehaviour"),
      IsScriptableObject = content.Contains(": ScriptableObject")
    };

    // Analyze namespace
    var nsMatch = Regex.Match(content, @"namespace\s+([^\s{]+)");
    metadata.Namespace = nsMatch.Success ? nsMatch.Groups[1].Value : "";

    // Analyze inheritance
    var typeMatches = Regex.Matches(content, @"class\s+\w+\s*:\s*([^{]+)");
    foreach (Match match in typeMatches)
    {
      string[] types = match.Groups[1].Value.Split(',')
        .Select(t => t.Trim())
        .Where(t => t != "MonoBehaviour" && t != "ScriptableObject")
        .ToArray();
      
      foreach (string type in types)
      {
        if (type.StartsWith("I"))
          metadata.Interfaces.Add(type);
        else
          metadata.BaseTypes.Add(type);
      }
    }

    scriptMetadata[scriptName] = metadata;
  }
  
  private void AnalyzeReferences(ScriptMetadata metadata)
  {
    string path = AssetDatabase.FindAssets($"t:Script {metadata.Name}")[0];
    path = AssetDatabase.GUIDToAssetPath(path);
    string content = File.ReadAllText(path);

    foreach (var otherScript in scriptMetadata.Keys)
    {
      if (otherScript == metadata.Name) continue;

      int referenceCount = 0;

      // Check direct type usage
      referenceCount += Regex.Matches(content, $@"\b{otherScript}\b").Count;

      // Check method calls
      var methodCalls = Regex.Matches(content, $@"\b{otherScript}\.\w+\s*\(");
      referenceCount += methodCalls.Count;
      if (methodCalls.Count > 0)
      {
        metadata.MethodCalls.Add(otherScript);
      }

      // Check field references
      var fieldRefs = Regex.Matches(content, $@"\b{otherScript}\.\w+\b");
      referenceCount += fieldRefs.Count;
      if (fieldRefs.Count > 0)
      {
        metadata.FieldReferences.Add(otherScript);
      }

      if (referenceCount > 0)
      {
        metadata.References[otherScript] = referenceCount;
      }
    }
  }
  
  private void ExportDotFile()
  {
    if (scriptMetadata.Count == 0)
    {
      AnalyzeDependencies();
    }

    StringBuilder dot = new StringBuilder();
    dot.AppendLine("digraph {");
    dot.AppendLine("  rankdir=LR;");
    dot.AppendLine("  compound=true;");
    dot.AppendLine("  newrank=true;");
    
    switch (currentView)
    {
      case ViewMode.SystemOverview:
        ExportSystemOverview(dot);
        break;
      case ViewMode.LayeredArchitecture:
        ExportHierarchicalView(dot);
        break;
      case ViewMode.CircularDependencies:
        ExportCircularDependencies(dot);
        break;
      case ViewMode.DependencyMatrix:
        ExportDetailedView(dot);
        break;
    }
    
    dot.AppendLine("}");
    
    string path = EditorUtility.SaveFilePanel("Save DOT File", "", "dependencies.dot", "dot");
    if (!string.IsNullOrEmpty(path))
    {
      File.WriteAllText(path, dot.ToString());
    }
  }
  
  private void ExportSystemOverview(StringBuilder dot)
  {
    // Find core scripts (most referenced)
    var coreScripts = scriptMetadata.Values
      .OrderByDescending(m => scriptMetadata.Values.Count(other => 
        other.References.ContainsKey(m.Name)))
      .Take(5)
      .Select(m => m.Name)
      .ToHashSet();

    dot.AppendLine("  subgraph cluster_core {");
    dot.AppendLine("    label=\"Core Systems\";");
    dot.AppendLine("    style=filled;");
    dot.AppendLine("    color=lightblue;");
    
    foreach (var script in coreScripts)
    {
      string nodeId = GenerateNodeId(script);
      dot.AppendLine($"    {nodeId} [label=\"{script}\"];");
    }
    dot.AppendLine("  }");

    // Group other scripts by their primary core dependency
    var peripheralGroups = scriptMetadata.Values
      .Where(m => !coreScripts.Contains(m.Name))
      .GroupBy(m => GetPrimaryCoreDependency(m, coreScripts))
      .Where(g => g.Key != null);

    foreach (var group in peripheralGroups)
    {
      dot.AppendLine($"  subgraph cluster_{GenerateNodeId(group.Key)} {{");
      dot.AppendLine($"    label=\"{group.Key} Subsystem\";");
      dot.AppendLine("    style=filled;");
      dot.AppendLine("    color=lightgrey;");
      
      foreach (var script in group)
      {
        string nodeId = GenerateNodeId(script.Name);
        dot.AppendLine($"    {nodeId} [label=\"{script.Name}\"];");
      }
      dot.AppendLine("  }");
    }

    // Add edges with reduced complexity
    AddFilteredEdges(dot, minReferenceWeight * 2);
  }

  private void ExportHierarchicalView(StringBuilder dot)
  {
    // Find root nodes (scripts that others depend on but have few dependencies themselves)
    var rootScripts = scriptMetadata.Values
      .Where(m => m.References.Count < 3 && 
                  scriptMetadata.Values.Any(other => other.References.ContainsKey(m.Name)))
      .Select(m => m.Name)
      .ToList();

    dot.AppendLine("  { rank=same; ");
    foreach (var root in rootScripts)
    {
      string nodeId = GenerateNodeId(root);
      dot.AppendLine($"    {nodeId} [label=\"{root}\", fillcolor=lightblue];");
    }
    dot.AppendLine("  }");

    // Build dependency tree levels
    var processed = new HashSet<string>(rootScripts);
    var currentLevel = rootScripts;
    int level = 0;

    while (currentLevel.Any())
    {
      var nextLevel = new List<string>();
      
      foreach (var script in currentLevel)
      {
        var dependents = scriptMetadata.Values
          .Where(m => m.References.ContainsKey(script) && !processed.Contains(m.Name))
          .Select(m => m.Name);
          
        nextLevel.AddRange(dependents);
        processed.UnionWith(dependents);
      }

      if (nextLevel.Any())
      {
        dot.AppendLine($"  {{ rank=same; level{level} ");
        foreach (var script in nextLevel)
        {
          string nodeId = GenerateNodeId(script);
          dot.AppendLine($"    {nodeId} [label=\"{script}\"];");
        }
        dot.AppendLine("  }");
      }

      currentLevel = nextLevel;
      level++;
    }

    AddFilteredEdges(dot, minReferenceWeight);
  }

  private void ExportCircularDependencies(StringBuilder dot)
  {
    var cycles = FindCircularDependencies();
    
    // Add all nodes first
    foreach (var metadata in scriptMetadata.Values)
    {
      string nodeId = GenerateNodeId(metadata.Name);
      string color = "white";
      
      // Highlight scripts involved in cycles
      if (cycles.Any(cycle => cycle.Contains(metadata.Name)))
      {
        color = "lightpink";
      }
      
      dot.AppendLine($"  {nodeId} [label=\"{EscapeString(metadata.Name)}\", style=filled, fillcolor={color}];");
    }

    // Add normal dependencies
    foreach (var metadata in scriptMetadata.Values)
    {
      string sourceId = GenerateNodeId(metadata.Name);
      foreach (var reference in metadata.References)
      {
        if (reference.Value >= minReferenceWeight)
        {
          string targetId = GenerateNodeId(reference.Key);
          
          // Check if this edge is part of a cycle
          bool isPartOfCycle = cycles.Any(cycle => 
            cycle.Contains(metadata.Name) && 
            cycle.Contains(reference.Key) &&
            ((cycle.IndexOf(metadata.Name) + 1) % cycle.Count == cycle.IndexOf(reference.Key) ||
             (cycle.IndexOf(reference.Key) + 1) % cycle.Count == cycle.IndexOf(metadata.Name)));

          if (isPartOfCycle)
          {
            dot.AppendLine($"  {sourceId} -> {targetId} [color=red, penwidth=2.0];");
          }
          else
          {
            dot.AppendLine($"  {sourceId} -> {targetId} [color=gray];");
          }
        }
      }
    }
  }

  private List<List<string>> FindCircularDependencies()
  {
    var cycles = new List<List<string>>();
    var visited = new HashSet<string>();
    
    foreach (var script in scriptMetadata.Keys)
    {
      if (!visited.Contains(script))
      {
        var path = new List<string>();
        var pathSet = new HashSet<string>();
        FindCyclesFromNode(script, path, pathSet, visited, cycles);
      }
    }
    
    return cycles;
  }

  private void FindCyclesFromNode(
    string current,
    List<string> path,
    HashSet<string> pathSet,
    HashSet<string> visited,
    List<List<string>> cycles)
  {
    path.Add(current);
    pathSet.Add(current);
    visited.Add(current);

    if (scriptMetadata.TryGetValue(current, out var metadata))
    {
      foreach (var reference in metadata.References.Keys)
      {
        if (pathSet.Contains(reference))
        {
          // Found a cycle
          int cycleStart = path.IndexOf(reference);
          var cycle = path.Skip(cycleStart).ToList();
          
          // Only add if it's a new cycle
          if (!cycles.Any(existingCycle => 
            existingCycle.Count == cycle.Count && 
            existingCycle.All(cycle.Contains)))
          {
            cycles.Add(cycle);
          }
        }
        else if (!visited.Contains(reference))
        {
          FindCyclesFromNode(reference, path, pathSet, visited, cycles);
        }
      }
    }

    path.RemoveAt(path.Count - 1);
    pathSet.Remove(current);
  }

  private void AddFilteredEdges(StringBuilder dot, float minWeight)
  {
    foreach (var metadata in scriptMetadata.Values)
    {
      string sourceId = GenerateNodeId(metadata.Name);
      foreach (var reference in metadata.References)
      {
        if (reference.Value >= minWeight)
        {
          string targetId = GenerateNodeId(reference.Key);
          float penWidth = Mathf.Min(1 + (reference.Value / 2f), 5);
          string tooltip = $"Weight: {reference.Value}\\n" +
                          (metadata.MethodCalls.Contains(reference.Key) ? "Calls Methods\\n" : "") +
                          (metadata.FieldReferences.Contains(reference.Key) ? "Uses Fields" : "");
          
          dot.AppendLine($"  {sourceId} -> {targetId} [penwidth={penWidth:F1}, tooltip=\"{tooltip}\"];");
        }
      }
    }
  }

  private string GetPrimaryCoreDependency(ScriptMetadata metadata, HashSet<string> coreScripts)
  {
    return metadata.References
      .Where(r => coreScripts.Contains(r.Key))
      .OrderByDescending(r => r.Value)
      .Select(r => r.Key)
      .FirstOrDefault();
  }

  private void ExportDetailedView(StringBuilder dot)
  {
    // Add all scripts as nodes
    foreach (var metadata in scriptMetadata.Values)
    {
      string color = metadata.IsMonoBehaviour ? "lightblue" : 
                    metadata.IsScriptableObject ? "lightgreen" : "white";
      string nodeId = GenerateNodeId(metadata.Name);
      dot.AppendLine($"  {nodeId} [label=\"{EscapeString(metadata.Name)}\", fillcolor={color}];");
    }

    // Add edges with weights
    foreach (var metadata in scriptMetadata.Values)
    {
      string sourceId = GenerateNodeId(metadata.Name);
      
      foreach (var reference in metadata.References)
      {
        if (reference.Value >= minReferenceWeight)
        {
          string targetId = GenerateNodeId(reference.Key);
          float penWidth = Mathf.Min(1 + (reference.Value / 2f), 5);
          string tooltip = $"Weight: {reference.Value}\\n" +
                          (metadata.MethodCalls.Contains(reference.Key) ? "Calls Methods\\n" : "") +
                          (metadata.FieldReferences.Contains(reference.Key) ? "Uses Fields" : "");
          
          dot.AppendLine($"  {sourceId} -> {targetId} [" +
            $"penwidth={penWidth:F1}, " +
            $"tooltip=\"{tooltip}\"];");
        }
      }
    }
  }

  private string GenerateNodeId(string name)
  {
    string id = name
      .Replace(".", "_")
      .Replace(" ", "_")
      .Replace("-", "_")
      .Replace("+", "_plus_");
    
    if (char.IsDigit(id[0]))
    {
      id = "n" + id;
    }
    
    return id;
  }
  
  private string EscapeString(string str)
  {
    return str
      .Replace("\"", "\\\"")
      .Replace("\n", "\\n")
      .Replace("\r", "\\r");
  }

  private bool HasDirectDependency(ScriptMetadata source, ScriptMetadata target)
  {
    return source.References.ContainsKey(target.Name);
  }

  private bool HasDependency(ScriptMetadata source, ScriptMetadata target, bool includeIndirect)
  {
    if (HasDirectDependency(source, target)) return true;
    if (!includeIndirect) return false;

    // Check for indirect dependencies
    var visited = new HashSet<string>();
    return HasIndirectDependency(source, target.Name, visited);
  }

  private bool HasIndirectDependency(ScriptMetadata source, string targetName, HashSet<string> visited)
  {
    if (!visited.Add(source.Name)) return false;

    foreach (var reference in source.References.Keys)
    {
      if (reference == targetName) return true;
      if (scriptMetadata.TryGetValue(reference, out var refMetadata))
      {
        if (HasIndirectDependency(refMetadata, targetName, visited)) return true;
      }
    }

    return false;
  }

  private string GetDependencyTooltip(ScriptMetadata source, ScriptMetadata target)
  {
    var details = new List<string>();
    
    if (source.References.TryGetValue(target.Name, out int weight))
    {
      details.Add($"Weight: {weight}");
      if (source.MethodCalls.Contains(target.Name))
        details.Add("Calls Methods");
      if (source.FieldReferences.Contains(target.Name))
        details.Add("Uses Fields");
    }
    
    if (target.References.ContainsKey(source.Name))
    {
      details.Add("⚠️ Mutual Dependency");
    }
    
    return string.Join("\n", details);
  }

  private List<ScriptMetadata> OrderByLayers()
  {
    var result = new List<ScriptMetadata>();
    var processed = new HashSet<string>();
    
    // Find base layer (scripts with fewest dependencies)
    var baseLayer = scriptMetadata.Values
      .Where(m => m.References.Count < 3)
      .OrderBy(m => m.References.Count);
    
    result.AddRange(baseLayer);
    processed.UnionWith(baseLayer.Select(m => m.Name));

    // Add remaining scripts by dependency count
    while (processed.Count < scriptMetadata.Count)
    {
      var nextLayer = scriptMetadata.Values
        .Where(m => !processed.Contains(m.Name))
        .OrderBy(m => m.References.Count(r => !processed.Contains(r.Key)));
        
      result.AddRange(nextLayer);
      processed.UnionWith(nextLayer.Select(m => m.Name));
    }

    return result;
  }

  private List<ScriptMetadata> OrderByComponents()
  {
    var result = new List<ScriptMetadata>();
    
    // Group by common dependencies
    var groups = scriptMetadata.Values
      .GroupBy(m => GetComponentGroup(m))
      .OrderBy(g => g.Key);
      
    foreach (var group in groups)
    {
      result.AddRange(group.OrderBy(m => m.References.Count));
    }
    
    return result;
  }

  private string GetComponentGroup(ScriptMetadata metadata)
  {
    // Try to identify component groups by naming or dependencies
    if (metadata.Name.Contains("Controller")) return "Controllers";
    if (metadata.Name.Contains("Manager")) return "Managers";
    if (metadata.IsScriptableObject) return "ScriptableObjects";
    if (metadata.References.Count == 0) return "Independent";
    return "Other";
  }

  private List<ScriptMetadata> FindHighlyConnectedScripts()
  {
    int averageConnections = (int)scriptMetadata.Values.Average(m => m.References.Count);
    return scriptMetadata.Values
      .Where(m => m.References.Count > averageConnections * 2)
      .ToList();
  }

  private string GetArchitecturalGroup(ScriptMetadata metadata)
  {
    // Pattern detection based on naming, inheritance, and dependencies
    if (metadata.Name.EndsWith("Controller") || metadata.Name.EndsWith("Manager"))
      return "Controllers";
    
    if (metadata.Name.EndsWith("View") || metadata.Name.EndsWith("Panel") || 
        metadata.Name.EndsWith("UI") || metadata.Name.EndsWith("HUD"))
      return "UI";
    
    if (metadata.Name.EndsWith("Service") || metadata.Name.EndsWith("Provider"))
      return "Services";
    
    if (metadata.Name.EndsWith("Repository") || metadata.Name.EndsWith("Data") || 
        metadata.IsScriptableObject)
      return "Data";
    
    if (metadata.Name.EndsWith("System") || metadata.Name.EndsWith("Manager"))
      return "Core";
    
    if (metadata.References.Count == 0 && metadata.Name.EndsWith("Utils"))
      return "Utilities";
    
    return "Domain";
  }

  private List<ArchitecturalGroup> DetectArchitecturalGroups()
  {
    var groups = new Dictionary<string, ArchitecturalGroup>();
    
    // First pass: Group scripts
    foreach (var metadata in scriptMetadata.Values)
    {
      string groupName = GetArchitecturalGroup(metadata);
      
      if (!groups.ContainsKey(groupName))
      {
        groups[groupName] = new ArchitecturalGroup { Name = groupName };
      }
      
      groups[groupName].Scripts.Add(metadata.Name);
    }
    
    // Second pass: Analyze dependencies between groups
    foreach (var group in groups.Values)
    {
      foreach (var script in group.Scripts)
      {
        if (scriptMetadata.TryGetValue(script, out var metadata))
        {
          foreach (var dep in metadata.References.Keys)
          {
            string depGroup = GetArchitecturalGroup(scriptMetadata[dep]);
            if (depGroup != group.Name && !group.Dependencies.Contains(depGroup))
            {
              group.Dependencies.Add(depGroup);
            }
          }
        }
      }
      
      // Detect architectural pattern
      group.Pattern = DetectPattern(group, groups);
    }
    
    return groups.Values.ToList();
  }

  private ArchitecturalPattern DetectPattern(ArchitecturalGroup group, 
    Dictionary<string, ArchitecturalGroup> allGroups)
  {
    switch (group.Name)
    {
      case "Controllers" when group.Dependencies.Contains("UI") && 
                            group.Dependencies.Contains("Services"):
        return ArchitecturalPattern.MVC;
        
      case "Services" when !group.Dependencies.Contains("UI") && 
                          group.Dependencies.Contains("Data"):
        return ArchitecturalPattern.Service;
        
      case "Data" when group.Dependencies.Count == 0:
        return ArchitecturalPattern.DataAccess;
        
      case "UI" when group.Dependencies.Contains("Controllers"):
        return ArchitecturalPattern.UI;
        
      case "Domain" when group.Dependencies.Contains("Services"):
        return ArchitecturalPattern.GameLogic;
        
      default:
        return ArchitecturalPattern.Utility;
    }
  }

  private void DrawArchitecturalView()
  {
    var groups = DetectArchitecturalGroups();
    
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("Architectural Analysis", EditorStyles.boldLabel);
    
    foreach (var group in groups)
    {
      EditorGUILayout.BeginHorizontal();
      
      string pattern = group.Pattern.ToString();
      EditorGUILayout.LabelField($"{group.Name} ({pattern})", EditorStyles.boldLabel, 
        GUILayout.Width(200));
      
      EditorGUILayout.BeginVertical();
      foreach (var script in group.Scripts)
      {
        EditorGUILayout.LabelField($"• {script}");
      }
      EditorGUILayout.EndVertical();
      
      EditorGUILayout.EndHorizontal();
      
      if (group.Dependencies.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField($"Dependencies: {string.Join(", ", group.Dependencies)}");
        EditorGUI.indentLevel--;
      }
      
      EditorGUILayout.Space(5);
    }
    
    EditorGUILayout.EndVertical();
  }

  private void DrawLayeredView()
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("Layered Architecture View", EditorStyles.boldLabel);
    
    var orderedScripts = OrderByLayers();
    foreach (var script in orderedScripts)
    {
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField(script.Name);
      EditorGUILayout.LabelField($"Dependencies: {script.References.Count}", GUILayout.Width(120));
      EditorGUILayout.EndHorizontal();
    }
    
    EditorGUILayout.EndVertical();
  }

  private void DrawComponentGroups()
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("Component Groups View", EditorStyles.boldLabel);
    
    var groupedScripts = scriptMetadata.Values
      .GroupBy(m => GetComponentGroup(m))
      .OrderBy(g => g.Key);
      
    foreach (var group in groupedScripts)
    {
      EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
      EditorGUI.indentLevel++;
      
      foreach (var script in group.OrderBy(m => m.Name))
      {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(script.Name);
        EditorGUILayout.LabelField($"Refs: {script.References.Count}", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();
      }
      
      EditorGUI.indentLevel--;
      EditorGUILayout.Space(5);
    }
    
    EditorGUILayout.EndVertical();
  }

  private void DrawCircularDependencies()
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("Circular Dependencies", EditorStyles.boldLabel);
    
    var cycles = FindCircularDependencies();
    if (cycles.Count == 0)
    {
      EditorGUILayout.HelpBox("No circular dependencies found.", MessageType.Info);
      EditorGUILayout.EndVertical();
      return;
    }
    
    foreach (var cycle in cycles)
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField($"Cycle of {cycle.Count} scripts:", EditorStyles.boldLabel);
      
      for (int i = 0; i < cycle.Count; i++)
      {
        string current = cycle[i];
        string next = cycle[(i + 1) % cycle.Count];
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{current} → {next}");
        
        if (scriptMetadata.TryGetValue(current, out var metadata) && 
            metadata.References.TryGetValue(next, out int weight))
        {
          EditorGUILayout.LabelField($"Weight: {weight}", GUILayout.Width(80));
        }
        
        EditorGUILayout.EndHorizontal();
      }
      
      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(5);
    }
    
    EditorGUILayout.EndVertical();
  }

  private void ExportForGraphViz()
  {
    if (scriptMetadata.Count == 0)
    {
      AnalyzeDependencies();
    }

    StringBuilder dot = new StringBuilder();
    dot.AppendLine("digraph GameDependencies {");
    dot.AppendLine("  rankdir=LR;");
    dot.AppendLine("  node [shape=box];");
    
    // Group by game systems
    var systems = scriptMetadata.Values
      .GroupBy(m => GetGameSystem(m))
      .OrderBy(g => g.Key);

    foreach (var system in systems)
    {
      dot.AppendLine($"  subgraph cluster_{system.Key.ToLower()} {{");
      dot.AppendLine($"    label=\"{system.Key}\";");
      dot.AppendLine("    style=filled;");
      dot.AppendLine("    color=lightgrey;");

      foreach (var script in system)
      {
        string color = script.IsMonoBehaviour ? "lightblue" : 
                      script.IsScriptableObject ? "lightgreen" : "white";
        string shape = script.Name.Contains("Manager") ? "doubleoctagon" : 
                      script.Name.Contains("Controller") ? "octagon" : "box";
                        
        dot.AppendLine($"    {GenerateNodeId(script.Name)} [");
        dot.AppendLine($"      label=\"{script.Name}\"");
        dot.AppendLine($"      shape={shape}");
        dot.AppendLine($"      style=filled");
        dot.AppendLine($"      fillcolor={color}");
        dot.AppendLine("    ];");
      }
      dot.AppendLine("  }");
    }

    // Add dependencies with meaningful attributes
    foreach (var metadata in scriptMetadata.Values)
    {
      foreach (var reference in metadata.References)
      {
        string style = "";
        if (metadata.MethodCalls.Contains(reference.Key))
          style = "solid";
        else if (metadata.FieldReferences.Contains(reference.Key))
          style = "dashed";
          
        float weight = reference.Value;
        float penWidth = Mathf.Min(1 + (weight / 3), 5);

        dot.AppendLine($"  {GenerateNodeId(metadata.Name)} -> {GenerateNodeId(reference.Key)} [");
        dot.AppendLine($"    style={style}");
        dot.AppendLine($"    penwidth={penWidth:F1}");
        dot.AppendLine($"    weight={weight}");
        dot.AppendLine("  ];");
      }
    }

    dot.AppendLine("}");

    string path = EditorUtility.SaveFilePanel(
      "Export Dependency Graph", 
      "", 
      $"game_dependencies_{currentView.ToString().ToLower()}.dot",
      "dot"
    );

    if (!string.IsNullOrEmpty(path))
    {
      File.WriteAllText(path, dot.ToString());
      Debug.Log($"Exported dependency graph to: {path}");
      
      // Show instructions
      EditorUtility.DisplayDialog(
        "Graph Exported", 
        "To visualize:\n" +
        "1. Install Graphviz (https://graphviz.org/)\n" +
        "2. Run: dot -Tpng game_dependencies.dot -o dependencies.png\n" +
        "Or use an online visualizer like GraphvizOnline",
        "OK"
      );
    }
  }

  private string GetGameSystem(ScriptMetadata metadata)
  {
    // Detect game system based on naming and inheritance
    if (metadata.Name.Contains("UI") || metadata.FolderPath.Contains("UI"))
      return "UI";
    if (metadata.Name.Contains("Input") || metadata.FolderPath.Contains("Input"))
      return "Input";
    if (metadata.Name.Contains("Audio") || metadata.FolderPath.Contains("Audio"))
      return "Audio";
    if (metadata.Name.Contains("Network") || metadata.FolderPath.Contains("Network"))
      return "Networking";
    if (metadata.IsScriptableObject || metadata.FolderPath.Contains("Data"))
      return "Data";
    if (metadata.Name.Contains("Manager") || metadata.Name.Contains("Controller"))
      return "Management";
    if (metadata.Name.Contains("System"))
      return "Core Systems";
    
    return "Gameplay";
  }

  private void DrawGameSystems()
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("Game Systems Overview", EditorStyles.boldLabel);

    var systemGroups = scriptMetadata.Values
      .GroupBy(m => GetGameSystem(m))
      .OrderBy(g => g.Key);

    foreach (var system in systemGroups)
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.SelectableLabel($"{system.Key} ({system.Count()} scripts)", EditorStyles.boldLabel);

      var systemDependencies = system
        .SelectMany(s => s.References.Keys)
        .Where(dep => GetGameSystem(scriptMetadata[dep]) != system.Key)
        .GroupBy(dep => GetGameSystem(scriptMetadata[dep]))
        .OrderBy(g => g.Key);

      if (systemDependencies.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Dependencies:", EditorStyles.boldLabel);
        foreach (var depSystem in systemDependencies)
        {
          EditorGUILayout.SelectableLabel($"→ {depSystem.Key} ({depSystem.Count()} connections)");
        }
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(5);
    }

    EditorGUILayout.EndVertical();
  }

  private void DrawMonoBehaviours()
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("MonoBehaviour Dependencies", EditorStyles.boldLabel);

    var monoBehaviours = scriptMetadata.Values
      .Where(m => m.IsMonoBehaviour)
      .OrderBy(m => m.Name);

    foreach (var mb in monoBehaviours)
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      
      // Header with basic info
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.SelectableLabel(mb.Name, EditorStyles.boldLabel);
      EditorGUILayout.SelectableLabel($"Path: {mb.FolderPath}", EditorStyles.miniLabel);
      EditorGUILayout.EndHorizontal();

      // Show inheritance
      if (mb.BaseTypes.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.SelectableLabel($"Inherits: {string.Join(", ", mb.BaseTypes)}");
        EditorGUI.indentLevel--;
      }

      // Show dependencies
      if (mb.References.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Dependencies:", EditorStyles.boldLabel);
        foreach (var dep in mb.References.OrderByDescending(r => r.Value))
        {
          string depType = scriptMetadata[dep.Key].IsMonoBehaviour ? "[MB]" :
                          scriptMetadata[dep.Key].IsScriptableObject ? "[SO]" : "";
          EditorGUILayout.SelectableLabel($"→ {dep.Key} {depType} (refs: {dep.Value})");
        }
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(2);
    }

    EditorGUILayout.EndVertical();
  }

  private void DrawScriptableObjects()
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("ScriptableObject Data Flow", EditorStyles.boldLabel);

    var scriptableObjects = scriptMetadata.Values
      .Where(m => m.IsScriptableObject)
      .OrderBy(m => m.Name);

    foreach (var so in scriptableObjects)
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      
      // Header
      EditorGUILayout.LabelField(so.Name, EditorStyles.boldLabel);

      // Show who references this SO
      var referencedBy = scriptMetadata.Values
        .Where(m => m.References.ContainsKey(so.Name))
        .OrderBy(m => m.Name);

      if (referencedBy.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Used by:", EditorStyles.boldLabel);
        foreach (var user in referencedBy)
        {
          string userType = user.IsMonoBehaviour ? "[MB]" :
                           user.IsScriptableObject ? "[SO]" : "";
          EditorGUILayout.SelectableLabel($"← {user.Name} {userType}");
        }
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(2);
    }

    EditorGUILayout.EndVertical();
  }

  private void DrawManagers()
  {
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField("Managers & Controllers", EditorStyles.boldLabel);

    var managers = scriptMetadata.Values
      .Where(m => m.Name.Contains("Manager") || m.Name.Contains("Controller"))
      .OrderBy(m => m.Name);

    foreach (var manager in managers)
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      
      // Header with type
      string type = manager.Name.Contains("Manager") ? "Manager" : "Controller";
      EditorGUILayout.LabelField($"{manager.Name} ({type})", EditorStyles.boldLabel);

      // Show what this manager controls/manages
      if (manager.References.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Manages:", EditorStyles.boldLabel);
        foreach (var dep in manager.References.OrderByDescending(r => r.Value))
        {
          string depSystem = GetGameSystem(scriptMetadata[dep.Key]);
          EditorGUILayout.SelectableLabel($"→ {dep.Key} [{depSystem}] (refs: {dep.Value})");
        }
        EditorGUI.indentLevel--;
      }

      // Show what depends on this manager
      var dependents = scriptMetadata.Values
        .Where(m => m.References.ContainsKey(manager.Name))
        .OrderBy(m => m.Name);

      if (dependents.Any())
      {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Used by:", EditorStyles.boldLabel);
        foreach (var dependent in dependents)
        {
          string depSystem = GetGameSystem(dependent);
          EditorGUILayout.SelectableLabel($"← {dependent.Name} [{depSystem}]");
        }
        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(2);
    }

    EditorGUILayout.EndVertical();
  }

  private void ExportAnalysisReport()
  {
    if (scriptMetadata.Count == 0)
    {
      AnalyzeDependencies();
    }

    StringBuilder report = new StringBuilder();
    report.AppendLine("# Script Dependency Analysis Report");
    report.AppendLine();

    // Project Overview
    report.AppendLine("## Project Overview");
    report.AppendLine($"- Total Scripts: {scriptMetadata.Count}");
    report.AppendLine($"- MonoBehaviours: {scriptMetadata.Values.Count(m => m.IsMonoBehaviour)}");
    report.AppendLine($"- ScriptableObjects: {scriptMetadata.Values.Count(m => m.IsScriptableObject)}");
    report.AppendLine();

    // Circular Dependencies
    var cycles = FindCircularDependencies();
    report.AppendLine("## Circular Dependencies");
    if (cycles.Any())
    {
      report.AppendLine("⚠️ Warning: Circular dependencies detected!");
      foreach (var cycle in cycles)
      {
        report.AppendLine($"- Cycle: {string.Join(" → ", cycle)} → {cycle[0]}");
      }
    }
    else
    {
      report.AppendLine("✅ No circular dependencies found.");
    }
    report.AppendLine();

    // Highly Connected Scripts
    var highlyConnected = FindHighlyConnectedScripts();
    report.AppendLine("## Highly Connected Scripts");
    foreach (var script in highlyConnected)
    {
      report.AppendLine($"- {script.Name}");
      report.AppendLine($"  - Dependencies: {script.References.Count}");
      report.AppendLine($"  - Referenced by: {scriptMetadata.Values.Count(m => m.References.ContainsKey(script.Name))}");
    }
    report.AppendLine();

    // System Analysis
    report.AppendLine("## System Analysis");
    var systems = scriptMetadata.Values
      .GroupBy(m => GetGameSystem(m))
      .OrderBy(g => g.Key);

    foreach (var system in systems)
    {
      report.AppendLine($"### {system.Key}");
      report.AppendLine($"Scripts: {system.Count()}");
      
      // System Dependencies
      var systemDeps = system
        .SelectMany(s => s.References.Keys)
        .Where(dep => GetGameSystem(scriptMetadata[dep]) != system.Key)
        .GroupBy(dep => GetGameSystem(scriptMetadata[dep]))
        .OrderBy(g => g.Key);

      if (systemDeps.Any())
      {
        report.AppendLine("Dependencies:");
        foreach (var dep in systemDeps)
        {
          report.AppendLine($"- {dep.Key}: {dep.Count()} connections");
        }
      }
      
      // List scripts in system
      report.AppendLine("Scripts:");
      foreach (var script in system.OrderBy(s => s.Name))
      {
        report.AppendLine($"- {script.Name}");
        if (script.References.Any())
        {
          report.AppendLine("  Dependencies:");
          foreach (var dep in script.References.OrderByDescending(r => r.Value))
          {
            report.AppendLine($"  - {dep.Key} (refs: {dep.Value})");
          }
        }
      }
      report.AppendLine();
    }

    // Architectural Analysis
    report.AppendLine("## Architectural Analysis");
    var groups = DetectArchitecturalGroups();
    foreach (var group in groups)
    {
      report.AppendLine($"### {group.Name} ({group.Pattern})");
      report.AppendLine($"Scripts: {group.Scripts.Count}");
      if (group.Dependencies.Any())
      {
        report.AppendLine("Dependencies:");
        foreach (var dep in group.Dependencies)
        {
          report.AppendLine($"- {dep}");
        }
      }
      report.AppendLine();
    }

    // Save the report
    string path = EditorUtility.SaveFilePanel(
      "Export Analysis Report",
      "",
      "script_analysis.md",
      "md"
    );

    if (!string.IsNullOrEmpty(path))
    {
      File.WriteAllText(path, report.ToString());
      Debug.Log($"Analysis report exported to: {path}");
      
      EditorUtility.DisplayDialog(
        "Analysis Exported",
        "The analysis report has been exported in Markdown format.\n" +
        "You can now share this file with Cursor IDE or other tools for further analysis.",
        "OK"
      );
    }
  }
}

