using System;

using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.Materials.Standard
{
    using static RMAttributes;

    [CreateAssetMenu(fileName = nameof(RMStandardMobileSLitMaterial), menuName = RMConstants.RM_EDITOR_MATERIAL_PATH + "Standard Mobile Simple Lit")]
    public class RMStandardMobileSLitMaterial : RMMaterialBase
    {
        protected const float ATTEDITOR_BACK_GRAY_OPACITY = 0.55f;
        protected const float ATTEDITOR_BACK_BRIGHT_OPACITY = 0.65f;

        [Space]
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public ColorOverrideSettings colorOverrideSettings = new ColorOverrideSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public NormalSettings normalSettings = new NormalSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public SpecularSettings specularSettings = new SpecularSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public ShadingSettings shadingSettings = new ShadingSettings();

        [Serializable]
        public sealed class ColorOverrideSettings
        {
            public bool useColorOverride = false;
            [ShowIf(nameof(useColorOverride), 1)]
            public Color colorOverride = Color.white;
            [ShowIf(nameof(useColorOverride), 1)]
            [Range(0f, 1f)] public float colorBlend = 1f;
        }

        [Serializable]
        public sealed class NormalSettings
        {
            [Range(0.001f, 0.5f)] public float normalShift = 0.05f;
        }

        [Serializable]
        public sealed class SpecularSettings
        {
            [Dependency("Use Specular Highlights", "Use Specular", typeof(RMRenderMaster))]
            public bool useSpecular = true;
            [ShowIf(nameof(useSpecular), 1)]
            [Range(0.0f, 10.0f)] public float specularIntensity = 1f;
            [ShowIf(nameof(useSpecular), 1)]
            [Range(0.0f, 1.0f)] public float specularGlossiness = 0.5f;
        }

        [Serializable]
        public sealed class ShadingSettings
        {
            [Dependency("Use Lighting & Shading", "Use Lighting & Shading", typeof(RMRenderMaster))]
            public bool useLambertianShading = true;
            [ShowIf(nameof(useLambertianShading), 1)]
            [Range(0f, 1f)] public float shadingOpacity = 0.5f;
            [ShowIf(nameof(useLambertianShading), 1)]
            [Range(-1f, 1f)] public float shadingCoverage = 0;
            [ShowIf(nameof(useLambertianShading), 1)]
            [Range(0f, 1f)] public float shadingSmoothness = 0.5f;
            [ShowIf(nameof(useLambertianShading), 1)]
            public Color shadingTint = Color.black;
        }

        public override RMMaterialDataBuffer MaterialCreateDataBufferInstance()
            => new RMStandardMobileSLitDataBuffer(new RMStandardMobileSLitIdentifier());

        public override bool UnpackedDataContainersSupported => true;
    }
}