using UnityEngine;

namespace Raymarcher.Objects.Fractals
{
    public sealed class RMSdfFractal_Mandelbulb : RMSdfObjectBase
    {
        [Range(2f, 0.01f)] public float fractalScale = 0.5f;
        [Range(0, 50)] public float fractalProgression = 10f;
        [Range(0, 50)] public float fractalColumns = 10f;
        [Range(0f, 1f)] public float fractalLiquify = 0f;
        [Range(0, 1)] public float fractalColorPhase = 1f;

        private const string fractalParams = "fractalParams";

#if UNITY_EDITOR

        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_FRACTALS_PATH + "Mandelbulb 3D")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdfFractal_Mandelbulb>();

#endif

        public override string SdfMethodBody =>
            $"{VARCONST_POSITION} *= fractalParams.w;\n" +
            $"float m = dot(p, p);\n" +
            $"float dz = 1.0;\n" +
            $"half colorProgression = 0.0;\n" +
            $"for (int i = 0; i < 5; i++)\n" +
            "{\n" +
            $"    dz = 8.0 * pow(max(EPSILON, abs(m)), 4) * dz + 1.0;\n" +
            $"    float r = length(p);\n" +
            $"    float b = fractalParams.x * acos(clamp(p.y / r, -1.0, 1.0));\n" +
            $"    float a = fractalParams.y * atan2(p.x, p.z);\n" +
            $"    p += pow(r, 8.0) * float3(sin(b) * sin(a), cos(b), sin(b) * cos(a));\n" +
            $"    colorProgression += lerp(1.0 - colorProgression, atan(log(i * 0.35)) * sin(i * 0.5), clamp(fractalParams.z, 0, 1));\n" +
            $"    p *= lerp(1., .3, fractalLiquify);\n" +
            $"    m = dot(p, p);\n" +
            $"    if (m > 2.0) break;\n" +
            "}\n" +
            $"{VARCONST_COLOR}" + 
            (RenderMaster.RenderingData.CompiledRenderType == RendererData.RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality ? " +=" : " *=")
            + " colorProgression;\n" +
            $"{VARCONST_RESULT} = (0.25 * log(m) * sqrt(m) / dz) / fractalParams.w;";

        public override string SdfMethodName => "GetMandelbulbFractalSdf";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[2]
        {
            new ISDFEntity.SDFUniformField(fractalParams, ISDFEntity.SDFUniformType.Float4),
            new ISDFEntity.SDFUniformField(nameof(fractalLiquify), ISDFEntity.SDFUniformType.Float),
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(fractalParams + iterationIndex, new Vector4(fractalProgression, fractalColumns, fractalColorPhase, fractalScale));
            raymarcherSceneMaterial.SetFloat(nameof(fractalLiquify) + iterationIndex, fractalLiquify);
        }
    }
}
