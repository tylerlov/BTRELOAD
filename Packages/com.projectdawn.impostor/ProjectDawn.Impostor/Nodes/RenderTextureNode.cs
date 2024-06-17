
using GraphProcessor;
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Texture/Temp RenderTexture")]
    public class RenderTextureNode : ImpostorNode
    {
        public override string name => "Temp RenderTexture";

        [SerializeField, Input]
        public int Resolution = 1024;
        [Output(allowMultiple = false)]
        public RenderTexture RenderTexture;

        public int Depth = 32;
        public GraphicsFormat Format = GraphicsFormat.R16G16B16A16_SFloat;

        protected override void Process()
        {
            RenderTexture = RenderTexture.GetTemporary(Resolution, Resolution, Depth, Format);
            if (RenderTexture == null)
                throw new Exception("Failed to get temp texture!");
        }

        public void Dispose()
        {
            RenderTexture.ReleaseTemporary(RenderTexture);
        }
    }
}