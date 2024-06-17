#if HIGH_DEFINITION_RENDER_PIPELINE
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;

namespace ProjectDawn.Impostor
{
    [RenderPipeline(typeof(HDRenderPipelineAsset))]
    public class HDScene : Scene
    {
        CapturePoints m_CapturePoints;
        Transform m_Root;
        Camera m_Camera;
#if UNITY_EDITOR
        bool m_CachedAllowAsync;
#endif

        public override CapturePoints CapturePoints => m_CapturePoints;

        public HDScene(Surface surface, CapturePoints capturePoints)
        {
            var rootGameObject = surface.GameObject;
            var renderers = surface.Renderers;
            var matrix = surface.Matrix;
            int layer = 31;

#if UNITY_EDITOR
            m_CachedAllowAsync = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
            m_CapturePoints = capturePoints;

            m_Root = new GameObject("SceneRoot").transform;
            m_Root.localPosition = new Vector3(matrix[0, 3], matrix[1, 3], matrix[2, 3]);
            m_Root.localRotation = matrix.rotation;
            m_Root.localScale = matrix.lossyScale;

            m_Camera = new GameObject("BakingCamera", typeof(Camera), typeof(HDAdditionalCameraData)).GetComponent<Camera>();
            m_Camera.clearFlags = CameraClearFlags.Color;
            m_Camera.backgroundColor = new Color(1, 0, 1, 0);
            m_Camera.aspect = 1.0f;
            m_Camera.orthographic = true;
            m_Camera.cullingMask = 1 << layer;

            try
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        Clone(renderer.transform, layer);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override void Cleanup()
        {
            GameObject.DestroyImmediate(m_Camera.gameObject);
            GameObject.DestroyImmediate(m_Root.gameObject);
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = m_CachedAllowAsync;
#endif
        }

        public override void Render(RenderTexture target, RenderMode mode)
        {
            var capturePoints = m_CapturePoints;
            int resolution = target.width;

            // TODO: Find cleaner way
            // HDCamera will use camera pixelrect as resolution, in order to have correct one, here we set proxy render target
            var cameraProxyTarget = RenderTexture.GetTemporary(resolution / capturePoints.Frames, resolution / capturePoints.Frames, 32);
            SetProxyTarget(cameraProxyTarget);

            var temp = RenderTexture.GetTemporary(resolution / capturePoints.Frames, resolution / capturePoints.Frames, 32, target.format);

            // Render all frames
            foreach (var snapshot in capturePoints.Points)
            {
                var pixelRect = new Rect(snapshot.Uv.x * resolution, snapshot.Uv.y * resolution,
                    snapshot.Uv.width * resolution, snapshot.Uv.height * resolution);

                // Camera.SubmitRenderRequests currently does not support no clear rendering, with it we could save some performance
                Render(snapshot.From, snapshot.To, 0, capturePoints.Radius * 2, capturePoints.Radius, mode, temp);

                // Pack frames into single texture
                var cmd = CommandBufferPool.Get("Pack Frames");
                cmd.SetRenderTarget(target, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                cmd.SetViewport(pixelRect);
                HDUtils.BlitQuad(cmd, temp, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), 0, false);
                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            RenderTexture.ReleaseTemporary(temp);
            RenderTexture.ReleaseTemporary(cameraProxyTarget);
        }

        public override void RenderCombinedMask(RenderTexture target, Material blitMaterial)
        {
            var capturePoints = m_CapturePoints;
            int resolution = target.width;

            // Clear target
            var cachedActive = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            RenderTexture.active = cachedActive;

            // TODO: Find cleaner way
            // HDCamera will use camera pixelrect as resolution, in order to have correct one, here we set proxy render target
            var cameraProxyTarget = RenderTexture.GetTemporary(resolution, resolution, 32);
            SetProxyTarget(cameraProxyTarget);

            var temp = RenderTexture.GetTemporary(resolution, resolution, 32, target.format);

            // Render all frames
            foreach (var snapshot in capturePoints.Points)
            {
                // Camera.SubmitRenderRequests currently does not support no clear rendering, with it we could save some performance
                Render(snapshot.From, snapshot.To, 0, capturePoints.Radius * 2, capturePoints.Radius, RenderMode.Alpha, temp);

                Graphics.Blit(temp, target, blitMaterial);
            }

            RenderTexture.ReleaseTemporary(temp);
            RenderTexture.ReleaseTemporary(cameraProxyTarget);
        }

        void Clone(Transform source, int layer)
        {
            var instance = GameObject.Instantiate(source.gameObject, null, false);
            var instanceRoot = instance.transform;

            instance.layer = layer;

            instanceRoot.localScale = source.lossyScale;
            instanceRoot.localPosition = source.position;
            instanceRoot.localRotation = source.rotation;

            // Clean out children (we only want the object itself)
            foreach (Transform child in instanceRoot)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }

            instanceRoot.SetParent(m_Root, false);
        }

        void SetProxyTarget(RenderTexture proxy)
        {
            m_Camera.targetTexture = proxy;
        }

        void Render(float3 from, float3 to, float nearClipPlane, float farClipPlane, float orthographicSize, RenderMode renderMode, RenderTexture target)
        {
            if (math.dot(math.normalize(to - from), new float3(0, 1, 0)) >= 1)
                throw new Exception($"Direction {math.normalize(to - from)} to close to up.");

            m_Camera.transform.position = from;
            m_Camera.transform.LookAt(to, Vector3.up);
            m_Camera.nearClipPlane = nearClipPlane;
            m_Camera.farClipPlane = farClipPlane;
            m_Camera.orthographicSize = orthographicSize;
            m_Camera.orthographic = true;

            if (renderMode == RenderMode.Emission)
            {
                // Here is workaround as for some reason emission includes sky which we do not want
                var data = m_Camera.GetComponent<HDAdditionalCameraData>();
                LayerMask volumeLayerMask = data.volumeLayerMask;
                Color backgroundColorHDR = data.backgroundColorHDR;
                data.volumeLayerMask = 0;
                data.backgroundColorHDR = Color.black;
                ProcessRenderRequests(m_Camera, renderMode, target);
                data.volumeLayerMask = volumeLayerMask;
                data.backgroundColorHDR = backgroundColorHDR;
            }
            else
            {
                ProcessRenderRequests(m_Camera, renderMode, target);
            }

        }

        void ProcessRenderRequests(Camera camera, RenderMode mode, RenderTexture target)
        {
            //var previousRT = RenderTexture.active;
            //RenderTexture.active = null;

            var requests = new AOVRequestBuilder();
            //foreach (var request in renderRequests)
            {
                var aovRequest = AOVRequest.NewDefault(); //.SetRenderRequestMode(request.mode);
                FillDebugData(ref aovRequest, mode);
                var aovBuffers = AOVBuffers.Color;
                if (mode == RenderMode.Depth)
                    aovBuffers = AOVBuffers.DepthStencil;

                requests.Add(
                    aovRequest,
                    (AOVBuffers aovBufferId) => RTHandles.Alloc(target),
                    null,
                    new[] { aovBuffers },
                    (cmd, textures, properties) =>
                    {
                    });
            }

            // Setup AOV
            var additionaCameraData = camera.GetComponent<HDAdditionalCameraData>();
            additionaCameraData.SetAOVRequests(requests.Build());

            m_Camera.Render();
        }

        void FillDebugData(ref AOVRequest debug, RenderMode mode)
        {
            switch (mode)
            {
                case RenderMode.BaseColor:
                    debug.SetFullscreenOutput(MaterialSharedProperty.Albedo);
                    break;
                case RenderMode.DiffuseColor:
                    debug.SetFullscreenOutput(LightingProperty.None);
                    break;
                case RenderMode.Alpha:
                    debug.SetFullscreenOutput(MaterialSharedProperty.Alpha);
                    break;
                case RenderMode.Depth:
                    debug.SetFullscreenOutput(DebugFullScreen.Depth);
                    break;
                case RenderMode.Metallic:
                    debug.SetFullscreenOutput(MaterialSharedProperty.Metal);
                    break;
                case RenderMode.Normal:
                    debug.SetFullscreenOutput(MaterialSharedProperty.Normal);
                    break;
                case RenderMode.Smoothness:
                    debug.SetFullscreenOutput(MaterialSharedProperty.Smoothness);
                    break;
                case RenderMode.Occlusion:
                    debug.SetFullscreenOutput(MaterialSharedProperty.AmbientOcclusion);
                    break;
                case RenderMode.Emission:
                    debug.SetFullscreenOutput(LightingProperty.EmissiveOnly);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}
#endif