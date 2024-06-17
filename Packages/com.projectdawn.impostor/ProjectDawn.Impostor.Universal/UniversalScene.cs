#if UNIVERSAL_RENDER_PIPELINE
using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectDawn.Impostor
{
    [RenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class UniversalScene : Scene
    {
#if UNITY_EDITOR
        bool m_CachedAllowAsync;
#endif

        Surface m_Surface;
        CapturePoints m_CapturePoints;
        Matrix4x4 m_WorldToObject;

        public override CapturePoints CapturePoints => m_CapturePoints;

        public UniversalScene(Surface surface, CapturePoints capturePoints)
        {
            CheckStripDebugVariants();

#if UNITY_EDITOR
            m_CachedAllowAsync = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
            m_Surface = surface;
            m_CapturePoints = capturePoints;
            m_WorldToObject = surface.Matrix;
        }

        protected override void Cleanup()
        {
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = m_CachedAllowAsync;
#endif
        }

        public override void Render(RenderTexture target, RenderMode mode)
        {
            CheckUniversalPipelineAsset();

            var capturePoints = m_CapturePoints;
            int resolution = target.width;

            var cmd = CommandBufferPool.Get($"Render {mode}");
            cmd.Clear();

//#if UNITY_EDITOR
//            UnityEditor.ShaderUtil.SetAsyncCompilation(cmd, false);
//endif

            var depth = Shader.PropertyToID("_DepthAttachment");
            cmd.GetTemporaryRT(depth, resolution, resolution, 24, target.filterMode, RenderTextureFormat.Depth);

            cmd.SetRenderTarget(new RenderTargetIdentifier(target), new RenderTargetIdentifier(depth));
            if (mode == RenderMode.Normal)
                cmd.ClearRenderTarget(true, true, Color.grey);
            else
                cmd.ClearRenderTarget(true, true, Color.black);

            var projection = Matrix4x4.Ortho(-capturePoints.Radius, capturePoints.Radius, -capturePoints.Radius, capturePoints.Radius, 0, capturePoints.Radius * 2);
            cmd.SetProjectionMatrix(projection);

            SetupDebug(cmd, mode);

            // Render all frames
            foreach (var snapshot in capturePoints.Points)
            {
                var pixelRect = new Rect(snapshot.Uv.x * resolution, snapshot.Uv.y * resolution,
                    snapshot.Uv.width * resolution, snapshot.Uv.height * resolution);

                var fromPoint = snapshot.From;
                var toPoint = snapshot.To;
                var up = Vector3.up;

                var lookMatrix = Matrix4x4.LookAt(fromPoint, toPoint, up);
                var viewMatrix = lookMatrix.inverse;

                // Flip the z-axis of the view matrix to convert from left-handed to right-handed
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;

                cmd.SetViewport(pixelRect);

                cmd.SetViewMatrix(viewMatrix);

                foreach (var renderer in m_Surface.Renderers)
                {
                    Render(cmd, renderer, mode == RenderMode.Depth);
                }
            }

            if (mode == RenderMode.Depth)
                cmd.Blit(depth, target);

            cmd.ReleaseTemporaryRT(depth);

//#if UNITY_EDITOR
//            UnityEditor.ShaderUtil.RestoreAsyncCompilation(cmd);
//#endif

            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void RenderCombinedMask(RenderTexture target, Material blitMaterial)
        {
            CheckUniversalPipelineAsset();

            var capturePoints = m_CapturePoints;
            int resolution = target.width;

            var cmd = CommandBufferPool.Get("Render Combined Mask");
            cmd.Clear();

            var color = Shader.PropertyToID("_ColorAttachment");
            cmd.GetTemporaryRT(color, resolution, resolution, 24, target.filterMode, target.graphicsFormat);

            cmd.SetRenderTarget(target);
            cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

            var projection = Matrix4x4.Ortho(-capturePoints.Radius, capturePoints.Radius, -capturePoints.Radius, capturePoints.Radius, 0, capturePoints.Radius * 2);
            cmd.SetProjectionMatrix(projection);

            SetupDebug(cmd, RenderMode.Alpha);

            // Render all frames
            foreach (var snapshot in capturePoints.Points)
            {
                var fromPoint = snapshot.From;
                var toPoint = snapshot.To;
                var up = Vector3.up;

                var lookMatrix = Matrix4x4.LookAt(fromPoint, toPoint, up);
                var viewMatrix = lookMatrix.inverse;

                // Flip the z-axis of the view matrix to convert from left-handed to right-handed
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;

                cmd.SetViewMatrix(viewMatrix);

                foreach (var renderer in m_Surface.Renderers)
                {
                    Render(cmd, renderer, false);
                }
            }

            cmd.Blit(color, target, blitMaterial);

            cmd.ReleaseTemporaryRT(color);

            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupDebug(CommandBuffer cmd, RenderMode mode)
        {
            cmd.SetKeyword(GlobalKeyword.Create("DEBUG_DISPLAY"), true);

            switch (mode)
            {
                case RenderMode.BaseColor:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 1);
                    break;
                case RenderMode.Alpha:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 3);
                    break;
                case RenderMode.Normal:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 7);
                    break;
                case RenderMode.Depth:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 0);
                    break;
                case RenderMode.Smoothness:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 4);
                    break;
                case RenderMode.Metallic:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 10);
                    break;
                case RenderMode.Occlusion:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 5);
                    break;
                case RenderMode.Emission:
                    cmd.SetGlobalFloat(Shader.PropertyToID("_DebugMaterialMode"), 6);
                    break;
                default:
                    throw new NotImplementedException(mode.ToString());
            }
        }

        void Render(CommandBuffer cmd, Renderer renderer, bool depth)
        {
            var mesh = renderer.GetSharedMesh();
            if (mesh == null)
            {
                UnityEngine.Debug.LogWarning("Failed to find shared mesh!", renderer);
                return;
            }

            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];

                int pass = 0;

                if (depth)
                    pass = material.FindPass("DepthOnly");

                if (pass == -1)
                    throw new System.Exception($"Failed to find pass DepthOnly in shader {material.shader} passCount={material.shader.passCount}. This can happen either shadaer does not have this pass or it was not compiled in editor yet.");

                cmd.DrawMesh(mesh, m_WorldToObject * renderer.transform.localToWorldMatrix, materials[i], i, pass);
            }
        }

        [Conditional("UNITY_EDITOR")]
        void CheckUniversalPipelineAsset()
        {
            if (UniversalRenderPipeline.asset == null)
                throw new InvalidOperationException("UniversalRenderPipeline.asset is null. Please create a new Universal Render Pipeline Asset in the Project Settings.");
        }

        static void CheckStripDebugVariants()
        {
            if (!Application.isPlaying)
                return;

            RenderPipelineGlobalSettings settings = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>();
            Type settingsType = settings.GetType();

            FieldInfo fieldInfo = settingsType.GetField("m_StripDebugVariants", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                throw new Exception("m_StripDebugVariants field not found");
            }

            bool stripDebugVariants = (bool)fieldInfo.GetValue(settings);
            if (stripDebugVariants)
                throw new InvalidOperationException($"In UniversalRenderPipelineGlobalSettings field Strip Debug Variants must be false to work at player");
        }
    }
}
#endif