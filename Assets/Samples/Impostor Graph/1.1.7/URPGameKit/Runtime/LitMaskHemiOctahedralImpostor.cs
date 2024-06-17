
using GraphProcessor;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Material/Lit Mask Hemi-Octahedral Impostor")]
    public class LitMaskHemiOctahedralImpostor : ImpostorNode
    {
        public override string name => "Lit Mask Hemi-Octahedral Impostor";

        [Input]
        public Texture2D BaseAlpha;
        [Input]
        public Texture2D NormalDepth;
        [Input]
        public Texture2D Mask;
        [Input]
        public Mesh Mesh;
        [Input]
        public CapturePoints CapturePoints;
        [Output]
        public Impostor Impostor;

        [Reload("Packages/com.projectdawn.impostor/Shaders/Lit Hemi Octahedral Impostor.mat")]
        public Material BaseMaterial;

        protected override void Process()
        {
            var textures = new List<Texture2D>();
            Impostor = new Impostor();
            Impostor.Material = new Material(BaseMaterial);
            Impostor.Material.SetFloat("_ImpostorFrames", CapturePoints.Frames);
            Impostor.Material.SetFloat("_ImpostorSize", CapturePoints.Bounds.radius * 2);
            Impostor.Material.SetVector("_ImpostorOffset", CapturePoints.Bounds.position);
            Impostor.Material.SetTexture("BaseAlpha", BaseAlpha);
            if (BaseAlpha)
            {
                BaseAlpha.name = "BaseAlpha";
                textures.Add(BaseAlpha);
            }
            Impostor.Material.SetTexture("NormalDepth", NormalDepth);
            if (NormalDepth)
            {
                NormalDepth.name = "NormalDepth";
                textures.Add(NormalDepth);
            }
            Impostor.Material.SetTexture("Mask", Mask);
            if (Mask)
            {
                Mask.name = "Mask";
                textures.Add(Mask);
            }
            Impostor.Mesh = Mesh;
            Impostor.Textures = textures.ToArray();
        }
    }
}