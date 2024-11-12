using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScriptAnalysis.Analyzers
{
    public class DependencyAnalyzer
    {
        public Dictionary<string, ScriptMetadata> ScriptMetadata { get; private set; } = new Dictionary<string, ScriptMetadata>();
        public event Action<float> OnAnalysisProgress;

        public void AnalyzeDependencies(string targetFolder)
        {
            ScriptMetadata.Clear();
            try
            {
                string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { targetFolder });
                float totalScripts = scriptGuids.Length;
                
                // First pass: Collect basic metadata
                for (int i = 0; i < scriptGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                    AnalyzeScript(path);
                    OnAnalysisProgress?.Invoke(i / (totalScripts * 2)); // First half of progress
                }

                // Second pass: Analyze references
                int currentScript = 0;
                foreach (var metadata in ScriptMetadata.Values)
                {
                    AnalyzeReferences(metadata);
                    currentScript++;
                    OnAnalysisProgress?.Invoke(0.5f + (currentScript / (float)ScriptMetadata.Count * 0.5f));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during dependency analysis: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Analysis Error", 
                    $"An error occurred during analysis:\n{ex.Message}", "OK");
            }
        }

        private void AnalyzeScript(string path)
        {
            try
            {
                string content = File.ReadAllText(path);
                string scriptName = Path.GetFileNameWithoutExtension(path);

                var metadata = new ScriptMetadata
                {
                    Name = scriptName,
                    FolderPath = path,
                    IsMonoBehaviour = content.Contains(" : MonoBehaviour"),
                    IsScriptableObject = content.Contains(" : ScriptableObject")
                };

                // Extract namespace
                var namespaceMatch = Regex.Match(content, @"namespace\s+([^\s{]+)");
                if (namespaceMatch.Success)
                {
                    metadata.Namespace = namespaceMatch.Groups[1].Value;
                }

                ScriptMetadata[scriptName] = metadata;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing script {path}: {ex.Message}");
            }
        }

        private void AnalyzeReferences(ScriptMetadata metadata)
        {
            try
            {
                string content = File.ReadAllText(metadata.FolderPath);

                foreach (var otherScript in ScriptMetadata.Keys)
                {
                    if (otherScript == metadata.Name) continue;

                    // Count references to other scripts
                    int count = Regex.Matches(content, $@"\b{otherScript}\b").Count;
                    if (count > 0)
                    {
                        metadata.References[otherScript] = count;
                    }

                    // Check for method calls
                    if (Regex.IsMatch(content, $@"\b{otherScript}\.\w+\s*\("))
                    {
                        metadata.MethodCalls.Add(otherScript);
                    }

                    // Check for field references
                    if (Regex.IsMatch(content, $@"\b{otherScript}\.\w+\b"))
                    {
                        metadata.FieldReferences.Add(otherScript);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing references for {metadata.Name}: {ex.Message}");
            }
        }
    }

    public class ScriptMetadata
    {
        public string Name;
        public string Namespace;
        public string FolderPath;
        public bool IsMonoBehaviour;
        public bool IsScriptableObject;
        public Dictionary<string, int> References = new Dictionary<string, int>();
        public HashSet<string> MethodCalls = new HashSet<string>();
        public HashSet<string> FieldReferences = new HashSet<string>();
    }
} 