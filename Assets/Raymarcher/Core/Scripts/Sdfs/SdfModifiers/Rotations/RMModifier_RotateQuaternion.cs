using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Rotate Quaternion")]
    public sealed class RMModifier_RotateQuaternion : RMObjectModifierBase
    {
        private const string PARAM = "quaternion";

        public override string SdfMethodBody =>
@"
float x = quaternion.x;
float y = quaternion.y;
float z = quaternion.z;
float w = quaternion.w;

p = mul(float4(p, 1), float4x4(
    1.0 - 2.0 * (y * y + z * z), 2.0 * (x * y - w * z), 2.0 * (x * z + w * y), 0.0,
    2.0 * (x * y + w * z), 1.0 - 2.0 * (x * x + z * z), 2.0 * (y * z - w * x), 0.0,
    2.0 * (x * z - w * y), 2.0 * (y * z + w * x), 1.0 - 2.0 * (x * x + y * y), 0.0,
    0.0, 0.0, 0.0, 1.0
));
";

        public override string SdfMethodName => "RotateQuaternionModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(PARAM, ISDFEntity.SDFUniformType.Float4),
        };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            Vector4 quaToVec = Vector4.zero;
            Quaternion qua = transform.rotation;
            quaToVec.x = qua.x;
            quaToVec.y = qua.y;
            quaToVec.z = qua.z;
            quaToVec.w = qua.w;
            raymarcherSceneMaterial.SetVector(PARAM + iterationIndex, quaToVec);
        }
    }
}