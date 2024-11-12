using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using ScriptAnalysis.Analyzers;

namespace ScriptAnalysis.Exporters
{
    public abstract class Exporter
    {
        public abstract void Export(string path, DependencyAnalyzer analyzer);
    }

    public class DotExporter : Exporter
    {
        public override void Export(string path, DependencyAnalyzer analyzer)
        {
            var scriptMetadata = analyzer.ScriptMetadata;

            StringBuilder dot = new StringBuilder();
            dot.AppendLine("digraph G {");
            dot.AppendLine("    rankdir=LR;");

            // Define node styles
            dot.AppendLine("    node [shape=box, style=filled, color=lightgrey];");

            // Define nodes
            foreach (var script in scriptMetadata.Values)
            {
                string color = script.IsMonoBehaviour ? "lightblue" :
                               script.IsScriptableObject ? "lightgreen" : "white";
                dot.AppendLine($"    \"{script.Name}\" [fillcolor={color}];");
            }

            // Define edges
            foreach (var script in scriptMetadata.Values)
            {
                foreach (var reference in script.References.Keys)
                {
                    dot.AppendLine($"    \"{script.Name}\" -> \"{reference}\";");
                }
            }

            dot.AppendLine("}");

            File.WriteAllText(path, dot.ToString());
        }
    }

    public class SvgExporter : Exporter
    {
        public override void Export(string path, DependencyAnalyzer analyzer)
        {
            // Implement SVG export logic
            EditorUtility.DisplayDialog("SVG Export", "SVG export not implemented yet.", "OK");
        }
    }

    public class HtmlExporter : Exporter
    {
        public override void Export(string path, DependencyAnalyzer analyzer)
        {
            // Implement HTML export logic
            EditorUtility.DisplayDialog("HTML Export", "HTML export not implemented yet.", "OK");
        }
    }
} 