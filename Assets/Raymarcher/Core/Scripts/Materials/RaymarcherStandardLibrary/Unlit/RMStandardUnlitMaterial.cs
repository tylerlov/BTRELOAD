using System;

using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.Materials.Standard
{
    using static RMAttributes;

    [CreateAssetMenu(fileName = nameof(RMStandardUnlitMaterial), menuName = RMConstants.RM_EDITOR_MATERIAL_PATH + "Standard Unlit")]
    public class RMStandardUnlitMaterial : RMMaterialBase
    {
        protected const float ATTEDITOR_BACK_GRAY_OPACITY = 0.55f;
        protected const float ATTEDITOR_BACK_BRIGHT_OPACITY = 0.65f;

        [Space]
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public ColorOverrideSettings colorOverrideSettings = new ColorOverrideSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public TextureSettings textureSettings = new TextureSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public NormalSettings normalSettings = new NormalSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public FresnelSettings fresnelSettings = new FresnelSettings();

        [Serializable]
        public sealed class ColorOverrideSettings
        {
            public bool useColorOverride = false;
            [ShowIf(nameof(useColorOverride), 1)]
            public Color colorOverride = Color.gray;
            [ShowIf(nameof(useColorOverride), 1)]
            [Range(-10f, 10f)] public float colorIntensity = 1f;
            [ShowIf(nameof(useColorOverride), 1)]
            [Range(0f, 1f)] public float colorBlend = 1f;
        }

        [Serializable]
        public sealed class TextureSettings
        {
            [Dependency("Use Triplanar Texture", "Use Triplanar Texture", typeof(RMRenderMaster))]
            public bool useTriplanarTexture = false;
            [ShowIf(nameof(useTriplanarTexture), 1)]
            public Texture2D mainAlbedo;
            [ShowIf(nameof(useTriplanarTexture), 1)]
            [Range(0f, 100f)] public float mainAlbedoTiling = 0.2f;
            [ShowIf(nameof(useTriplanarTexture), 1)]
            [Range(0f, 30f)] public float mainAlbedoTriplanarBlend = 15f;
            [ShowIf(nameof(useTriplanarTexture), 1)]
            [Range(0f, 1f)] public float mainAlbedoOpacity = 0.5f;
        }

        [Serializable]
        public sealed class NormalSettings
        {
            [Range(1.0e-5f, 0.5f)] public float normalShift = 0.05f;
        }

        [Serializable]
        public sealed class FresnelSettings
        {
            [Dependency("Use Fresnel Effect", "Use Fresnel Effect", typeof(RMRenderMaster))]
            public bool useFresnel = false;
            [ShowIf(nameof(useFresnel), 1)]
            [Range(0f, 1f)] public float fresnelCoverage = 0.5f;
            [ShowIf(nameof(useFresnel), 1)]
            [Range(0f, 100f)] public float fresnelDensity = 32.0f;
            [ShowIf(nameof(useFresnel), 1)]
            public Color fresnelColor = Color.cyan;
        }

        public override RMMaterialDataBuffer MaterialCreateDataBufferInstance()
            => new RMStandardUnlitDataBuffer(new RMStandardUnlitIdentifier());

        public override Texture2D[] MaterialTexturesPerInstance
            => new Texture2D[1] { textureSettings.mainAlbedo };
    }
}