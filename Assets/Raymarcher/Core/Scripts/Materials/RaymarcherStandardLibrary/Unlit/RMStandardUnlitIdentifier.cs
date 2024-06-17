using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardUnlitIdentifier : RMMaterialIdentifier
    {
        public override string MaterialTypeName => "Raymarcher Standard Unlit";

        public override string MaterialFamilyName => "Raymarcher Standard Material Library";

        public override Texture2D MaterialEditorIcon => Resources.Load<Texture2D>("RMEditorIcon_RMStandardLibrary");

        public override MaterialMethodContainer MaterialMainMethod
            => new MaterialMethodContainer("CalculateUnlitModel", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_UnlitModel").text);

        public override MaterialMethodContainer[] MaterialOtherMethods
            => new MaterialMethodContainer[]
            {
                new MaterialMethodContainer("CommonLibrary", Resources.Load<TextAsset>("RMHLSLStdLibMat_Common").text),
                new MaterialMethodContainer("CalculateUnlitModelWrapper", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_UnlitModelWrapper").text)
            };

        public override MaterialGlobalKeywords[] ShaderKeywordFeaturesGlobal
            => new MaterialGlobalKeywords[]
                {
                    new MaterialGlobalKeywords("STANDARD_TRIPLANAR_TEXTURE", true, "Use Triplanar Texture", 10),
                    new MaterialGlobalKeywords("STANDARD_FRESNEL", false, "Use Fresnel Effect", 20)
                };

        public override int ShaderKeywordFeaturesGlobalPerformanceCost => 50;

        public override MaterialUniformField[] MaterialUniformFieldsPerInstance
            => new MaterialUniformField[]
                {
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.colorOverride), MaterialUniformType.Float4),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.colorBlend), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.mainAlbedoOpacity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.mainAlbedoTiling), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.mainAlbedoTriplanarBlend), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.mainAlbedoIndex), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.normalShift), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.fresnelCoverage), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.fresnelDensity), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData.fresnelColor), MaterialUniformType.Float4)
                };

        public override string MaterialDataContainerTypePerInstance => nameof(RMStandardUnlitDataBuffer.RMStandardUnlitData);

        public override bool MaterialIsUsingTexturesPerInstance => true;
    }
}