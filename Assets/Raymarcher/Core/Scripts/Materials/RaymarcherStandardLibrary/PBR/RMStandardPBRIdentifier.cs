using System.Linq;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardPBRIdentifier : RMStandardLitIdentifier
    {
        public override string MaterialTypeName => "Raymarcher Standard PBR";

        public override MaterialMethodContainer MaterialMainMethod
           => new MaterialMethodContainer("CalculatePBRModel", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_PBRModel").text);

        public override MaterialMethodContainer[] MaterialOtherMethods
        {
            get
            {
                MaterialMethodContainer[] thisMethodContainer = new MaterialMethodContainer[]
                {
                    new MaterialMethodContainer("SceneRefraction", Resources.Load<TextAsset>("RMHLSLStdLibMat_SceneRefraction").text),
                    new MaterialMethodContainer("PBRModelWrapper", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_PBRModelWrapper").text)
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
                    new MaterialGlobalKeywords("STANDARD_REFLECTION", false, "Use Reflection", 80),
                    new MaterialGlobalKeywords("STANDARD_REFRACTION", false, "Use Scene Refraction", 20),
                    new MaterialGlobalKeywords("STANDARD_REFRACTION_MAGNIFY", true, "Magnify Refraction", 0),
                    new MaterialGlobalKeywords("STANDARD_RESPECT_DENSITY", false, "Refraction Respect Density", 40)
                };

                return base.ShaderKeywordFeaturesGlobal.Concat(thisGlobalKeywords).ToArray();
            }
        }

        public override int ShaderKeywordFeaturesGlobalPerformanceCost => 300;

        public override MaterialUniformField[] MaterialUniformFieldsPerInstance
            => new MaterialUniformField[7]
                {
                    new MaterialUniformField(nameof(RMStandardPBRDataBuffer.RMStandardPBRData.litData), new RMStandardLitIdentifier()),
                    new MaterialUniformField(nameof(RMStandardPBRDataBuffer.RMStandardPBRData.reflectionIntensity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardPBRDataBuffer.RMStandardPBRData.reflectionJitter), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardPBRDataBuffer.RMStandardPBRData.refractionOpacity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardPBRDataBuffer.RMStandardPBRData.refractionIntensity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardPBRDataBuffer.RMStandardPBRData.refractionDensity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardPBRDataBuffer.RMStandardPBRData.refractionInverse), MaterialUniformType.Float)
                };

        public override string MaterialDataContainerTypePerInstance => nameof(RMStandardPBRDataBuffer.RMStandardPBRData);

        public override MaterialMethodContainer? PostMaterialMainMethod
            => new MaterialMethodContainer("CalculateReflections", Resources.Load<TextAsset>("RMHLSLStdLibMat_Reflections").text);

        public override bool PostMaterialMainMethodGlobalSupported => true;
    }
}