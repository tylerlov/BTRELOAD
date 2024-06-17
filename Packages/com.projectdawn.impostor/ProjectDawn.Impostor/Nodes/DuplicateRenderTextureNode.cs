
using GraphProcessor;
using System;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Texture/Duplicate RenderTexture")]
    public class DuplicateRenderTexture : ImpostorNode, IDisposable
    {
        public override string name => "Duplicate RenderTexture";

        [Input]
        public RenderTexture Source;
        [SerializeField, Input]
        public int Resolution = 1024;
        [Output(allowMultiple = false)]
        public RenderTexture Destination;
        [Output]
        public RenderTexture Copy;

        protected override void Process()
        {
            Copy = RenderTexture.GetTemporary(Resolution, Resolution, Source.depth, Source.format);
            Graphics.Blit(Source, Copy);
            Destination = Source;
        }

        public void Dispose()
        {
            RenderTexture.ReleaseTemporary(Copy);
        }
    }
}