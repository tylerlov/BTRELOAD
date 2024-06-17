using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Morph")]
    public sealed class RMModifier_Morph : RMObjectModifierBase
    {
        [SerializeField] private RMSdfObjectBase targetSdf;
        [SerializeField, HideInInspector] private RMSdfObjectBase cachedTargetSdf;
        [Range(0f, 1f)] public float morphValue = 0.0f;

        public override string SdfMethodBody =>
@"
#ifdef RAYMARCHER_TYPE_QUALITY
    float sdfB = targetSdf[0].x;
#else
    float sdfB = targetSdf.x;
#endif
    sdf = lerp(sdf, sdfB, morphValue);
";

        public override string SdfMethodName => "MorphModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[2]
        {
            new ISDFEntity.SDFUniformField(targetSdf == null ? "1" : targetSdf.GetMyIdentifierFromMappingMaster(), ISDFEntity.SDFUniformType.DefineByRenderType, true, nameof(targetSdf), true),
            new ISDFEntity.SDFUniformField(nameof(morphValue), ISDFEntity.SDFUniformType.Float)
        };

        public override InlineMode ModifierInlineMode() => InlineMode.PostSdfBuffer;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetFloat(nameof(morphValue) + iterationIndex, morphValue);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (cachedTargetSdf != targetSdf)
                SdfTarget.RenderMaster.SetRecompilationRequired(true);
        }

        public override void SdfBufferRecompiled()
        {
            base.SdfBufferRecompiled();
            cachedTargetSdf = targetSdf;
        }
#endif
    }
}