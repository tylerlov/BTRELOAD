using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Deform")]
    public sealed class RMModifier_LinearDeform : RMObjectModifierBase
    {
        [Space]
        public float deformAmplifier = 1;
        public float deformStep = 0.5f;
        public float deformSmoothness = 0.1f;

        private const string DEFORM_DATA = "DeformData";

        public override string SdfMethodBody =>
            $"{VARCONST_POSITION}.xz *= lerp(1., {DEFORM_DATA}.z, smoothstep({DEFORM_DATA}.x - {DEFORM_DATA}.y, {DEFORM_DATA}.x + {DEFORM_DATA}.y, {VARCONST_POSITION}.y));";
        public override string SdfMethodName => "DeformModifier";

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(DEFORM_DATA, ISDFEntity.SDFUniformType.Float3)
        };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(DEFORM_DATA + iterationIndex, new Vector3(deformStep, deformSmoothness, deformAmplifier));
        }
    }
}