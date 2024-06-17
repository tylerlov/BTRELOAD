using System.Linq;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardSlopeLitIdentifier : RMStandardLitIdentifier
    {
        public override string MaterialTypeName => "Raymarcher Standard Slope Lit";

        public override MaterialMethodContainer MaterialMainMethod
            => new MaterialMethodContainer("CalculateSlopeLitModel", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_SlopeLitModel").text);

        public override MaterialMethodContainer[] MaterialOtherMethods
        {
            get
            {
                MaterialMethodContainer[] thisMethodContainer = new MaterialMethodContainer[]
                {
                    new MaterialMethodContainer("SlopeLitModelWrapper", Resources.Load<TextAsset>("Models/RMHLSLStdLibMat_SlopeLitModelWrapper").text)
                };

                return base.MaterialOtherMethods.Concat(thisMethodContainer).ToArray();
            }
        }

        public override MaterialUniformField[] MaterialUniformFieldsPerInstance
            => new MaterialUniformField[7]
                {
                    new MaterialUniformField(nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData.litData), new RMStandardLitIdentifier()),
                    new MaterialUniformField(nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData.slopeTextureIndex), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData.slopeCoverage), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData.slopeSmoothness), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData.slopeTextureBlend), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData.slopeTextureScatter), MaterialUniformType.Float),
                    new MaterialUniformField(nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData.slopeTextureEmission), MaterialUniformType.Float)
                };

        public override string MaterialDataContainerTypePerInstance => nameof(RMStandardSlopeLitDataBuffer.RMStandardSlopeLitData);
    }
}