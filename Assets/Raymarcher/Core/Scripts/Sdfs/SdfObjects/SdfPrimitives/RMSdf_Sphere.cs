using UnityEngine;

namespace Raymarcher.Objects.Primitives
{
    public sealed class RMSdf_Sphere : RMSdfObjectBase
    {
        public float sphereRadius = 1.0f;
        public float sphereHeight = 0.0f;

        private const string SphereSdfData = "sphereSdfData";

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Sphere")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_Sphere>();

        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Capsule")]
        private static void CreateSdfObjectInEditor2()
        {
            var caps = CreateSDFObject<RMSdf_Sphere>();
            if (!caps)
                return;

            caps.sphereHeight = 1f;
            caps.sphereRadius = 0.5f;
        }
#endif

        public override string SdfMethodBody =>
            $"{VARCONST_POSITION}.y -= clamp({VARCONST_POSITION}.y, 0.0, {SphereSdfData}.y);\n" +
            $"{VARCONST_RESULT} = length({VARCONST_POSITION}) - {SphereSdfData}.x;";
        
        public override string SdfMethodName => "GetSphereSdf";
        
        public override ISDFEntity.SDFUniformField[] SdfUniformFields =>
            new ISDFEntity.SDFUniformField[1]
            {
                new ISDFEntity.SDFUniformField(SphereSdfData, ISDFEntity.SDFUniformType.Float2)
            };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(SphereSdfData + iterationIndex, new Vector2(Mathf.Abs(sphereRadius), Mathf.Abs(sphereHeight)));
        }
    }
}