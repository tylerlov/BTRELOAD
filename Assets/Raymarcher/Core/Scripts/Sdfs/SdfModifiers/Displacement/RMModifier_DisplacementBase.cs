using System;

using UnityEngine;

namespace Raymarcher.Objects.Modifiers
{
    [Serializable]
    public abstract class RMModifier_DisplacementBase : RMObjectModifierBase
    {
        [Space]
        [SerializeField] private DisplacementModifierData displaceModifier;

        public DisplacementModifierData DisplaceModifier => displaceModifier;

        [Serializable]
        public sealed class DisplacementModifierData
        {
            public Texture2D displaceTexture;
            public float textureTiling = 0.2f;
            [Range(-2, 2)] public float displacementAmount = 0.05f;
        }

        public override string SdfMethodBody => "This must be overrided in the nested class!";
        public override string SdfMethodName => "Same here";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields
            => new ISDFEntity.SDFUniformField[3]
            {
                new ISDFEntity.SDFUniformField(nameof(DisplacementModifierData.displaceTexture), ISDFEntity.SDFUniformType.Sampler2D),
                new ISDFEntity.SDFUniformField(nameof(DisplacementModifierData.textureTiling), ISDFEntity.SDFUniformType.Float),
                new ISDFEntity.SDFUniformField(nameof(DisplacementModifierData.displacementAmount), ISDFEntity.SDFUniformType.Float)
            };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override object CreateSharedModifierContainer => new DisplacementModifierData();

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetTexture(nameof(DisplacementModifierData.displaceTexture) + iterationIndex, DisplaceModifier.displaceTexture);
            raymarcherSceneMaterial.SetFloat(nameof(DisplacementModifierData.textureTiling) + iterationIndex, DisplaceModifier.textureTiling);
            raymarcherSceneMaterial.SetFloat(nameof(DisplacementModifierData.displacementAmount) + iterationIndex, DisplaceModifier.displacementAmount);
        }

        public override bool ModifierSupportsSharedContainer => true;

        public override void PassModifierDataToSharedContainer()
        {
            if (!TryToGetSharedModifierContainerType<DisplacementModifierData>(out var data))
                return;

            data.displacementAmount = DisplaceModifier.displacementAmount;
            data.displaceTexture = DisplaceModifier.displaceTexture;
            data.textureTiling = DisplaceModifier.textureTiling;

            base.PassModifierDataToSharedContainer();
        }

        public override void PassSharedContainerDataToModifier()
        {
            if (!TryToGetSharedModifierContainerType<DisplacementModifierData>(out var data))
                return;

            DisplaceModifier.displacementAmount = data.displacementAmount;
            DisplaceModifier.displaceTexture = data.displaceTexture;
            DisplaceModifier.textureTiling = data.textureTiling;
        }
    }
}