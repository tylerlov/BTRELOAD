using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    public static class Dilate
    {
        public static void Execute(CommandBuffer cmd, RenderTexture target, RenderTexture mask, Material blitMaskMaterial, Material dilateMaterial, int frames, int iterations = 10)
        {
            if (iterations == 0)
                return;

            var temp = Shader.PropertyToID("_DilateTemp");
            cmd.GetTemporaryRT(temp, target.width, target.height, 0);

            BlitMask.Execute(cmd, mask, target, Packing.RToA, blitMaskMaterial);

            cmd.SetGlobalFloat("_Frames", frames);

            for (int i = 0; i < iterations; i++)
            {
                cmd.Blit(target, temp, dilateMaterial, 0);
                cmd.Blit(temp, target);
            }

            cmd.ReleaseTemporaryRT(temp);
        }
    }
}