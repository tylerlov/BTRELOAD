using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.LSPP.Runtime
{
    public class LightScatteringRenderFeature : ScriptableRendererFeature
    {
        class LightScatteringRenderPass : ScriptableRenderPass
        {
            private Material occluderMaterial = null;
            private Material mergeMaterial = null;
            private Material blitMaterial = null;
            private Material lightScatterMaterial = null;

            private RTHandle source;
            private RTHandle occluderRT;
            private RTHandle lightScatteringRT;
            private RTHandle mergeRT;

            private const string occluderRtId = "_Occluders_LSPP";
            private const string lightScatterRtId = "_Scattering_LSPP";
            private const string mergeRtId = "_Merge_LSPP";
            private const string bufferPoolId = "LightScatteringPP";

            private const string lightScatterMaterialPath = "OccaSoftware/LSPP/LightScatter";
            private const string occluderMaterialPath = "OccaSoftware/LSPP/Occluders";
            private const string mergeMaterialPath = "OccaSoftware/LSPP/Merge";
            private const string blitMaterialPath = "OccaSoftware/LSPP/Blit";

            private LightScatteringPostProcess lspp;

            public LightScatteringRenderPass()
            {
                occluderRT = RTHandles.Alloc(Shader.PropertyToID(occluderRtId), name: occluderRtId);
                lightScatteringRT = RTHandles.Alloc(Shader.PropertyToID(lightScatterRtId), name: lightScatterRtId);
                mergeRT = RTHandles.Alloc(Shader.PropertyToID(mergeRtId), name: mergeRtId);
            }

            public void SetTarget(RTHandle colorHandle)
            {
                source = colorHandle;
            }

            internal void Setup()
            {
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            internal void SetupMaterials()
            {
                SetupMaterial(ref lightScatterMaterial, lightScatterMaterialPath);
                SetupMaterial(ref occluderMaterial, occluderMaterialPath);
                SetupMaterial(ref mergeMaterial, mergeMaterialPath);
                SetupMaterial(ref blitMaterial, blitMaterialPath);
            }

            internal void SetupMaterial(ref Material material, string path)
            {
                if (material != null)
                    return;

                Shader s = Shader.Find(path);
                if (s != null)
                {
                    material = CoreUtils.CreateEngineMaterial(s);
                }
            }

            internal bool HasAllMaterials()
            {
                if (lightScatterMaterial == null)
                    return false;

                if (occluderMaterial == null)
                    return false;

                if (mergeMaterial == null)
                    return false;

                if (blitMaterial == null)
                    return false;

                return true;
            }

            internal bool RegisterStackComponent()
            {
                lspp = VolumeManager.instance.stack.GetComponent<LightScatteringPostProcess>();

                if (lspp == null)
                    return false;

                return lspp.IsActive();
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                descriptor.sRGB = false;
                descriptor.width = Mathf.Max(1, descriptor.width);
                descriptor.height = Mathf.Max(1, descriptor.height);

                // Setup Merge Target
                RenderingUtils.ReAllocateIfNeeded(ref mergeRT, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: mergeRtId);

                // Setup Light Scatter Target
                descriptor.width = descriptor.width >> 1;
                descriptor.height = descriptor.height >> 1;
                RenderingUtils.ReAllocateIfNeeded(ref lightScatteringRT, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: lightScatterRtId);

                // Setup Occluder target.
                RenderTextureDescriptor occluderDescriptor = descriptor;
                occluderDescriptor.colorFormat = RenderTextureFormat.R8;
                RenderingUtils.ReAllocateIfNeeded(ref occluderRT, occluderDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: occluderRtId);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                UnityEngine.Profiling.Profiler.BeginSample("LSPP");

                // Early exit
                if (!HasAllMaterials())
                    return;

                // Setup commandbuffer
                CommandBuffer cmd = CommandBufferPool.Get(bufferPoolId);

                Shader.SetGlobalMatrix(
                    "lspp_Matrix_VP",
                    renderingData.cameraData.camera.projectionMatrix * renderingData.cameraData.camera.worldToCameraMatrix
                );

                // Draw to occluder texture
                Blitter.BlitCameraTexture(cmd, source, occluderRT, occluderMaterial, 0);

                // Set up scattering data texture
                UpdateLSPPMaterial(cmd);
                cmd.SetGlobalTexture(occluderRtId, occluderRT);
                Blitter.BlitCameraTexture(cmd, source, lightScatteringRT, lightScatterMaterial, 0);

                // Blit to Merge Target
                cmd.SetGlobalTexture("_Source", source);
                cmd.SetGlobalTexture(lightScatterRtId, lightScatteringRT);
                Blitter.BlitCameraTexture(cmd, source, mergeRT, mergeMaterial, 0);

                // Blit to screen
                cmd.SetGlobalTexture("_Source", mergeRT);
                Blitter.BlitCameraTexture(cmd, mergeRT, source, blitMaterial, 0);

                // Clean up command buffer
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
                UnityEngine.Profiling.Profiler.EndSample();

                void UpdateLSPPMaterial(CommandBuffer cmd)
                {
                    cmd.SetGlobalFloat(Params.Density.Id, lspp.fogDensity.value * 0.1f);
                    cmd.SetGlobalInt(Params.DoSoften.Id, BoolToInt(lspp.softenScreenEdges.value));
                    cmd.SetGlobalInt(Params.DoAnimate.Id, BoolToInt(lspp.animateSamplingOffset.value));
                    cmd.SetGlobalFloat(Params.MaxRayDistance.Id, lspp.maxRayDistance.value);
                    cmd.SetGlobalInt(Params.SampleCount.Id, lspp.numberOfSamples.value);
                    cmd.SetGlobalColor(Params.Tint.Id, lspp.tint.value);
                    cmd.SetGlobalInt(Params.LightOnScreenRequired.Id, BoolToInt(lspp.lightMustBeOnScreen.value));
                    cmd.SetGlobalInt(Params.FalloffDirective.Id, (int)lspp.falloffBasis.value);
                    cmd.SetGlobalFloat(Params.FalloffIntensity.Id, lspp.falloffIntensity.value);
                    cmd.SetGlobalFloat(Params.OcclusionAssumption.Id, lspp.occlusionAssumption.value);
                    cmd.SetGlobalFloat(Params.OcclusionOverDistanceAmount.Id, lspp.occlusionOverDistanceAmount.value);

                    static int BoolToInt(bool a)
                    {
                        return a == false ? 0 : 1;
                    }
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd) { }
        }

        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public Settings settings = new Settings();
        LightScatteringRenderPass lightScatteringPass;

        public override void Create()
        {
            lightScatteringPass = new LightScatteringRenderPass();
            lightScatteringPass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType == CameraType.Reflection)
                return;

            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                return;

            if (!renderingData.cameraData.postProcessEnabled)
                return;

            if (!lightScatteringPass.RegisterStackComponent())
                return;

            lightScatteringPass.SetupMaterials();
            if (!lightScatteringPass.HasAllMaterials())
                return;

            renderer.EnqueuePass(lightScatteringPass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            lightScatteringPass.Setup();
            lightScatteringPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            lightScatteringPass.SetTarget(renderer.cameraColorTargetHandle);
        }

        private static class Params
        {
            public readonly struct Param
            {
                public Param(string property)
                {
                    Property = property;
                    Id = Shader.PropertyToID(property);
                }

                readonly public string Property;
                readonly public int Id;
            }

            public static Param Density = new Param("_Density");
            public static Param DoSoften = new Param("_DoSoften");
            public static Param DoAnimate = new Param("_DoAnimate");
            public static Param MaxRayDistance = new Param("_MaxRayDistance");
            public static Param SampleCount = new Param("lspp_NumSamples");
            public static Param Tint = new Param("_Tint");
            public static Param LightOnScreenRequired = new Param("_LightOnScreenRequired");
            public static Param FalloffDirective = new Param("_FalloffDirective");
            public static Param FalloffIntensity = new Param("_FalloffIntensity");
            public static Param OcclusionAssumption = new Param("_OcclusionAssumption");
            public static Param OcclusionOverDistanceAmount = new Param("_OcclusionOverDistanceAmount");
        }
    }
}
