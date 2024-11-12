using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using ScriptAnalysis.Exporters;
using ScriptAnalysis.Analyzers;

namespace ScriptAnalysis.Editor
{
    public class ExportManager
    {
        private Exporter currentExporter = new DotExporter();
        private DependencyAnalyzer analyzer;

        public ExportManager(DependencyAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public void DrawExportOptions()
        {
            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Export to DOT"))
            {
                currentExporter = new DotExporter();
                ExportAnalysis();
            }

            if (GUILayout.Button("Export to SVG"))
            {
                currentExporter = new SvgExporter();
                ExportAnalysis();
            }

            if (GUILayout.Button("Export to HTML"))
            {
                currentExporter = new HtmlExporter();
                ExportAnalysis();
            }

            if (GUILayout.Button("Export Interactive HTML"))
            {
                ExportInteractiveHTML();
            }
        }

        private void ExportAnalysis()
        {
            string extension = currentExporter switch
            {
                DotExporter => "dot",
                SvgExporter => "svg",
                HtmlExporter => "html",
                _ => "txt"
            };

            string path = EditorUtility.SaveFilePanel(
                "Export Analysis", 
                "", 
                $"dependency_analysis.{extension}",
                extension
            );

            if (!string.IsNullOrEmpty(path))
            {
                currentExporter.Export(path, analyzer);
                EditorUtility.DisplayDialog(
                    "Export Complete", 
                    $"Analysis exported to:\n{path}", 
                    "OK"
                );
            }
        }

        private void ExportInteractiveHTML()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export Interactive HTML",
                "",
                "dependency_analysis.html",
                "html"
            );

            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("  <title>Script Dependency Analysis</title>");
            sb.AppendLine("  <script src=\"https://d3js.org/d3.v7.min.js\"></script>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    .node { cursor: pointer; }");
            sb.AppendLine("    .link { stroke: #999; stroke-opacity: 0.6; }");
            sb.AppendLine("    .tooltip { position: absolute; background: white; padding: 5px; border: 1px solid #999; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div id=\"graph\"></div>");
            sb.AppendLine("  <script>");
            
            // Add data
            sb.AppendLine("    const data = {");
            sb.AppendLine("      nodes: [");
            foreach (var script in analyzer.ScriptMetadata.Values)
            {
                sb.AppendLine($"        {{ id: \"{script.Name}\", type: \"{GetScriptType(script)}\" }},");
            }
            sb.AppendLine("      ],");
            sb.AppendLine("      links: [");
            foreach (var script in analyzer.ScriptMetadata.Values)
            {
                foreach (var dep in script.References.Keys)
                {
                    sb.AppendLine($"        {{ source: \"{script.Name}\", target: \"{dep}\" }},");
                }
            }
            sb.AppendLine("      ]");
            sb.AppendLine("    };");

            // Add D3.js visualization code
            sb.AppendLine(GetD3Visualization());
            
            sb.AppendLine("  </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(path, sb.ToString());

            EditorUtility.DisplayDialog(
                "Export Complete",
                "Interactive HTML visualization has been exported.\n" +
                "Open in a web browser to view.",
                "OK"
            );
        }

        private string GetScriptType(ScriptMetadata script)
        {
            if (script.IsMonoBehaviour) return "MonoBehaviour";
            if (script.IsScriptableObject) return "ScriptableObject";
            return "Script";
        }

        private string GetD3Visualization()
        {
            return @"
                const width = window.innerWidth;
                const height = window.innerHeight;

                const svg = d3.select('#graph')
                    .append('svg')
                    .attr('width', width)
                    .attr('height', height);

                const simulation = d3.forceSimulation(data.nodes)
                    .force('link', d3.forceLink(data.links).id(d => d.id))
                    .force('charge', d3.forceManyBody().strength(-300))
                    .force('center', d3.forceCenter(width / 2, height / 2));

                const link = svg.append('g')
                    .selectAll('line')
                    .data(data.links)
                    .join('line')
                    .attr('class', 'link');

                const node = svg.append('g')
                    .selectAll('circle')
                    .data(data.nodes)
                    .join('circle')
                    .attr('class', 'node')
                    .attr('r', 5)
                    .attr('fill', d => d.type === 'MonoBehaviour' ? '#4CAF50' : 
                                     d.type === 'ScriptableObject' ? '#2196F3' : '#9E9E9E');

                node.append('title')
                    .text(d => `${d.id}\n${d.type}`);

                simulation.on('tick', () => {
                    link
                        .attr('x1', d => d.source.x)
                        .attr('y1', d => d.source.y)
                        .attr('x2', d => d.target.x)
                        .attr('y2', d => d.target.y);

                    node
                        .attr('cx', d => d.x)
                        .attr('cy', d => d.y);
                });
            ";
        }
    }
} 