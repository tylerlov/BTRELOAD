using GraphProcessor;
using UnityEditor;
using UnityEngine;

namespace ProjectDawn.Impostor.Editor
{
    public class ImpostorGraphWindow : BaseGraphWindow
    {
        public static BaseGraphWindow Open(BaseGraph graph)
        {
            // Check if window is already opened
            foreach (var window in Resources.FindObjectsOfTypeAll<ImpostorGraphWindow>())
            {
                if (window.graph == graph)
                {
                    window.Focus();
                    return window;
                }
            }

            var graphWindow = CreateWindow<ImpostorGraphWindow>(typeof(SceneView));
            graphWindow.InitializeGraph(graph);
            graphWindow.Focus();
            return graphWindow;
        }

        protected override void OnDestroy()
        {
            graphView?.Dispose();
        }

        protected override void InitializeWindow(BaseGraph graph)
        {
            titleContent = EditorGUIUtility.TrTextContentWithIcon(graph.name,
                EditorGUIUtility.isProSkin ? "Packages/com.projectdawn.impostor/Editor/Resources/Icons/d_ImpostorGraphView@2x.png" : "Packages/com.projectdawn.impostor/Editor/Resources/Icons/ImpostorGraphView@2x.png");

            if (graphView == null)
            {
                graphView = new ImpostorGraphView(this);
                graphView.Add(new ImpostorToolbarView(graphView));
            }

            rootView.Add(graphView);
        }

        protected override void InitializeGraphView(BaseGraphView view)
        {
            view.OpenPinned<ExposedParameterView>();
        }
    }
}
