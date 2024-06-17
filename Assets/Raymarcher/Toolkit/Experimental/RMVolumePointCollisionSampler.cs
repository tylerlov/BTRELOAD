using System.Collections.Generic;

using UnityEngine;

using Raymarcher.Objects.Volumes;
using Raymarcher.Utilities;

namespace Raymarcher.Toolkit.Experimental
{
    using static RMVolumeUtils;
    using static RMTextureUtils;

    [System.Obsolete("Highly experimental feature! It may be removed in the future, and using it is at your own risk.")]
    public static class RMVolumePointCollisionSampler
    {
        private const string COMPUTE_NAME = "RMTex3DPointCollisionSamplerCompute";
        private const string COMPUTE_KERNEL_NAME = "CollisionSampler";
        private const string COMPUTE_TEX3D = "Tex3DInput";
        private const string COMPUTE_TEXRES = "TexRes";
        private const string COMPUTE_THRESH = "Threshold";
        private const string COMPUTE_RESULT = "Result";

        private const int THREAD_GROUPS = 8;

        [System.Obsolete("Highly experimental feature! It may be removed in the future, and using it is at your own risk.")]
        public static bool GeneratePrimitiveCollidersOnVolumePoints(ref List<Transform> outputBoxColliders,
            Transform container, RMSdf_VolumeBoxBase targetVolume, RenderTexture targetRT3DCanvas,
            CommonVolumeResolution canvasVolumeResolution, float colliderSizeMultiplier = 1, float pixelThreshold = 0.2f,
            bool generateSphereColliders = true)
        {
            const string COLLIDER_NAME = "VolumeCollider";

            if (outputBoxColliders == null)
                outputBoxColliders = new List<Transform>();
            outputBoxColliders.Clear();

            if(targetRT3DCanvas == null)
            {
                RMDebug.Debug(typeof(RMVolumePointCollisionSampler), "Target 3D canvas RT is empty!", true);
                return false;
            }

            ComputeShader volumeComputeShader = Resources.Load<ComputeShader>(COMPUTE_NAME);
            if (volumeComputeShader == null)
            {
                RMDebug.Debug(typeof(RMVolumePointCollisionSampler), "Couldn't find a compute shader for modifying a 3D render texture while initializing the collision sampler canvas", true);
                return false;
            }

            int currentResolution = GetCommonVolumeResolution(canvasVolumeResolution);
            if (!CompareRT3DDimensions(targetRT3DCanvas, currentResolution))
            {
                RMDebug.Debug(typeof(RMVolumePointCollisionSampler), $"Entry Canvas of 3D RT doesn't match the volume resolution ({currentResolution} vs {targetRT3DCanvas.width}x{targetRT3DCanvas.height}x{targetRT3DCanvas.volumeDepth})! Volume dimensions must be equal.", true);
                return false;
            }

            int computeShaderKernelID = volumeComputeShader.FindKernel(COMPUTE_KERNEL_NAME);
            volumeComputeShader.SetTexture(computeShaderKernelID, COMPUTE_TEX3D, targetRT3DCanvas);
            volumeComputeShader.SetInt(COMPUTE_TEXRES, currentResolution);
            volumeComputeShader.SetFloat(COMPUTE_THRESH, Mathf.Clamp01(pixelThreshold));

            Vector4[] result = new Vector4[currentResolution * currentResolution * currentResolution];
            ComputeBuffer resultBuffer = new ComputeBuffer(result.Length, sizeof(float) * 4);
            volumeComputeShader.SetBuffer(computeShaderKernelID, COMPUTE_RESULT, resultBuffer);
            volumeComputeShader.SetTexture(computeShaderKernelID, COMPUTE_TEX3D, targetRT3DCanvas);

            volumeComputeShader.SetBuffer(computeShaderKernelID, COMPUTE_RESULT, resultBuffer);

            int threadGroupWorker = currentResolution / THREAD_GROUPS;
            volumeComputeShader.Dispatch(computeShaderKernelID, threadGroupWorker, threadGroupWorker, threadGroupWorker);
            resultBuffer.GetData(result);

            Transform volumeTransform = targetVolume.transform;
            for (int x = 0; x < result.Length; x++)
            {
                if (result[x].w == 0)
                    continue;

                float step = targetVolume.volumeSize / currentResolution;

                Vector3 worldPos = ConvertVolumeTextureSpaceToWorld(result[x],
                    currentResolution, volumeTransform.position, volumeTransform.rotation, volumeTransform.localScale, targetVolume.volumeSize)
                    + Vector3.right * step + Vector3.up * step + Vector3.forward * step;
                Vector3 scale = Vector3.one * (step * (2f * colliderSizeMultiplier));

                Transform bc = new GameObject(COLLIDER_NAME).transform;
                if(generateSphereColliders)
                    bc.gameObject.AddComponent<SphereCollider>();
                else
                    bc.gameObject.AddComponent<BoxCollider>();
                bc.position = worldPos;
                bc.localScale = scale;

                bc.SetParent(container);
                outputBoxColliders.Add(bc);
            }

            resultBuffer?.Release();

            return true;
        }
    }
}