using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ScriptAnalysis.Core;

namespace ScriptAnalysis.Analyzers
{
    public class CodeMetricsAnalyzer
    {
        private DependencyAnalyzer dependencyAnalyzer;
        private AnalysisSettings settings;
        public Dictionary<string, CodeMetrics> CodeMetrics { get; private set; } = new Dictionary<string, CodeMetrics>();
        public List<CodeInsight> CodeInsights { get; private set; } = new List<CodeInsight>();

        public CodeMetricsAnalyzer(DependencyAnalyzer analyzer)
        {
            this.dependencyAnalyzer = analyzer;
            this.settings = new AnalysisSettings();
        }

        public void UpdateSettings(AnalysisSettings newSettings)
        {
            settings = newSettings;
        }

        public void AnalyzeMetrics(Dictionary<string, ScriptMetadata> scriptMetadata)
        {
            CodeMetrics.Clear();
            CodeInsights.Clear();

            foreach (var script in scriptMetadata.Values)
            {
                try
                {
                    var metrics = AnalyzeScript(script);
                    CodeMetrics[script.Name] = metrics;
                    DetectCodeInsights(script, metrics);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error analyzing metrics for {script.Name}: {ex.Message}");
                }
            }
        }

        private CodeMetrics AnalyzeScript(ScriptMetadata script)
        {
            string content = File.ReadAllText(script.FolderPath);
            var metrics = new CodeMetrics();

            // Count lines of code (excluding empty lines and comments)
            var lines = content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !line.TrimStart().StartsWith("//"))
                .Count();
            metrics.LinesOfCode = lines;

            // Count methods
            metrics.MethodCount = Regex.Matches(content, @"\b(public|private|protected|internal)\s+\w+\s+\w+\s*\(").Count;

            // Calculate cyclomatic complexity (rough estimate)
            metrics.CyclomaticComplexity = CalculateCyclomaticComplexity(content);

            // Count dependencies
            metrics.DependencyCount = script.References.Count;

            // Detect design patterns
            metrics.DesignPatterns = DetectDesignPatterns(content);

            // Calculate maintenance index
            metrics.MaintenanceIndex = CalculateMaintenanceIndex(metrics);

            return metrics;
        }

        private int CalculateCyclomaticComplexity(string content)
        {
            int complexity = 1; // Base complexity

            // Count control flow statements
            complexity += Regex.Matches(content, @"\b(if|while|for|foreach|case)\b").Count;
            complexity += Regex.Matches(content, @"&&|\|\|").Count;
            complexity += Regex.Matches(content, @"\?[^.]").Count; // Conditional operators

            return complexity;
        }

        private List<string> DetectDesignPatterns(string content)
        {
            var patterns = new List<string>();

            // Singleton pattern
            if (Regex.IsMatch(content, @"private\s+static\s+\w+\s+instance\b"))
                patterns.Add("Singleton");

            // Observer pattern
            if (content.Contains("event") || content.Contains("UnityEvent"))
                patterns.Add("Observer");

            // Factory pattern
            if (Regex.IsMatch(content, @"Create\w+"))
                patterns.Add("Factory");

            // Command pattern
            if (content.Contains("ICommand") || content.Contains("Execute"))
                patterns.Add("Command");

            return patterns;
        }

        private float CalculateMaintenanceIndex(CodeMetrics metrics)
        {
            // Simplified maintenance index calculation
            float volume = metrics.LinesOfCode * (float)Math.Log(metrics.CyclomaticComplexity);
            float maintainability = 171 - 5.2f * (float)Math.Log(volume) - 0.23f * metrics.CyclomaticComplexity;
            return Mathf.Clamp(maintainability, 0, 100);
        }

        private void DetectCodeInsights(ScriptMetadata script, CodeMetrics metrics)
        {
            // Architecture Insights
            DetectArchitecturalInsights(script, metrics);
            
            // Complexity Insights
            DetectComplexityInsights(script, metrics);
            
            // Dependency Insights
            DetectDependencyInsights(script, metrics);
            
            // Naming and Convention Insights
            DetectNamingInsights(script);
            
            // Pattern Usage Insights
            DetectPatternInsights(script, metrics);
            
            // Unity-Specific Insights
            DetectUnitySpecificInsights(script);
        }

