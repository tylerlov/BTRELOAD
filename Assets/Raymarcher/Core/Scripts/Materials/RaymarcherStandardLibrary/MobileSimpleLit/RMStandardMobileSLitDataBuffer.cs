using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Raymarcher.Materials.Standard
{
    public class RMStandardMobileSLitDataBuffer : RMMaterialDataBuffer
    {
        public RMStandardMobileSLitDataBuffer(in RMMaterialIdentifier materialIdentifier) : base(materialIdentifier)
        {

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RMStandardMobileSLitData
        {
            public Vector4 colorOverride;
            public float normalShift;
            public Vector2 specularIntensAndGloss;
            public Vector2 shadingCovAndSmh;
            public Vector4 shadingTint;
        }

        // Packed data
        [SerializeField] protected RMStandardMobileSLitData[] dataMobileSLitPerInstance;
        // Unpacked data
        [SerializeField] protected Vector4[] mobileSLit_UNPACKED_colorOverride;
        [SerializeField] protected float[] mobileSLit_UNPACKED_normalShift;
        [SerializeField] protected Vector4[] mobileSLit_UNPACKED_specularIntensAndGloss;
        [SerializeField] protected Vector4[] mobileSLit_UNPACKED_shadingCovAndSmh;
        [SerializeField] protected Vector4[] mobileSLit_UNPACKED_shadingTint;

        [SerializeField] private bool unpackedDataDirective;

        public override void InitializeDataContainerPerInstance(in IReadOnlyList<RMMaterialBase> materialInstances, in bool sceneObjectsAreUsingSomeInstances, in bool unpackedDataContainerDirective)
        {
            base.InitializeDataContainerPerInstance(materialInstances, sceneObjectsAreUsingSomeInstances, unpackedDataContainerDirective);

            unpackedDataDirective = unpackedDataContainerDirective;

            if (!unpackedDataContainerDirective)
                dataMobileSLitPerInstance = new RMStandardMobileSLitData[materialInstances.Count];
            else
            {
                mobileSLit_UNPACKED_colorOverride = new Vector4[materialInstances.Count];
                mobileSLit_UNPACKED_normalShift = new float[materialInstances.Count];
                mobileSLit_UNPACKED_specularIntensAndGloss = new Vector4[materialInstances.Count];
                mobileSLit_UNPACKED_shadingCovAndSmh = new Vector4[materialInstances.Count];
                mobileSLit_UNPACKED_shadingTint = new Vector4[materialInstances.Count];
            }
        }

        public override (int dataLength, int strideSize) GetComputeBufferLengthAndStride()
            => (dataMobileSLitPerInstance.Length, sizeof(float) * 13);

        public override Array GetComputeBufferDataContainer
            => dataMobileSLitPerInstance;

        public override void SyncDataContainerPerInstanceWithMaterialInstance(in int iterationIndex, in RMMaterialBase materialInstance)
        {
            RMStandardMobileSLitMaterial material = (RMStandardMobileSLitMaterial)materialInstance;

            RMStandardMobileSLitData data;

            Color coverride = material.colorOverrideSettings.colorOverride;
            data.colorOverride = material.colorOverrideSettings.useColorOverride
                ? new Color(coverride.r, coverride.g, coverride.b, material.colorOverrideSettings.colorBlend)
                : Color.clear;

            data.normalShift = material.normalSettings.normalShift;

            data.specularIntensAndGloss = material.specularSettings.useSpecular
                ? new Vector2(material.specularSettings.specularIntensity, material.specularSettings.specularGlossiness)
                : Vector2.zero;

            data.shadingCovAndSmh = material.shadingSettings.useLambertianShading
                ? new Vector2(material.shadingSettings.shadingCoverage, material.shadingSettings.shadingSmoothness)
                : new Vector2(-1, 0);

            data.shadingTint = Color.Lerp(Color.white, material.shadingSettings.shadingTint, material.shadingSettings.shadingOpacity);

            if (!unpackedDataDirective)
                dataMobileSLitPerInstance[iterationIndex] = data;
            else
            {
                mobileSLit_UNPACKED_colorOverride[iterationIndex] = data.colorOverride;
                mobileSLit_UNPACKED_normalShift[iterationIndex] = data.normalShift;
                mobileSLit_UNPACKED_specularIntensAndGloss[iterationIndex] = data.specularIntensAndGloss;
                mobileSLit_UNPACKED_shadingCovAndSmh[iterationIndex] = data.shadingCovAndSmh;
                mobileSLit_UNPACKED_shadingTint[iterationIndex] = data.shadingTint;
            }
        }

        public override void PushUnpackedDataToShader(in Material raymarcherSessionMaterial, in int actualCountOfMaterialInstances)
        {
            if (mobileSLit_UNPACKED_colorOverride == null)
                return;
            if (mobileSLit_UNPACKED_colorOverride.Length == 0)
                return;
            if (mobileSLit_UNPACKED_colorOverride.Length != actualCountOfMaterialInstances)
                return;

            raymarcherSessionMaterial.SetVectorArray(nameof(RMStandardMobileSLitData.colorOverride), mobileSLit_UNPACKED_colorOverride);
            raymarcherSessionMaterial.SetFloatArray(nameof(RMStandardMobileSLitData.normalShift), mobileSLit_UNPACKED_normalShift);
            raymarcherSessionMaterial.SetVectorArray(nameof(RMStandardMobileSLitData.specularIntensAndGloss), mobileSLit_UNPACKED_specularIntensAndGloss);
            raymarcherSessionMaterial.SetVectorArray(nameof(RMStandardMobileSLitData.shadingCovAndSmh), mobileSLit_UNPACKED_shadingCovAndSmh);
            raymarcherSessionMaterial.SetVectorArray(nameof(RMStandardMobileSLitData.shadingTint), mobileSLit_UNPACKED_shadingTint);
        }
    }
}