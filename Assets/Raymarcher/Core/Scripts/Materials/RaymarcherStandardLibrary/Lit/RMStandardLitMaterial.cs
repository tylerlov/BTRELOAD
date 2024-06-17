using System;

using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.Materials.Standard
{
    using static RMAttributes;

    [CreateAssetMenu(fileName = nameof(RMStandardLitMaterial), menuName = RMConstants.RM_EDITOR_MATERIAL_PATH + "Standard Lit")]
    public class RMStandardLitMaterial : RMStandardUnlitMaterial
    {
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public SpecularSettings specularSettings = new SpecularSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public ShadingSettings shadingSettings = new ShadingSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public LightingSettings lightingSettings = new LightingSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public ShadowSettings shadowSettings = new ShadowSettings();

        [Serializable]
        public sealed class SpecularSettings
        {
            [Dependency("Use Specular Highlights", "Use Specular", typeof(RMRenderMaster))]
            public bool useSpecular = true;
            [ShowIf(nameof(useSpecular), 1)]
            [Range(0.0f, 50.0f)] public float specularIntensity = 1f;
            [ShowIf(nameof(useSpecular), 1)]
            [Range(0.0f, 1.0f)] public float specularSize = 0.5f;
            [ShowIf(nameof(useSpecular), 1)]
            [Range(0.0f, 1.0f)] public float specularGlossiness = 0.5f;
        }

        [Serializable]
        public sealed class ShadingSettings
        {
            [Dependency("Use Lighting & Shading", "Use Lighting & Shading", typeof(RMRenderMaster))]
            public bool useLambertianShading = true;
            [ShowIf(nameof(useLambertianShading), 1)]
            [Range(0f, 1f)] public float shadingOpacity = 1.0f;
            [ShowIf(nameof(useLambertianShading), 1)]
            [Range(-1f, 1f)] public float shadingCoverage = 0;
            [ShowIf(nameof(useLambertianShading), 1)]
            [Range(0f, 1f)] public float shadingSmoothness = 0.5f;
            [ShowIf(nameof(useLambertianShading), 1)]
            public Color shadingTint = Color.white * 0.1f;
        }

        [Serializable]
        public sealed class LightingSettings
        {
            [Dependency("Main Directional Light", "Use Main Directional Light", typeof(RMRenderMaster))]
            public bool includeDirectionalLight = true;
            [Dependency("Additional Lights", "Use Additional Lights", typeof(RMRenderMaster))]
            public bool includeAdditionalLights = true;

            [Header("Light Translucency")]
            [Dependency("Sample Translucency", "Sample Translucency", typeof(RMRenderMaster), true)]
            [Range(-1f, 1f)] public float translucencyMinAbsorption = -1.0f;
            [Range(-1f, 1f)] public float translucencyMaxAbsorption = -1.0f;
        }

        [Serializable]
        public sealed class ShadowSettings
        {
            [Dependency("Use Shadows", "Use Shadows", typeof(RMRenderMaster))]
            public bool useRaymarcherShadows = true;
            [ShowIf(nameof(useRaymarcherShadows), 1)]
            public Vector2 shadowDistanceMinMax = new Vector2(0.1f, 10f);
            [ShowIf(nameof(useRaymarcherShadows), 1)]
            [Range(1f, -1f)] public float shadowAmbience = 0.2f;
        }
        public override RMMaterialDataBuffer MaterialCreateDataBufferInstance()
            => new RMStandardLitDataBuffer(new RMStandardLitIdentifier());
    }
}