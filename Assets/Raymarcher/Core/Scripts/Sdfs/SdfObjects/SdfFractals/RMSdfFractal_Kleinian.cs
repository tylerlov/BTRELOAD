using UnityEngine;

namespace Raymarcher.Objects.Fractals
{
    public sealed class RMSdfFractal_Kleinian : RMSdfObjectBase
    {
        [Range(2f, 0.01f)] public float fractalScale = 0.5f;
        [Range(0.001f, 3)] public float fractalProgression = 2.5f;
        [Range(0.8f, 2)] public float fractalSpread = 1f;
        [Range(0, 1)] public float fractalColorPhase = 1f;

        private const string fractalParams = "fractalParams";

#if UNITY_EDITOR

        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_FRACTALS_PATH + "Kleinian")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdfFractal_Kleinian>();

#endif

        public override string SdfMethodBody =>
            $"float DEfactor = 1.0;\n" +
            $"half colorProgression = 0.0;\n" +
            $"{VARCONST_POSITION} *= {fractalParams}.w;\n" +
            $"for (int i = 0; i < 6; i++)\n" +
            "{\n" +
            $"    {VARCONST_POSITION} = -1.0 + 2.0 * frac(0.5 * {VARCONST_POSITION} + 0.5);\n" +
            $"    float k = max(0.70968 / dot({VARCONST_POSITION}, {VARCONST_POSITION}) * {fractalParams}.x, 1);\n" +
            $"    colorProgression += lerp(1.0 - colorProgression, sin(i * 2.5) * pow(k, 0.5) + 0.15, clamp({fractalParams}.z, 0, 1));\n" +
            $"    {VARCONST_POSITION} *= k;\n" +
            $"    {VARCONST_POSITION}.xy *= {fractalParams}.y;\n" +
            $"    DEfactor *= k + 0.05;\n" +
            "}\n" +
            $"float rxy = length({VARCONST_POSITION}.xy);\n" +
            $"{VARCONST_COLOR}" + 
            (RenderMaster.RenderingData.CompiledRenderType == RendererData.RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality ? " +=" : " *=")
            + " colorProgression;\n" +
            $"{VARCONST_RESULT} = (max(rxy - 0.92784, abs(rxy * {VARCONST_POSITION}.z) / length({VARCONST_POSITION})) / DEfactor) / {fractalParams}.w;";

        public override string SdfMethodName => "GetKleinianFractalSdf";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[1]
        {
            new ISDFEntity.SDFUniformField(fractalParams, ISDFEntity.SDFUniformType.Float4)
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(fractalParams + iterationIndex, new Vector4(fractalProgression, fractalSpread, fractalColorPhase, fractalScale));
        }
    }
}
