using System;

using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.Materials.Standard
{
    using static RMAttributes;

    [CreateAssetMenu(fileName = nameof(RMStandardSlopeLitMaterial), menuName = RMConstants.RM_EDITOR_MATERIAL_PATH + "Standard Slope Lit")]
    public class RMStandardSlopeLitMaterial : RMStandardLitMaterial
    {
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public SlopeSettings slopeSettings = new SlopeSettings();

        [Serializable]
        public sealed class SlopeSettings
        {
            [Dependency("Use Triplanar Texture", "Use Triplanar Texture", typeof(RMRenderMaster))]
            public Texture2D slopeTriplanarTexture;
            [Range(-1f, 1f)] public float slopeCoverage = 0.5f;
            [Range(0f, 1f)] public float slopeSmoothness = 0.2f;
            [Range(0f, 30f)] public float slopeTextureBlend = 0.5f;
            [Space]
            [Range(0.001f, 10f)] public float slopeTextureScatter = 1f;
            [Range(0f, 100f)] public float slopeTextureEmission = 0f;
        }
       
        public override RMMaterialDataBuffer MaterialCreateDataBufferInstance()
            => new RMStandardSlopeLitDataBuffer(new RMStandardSlopeLitIdentifier());

        public override Texture2D[] MaterialTexturesPerInstance
            => new Texture2D[2] { textureSettings.mainAlbedo, slopeSettings.slopeTriplanarTexture };
    }
}