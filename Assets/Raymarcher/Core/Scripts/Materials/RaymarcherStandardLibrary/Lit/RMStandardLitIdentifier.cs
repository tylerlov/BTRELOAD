using System.Linq;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardLitIdentifier : RMStandardUnlitIdentifier
    {
        public override string MaterialTypeName => "Raymarcher Standard Lit";

        public override MaterialMethodContainer MaterialMainMethod
            => new MaterialMethodContainer("CalculateLitModel", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_LitModel").text);

        public override MaterialMethodContainer[] MaterialOtherMethods
        {
            get
            {
                MaterialMethodContainer[] thisMethodContainer = new MaterialMethodContainer[]
                {
                    new MaterialMethodContainer("Shadows", Resources.Load<TextAsset>("RMHLSLStdLibMat_Shadows").text),
                    new MaterialMethodContainer("Shading", Resources.Load<TextAsset>("RMHLSLStdLibMat_LambertShading").text),
                    new MaterialMethodContainer("Lighting", Resources.Load<TextAsset>("RMHLSLStdLibMat_Lighting").text),
                    new MaterialMethodContainer("LitModelWrapper", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_LitModelWrapper").text)
                };

                return base.MaterialOtherMethods.Concat(thisMethodContainer).ToArray();
            }
        }

        public override MaterialGlobalKeywords[] ShaderKeywordFeaturesGlobal
        {
            get
            {
                MaterialGlobalKeywords[] thisGlobalKeywords = new MaterialGlobalKeywords[]
                {
                    new MaterialGlobalKeywords("STANDARD_LIGHTING", true, "Use Lighting & Shading", 40),
                    new MaterialGlobalKeywords("STANDARD_ADDITIONAL_LIGHTS_LINEAR_ATTENUATION", false, "Add Lights Linear Attenuation", 5),
                    new MaterialGlobalKeywords("STANDARD_SAMPLE_TRANSLUCENCY", true, "Sample Translucency", 20),
                    new MaterialGlobalKeywords("STANDARD_SPECULAR", true, "Use Specular", 10),
                    new MaterialGlobalKeywords("STANDARD_SHADOWS", true, "Use Shadows", 50),
                    new MaterialGlobalKeywords("STANDARD_SHADOWS_SOFT", true, "Use Soft Shadows", 10)
                };

                return base.ShaderKeywordFeaturesGlobal.Concat(thisGlobalKeywords).ToArray();
            }
        }

        public override int ShaderKeywordFeaturesGlobalPerformanceCost => 200;

        public const string GLOBAL_FIELD_SHADOW_QUALITY = "_StandardShadowQuality";
        public const string GLOBAL_FIELD_SHADOW_SOFTNESS = "_StandardShadowSoftness";

        public override MaterialUniformField[] MaterialUniformFieldsGlobal
            => new MaterialUniformField[2]
                {
                    new MaterialUniformField(GLOBAL_FIELD_SHADOW_QUALITY, MaterialUniformType.Float, true),
                    new MaterialUniformField(GLOBAL_FIELD_SHADOW_SOFTNESS, MaterialUniformType.Float, true)
                };

        public override MaterialUniformField[] MaterialUniformFieldsPerInstance
            => new MaterialUniformField[14]
                {
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.unlitData), new RMStandardUnlitIdentifier()),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.specularIntensity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.specularSize), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.specularGlossiness), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.shadingCoverage), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.shadingSmoothness), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.shadingTint), MaterialUniformType.Float3),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.useShadows), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.shadowDistanceMinMax), MaterialUniformType.Float2),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.shadowAmbience), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.includeDirectionalLight), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.includeAdditionalLights), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.translucencyMinAbsorption), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardLitDataBuffer.RMStandardLitData.translucencyMaxAbsorption), MaterialUniformType.Float)
                };

        public override string MaterialDataContainerTypePerInstance => nameof(RMStandardLitDataBuffer.RMStandardLitData);

        public override (string containerType, string displayName) MaterialDataContainerTypeGlobal => ("dataLitGlobal", "Global Lit Material Settings");
    }
}