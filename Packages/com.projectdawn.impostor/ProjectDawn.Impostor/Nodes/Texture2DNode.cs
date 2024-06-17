
using GraphProcessor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Texture/Texture 2D")]
    public class Texture2DNode : ImpostorNode
    {
        public override string name => "Texture2D";

        [Input]
        public RenderTexture Source;
        [Output(allowMultiple = false)]
        public Texture2D Destination;

        public GraphicsFormat Format = GraphicsFormat.B8G8R8A8_SRGB;
        public FilterMode Filter = FilterMode.Bilinear;
        public bool Mipmaps = false;

        protected override void Process()
        {
            Destination = Source.ToTexture2D(Format, Filter, Mipmaps);
        }
    }
}