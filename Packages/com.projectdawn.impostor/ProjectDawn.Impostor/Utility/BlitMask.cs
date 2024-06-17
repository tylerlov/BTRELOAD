using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    public enum Packing
    {
        RGBA,
        RGB,
        RToA,
        RToR,
        RToG,
        RToB,
    }

    public static class BlitMask
    {
        public static void Execute(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Packing packing, Material material)
        {
            switch (packing)
            {
                case Packing.RGB:
                    cmd.SetGlobalInteger("_ColorWriteMask", (int)(ColorWriteMask.Red | ColorWriteMask.Green | ColorWriteMask.Blue));
                    cmd.SetGlobalVector("_ColorMixR", new Vector4(1, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixG", new Vector4(0, 1, 0, 0));
                    cmd.SetGlobalVector("_ColorMixB", new Vector4(0, 0, 1, 0));
                    cmd.SetGlobalVector("_ColorMixA", new Vector4(0, 0, 0, 0));
                    break;
                case Packing.RGBA:
                    cmd.SetGlobalInteger("_ColorWriteMask", (int)ColorWriteMask.All);
                    cmd.SetGlobalVector("_ColorMixR", new Vector4(1, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixG", new Vector4(0, 1, 0, 0));
                    cmd.SetGlobalVector("_ColorMixB", new Vector4(0, 0, 1, 0));
                    cmd.SetGlobalVector("_ColorMixA", new Vector4(0, 0, 0, 1));
                    break;
                case Packing.RToA:
                    cmd.SetGlobalInteger("_ColorWriteMask", (int)ColorWriteMask.Alpha);
                    cmd.SetGlobalVector("_ColorMixR", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixG", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixB", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixA", new Vector4(1, 0, 0, 0));
                    break;
                case Packing.RToR:
                    cmd.SetGlobalInteger("_ColorWriteMask", (int)ColorWriteMask.Red);
                    cmd.SetGlobalVector("_ColorMixR", new Vector4(1, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixG", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixB", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixA", new Vector4(0, 0, 0, 0));
                    break;
                case Packing.RToG:
                    cmd.SetGlobalInteger("_ColorWriteMask", (int)ColorWriteMask.Green);
                    cmd.SetGlobalVector("_ColorMixR", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixG", new Vector4(1, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixB", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixA", new Vector4(0, 0, 0, 0));
                    break;
                case Packing.RToB:
                    cmd.SetGlobalInteger("_ColorWriteMask", (int)ColorWriteMask.Blue);
                    cmd.SetGlobalVector("_ColorMixR", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixG", new Vector4(0, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixB", new Vector4(1, 0, 0, 0));
                    cmd.SetGlobalVector("_ColorMixA", new Vector4(0, 0, 0, 0));
                    break;
                default:
                    throw new System.NotImplementedException($"Packing {packing} is not implemented");
            }
            cmd.Blit(source, destination, material, 1);
        }
    }
}