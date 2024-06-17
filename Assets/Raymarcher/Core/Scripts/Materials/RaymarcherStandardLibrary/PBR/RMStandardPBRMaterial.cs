using System;

using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.Materials.Standard
{
    using static RMAttributes;

    [CreateAssetMenu(fileName = nameof(RMStandardPBRMaterial), menuName = RMConstants.RM_EDITOR_MATERIAL_PATH + "Standard PBR")]
    public class RMStandardPBRMaterial : RMStandardLitMaterial
    {
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public ReflectionSettings reflectionSettings = new ReflectionSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public RefractionSettings refractionSettings = new RefractionSettings();

        [Serializable]
        public sealed class ReflectionSettings
        {
            [Dependency("Use Reflection", "Use Reflection", typeof(RMRenderMaster))]
            public bool useReflection = true;
            [ShowIf(nameof(useReflection), 1)]
            [Range(0, 2)] public float reflectionIntensity = 1;
            [ShowIf(nameof(useReflection), 1)]
            [Range(0, 0.25f)] public float reflectionJitter = 0;
        }

        [Serializable]
        public sealed class RefractionSettings
        {
            [Dependency("Use Scene Refraction", "Use Scene Refraction", typeof(RMRenderMaster))]
            public bool useRefraction = false;
            [ShowIf(nameof(useRefraction), 1)]
            [Range(0f, 1f)] public float refractionOpacity = 0.8f;
            [ShowIf(nameof(useRefraction), 1)]
            [Range(0f, 1f)] public float refractionIntensity = 0.02f;
            [ShowIf(nameof(useRefraction), 1)]
            [Range(0f, 1f)] public float refractionDensity = 0.5f;
            [ShowIf(nameof(useRefraction), 1)]
            public bool refractionInverseDensity = false;
        }

        public override RMMaterialDataBuffer MaterialCreateDataBufferInstance()
            => new RMStandardPBRDataBuffer(new RMStandardPBRIdentifier());
    }
}