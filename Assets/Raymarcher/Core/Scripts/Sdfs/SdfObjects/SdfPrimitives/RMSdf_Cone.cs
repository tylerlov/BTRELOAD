using UnityEngine;

namespace Raymarcher.Objects.Primitives
{
    public sealed class RMSdf_Cone : RMSdfObjectBase
    {
        public float coneSize = 0.3f;
        public float coneHeight = 1f;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Cone")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_Cone>();
#endif

        public override string SdfMethodBody =>
             $"{VARCONST_RESULT} = max(dot(float2(0.5, {nameof(coneSize)}), float2(length({VARCONST_POSITION}.xz), {VARCONST_POSITION}.y - {nameof(coneHeight)})), -{VARCONST_POSITION}.y - {nameof(coneHeight)});";
        
        public override string SdfMethodName => "GetConeSdf";
        
        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[2]
        {
            new ISDFEntity.SDFUniformField(nameof(coneHeight), ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(nameof(coneSize), ISDFEntity.SDFUniformType.Float)
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetFloat(nameof(coneHeight) + iterationIndex, Mathf.Abs(coneHeight));
            raymarcherSceneMaterial.SetFloat(nameof(coneSize) + iterationIndex, Mathf.Abs(coneSize));
        }
    }
}