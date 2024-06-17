using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardLitDataBuffer : RMStandardUnlitDataBuffer
    {
        public RMStandardLitDataBuffer(in RMMaterialIdentifier materialIdentifier) : base(materialIdentifier)
        {
            dataLitGlobal = new RMStandardLitDataGlobal(false);
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RMStandardLitData
        {
            public RMStandardUnlitData unlitData;

            public float specularIntensity;
            public float specularSize;
            public float specularGlossiness;

            public float shadingCoverage;
            public float shadingSmoothness;
            public Vector3 shadingTint;

            public float useShadows;
            public Vector2 shadowDistanceMinMax;
            public float shadowAmbience;

            public float includeDirectionalLight;
            public float includeAdditionalLights;
            public float translucencyMinAbsorption;
            public float translucencyMaxAbsorption;
        }

        [Serializable]
        public struct RMStandardLitDataGlobal
        {
            [Range(1.0f, 100.0f)] public float shadowSoftness;

            [Tooltip("Default     - default precision (recommended)\nHigh          - high precision\nExtreme    - highest precision (RTX only)")]
            public RaymarcherShadowQuality shadowQuality;
            public enum RaymarcherShadowQuality { Default, High, Extreme };

            public RMStandardLitDataGlobal(bool _)
            {
                shadowSoftness = 32f;
                shadowQuality = RaymarcherShadowQuality.Default;
            }
        }

        [SerializeField] protected RMStandardLitData[] dataLitPerInstance;
        [SerializeField] public RMStandardLitDataGlobal dataLitGlobal;

        public override void InitializeDataContainerPerInstance(in IReadOnlyList<RMMaterialBase> materialInstances, in bool sceneObjectsAreUsingSomeInstances, in bool unpackedDataContainerDirective)
        {
            base.InitializeDataContainerPerInstance(materialInstances, sceneObjectsAreUsingSomeInstances, unpackedDataContainerDirective);
            dataLitPerInstance = new RMStandardLitData[materialInstances.Count];
        }

        public override (int dataLength, int strideSize) GetComputeBufferLengthAndStride()
            => (dataLitPerInstance.Length, base.GetComputeBufferLengthAndStride().strideSize + sizeof(float) * 16);
        
        public override Array GetComputeBufferDataContainer
            => dataLitPerInstance;

        public override void SyncDataContainerPerInstanceWithMaterialInstance(in int iterationIndex, in RMMaterialBase materialInstance)
        {
            base.SyncDataContainerPerInstanceWithMaterialInstance(iterationIndex, materialInstance);

            RMStandardLitMaterial material = (RMStandardLitMaterial)materialInstance;
            RMStandardLitData data;

            data.unlitData = dataUnlitPerInstance[iterationIndex];

            data.specularIntensity = material.specularSettings.useSpecular ? material.specularSettings.specularIntensity : 0;
            data.specularSize = material.specularSettings.specularSize;
            data.specularGlossiness = material.specularSettings.specularGlossiness;

            float opac = material.shadingSettings.useLambertianShading ? material.shadingSettings.shadingOpacity : 0;
            data.shadingCoverage = Mathf.Lerp(-1, material.shadingSettings.shadingCoverage, opac);
            data.shadingSmoothness = Mathf.Lerp(0, material.shadingSettings.shadingSmoothness, opac);
            Color shdCol = material.shadingSettings.shadingTint;
            data.shadingTint = new Vector3(shdCol.r, shdCol.g, shdCol.b);

            data.useShadows = material.shadowSettings.useRaymarcherShadows ? 1 : 0;
            data.shadowAmbience = material.shadowSettings.shadowAmbience;
            Vector3 minMax = material.shadowSettings.shadowDistanceMinMax;
            minMax.x = Mathf.Min(Mathf.Abs(minMax.x), Mathf.Abs(minMax.y));
            minMax.y = Mathf.Max(Mathf.Abs(minMax.y), Mathf.Abs(minMax.x));
            data.shadowDistanceMinMax = minMax;

            data.includeDirectionalLight = material.lightingSettings.includeDirectionalLight ? 1 : 0;
            data.includeAdditionalLights = material.lightingSettings.includeAdditionalLights ? 1 : 0;
            data.translucencyMinAbsorption = material.lightingSettings.translucencyMinAbsorption;
            data.translucencyMaxAbsorption = material.lightingSettings.translucencyMaxAbsorption;

            dataLitPerInstance[iterationIndex] = data;
        }

        public override void PushGlobalDataContainerToShader(in Material raymarcherSceneMaterial)
        {
            float shadowQuality = 1.0f;
            switch (dataLitGlobal.shadowQuality)
            {
                case RMStandardLitDataGlobal.RaymarcherShadowQuality.High: shadowQuality = 0.5f; break;
                case RMStandardLitDataGlobal.RaymarcherShadowQuality.Extreme: shadowQuality = 0.1f; break;
            }

            raymarcherSceneMaterial.SetFloat(RMStandardLitIdentifier.GLOBAL_FIELD_SHADOW_QUALITY, shadowQuality);
            raymarcherSceneMaterial.SetFloat(RMStandardLitIdentifier.GLOBAL_FIELD_SHADOW_SOFTNESS, dataLitGlobal.shadowSoftness);
        }
    }
}
