using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardNoiseIdentifier : RMMaterialIdentifier
    {
        public override string MaterialTypeName => "Raymarcher Standard Volume Noise";

        public override Texture2D MaterialEditorIcon => Resources.Load<Texture2D>("RMEditorIcon_RMStandardNoise");

        public override MaterialMethodContainer MaterialMainMethod
            => new MaterialMethodContainer("CalculateVolNoiseModel", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_VolNoiseModel").text);

        public override MaterialMethodContainer[] MaterialOtherMethods
            => new MaterialMethodContainer[]
            {
                new MaterialMethodContainer("VolNoiseModelWrapper", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_VolNoiseModelWrapper").text)
            };

        public override MaterialGlobalKeywords[] ShaderKeywordFeaturesGlobal
            => new MaterialGlobalKeywords[]
            {
                new MaterialGlobalKeywords("STANDARD_NOISE_EDGESMOOTHNESS", false, "Use Noise Edge Smoothness")
            };

        public override MaterialUniformField[] MaterialUniformFieldsPerInstance
            => new MaterialUniformField[]
                {
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.useColorOverride), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.colorOverride), MaterialUniformType.Float4),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.colorBlend), MaterialUniformType.Float),

                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.absorptionStep), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.volumeStep), MaterialUniformType.Float),

                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.noiseScale), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.noiseTiling), MaterialUniformType.Float2),

                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.fillOpacity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.noiseDensity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.noiseSmoothness), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.depthAbsorption), MaterialUniformType.Float),

                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.edgeSmoothnessWorldPivot), MaterialUniformType.Float3),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.edgeCoverage), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.edgeSmoothness), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.edgeSize), MaterialUniformType.Float4),

                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.sceneDepthOffset), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData.includeAdditionalLights), MaterialUniformType.Float)
                };

        public override string MaterialDataContainerTypePerInstance => nameof(RMStandardNoiseDataBuffer.RMStandardNoiseData);

        public override bool MaterialIsUsingTexturesPerInstance => false;
    }
}