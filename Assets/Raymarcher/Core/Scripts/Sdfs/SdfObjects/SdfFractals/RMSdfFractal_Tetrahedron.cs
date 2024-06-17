using UnityEngine;

namespace Raymarcher.Objects.Fractals
{
    public sealed class RMSdfFractal_Tetrahedron : RMSdfObjectBase
    {
        [Range(1, 3)] public float fractalProgression = 1.75f;
        public float fractalSize = 5f;
        [Range(0, 1)] public float fractalBurst = 1f;

        private const string fractalParams = "fractalParams";

#if UNITY_EDITOR

        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_FRACTALS_PATH + "Tetrahedron 3D")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdfFractal_Tetrahedron>();

#endif

        public override string SdfMethodBody =>
            $"const float3 a1 = float3(1.0, 1.0, 1.0);\n" +
            $"const float3 a2 = float3(-1.0, -1.0, 1.0);\n" +
            $"const float3 a3 = float3(1.0, -1.0, -1.0);\n" +
            $"const float3 a4 = float3(-1.0, 1.0, -1.0);\n" +
            $"float d;\n" +
            $"for (int n = 0; n < 30; ++n)\n" +
            "{\n" +
            $"    float3 c = a1;\n" +
            $"    float minDist = length(p - a1);\n" +
            $"    d = length(p - a2) * fractalParams.z;\n" +
            $"    if (d < minDist) {{ c = a2; minDist = d; }}\n" +
            $"    d = length(p - a3);\n" +
            $"    if (d < minDist) {{ c = a3; minDist = d; }}\n" +
            $"    d = length(p - a4);\n" +
            $"    if (d < minDist) {{ c = a4; minDist = d; }}\n" +
            $"    p = fractalParams.x * p - c * (fractalParams.x - 1.0) * fractalParams.y;\n" +
            "}\n" +
            $"{VARCONST_RESULT} = length(p) * pow(max(EPSILON, fractalParams.x), float(-n));";

        public override string SdfMethodName => "GetTetrahedronFractalSdf";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[1]
        {
            new ISDFEntity.SDFUniformField(fractalParams, ISDFEntity.SDFUniformType.Float3)
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(fractalParams + iterationIndex, new Vector3(fractalProgression, fractalSize, fractalBurst));
        }
    }
}
