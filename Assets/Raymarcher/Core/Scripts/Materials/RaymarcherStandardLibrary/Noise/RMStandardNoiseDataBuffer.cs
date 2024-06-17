using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardNoiseDataBuffer : RMMaterialDataBuffer
    {
        public RMStandardNoiseDataBuffer(in RMMaterialIdentifier materialIdentifier) : base(materialIdentifier)
        {

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RMStandardNoiseData
        {
            public float useColorOverride;
            public Color colorOverride;
            public float colorBlend;

            public float absorptionStep;
            public float volumeStep;

            public float noiseScale;
            public Vector2 noiseTiling;

            public float fillOpacity;
            public float noiseDensity;
            public float noiseSmoothness;
            public float depthAbsorption;

            public Vector3 edgeSmoothnessWorldPivot;
            public float edgeCoverage;
            public float edgeSmoothness;
            public Vector4 edgeSize;

            public float sceneDepthOffset;

            public float includeAdditionalLights;
        }

        [SerializeField] protected RMStandardNoiseData[] dataFogPerInstance;

        public override void InitializeDataContainerPerInstance(in IReadOnlyList<RMMaterialBase> materialInstances, in bool sceneObjectsAreUsingSomeInstances, in bool unpackedDataContainerDirective)
        {
            base.InitializeDataContainerPerInstance(materialInstances, sceneObjectsAreUsingSomeInstances, unpackedDataContainerDirective);
            dataFogPerInstance = new RMStandardNoiseData[materialInstances.Count];
        }

        public override (int dataLength, int strideSize) GetComputeBufferLengthAndStride()
            => (dataFogPerInstance.Length, sizeof(float) * 26);

        public override Array GetComputeBufferDataContainer
            => dataFogPerInstance;

        public override void SyncDataContainerPerInstanceWithMaterialInstance(in int iterationIndex, in RMMaterialBase materialInstance)
        {
            RMStandardNoiseMaterial material = (RMStandardNoiseMaterial)materialInstance;
            RMStandardNoiseData data;

            data.useColorOverride = material.colorOverrideSettings.useColorOverride ? 1 : 0;
            data.colorBlend = material.colorOverrideSettings.colorBlend;

            Color colorOverride = material.colorOverrideSettings.colorOverride;
            colorOverride *= material.colorOverrideSettings.colorIntensity;
            colorOverride.a = material.colorOverrideSettings.colorOverride.a;
            data.colorOverride = colorOverride;

            data.absorptionStep = material.noiseSettings.absorptionStep;
            data.volumeStep = material.noiseSettings.volumeStep;

            data.noiseScale = material.noiseSettings.noiseScale;
            data.noiseTiling = material.noiseSettings.noiseTiling;

            data.fillOpacity = material.noiseSettings.fillOpacity;
            data.noiseDensity = material.noiseSettings.noiseDensity;
            data.noiseSmoothness = material.noiseSettings.noiseSmoothness;
            data.depthAbsorption = material.noiseSettings.depthAbsorption;

            bool notNode = material.noiseSettings.edgeSmoothnessType != RMStandardNoiseMaterial.NoiseSettings.EdgeSmoothness.None;
            data.edgeCoverage = material.noiseSettings.edgeCoverage * (notNode ? 1 : 0);

            Vector4 eSize = Vector4.zero;
            switch (material.noiseSettings.edgeSmoothnessType)
            {
                case RMStandardNoiseMaterial.NoiseSettings.EdgeSmoothness.Radial:
                    eSize = Vector3.one * material.noiseSettings.radialEdgeRadius;
                    break;

                case RMStandardNoiseMaterial.NoiseSettings.EdgeSmoothness.Cubical:
                    eSize = material.noiseSettings.cubicalEdgeSize;
                    break;
            }
            eSize.w = (int)material.noiseSettings.edgeSmoothnessType;
            data.edgeSize = eSize;

            data.edgeSmoothness = material.noiseSettings.edgeSmoothness * (notNode ? 1 : 0);
            data.edgeSmoothnessWorldPivot = material.noiseSettings.edgeSmoothnessWorldPivot;

            data.sceneDepthOffset = material.noiseSettings.sceneDepthOffset;

            data.includeAdditionalLights = material.noiseSettings.includeAdditionalLights ? 1 : 0;

            dataFogPerInstance[iterationIndex] = data;
        }
    }
}