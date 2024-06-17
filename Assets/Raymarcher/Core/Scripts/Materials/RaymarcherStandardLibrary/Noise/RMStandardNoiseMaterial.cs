using System;

using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.Materials.Standard
{
    using static RMAttributes;

    [CreateAssetMenu(fileName = nameof(RMStandardNoiseMaterial), menuName = RMConstants.RM_EDITOR_MATERIAL_PATH + "Standard Volume Noise")]
    public class RMStandardNoiseMaterial : RMMaterialBase
    {
        protected const float ATTEDITOR_BACK_GRAY_OPACITY = 0.55f;
        protected const float ATTEDITOR_BACK_BRIGHT_OPACITY = 0.65f;

        [Space]
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public ColorOverrideSettings colorOverrideSettings = new ColorOverrideSettings();
        [DrawBackgroundPanel(ATTEDITOR_BACK_BRIGHT_OPACITY, ATTEDITOR_BACK_GRAY_OPACITY)]
        public NoiseSettings noiseSettings = new NoiseSettings();

        [Serializable]
        public sealed class ColorOverrideSettings
        {
            public bool useColorOverride = false;
            [ShowIf(nameof(useColorOverride), 1)]
            public Color colorOverride = Color.white;
            [ShowIf(nameof(useColorOverride), 1)]
            [Range(-10f, 10f)] public float colorIntensity = 1f;
            [ShowIf(nameof(useColorOverride), 1)]
            [Range(0f, 1f)] public float colorBlend = 1f;
        }

        [Serializable]
        public sealed class NoiseSettings
        {
            [Range(4, 64)] public float absorptionStep = 12f;
            [Range(0.01f, 5f)] public float volumeStep = 0.05f;
            [Space]
            public float noiseScale = 4f;
            public Vector2 noiseTiling = Vector3.up * 10;
            [Range(0f, 2f)] public float noiseDensity = 0.6f;
            [Range(0f, 100f)] public float noiseSmoothness = 10.0f;
            [Space]
            [Range(0f, 1f)] public float fillOpacity = 0;
            [Range(0f, 10f)] public float depthAbsorption = 1.5f;
            [Space]
            [Dependency("Noise Edge Smoothness", "Use Noise Edge Smoothness", typeof(RMRenderMaster))]
            public EdgeSmoothness edgeSmoothnessType = EdgeSmoothness.None;
            [ShowIf("edgeSmoothnessType", 0, notEquals: true)] public Vector3 edgeSmoothnessWorldPivot = Vector3.zero;
            [ShowIf("edgeSmoothnessType", 0, notEquals: true)][Range(0f, 1f)] public float edgeCoverage = 1f;
            [ShowIf("edgeSmoothnessType", 0, notEquals: true)][Range(0f, 1f)] public float edgeSmoothness = 1f;
            [ShowIf("edgeSmoothnessType", 1)] public float radialEdgeRadius = 1;
            [ShowIf("edgeSmoothnessType", 2)] public Vector3 cubicalEdgeSize = Vector3.one * 5;
            [Space]
            [Dependency("Scene Depth", "Use Scene Depth", typeof(RMRenderMaster))]
            public float sceneDepthOffset = 0;
            [Space]
            [Dependency("Additional Lights", "Use Additional Lights", typeof(RMRenderMaster))]
            public bool includeAdditionalLights = true;

            public enum EdgeSmoothness : int { None = 0, Radial = 1, Cubical = 2 };
        }

        public override RMMaterialDataBuffer MaterialCreateDataBufferInstance()
            => new RMStandardNoiseDataBuffer(new RMStandardNoiseIdentifier());
    }
}