using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardMobileSLitIdentifier : RMMaterialIdentifier
    {
        public override string MaterialTypeName => "Raymarcher Standard Mobile Simple Lit";

        public override string MaterialFamilyName => "Raymarcher Standard Material Library";

        public override Texture2D MaterialEditorIcon => Resources.Load<Texture2D>("RMEditorIcon_RMStandardLibrary");

        public override MaterialMethodContainer MaterialMainMethod
            => new MaterialMethodContainer("CalculateMobileSLitModel", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_MobileSLitModel").text);

        public override MaterialMethodContainer[] MaterialOtherMethods
            => new MaterialMethodContainer[]
            {
                new MaterialMethodContainer("CommonLibrary", Resources.Load<TextAsset>("RMHLSLStdLibMat_Common").text),
                new MaterialMethodContainer("Shading", Resources.Load<TextAsset>("RMHLSLStdLibMat_LambertShading").text),
                new MaterialMethodContainer("CalculateMobileSLitModelWrapper", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_MobileSLitModelWrapper").text)
            };

        public override MaterialGlobalKeywords[] ShaderKeywordFeaturesGlobal
            => new MaterialGlobalKeywords[]
                {
                    new MaterialGlobalKeywords("STANDARD_LIGHTING", true, "Use Lighting & Shading", 20),
                    new MaterialGlobalKeywords("STANDARD_SPECULAR", true, "Use Specular", 10),
                    new MaterialGlobalKeywords("STANDARD_REFRACTION", false, "Use Scene Refraction", 20),
                    new MaterialGlobalKeywords("STANDARD_REFRACTION_MAGNIFY", true, "Magnify Refraction", 0)
                };

        public override int ShaderKeywordFeaturesGlobalPerformanceCost => 50;

        public override MaterialUniformField[] MaterialUniformFieldsPerInstance
            => new MaterialUniformField[]
                {
                    new MaterialUniformField(nameof(RMStandardMobileSLitDataBuffer.RMStandardMobileSLitData.colorOverride), MaterialUniformType.Float4),
                    new MaterialUniformField(nameof(RMStandardMobileSLitDataBuffer.RMStandardMobileSLitData.normalShift), MaterialUniformType.Float),
                    new MaterialUniformField("specularIntensAndGloss", MaterialUniformType.Float2),
                    new MaterialUniformField("shadingCovAndSmh", MaterialUniformType.Float2),
                    new MaterialUniformField(nameof(RMStandardMobileSLitDataBuffer.RMStandardMobileSLitData.shadingTint), MaterialUniformType.Float4),
                };

        public override string MaterialDataContainerTypePerInstance => nameof(RMStandardMobileSLitDataBuffer.RMStandardMobileSLitData);

        public override bool MaterialIsUsingTexturesPerInstance => false;
    }
}