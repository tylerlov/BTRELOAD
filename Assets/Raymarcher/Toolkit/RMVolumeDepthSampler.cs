using System;

using UnityEngine;

using Raymarcher.Objects.Volumes;
using Raymarcher.Utilities;

namespace Raymarcher.Toolkit
{
    using static RMVolumeUtils;

    public sealed class RMVolumeDepthSampler : IDisposable
    {
        private const string COMPUTE_RAY_NAME = "RMTex3DDepthRayTraceCompute";
        private const string COMPUTE_RAY_KERNEL_NAME = "DepthRayTrace";
        private const string COMPUTE_PIXEL_KERNEL_NAME = "DepthSamplePixel";

        private const string COMPUTE_TEX3D = "Tex3DInput";
        private const string COMPUTE_TEXRES = "TexRes";
        private const string COMPUTE_RAYORIGIN = "RayOrigin";
        private const string COMPUTE_RAYDIR = "RayDirection";
        private const string COMPUTE_ITERS = "Iterations";
        private const string COMPUTE_THRESH = "Threshold";
        private const string COMPUTE_RESULT = "Result";

        private const int THREAD_GROUPS = 8;

        private RenderTexture canvasRenderTexture;
        private ComputeShader targetCS;
        private ComputeBuffer resultBuffer;
        private int csKernelIDRayTracer;
        private int csKernelIDSamplePixel;
        private int threadGroupWorker;

        private Vector4[] result;

        public bool Initialized { get; private set; }
        private int CurrentResolution { get; set; }

        public struct RayTraceData
        {
            public int iterations;
            public CommonVolumeResolution volumeCanvasResolution;
            public float pixelThreshold;

            public RayTraceData(CommonVolumeResolution volumeCanvasResolution, float pixelThreshold = 0.1f, int iterations = 64)
            {
                this.iterations = iterations;
                this.volumeCanvasResolution = volumeCanvasResolution;
                this.pixelThreshold = pixelThreshold;
            }
        }

        public RMVolumeDepthSampler(RenderTexture entryCanvas, RayTraceData raytraceData)
        {
            Initialize(entryCanvas, raytraceData);
        }

        public RMVolumeDepthSampler(RenderTexture entryCanvas, int iterations, CommonVolumeResolution volumeCanvasResolution, float pixelThreshold)
        {
            Initialize(entryCanvas, new RayTraceData(volumeCanvasResolution, pixelThreshold, iterations));
        }

        public RMVolumeDepthSampler(RMVolumeVoxelPainter targetVolumeVoxelPainter, int iterations, float pixelThreshold)
        {
            if(!targetVolumeVoxelPainter.IsInitialized)
            {
                RMDebug.Debug(this, "Couldn't initialize the volume depth sampler. Volume Voxel Painter is not initialized!", true);
                return;
            }
            Initialize(targetVolumeVoxelPainter.WorkingVolumeCanvas3D, new RayTraceData(targetVolumeVoxelPainter.CurrentCommonVolumeResolution, pixelThreshold, iterations));
        }

        private void Initialize(RenderTexture entryCanvas, RayTraceData raytraceData)
        {
            if (Initialized)
            {
                RMDebug.Debug(typeof(RMVolumeDepthSampler), $"Volume Depth Sampler is already initialized! Call '{nameof(Dispose)}' to release or '{nameof(Update)}' to update the current depth sampler.", false);
                return;
            }

            if (entryCanvas == null)
            {
                RMDebug.Debug(typeof(RMVolumeDepthSampler), $"Couldn't initialize RT canvas. '{nameof(entryCanvas)}' is null!", true);
                return;
            }

            CurrentResolution = GetCommonVolumeResolution(raytraceData.volumeCanvasResolution);

            if (entryCanvas.width != CurrentResolution || entryCanvas.height != CurrentResolution || entryCanvas.volumeDepth != CurrentResolution)
            {
                RMDebug.Debug(typeof(RMVolumeDepthSampler), "Entry Canvas of 3D RT doesn't match the volume resolution! Volume dimensions must be equal", true);
                return;
            }

            var shaderSource = Resources.Load<ComputeShader>(COMPUTE_RAY_NAME);
            if (shaderSource == null)
            {
                RMDebug.Debug(typeof(RMVolumeDepthSampler), "Couldn't find a compute shader for modifying a 3D render texture while initializing the depth detection canvas", true);
                return;
            }

            if (targetCS != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(targetCS);
                else
                    GameObject.DestroyImmediate(targetCS);
            }

            targetCS = GameObject.Instantiate(shaderSource);
            targetCS.name = COMPUTE_RAY_NAME;

            canvasRenderTexture = entryCanvas;
            threadGroupWorker = CurrentResolution / THREAD_GROUPS;

            csKernelIDRayTracer = targetCS.FindKernel(COMPUTE_RAY_KERNEL_NAME);
            csKernelIDSamplePixel = targetCS.FindKernel(COMPUTE_PIXEL_KERNEL_NAME);

            targetCS.SetTexture(csKernelIDRayTracer, COMPUTE_TEX3D, canvasRenderTexture);
            targetCS.SetTexture(csKernelIDSamplePixel, COMPUTE_TEX3D, canvasRenderTexture);

            targetCS.SetInt(COMPUTE_TEXRES, CurrentResolution);
            targetCS.SetInt(COMPUTE_ITERS, Mathf.Clamp(raytraceData.iterations, 8, 512));
            targetCS.SetFloat(COMPUTE_THRESH, Mathf.Clamp01(raytraceData.pixelThreshold));

            result = new Vector4[1];
            if (resultBuffer != null)
                resultBuffer.Release();
            resultBuffer = new ComputeBuffer(result.Length, sizeof(float) * 4);

            Initialized = true;
        }

