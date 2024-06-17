
using GraphProcessor;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    [HelpURL("https://lukaschod.github.io/impostor-graph-docs/manual/nodes/scene-node.html")]
    [NodeMenuItem("Scene/Scene")]
    public class SceneNode : ImpostorNode, IDisposable
    {
        public override string name => "Scene";

        [Input]
        public Surface Surface;
        [Input]
        public CapturePoints CapturePoints;
        [Output]
        public Scene Scene;

        protected override void Process()
        {
            var renderPipeline = GraphicsSettings.defaultRenderPipeline;
            if (GraphicsSettings.defaultRenderPipeline == null)
                throw new Exception("Render pipeline must be set either in Quality or Graphics settings.");

            var renderPipelineType = renderPipeline.GetType();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var sceneTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Scene)));
                foreach (var sceneType in sceneTypes)
                {
                    var renderPipelineAttributes = sceneType.GetCustomAttributes(typeof(RenderPipelineAttribute), true);
                    if (renderPipelineAttributes.Length > 0 && ((RenderPipelineAttribute)renderPipelineAttributes[0]).PipelineType == renderPipelineType)
                    {
                        Scene = (Scene)Activator.CreateInstance(sceneType, Surface, CapturePoints);
                        return;
                    }
                }
            }

            throw new Exception("Failed to find scene with particular render pipeline.");
        }

        public void Dispose()
        {
            Scene?.Dispose();
        }
    }
}