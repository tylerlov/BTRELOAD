
using GraphProcessor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    [HelpURL("https://lukaschod.github.io/impostor-graph-docs/manual/nodes/dilate-node.html")]
    [NodeMenuItem("Texture/Dilate")]
    public class DilateNode : ImpostorNode
    {
        public override string name => "Dilate";

        [Input]
        public RenderTexture Source;
        [Input]
        public RenderTexture Mask;
        [Input, SerializeField]
        public int Frames = 12;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;

        [Reload("Packages/com.projectdawn.impostor/Shaders/Blit.mat")]
        public Material BlitMaskMaterial;
        [Reload("Packages/com.projectdawn.impostor/Shaders/Dilate.mat")]
        public Material DilateMaterial;
        public int Iterations = 10;

        protected override void Process()
        {
            var cmd = CommandBufferPool.Get();
            Dilate.Execute(cmd, Source, Mask, BlitMaskMaterial, DilateMaterial, Frames, Iterations);
            Destination = Source;
            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}