        public void Update(RenderTexture entryCanvas, RayTraceData raytraceData)
        {
            if (!Initialized)
            {
                RMDebug.Debug(typeof(RMVolumeDepthSampler), $"Volume Depth Sampler is not initialized! Call '{nameof(Initialize)}' first.", false);
                return;
            }
            Dispose();
            Initialize(entryCanvas, raytraceData);
        }

        public void Dispose()
        {
            canvasRenderTexture = null;
            if (targetCS != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(targetCS);
                else
                    UnityEngine.Object.DestroyImmediate(targetCS);
            }
            targetCS = null;
            resultBuffer?.Release();
            resultBuffer = null;
            result = null;
            Initialized = false;
        }

        public bool SampleVolumeDepth(out Vector3 hitPositionResult, Vector3 rayOriginWorldSpace, Vector3 rayDirection, RMSdf_VolumeBoxBase targetVolume, float rayLength = 1)
        {
            hitPositionResult = Vector3.zero;
            if (!Initialized)
            {
                RMDebug.Debug(typeof(RMVolumeDepthSampler), "Volume Depth Detection is not initialized. Call 'Initialize' first.", true);
                return false;
            }

            targetCS.SetVector(COMPUTE_RAYORIGIN, ConvertWorldToVolumeTextureSpace(rayOriginWorldSpace, targetVolume, CurrentResolution));
            targetCS.SetVector(COMPUTE_RAYDIR, ConvertWorldToVolumeTextureSpace(rayOriginWorldSpace + rayLength * rayDirection, targetVolume, CurrentResolution));
            result = new Vector4[1];
            resultBuffer.SetData(result);
            targetCS.SetBuffer(csKernelIDRayTracer, COMPUTE_RESULT, resultBuffer);
            targetCS.Dispatch(csKernelIDRayTracer, threadGroupWorker, threadGroupWorker, threadGroupWorker);

            resultBuffer.GetData(result);

            if (result[0].w != 0)
            {
                Transform volumeTrans = targetVolume.transform;
                hitPositionResult = ConvertVolumeTextureSpaceToWorld(result[0],
                    CurrentResolution,
                    volumeTrans.position,
                    volumeTrans.rotation,
                    volumeTrans.localScale,
                    targetVolume.volumeSize);
                return true;
            }
            else
                return false;
        }

        public void SampleVolumePixel(out Vector2 sampledPixelResult, Vector3 pointWorldSpace, RMSdf_VolumeBoxBase targetVolume)
        {
            sampledPixelResult = Vector2.zero;
            if (!Initialized)
            {
                RMDebug.Debug(typeof(RMVolumeDepthSampler), "Volume Depth Detection is not initialized. Call 'Initialize' first.", true);
                return;
            }

            targetCS.SetVector(COMPUTE_RAYORIGIN, ConvertWorldToVolumeTextureSpace(pointWorldSpace, targetVolume, CurrentResolution));
            result = new Vector4[1];
            resultBuffer.SetData(result);
            targetCS.SetBuffer(csKernelIDSamplePixel, COMPUTE_RESULT, resultBuffer);
            targetCS.Dispatch(csKernelIDSamplePixel, threadGroupWorker, threadGroupWorker, threadGroupWorker);

            resultBuffer.GetData(result);

            sampledPixelResult = new Vector2(result[0].x, result[0].y);
        }
    }
}