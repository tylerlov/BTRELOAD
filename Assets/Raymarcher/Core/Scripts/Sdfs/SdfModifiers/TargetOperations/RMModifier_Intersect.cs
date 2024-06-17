using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Intersect")]
    public sealed class RMModifier_Intersect : RMObjectModifierBase
    {
        [SerializeField] private RMSdfObjectBase targetSdf;
        [SerializeField, HideInInspector] private RMSdfObjectBase cachedTargetSdf;
        public float intersectSmoothness = 0.25f;
        public bool includeMaterial = true;

        public override string SdfMethodBody =>
@"
#ifdef RAYMARCHER_TYPE_QUALITY
    float sdfB = targetSdf[0].x;
    half3 colorB = targetSdf[0].gba;
    half materialTypeB = targetSdf[1].x;
    half materialInstanceB = targetSdf[1].y;
#elif defined(RAYMARCHER_TYPE_STANDARD)
    float sdfB = targetSdf.x;
    half3 colorB = targetSdf.y;
    half materialTypeB = targetSdf.z;
    half materialInstanceB = targetSdf.w;
#else
    float sdfB = targetSdf.x;
    half3 colorB = targetSdf.y;
#endif
    intersectSmoothness = abs(intersectSmoothness);
// Credits for the original formula: Inigo Quilez
// https://iquilezles.org/articles/distfunctions/
    float h = saturate(0.5 - 0.5 * (sdfB - sdf) / max(EPSILON, intersectSmoothness));
    sdf = lerp(sdfB, sdf, h ) + intersectSmoothness * h * (1.0 - h);
    color = lerp(colorB, color, h * includeMaterial);
#ifndef RAYMARCHER_TYPE_PERFORMANT
    materialType = lerp(materialTypeB, materialType, h);
    materialInstance = lerp(materialInstanceB, materialInstance, h * includeMaterial);
#endif
";

        public override string SdfMethodName => "IntersectionModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[3]
        {
            new ISDFEntity.SDFUniformField(targetSdf == null ? "1" : targetSdf.GetMyIdentifierFromMappingMaster(), ISDFEntity.SDFUniformType.DefineByRenderType, true, nameof(targetSdf), true),
            new ISDFEntity.SDFUniformField(nameof(intersectSmoothness), ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(nameof(includeMaterial), ISDFEntity.SDFUniformType.Float)
        };

        public override InlineMode ModifierInlineMode() => InlineMode.PostSdfBuffer;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetFloat(nameof(intersectSmoothness) + iterationIndex, intersectSmoothness);
            raymarcherSceneMaterial.SetFloat(nameof(includeMaterial) + iterationIndex, includeMaterial ? 1 : 0);
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
