using UnityEngine;

namespace Raymarcher.Objects.Primitives
{
    public sealed class RMSdf_Cube : RMSdfObjectBase
    {
        public float cubeSizeUniform = 1;
        public Vector3 cubeSize = Vector3.one;
        [Range(0, 1)] public float cubeRoundness = 0.1f;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Cube")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_Cube>();

        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Rounded Cube")]
        private static void CreateSdfObjectInEditor2()
        {
            var sdf = CreateSDFObject<RMSdf_Cube>();
            if (!sdf)
                return;

            sdf.cubeSizeUniform = 0.5f;
        }
#endif

        public override string SdfMethodBody => 
            $"{VARCONST_RESULT} = length(max(abs({VARCONST_POSITION}) - abs({nameof(cubeSize)}) + {nameof(cubeRoundness)}, 0.0)) - {nameof(cubeRoundness)};";
        
        public override string SdfMethodName => "GetCubeSdf";
        
        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[2]
        {
            new ISDFEntity.SDFUniformField(nameof(cubeRoundness), ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(nameof(cubeSize), ISDFEntity.SDFUniformType.Float3)
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(nameof(cubeSize) + iterationIndex, cubeSize * cubeSizeUniform);
            raymarcherSceneMaterial.SetFloat(nameof(cubeRoundness) + iterationIndex, cubeRoundness);
        }
    }
}