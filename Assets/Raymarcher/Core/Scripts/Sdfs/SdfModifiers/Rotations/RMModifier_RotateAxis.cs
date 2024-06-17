using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Rotate Axis")]
    public sealed class RMModifier_RotateAxis : RMObjectModifierBase
    {
        [Space]
        public Vector3 rotationAxis = Vector3.up;
        public float angleAxis;

        public override string SdfMethodBody =>
@"
// Rodrigue's rotation https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
half3 ra = normalize(rotationAxis);
angleAxis = radians(angleAxis);
p = cos(angleAxis) * p + cross(p, ra) * sin(angleAxis) + dot(ra, p) * ra * (1. - cos(angleAxis));
";

        public override string SdfMethodName => "RotateAxisModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(nameof(rotationAxis), ISDFEntity.SDFUniformType.Float3),
            new ISDFEntity.SDFUniformField(nameof(angleAxis), ISDFEntity.SDFUniformType.Float),
        };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetFloat(nameof(angleAxis) + iterationIndex, angleAxis);
            raymarcherSceneMaterial.SetVector(nameof(rotationAxis) + iterationIndex, rotationAxis == Vector3.zero ? Vector3.up : rotationAxis);
        }
    }
}