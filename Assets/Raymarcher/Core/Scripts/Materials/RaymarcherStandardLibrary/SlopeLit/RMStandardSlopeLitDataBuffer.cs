using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardSlopeLitDataBuffer : RMStandardLitDataBuffer
    {
        public RMStandardSlopeLitDataBuffer(in RMMaterialIdentifier materialIdentifier) : base(materialIdentifier)
        {
            dataLitGlobal = new RMStandardLitDataGlobal(false);
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RMStandardSlopeLitData
        {
            public RMStandardLitData litData;

            public float slopeTextureIndex;
            public float slopeCoverage;
            public float slopeSmoothness;
            public float slopeTextureBlend;

            public float slopeTextureScatter;
            public float slopeTextureEmission;
        }

        [SerializeField] protected RMStandardSlopeLitData[] dataSlopeLitPerInstance;

        public override void InitializeDataContainerPerInstance(in IReadOnlyList<RMMaterialBase> materialInstances, in bool sceneObjectsAreUsingSomeInstances, in bool unpackedDataContainerDirective)
        {
            base.InitializeDataContainerPerInstance(materialInstances, sceneObjectsAreUsingSomeInstances, unpackedDataContainerDirective);
            dataSlopeLitPerInstance = new RMStandardSlopeLitData[materialInstances.Count];
        }

        public override (int dataLength, int strideSize) GetComputeBufferLengthAndStride()
            => (dataSlopeLitPerInstance.Length, base.GetComputeBufferLengthAndStride().strideSize + sizeof(float) * 6);
        
        public override Array GetComputeBufferDataContainer
            => dataSlopeLitPerInstance;

        public override void SyncDataContainerPerInstanceWithMaterialInstance(in int iterationIndex, in RMMaterialBase materialInstance)
        {
            base.SyncDataContainerPerInstanceWithMaterialInstance(iterationIndex, materialInstance);

            RMStandardSlopeLitMaterial material = (RMStandardSlopeLitMaterial)materialInstance;
            RMStandardSlopeLitData data;

            data.litData = dataLitPerInstance[iterationIndex];
            data.slopeTextureIndex = GetInstanceTextureIndexFromCachedArray(material.slopeSettings.slopeTriplanarTexture);
            data.slopeCoverage = material.slopeSettings.slopeCoverage;
            data.slopeSmoothness = material.slopeSettings.slopeSmoothness;
            data.slopeTextureBlend = material.slopeSettings.slopeTextureBlend;
            data.slopeTextureScatter = material.slopeSettings.slopeTextureScatter;
            data.slopeTextureEmission = material.slopeSettings.slopeTextureEmission;

            dataSlopeLitPerInstance[iterationIndex] = data;
        }
    }
}
