#if URP_INSTALLED
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JPG.Universal
{
    public class JPGRendererFeature : ScriptableRendererFeature
    {
        private class JPGRenderPass : ScriptableRenderPass
        {
            public JPG v;
            
            float colorCrunch;
            int downscaling;
            JPG._BlockSize blockSize;
            float reprojectPercent;
            float reprojectLengthInfluence;
            
            void CheckParameters()
            {
                colorCrunch = v.ColorCrunch.value * v.EffectIntensity.value;
                downscaling = Mathf.Max(1, Mathf.CeilToInt(v.Downscaling.value * v.EffectIntensity.value));
                blockSize = (JPG._BlockSize)Mathf.RoundToInt((int)v.BlockSize.value * v.EffectIntensity.value);
                reprojectPercent = v.ReprojectBaseNoise.value * v.EffectIntensity.value;
                reprojectLengthInfluence = v.ReprojectLengthInfluence.value * v.EffectIntensity.value;
            }

            bool DoOnlyStenciled => v.OnlyStenciled.value && !v.VisualizeMotionVectors.value;
            bool DoReprojection => (reprojectPercent > 0f || reprojectLengthInfluence > 0f || v.VisualizeMotionVectors.value) && !cameraData.isSceneViewCamera;

            Material mat;

            public void Setup(Shader shader, ScriptableRenderer renderer, RenderingData renderingData)
            {
                if (mat == null) mat = CoreUtils.CreateEngineMaterial(shader);
            }
            
            public void Cleanup()
            {
                CoreUtils.Destroy(mat);
            }

            private void FetchVolumeComponent()
            {
                if (v == null)
                    v = VolumeManager.instance.stack.GetComponent<JPG>();
            }

            RenderTargetIdentifier source;
            RenderTargetIdentifier sourceDepthStencil;
            CameraData cameraData;
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                FetchVolumeComponent();
                
                #if UNITY_2022_1_OR_NEWER
                    var renderer = renderingData.cameraData.renderer;
                    source = renderer.cameraColorTargetHandle;
                    sourceDepthStencil = renderer.cameraDepthTargetHandle;
                #else
                    source = renderingData.cameraData.renderer.cameraColorTarget;
                    sourceDepthStencil = renderingData.cameraData.renderer.cameraDepthTarget != new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget) ? renderingData.cameraData.renderer.cameraDepthTarget : renderingData.cameraData.renderer.cameraColorTarget;
                #endif
                
                cameraData = renderingData.cameraData;

                ConfigureInput(DoReprojection ? ScriptableRenderPassInput.Motion : ScriptableRenderPassInput.Color);
            }

            [System.Obsolete]
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (mat == null) return;

                FetchVolumeComponent();

                if (!v.IsActive()) return;

                CheckParameters();
                UpdateMaterialProperties();
                UpdateShaderKeywords();
            }
            
            void UpdateMaterialProperties()
            {
                var width = cameraData.cameraTargetDescriptor.width;
                var height = cameraData.cameraTargetDescriptor.height;

                int widthDownscaled = Mathf.FloorToInt(width / downscaling / 2f) * 2;
                int heightDownscaled = Mathf.FloorToInt(height / downscaling / 2f) * 2;
            
                mat.SetVector("_Screen_TexelSize", new Vector4(1f / width, 1f / height, width, height));
                mat.SetVector("_Downscaled_TexelSize", new Vector4(1f / widthDownscaled, 1f / heightDownscaled, widthDownscaled, heightDownscaled));

                mat.SetFloat("_ColorCrunch", colorCrunch);
                mat.SetFloat("_Sharpening", v.Oversharpening.value);
                
                mat.SetFloat("_ReprojectPercent", reprojectPercent);
                mat.SetFloat("_ReprojectSpeed", v.ReprojectBaseRerollSpeed.value);
                mat.SetFloat("_ReprojectLengthInfluence", reprojectLengthInfluence);
            }
            
            string[] keywords = new string[4];
            void UpdateShaderKeywords()
            {
                keywords[0] = blockSize == JPG._BlockSize._4x4 ? "BLOCK_SIZE_4" : blockSize == JPG._BlockSize._8x8 ? "BLOCK_SIZE_8" : blockSize == JPG._BlockSize._16x16 ? "BLOCK_SIZE_16" : "";
                keywords[1] = !v.DontCrunchSkybox.value ? "COLOR_CRUNCH_SKYBOX" : "";
                keywords[2] = DoReprojection ? "REPROJECTION" : "";
                keywords[3] = v.VisualizeMotionVectors.value ? "VIZ_MOTION_VECTORS" : "";
                mat.shaderKeywords = keywords;
            }

            static class Pass
            {
                public const int Downscale = 0;
                public const int Encode = 1;
                public const int Decode = 2;
                public const int UpscalePull = 3;
                public const int UpscalePullStenciled = 4;
                public const int CopyToPrev = 5;
            }
            RenderTexture prevScreenTex;
            int prevWidth = -1;
            int prevHeight = -1;
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (mat == null)
                {
                    Debug.LogError("JPG material has not been correctly initialized...");
                    return;
                }
                if (!v.IsActive()) return;
                var cmd = CommandBufferPool.Get("JPG");
                
                // Have to refetch color target here because post-processing will use texture B instead of A and in OnCameraSetup() it always returns A. So events like AfterRenderingPostProcessing start working.
                #if UNITY_2022_1_OR_NEWER
                    var renderer = renderingData.cameraData.renderer;
                    source = renderer.cameraColorTargetHandle;
                    sourceDepthStencil = renderer.cameraDepthTargetHandle;
                #else
                    source = renderingData.cameraData.renderer.cameraColorTarget;
                    sourceDepthStencil = renderingData.cameraData.renderer.cameraDepthTarget != new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget) ? renderingData.cameraData.renderer.cameraDepthTarget : renderingData.cameraData.renderer.cameraColorTarget;
                #endif

                var width = cameraData.cameraTargetDescriptor.width;
                var height = cameraData.cameraTargetDescriptor.height;

                if (prevWidth != width || prevHeight != height)
                {
                    prevWidth = width;
                    prevHeight = height;
                    if (prevScreenTex != null) RenderTexture.ReleaseTemporary(prevScreenTex);
                    prevScreenTex = RenderTexture.GetTemporary(prevWidth, prevHeight, 0, GraphicsFormat.R32G32B32A32_SFloat);
                    prevScreenTex.name = "_PrevScreen RT";
                }
                
                {
                    int widthDownscaled = Mathf.FloorToInt(width / downscaling / 2f) * 2;
                    int heightDownscaled = Mathf.FloorToInt(height / downscaling / 2f) * 2;

                    var downscaledTex = Shader.PropertyToID("_JpgScreenDownscaled");
                    cmd.GetTemporaryRT(downscaledTex, widthDownscaled, heightDownscaled, 0, FilterMode.Bilinear, GraphicsFormat.R32G32B32A32_SFloat);

                    RenderWith(source, downscaledTex, cmd, mat, Pass.Downscale);

                    var blocksTex = Shader.PropertyToID("_JpgBlocks");
                    cmd.GetTemporaryRT(blocksTex, widthDownscaled, heightDownscaled, 0, FilterMode.Bilinear, GraphicsFormat.R32G32B32A32_SFloat);

                    RenderWith(downscaledTex, blocksTex, cmd, mat, Pass.Encode);
                    RenderWith(blocksTex, downscaledTex, cmd, mat, Pass.Decode);

                    cmd.SetGlobalTexture("_PrevScreen", prevScreenTex);
                    RenderWith(downscaledTex, source, cmd, mat, DoOnlyStenciled ? Pass.UpscalePullStenciled : Pass.UpscalePull, rebindStencil: true);
                    
                    if (DoReprojection)
                        RenderWith(source, prevScreenTex, cmd, mat, Pass.CopyToPrev);

                    cmd.ReleaseTemporaryRT(downscaledTex);
                    cmd.ReleaseTemporaryRT(blocksTex);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            Mesh fullscreenTriangle;
            Mesh FullscreenTriangle
            {
                get
                {
                    if (fullscreenTriangle != null) return fullscreenTriangle;
                    fullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };
                    fullscreenTriangle.SetVertices(new List<Vector3> { new Vector3(-1f, -1f, 0f), new Vector3(-1f, 3f, 0f), new Vector3(3f, -1f, 0f) });
                    fullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                    fullscreenTriangle.UploadMeshData(false);
                    return fullscreenTriangle;
                }
            }
            public void RenderWith(RenderTargetIdentifier source, RenderTargetIdentifier destination, CommandBuffer cmd, Material material, int pass, bool rebindStencil = false)
            {
                cmd.SetGlobalTexture("_Input", source);
                // Why we rebind stencil: gist.github.com/ScottJDaley/6cddf0c8995ed61cac7088e22c983de1?permalink_comment_id=4976348#gistcomment-4976348
                if (rebindStencil)
                    cmd.SetRenderTarget(destination, sourceDepthStencil);
                else
                    cmd.SetRenderTarget(destination);
                cmd.DrawMesh(FullscreenTriangle, Matrix4x4.identity, material, 0, pass);
            }
            public void Render(RenderTargetIdentifier destination, CommandBuffer cmd, Material material, int pass)
            {
                cmd.SetRenderTarget(destination);
                cmd.DrawMesh(FullscreenTriangle, Matrix4x4.identity, material, 0, pass);
            }
        }

        [Space(15), SerializeField, Header("You can now add JPG to your Post Process Volume.")]
        Shader shader;
        JPGRenderPass renderPass;

        [SerializeField]
        RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        public override void Create()
        {
            if (!isActive)
            {
                renderPass?.Cleanup();
                renderPass = null;
                return;
            }
            name = "JPG";
            renderPass = new JPGRenderPass();
            // Add this line to set the render pass event
            renderPass.renderPassEvent = renderPassEvent;
        }

        void OnDisable()
        {
            renderPass?.Cleanup();
        }
        
        protected override void Dispose(bool disposing)
        {
            renderPass?.Cleanup();
            renderPass = null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            shader = Shader.Find("Hidden/Universal Render Pipeline/JPG");
            if (shader == null)
            {
                Debug.LogWarning("JPG shader was not found. Please ensure it compiles correctly");
                return;
            }

            if (renderingData.cameraData.postProcessEnabled && !renderingData.cameraData.isSceneViewCamera)
            {
                renderPass.Setup(shader, renderer, renderingData);
                renderer.EnqueuePass(renderPass);
            }
        }
    }
}
#endif
