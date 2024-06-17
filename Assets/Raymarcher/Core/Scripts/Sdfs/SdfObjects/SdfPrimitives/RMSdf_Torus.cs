using UnityEngine;

namespace Raymarcher.Objects.Primitives
{
    public sealed class RMSdf_Torus : RMSdfObjectBase
    {
        public float torusRadius = 0.5f;
        public float torusThickness = 0.5f;
        public float torusHeight = 0;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Torus")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_Torus>();
#endif

        public override string SdfMethodBody =>
            $"{VARCONST_POSITION}.y -= clamp({VARCONST_POSITION}.y, 0.0, {nameof(torusHeight)});\n" +
            $"{VARCONST_RESULT} = length(float2(length({VARCONST_POSITION}.xz) - {nameof(torusRadius)}, {VARCONST_POSITION}.y)) - {nameof(torusThickness)};";
        
        public override string SdfMethodName => "GetTorusSdf";
        
        public override ISDFEntity.SDFUniformField[] SdfUniformFields =>
            new ISDFEntity.SDFUniformField[3]
            {
                new ISDFEntity.SDFUniformField(nameof(torusThickness), ISDFEntity.SDFUniformType.Float),
                new ISDFEntity.SDFUniformField(nameof(torusRadius), ISDFEntity.SDFUniformType.Float),
                new ISDFEntity.SDFUniformField(nameof(torusHeight), ISDFEntity.SDFUniformType.Float)
            };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetFloat(nameof(torusThickness) + iterationIndex, Mathf.Abs(torusThickness));
            raymarcherSceneMaterial.SetFloat(nameof(torusRadius) + iterationIndex, Mathf.Abs(torusRadius));
            raymarcherSceneMaterial.SetFloat(nameof(torusHeight) + iterationIndex, Mathf.Abs(torusHeight));
        }
    }
}