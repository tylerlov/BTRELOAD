using UnityEngine;

namespace Raymarcher.Objects.Fractals
{
    public sealed class RMSdfFractal_Apollonian : RMSdfObjectBase
    {
        [Range(2f, 0.01f)] public float fractalScale = 0.5f;
        [Range(0.001f, 3)] public float fractalProgression = 1.5f;
        [Range(0, 5)] public float fractalSpread = 1.2f;
        [Range(0, 1)] public float fractalColorPhase = 1f;

        private const string fractalParams = "fractalParams";

#if UNITY_EDITOR

        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_FRACTALS_PATH + "Apollonian")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdfFractal_Apollonian>();

#endif

        public override string SdfMethodBody =>
            $"float sc = 1.0;\n" +
            $"half colorProgression = 0.0;\n" +
            $"{VARCONST_POSITION} *= {fractalParams}.w;\n" +
            $"for (int i = 0; i < 8; i++)\n" +
            "{\n" +
            $"    {VARCONST_POSITION} = -1.0 + 2.0 * frac(0.5 * {VARCONST_POSITION} + 0.5);\n" +
            $"    float r2 = dot({VARCONST_POSITION}, {VARCONST_POSITION});\n" +
            $"    colorProgression += lerp(1.0f - colorProgression, (float)(cos(i * 0.5) * pow(r2, 0.5) + 0.15), clamp({fractalParams}.z, 0, 1));\n" +
            $"    float k = {fractalParams}.x / r2;\n" +
            $"    {VARCONST_POSITION} *= k;\n" +
            $"    {VARCONST_POSITION}.x *= {fractalParams}.y;\n" +
            $"    sc *= k;\n" +
            "}\n" + 
            $"{VARCONST_COLOR}" + 
            (RenderMaster.RenderingData.CompiledRenderType == RendererData.RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality ? " +=" : " *=")
            + " colorProgression;\n" +
            $"{VARCONST_RESULT} = (0.25 * abs({VARCONST_POSITION}.y) / sc) / {fractalParams}.w;";

        public override string SdfMethodName => "GetApollonianFractalSdf";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => 
            new ISDFEntity.SDFUniformField[1]
            {
                new ISDFEntity.SDFUniformField(fractalParams, ISDFEntity.SDFUniformType.Float4)
            };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(fractalParams + iterationIndex, new Vector4(fractalProgression, fractalSpread, fractalColorPhase, fractalScale));
        }
    }
}
