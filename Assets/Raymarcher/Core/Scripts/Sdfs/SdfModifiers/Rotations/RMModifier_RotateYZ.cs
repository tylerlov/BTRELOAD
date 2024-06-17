using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Rotate YZ")]
    public sealed class RMModifier_RotateYZ : RMObjectModifierBase
    {
        [Space]
        public float angleX;

        public override string SdfMethodBody =>
@"
angleX = radians(angleX);
float cosA = cos(angleX);
float sinA = sin(angleX);

p = mul(p, float3x3(
    1.0, 0.0, 0.0,
    0.0, cosA, -sinA,
    0.0, sinA, cosA
    ));
";

        public override string SdfMethodName => "RotateYZModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(nameof(angleX), ISDFEntity.SDFUniformType.Float),
        };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetFloat(nameof(angleX) + iterationIndex, angleX);
        }
    }
}