using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardUnlitDataBuffer : RMMaterialDataBuffer
    {
        public RMStandardUnlitDataBuffer(in RMMaterialIdentifier materialIdentifier) : base(materialIdentifier)
        {

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RMStandardUnlitData
        {
            public Color colorOverride;
            public float colorBlend;

            public float mainAlbedoOpacity;
            public float mainAlbedoTiling;
            public float mainAlbedoTriplanarBlend;
            public float mainAlbedoIndex;

            public float normalShift;

            public float fresnelCoverage;
            public float fresnelDensity;
            public Color fresnelColor;
        }

        [SerializeField] protected RMStandardUnlitData[] dataUnlitPerInstance;

        public override void InitializeDataContainerPerInstance(in IReadOnlyList<RMMaterialBase> materialInstances, in bool sceneObjectsAreUsingSomeInstances, in bool unpackedDataContainerDirective)
        {
            base.InitializeDataContainerPerInstance(materialInstances, sceneObjectsAreUsingSomeInstances, unpackedDataContainerDirective);
            dataUnlitPerInstance = new RMStandardUnlitData[materialInstances.Count];
        }

        public override (int dataLength, int strideSize) GetComputeBufferLengthAndStride()
            => (dataUnlitPerInstance.Length, sizeof(float) * 16);

        public override Array GetComputeBufferDataContainer
            => dataUnlitPerInstance;

        public override void SyncDataContainerPerInstanceWithMaterialInstance(in int iterationIndex, in RMMaterialBase materialInstance)
        {
            RMStandardUnlitMaterial material = (RMStandardUnlitMaterial)materialInstance;
            RMStandardUnlitData data;

            Color colorOverride = material.colorOverrideSettings.colorOverride;
            colorOverride *= material.colorOverrideSettings.colorIntensity;
            colorOverride.a = material.colorOverrideSettings.colorOverride.a;
            data.colorOverride = colorOverride;
            data.colorBlend = material.colorOverrideSettings.colorBlend * (material.colorOverrideSettings.useColorOverride ? 1 : 0);

            data.mainAlbedoOpacity = material.textureSettings.useTriplanarTexture ? material.textureSettings.mainAlbedoOpacity : 0;
            data.mainAlbedoTiling = material.textureSettings.mainAlbedoTiling;
            data.mainAlbedoTriplanarBlend = material.textureSettings.mainAlbedoTriplanarBlend;
            data.mainAlbedoIndex = GetInstanceTextureIndexFromCachedArray(material.textureSettings.mainAlbedo);

            data.normalShift = material.normalSettings.normalShift;

            int useFresnel = material.fresnelSettings.useFresnel ? 1 : 0;
            data.fresnelCoverage = material.fresnelSettings.fresnelCoverage * useFresnel;
            data.fresnelDensity = material.fresnelSettings.fresnelDensity * useFresnel;
            data.fresnelColor = material.fresnelSettings.fresnelColor;

            dataUnlitPerInstance[iterationIndex] = data;
        }
    }
}