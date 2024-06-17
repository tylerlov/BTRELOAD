using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Rotate XY")]
    public sealed class RMModifier_RotateXY : RMObjectModifierBase
    {
        [Space]
        public float angleZ;

        public override string SdfMethodBody =>
@"
angleZ = radians(angleZ);
float cosA = cos(angleZ);
float sinA = sin(angleZ);

p = mul(p, float3x3(
    cosA, -sinA, 0.0,
    sinA, cosA, 0.0,
    0.0, 0.0, 1.0
    ));
";

        public override string SdfMethodName => "RotateXYModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(nameof(angleZ), ISDFEntity.SDFUniformType.Float),
        };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetFloat(nameof(angleZ) + iterationIndex, angleZ);
        }
    }
}