using UnityEngine;

namespace Raymarcher.Objects.Primitives
{
    public sealed class RMSdf_CubeFrame : RMSdfObjectBase
    {
        public float cubeSizeUniform = 1;
        public Vector3 cubeSize = Vector3.one;
        public float frameSize = 0.2f;
        [Range(0.1f, 1)] public float cubeRoundness = 0.1f;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Cube Frame")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_CubeFrame>();
#endif

        public override string SdfMethodBody =>
            $"float cYframe = length(max(abs({VARCONST_POSITION}) - (abs({nameof(cubeSize)}) + half3(0, 1, 0) - abs({nameof(frameSize)}) - {nameof(cubeRoundness)}), 0.0)) - {nameof(cubeRoundness)};\n" +
            $"float cZframe = length(max(abs({VARCONST_POSITION}) - (abs({nameof(cubeSize)}) + half3(0, 0, 1) - abs({nameof(frameSize)}) - {nameof(cubeRoundness)}), 0.0)) - {nameof(cubeRoundness)};\n" +
            $"float cXframe = length(max(abs({VARCONST_POSITION}) - (abs({nameof(cubeSize)}) + half3(1, 0, 0) - abs({nameof(frameSize)}) - {nameof(cubeRoundness)}), 0.0)) - {nameof(cubeRoundness)};\n" +
            $"{VARCONST_RESULT} = max(-min(cYframe, min(cZframe, cXframe)), length(max(abs({VARCONST_POSITION}) - abs({nameof(cubeSize)}) + {nameof(cubeRoundness)}, 0.0)) - {nameof(cubeRoundness)});";
        
        public override string SdfMethodName => "GetCubeFrameSdf";
        
        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(nameof(cubeRoundness), ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(nameof(cubeSize), ISDFEntity.SDFUniformType.Float3),
            new ISDFEntity.SDFUniformField(nameof(frameSize), ISDFEntity.SDFUniformType.Float)
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(nameof(cubeSize) + iterationIndex, cubeSize * cubeSizeUniform);
            raymarcherSceneMaterial.SetFloat(nameof(cubeRoundness) + iterationIndex, cubeRoundness);
            raymarcherSceneMaterial.SetFloat(nameof(frameSize) + iterationIndex, frameSize);
        }
    }
}