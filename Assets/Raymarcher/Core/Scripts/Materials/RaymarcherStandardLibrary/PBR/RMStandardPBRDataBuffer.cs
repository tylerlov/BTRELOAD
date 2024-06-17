using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardPBRDataBuffer : RMStandardLitDataBuffer
    {
        public RMStandardPBRDataBuffer(in RMMaterialIdentifier materialIdentifier) : base(materialIdentifier)
        {

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RMStandardPBRData
        {
            public RMStandardLitData litData;

            public float reflectionIntensity;
            public float reflectionJitter;
            public float refractionOpacity;
            public float refractionIntensity;
            public float refractionDensity;
            public float refractionInverse;
        }

        [SerializeField] protected RMStandardPBRData[] dataPBRPerInstance;

        public override void InitializeDataContainerPerInstance(in IReadOnlyList<RMMaterialBase> materialInstances, in bool sceneObjectsAreUsingSomeInstances, in bool unpackedDataContainerDirective)
        {
            base.InitializeDataContainerPerInstance(materialInstances, sceneObjectsAreUsingSomeInstances, unpackedDataContainerDirective);
            dataPBRPerInstance = new RMStandardPBRData[materialInstances.Count];
        }

        public override (int dataLength, int strideSize) GetComputeBufferLengthAndStride()
            => (dataPBRPerInstance.Length, base.GetComputeBufferLengthAndStride().strideSize + sizeof(float) * 6);

        public override Array GetComputeBufferDataContainer
            => dataPBRPerInstance;

        public override void SyncDataContainerPerInstanceWithMaterialInstance(in int iterationIndex, in RMMaterialBase materialInstance)
        {
            base.SyncDataContainerPerInstanceWithMaterialInstance(iterationIndex, materialInstance);

            RMStandardPBRMaterial material = (RMStandardPBRMaterial)materialInstance;
            RMStandardPBRData data;

            data.litData = dataLitPerInstance[iterationIndex];

            data.reflectionIntensity = material.reflectionSettings.useReflection ? material.reflectionSettings.reflectionIntensity : 0;
            data.reflectionJitter = material.reflectionSettings.reflectionJitter;
            data.refractionOpacity = material.refractionSettings.useRefraction ? material.refractionSettings.refractionOpacity : 0;
            data.refractionDensity = material.refractionSettings.refractionDensity;
            data.refractionIntensity = material.refractionSettings.refractionIntensity;
            data.refractionInverse = material.refractionSettings.refractionInverseDensity ? 1 : 0;

            dataPBRPerInstance[iterationIndex] = data;
        }
    }
}