        private void DetectArchitecturalInsights(ScriptMetadata script, CodeMetrics metrics)
        {
            // Circular Dependencies
            if (settings.DetectCircularDependencies)
            {
                var circularDeps = FindCircularDependencies(script);
                if (circularDeps.Any())
                {
                    AddCodeInsight(new CodeInsight
                    {
                        Name = "Circular Dependency Detected",
                        Description = $"Circular dependency chain: {string.Join(" -> ", circularDeps)}",
                        Severity = InsightSeverity.High,
                        AffectedScripts = circularDeps,
                        Recommendation = "Break the circular dependency using interfaces, events, or ScriptableObjects"
                    });
                }
            }

            // Layer Violation
            if (settings.DetectLayerViolations && IsLayerViolation(script))
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Architecture Layer Violation",
                    Description = "Script references components from a higher layer",
                    Severity = InsightSeverity.High,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Maintain proper layering: Presentation -> Business -> Data. Use interfaces or events for upward communication"
                });
            }

            // God Class
            if (settings.DetectGodClass && metrics.MethodCount > settings.GodClassMethodCount && metrics.DependencyCount > settings.GodClassDependencyCount && metrics.LinesOfCode > settings.GodClassLineCount)
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "God Class Detected",
                    Description = "Class has too many responsibilities (high method count, dependencies, and size)",
                    Severity = InsightSeverity.Critical,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Split into multiple focused classes following Single Responsibility Principle"
                });
            }
        }

        private void DetectComplexityInsights(ScriptMetadata script, CodeMetrics metrics)
        {
            // High Cyclomatic Complexity - adjusted thresholds and severity
            if (metrics.CyclomaticComplexity > settings.HighComplexityThreshold) // Was 20, increased to 30
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "High Cyclomatic Complexity",
                    Description = "Methods are very complex with many decision points",
                    Severity = InsightSeverity.Critical,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Break down complex methods into smaller, focused methods"
                });
            }
            else if (metrics.CyclomaticComplexity > settings.ModerateComplexityThreshold) // New medium severity threshold
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Moderate Cyclomatic Complexity",
                    Description = "Methods have above average complexity",
                    Severity = InsightSeverity.Medium,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Consider if some logic could be simplified or extracted into helper methods"
                });
            }

            // Large Methods - adjusted thresholds
            if (HasLargeMethods(script)) // Added threshold parameter, was implicitly 50
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Large Methods Detected",
                    Description = "Contains methods with excessive line count (>100 lines)",
                    Severity = InsightSeverity.High, // Changed from Medium
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Consider breaking down large methods into smaller, focused methods"
                });
            }

            // Low Maintainability
            if (metrics.MaintenanceIndex < 40)
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Low Maintainability Index",
                    Description = "Code is difficult to maintain due to complexity and size",
                    Severity = InsightSeverity.High,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Reduce method complexity, improve naming, and add documentation"
                });
            }
        }

        private void DetectDependencyInsights(ScriptMetadata script, CodeMetrics metrics)
        {
            // High Outgoing Dependencies
            if (metrics.DependencyCount > settings.HighDependencyCount)
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "High Outgoing Dependencies",
                    Description = "Class depends on too many other classes",
                    Severity = InsightSeverity.Medium,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Consider dependency injection or facade pattern to reduce direct dependencies"
                });
            }

            // High Incoming Dependencies
            if (GetIncomingDependencyCount(script) > settings.HighIncomingDependencyCount)
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "High Incoming Dependencies",
                    Description = "Many classes depend on this class",
                    Severity = InsightSeverity.Medium,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Consider if this class should be split or if it's violating Interface Segregation Principle"
                });
            }

            // Unstable Dependencies
            if (HasUnstableDependencies(script))
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Unstable Dependencies",
                    Description = "Depends on classes that change frequently",
                    Severity = InsightSeverity.Medium,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Consider depending on interfaces or stable abstractions instead"
                });
            }
        }

        private void DetectNamingInsights(ScriptMetadata script)
        {
            // Inconsistent Naming
            if (settings.DetectNamingIssues && HasInconsistentNaming(script))
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Inconsistent Naming Conventions",
                    Description = "Class contains inconsistently named elements",
                    Severity = InsightSeverity.Low,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Follow C# naming conventions: PascalCase for public members, camelCase for private"
                });
            }

            // Ambiguous Names
            if (settings.DetectNamingIssues && HasAmbiguousNames(script))
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Ambiguous Naming",
                    Description = "Contains vaguely named elements (e.g., 'Manager', 'Processor', 'Handler')",
                    Severity = InsightSeverity.Low,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Use more specific, descriptive names that indicate purpose"
                });
            }
        }

        private void DetectPatternInsights(ScriptMetadata script, CodeMetrics metrics)
        {
            // Singleton Overuse
            if (metrics.DesignPatterns.Count(p => p == "Singleton") > 1)
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Singleton Pattern Overuse",
                    Description = "Multiple singletons detected which may indicate design issues",
                    Severity = InsightSeverity.Medium,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Consider using dependency injection or ScriptableObjects instead of singletons"
                });
            }

            // Missing Pattern Opportunity
            var missedPatterns = DetectMissedPatternOpportunities(script);
            foreach (var pattern in missedPatterns)
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = $"Potential {pattern} Pattern Opportunity",
                    Description = $"Code structure suggests {pattern} pattern might be beneficial",
                    Severity = InsightSeverity.Low,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = $"Consider implementing {pattern} pattern to improve code structure"
                });
            }
        }

        private void DetectUnitySpecificInsights(ScriptMetadata script)
        {
            // Update Method Overuse
            if (settings.DetectUnitySpecificIssues && HasUpdateMethodOveruse(script))
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Update Method Overuse",
                    Description = "Heavy processing in Update method",
                    Severity = InsightSeverity.High,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Use coroutines, InvokeRepeating, or custom update manager for better performance"
                });
            }

            // Component Reference Issues
            if (settings.DetectUnitySpecificIssues && HasComponentReferenceIssues(script))
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Inefficient Component References",
                    Description = "Frequent GetComponent calls or missing caching",
                    Severity = InsightSeverity.Medium,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Cache component references in Awake or Start methods"
                });
            }

            // MonoBehaviour Misuse
            if (settings.DetectUnitySpecificIssues && IsPotentialMonoBehaviourMisuse(script))
            {
                AddCodeInsight(new CodeInsight
                {
                    Name = "Potential MonoBehaviour Misuse",
                    Description = "Class inherits MonoBehaviour but might not need to",
                    Severity = InsightSeverity.Medium,
                    AffectedScripts = new List<string> { script.Name },
                    Recommendation = "Consider making this a regular class or ScriptableObject if no Unity lifecycle methods are needed"
                });
            }
        }

        private void AddCodeInsight(CodeInsight insight)
        {
            var existing = CodeInsights.FirstOrDefault(s => s.Name == insight.Name && 
                s.AffectedScripts.SequenceEqual(insight.AffectedScripts));

            if (existing == null)
            {
                CodeInsights.Add(insight);
            }
        }

        private List<string> FindCircularDependencies(ScriptMetadata script)
        {
            var visited = new HashSet<string>();
            var path = new List<string>();
            var result = new List<string>();

            void DFS(string currentScript)
            {
                if (path.Contains(currentScript))
                {
                    int startIndex = path.IndexOf(currentScript);
                    result = path.Skip(startIndex).ToList();
                    result.Add(currentScript);
                    return;
                }

                if (!visited.Contains(currentScript))
                {
                    visited.Add(currentScript);
                    path.Add(currentScript);

                    if (dependencyAnalyzer.ScriptMetadata.TryGetValue(currentScript, out var metadata))
                    {
                        foreach (var reference in metadata.References.Keys)
                        {
                            DFS(reference);
                        }
                    }

                    path.RemoveAt(path.Count - 1);
                }
            }

            DFS(script.Name);
            return result;
        }

        private bool IsLayerViolation(ScriptMetadata script)
        {
            var layers = GetArchitecturalLayers();
            string scriptLayer = GetScriptLayer(script);
            
            foreach (var reference in script.References.Keys)
            {
                if (dependencyAnalyzer.ScriptMetadata.TryGetValue(reference, out var referencedScript))
                {
                    string refLayer = GetScriptLayer(referencedScript);
                    if (IsHigherLayer(refLayer, scriptLayer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HasLargeMethods(ScriptMetadata script)
        {
            string content = File.ReadAllText(script.FolderPath);
            var methodMatches = Regex.Matches(content, @"(?<=\{)[^{}]*(?:\{[^{}]*\}[^{}]*)*(?=\})");
            
            foreach (Match match in methodMatches)
            {
                int lineCount = match.Value.Split('\n').Length;
                if (lineCount > settings.LargeMethodLineThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        private int GetIncomingDependencyCount(ScriptMetadata script)
        {
            return dependencyAnalyzer.ScriptMetadata.Values
                .Count(s => s.References.ContainsKey(script.Name));
        }

        private bool HasUnstableDependencies(ScriptMetadata script)
        {
            int unstableCount = 0;
            foreach (var reference in script.References.Keys)
            {
                if (dependencyAnalyzer.ScriptMetadata.TryGetValue(reference, out var referencedScript))
                {
                    if (GetIncomingDependencyCount(referencedScript) > settings.UnstableDependencyThreshold && 
                        referencedScript.References.Count > settings.UnstableDependencyThreshold)
                    {
                        unstableCount++;
                    }
                }
            }
            return unstableCount > 3; // Threshold for unstable dependencies
        }

        private bool HasInconsistentNaming(ScriptMetadata script)
        {
            string content = File.ReadAllText(script.FolderPath);
            
            // Check for mixed naming conventions
            bool hasPascalCase = Regex.IsMatch(content, @"public\s+\w+\s+[A-Z][a-z0-9]+[A-Za-z0-9]*\s");
            bool hasCamelCase = Regex.IsMatch(content, @"public\s+\w+\s+[a-z]+[A-Za-z0-9]*\s");
            bool hasUnderscores = Regex.IsMatch(content, @"public\s+\w+\s+\w+_\w+\s");
            
            return (hasPascalCase && hasCamelCase) || hasUnderscores;
        }

        private bool HasAmbiguousNames(ScriptMetadata script)
        {
            string content = File.ReadAllText(script.FolderPath);
            var ambiguousTerms = new[] { 
                @"\w*Manager\w*", @"\w*Processor\w*", @"\w*Handler\w*", 
                @"\w*Helper\w*", @"\w*Utility\w*", @"\w*Data\w*" 
            };
            
            int ambiguousCount = ambiguousTerms
                .Sum(term => Regex.Matches(content, term).Count);
            
            return ambiguousCount > 2; // Threshold for ambiguous names
        }

        private List<string> DetectMissedPatternOpportunities(ScriptMetadata script)
        {
            string content = File.ReadAllText(script.FolderPath);
            var opportunities = new List<string>();

            // Factory pattern opportunity
            if (Regex.IsMatch(content, @"new\s+\w+\([^)]*\)", RegexOptions.Multiline) && 
                !content.Contains("Factory"))
            {
                opportunities.Add("Factory");
            }

            // Observer pattern opportunity
            if ((content.Contains("Update") || content.Contains("OnEnable")) && 
                !content.Contains("event") && 
                !content.Contains("UnityEvent"))
            {
                opportunities.Add("Observer");
            }

            // Strategy pattern opportunity
            if (Regex.Matches(content, @"switch\s*\([^)]+\)|if\s*\([^)]+\)").Count > 5)
            {
                opportunities.Add("Strategy");
            }

            return opportunities;
        }

        private bool HasUpdateMethodOveruse(ScriptMetadata script)
        {
            string content = File.ReadAllText(script.FolderPath);
            var updateMethod = Regex.Match(content, @"void\s+Update\s*\([^)]*\)\s*{[^}]*}");
            
            if (updateMethod.Success)
            {
                int complexity = CalculateCyclomaticComplexity(updateMethod.Value);
                int lines = updateMethod.Value.Split('\n').Length;
                return complexity > settings.UpdateMethodComplexityThreshold || lines > settings.UpdateMethodLineThreshold;
            }
            return false;
        }

        private bool HasComponentReferenceIssues(ScriptMetadata script)
        {
            string content = File.ReadAllText(script.FolderPath);
            
            // Check for GetComponent calls outside Awake/Start
            int getComponentCalls = Regex.Matches(content, @"GetComponent<").Count;
            bool hasAwakeOrStart = content.Contains("void Awake") || content.Contains("void Start");
            
            return getComponentCalls > 0 && !hasAwakeOrStart;
        }

        private bool IsPotentialMonoBehaviourMisuse(ScriptMetadata script)
        {
            if (!script.IsMonoBehaviour) return false;

            string content = File.ReadAllText(script.FolderPath);
            var unityMethods = new[] { 
                "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", 
                "OnEnable", "OnDisable", "OnDestroy" 
            };

            return !unityMethods.Any(method => content.Contains($"void {method}"));
        }

        private string GetScriptLayer(ScriptMetadata script)
        {
            if (script.Name.EndsWith("UI") || script.Name.EndsWith("View"))
                return "Presentation";
            if (script.Name.EndsWith("Controller") || script.Name.EndsWith("Manager"))
                return "Business";
            if (script.IsScriptableObject || script.Name.EndsWith("Data"))
                return "Data";
            if (script.Name.EndsWith("Service") || script.Name.EndsWith("Provider"))
                return "Infrastructure";
            return "Other";
        }

        private bool IsHigherLayer(string layer1, string layer2)
        {
            var layerOrder = new Dictionary<string, int>
            {
                { "Presentation", 3 },
                { "Business", 2 },
                { "Data", 1 },
                { "Infrastructure", 0 },
                { "Other", -1 }
            };

            return layerOrder.TryGetValue(layer1, out int value1) && 
                   layerOrder.TryGetValue(layer2, out int value2) && 
                   value1 > value2;
        }

        private Dictionary<string, List<ScriptMetadata>> GetArchitecturalLayers()
        {
            return dependencyAnalyzer.ScriptMetadata.Values
                .GroupBy(GetScriptLayer)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    public class CodeMetrics
    {
        public int CyclomaticComplexity;
        public int LinesOfCode;
        public int MethodCount;
        public int DependencyCount;
        public List<string> DesignPatterns = new List<string>();
        public float MaintenanceIndex;
    }

    public class CodeInsight
    {
        public string Name;
        public string Description;
        public InsightSeverity Severity;
        public List<string> AffectedScripts = new List<string>();
        public string Recommendation;
    }

    public enum InsightSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
} 