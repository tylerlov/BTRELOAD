using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.LocalGI.Runtime
{
    public class LocalGIRendererFeature : ScriptableRendererFeature
    {
        private class LocalGIRenderPass : ScriptableRenderPass
        {
            private const string shaderName = "LocalGICompute";
            private ComputeShader shader = null;

            private RenderTargetHandle globalIlluminationTarget;

            private const string globalIlluminationTargetId = "_GlobalIlluminationTarget";

            private const string profilerTag = "Local GI";
            private const string cmdBufferName = "Local GI";
            int targetKernel;
            int groupsX;
            int groupsY;

            /// <summary>
            /// The LocalGIHandler for the local global illumination.
            /// </summary>
            LocalGIHandler localGIHandler;

            /// <summary>
            /// Initializes a new instance of the LocalGlobalIlluminationPass class.
            /// </summary>
            public LocalGIRenderPass()
            {
                globalIlluminationTarget.Init(globalIlluminationTargetId);
            }

            /// <summary>
            /// Sets up the LocalGlobalIlluminationPass.
            /// </summary>
            /// <param name="localGIHandler">The LocalGIHandler to use for the local global illumination.</param>
            public void Setup(LocalGIHandler localGIHandler)
            {
                this.localGIHandler = localGIHandler;
                ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
            }

            /// <summary>
            /// Loads the compute shader for LocalGlobalIllumination.
            /// </summary>
            /// <returns>True if the shader was successfully loaded, false otherwise.</returns>
            public bool LoadComputeShader()
            {
                if (shader != null)
                    return true;

                shader = (ComputeShader)Resources.Load(shaderName);
                if (shader == null)
                    return false;

                return true;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.enableRandomWrite = true;
                descriptor.msaaSamples = 1;

                cmd.GetTemporaryRT(globalIlluminationTarget.id, descriptor);

                targetKernel = shader.FindKernel("ComputeScreenSpaceLocalGI");

                shader.GetKernelThreadGroupSizes(targetKernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
                groupsX = GetGroupCount(descriptor.width, threadGroupSizeX);
                groupsY = GetGroupCount(descriptor.height, threadGroupSizeY);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (localGIHandler == null)
                    return;

                Profiler.BeginSample(profilerTag);
                CommandBuffer cmd = CommandBufferPool.Get(cmdBufferName);

                ConfigureTarget(globalIlluminationTarget.Identifier());
                ConfigureClear(ClearFlag.All, Color.black);

                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

                cmd.SetGlobalVector(ShaderParams._LocalGIProbePosition, localGIHandler.transform.position);
                cmd.SetGlobalFloat(ShaderParams._LocalGIMaxDistance, localGIHandler.maximumInfluenceDistance);
                cmd.SetGlobalTexture(ShaderParams._DiffuseIrradianceData, localGIHandler.IrradianceData.IrradianceTexture);

                cmd.SetComputeTextureParam(shader, targetKernel, ShaderParams._ScreenTexture, source);
                cmd.SetComputeVectorParam(
                    shader,
                    ShaderParams._ScreenSizePx,
                    new Vector2(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height)
                );

                cmd.DispatchCompute(shader, targetKernel, groupsX, groupsY, 1);

                Blit(cmd, globalIlluminationTarget.Identifier(), source);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
                Profiler.EndSample();
            }

            private static class ShaderParams
            {
                public static int _EnvironmentMap = Shader.PropertyToID("_EnvironmentMap");
                public static int _LocalGIProbePosition = Shader.PropertyToID("_LocalGIProbePosition");
                public static int _DiffuseIrradianceData = Shader.PropertyToID("_DiffuseIrradianceData");
                public static int _ScreenTexture = Shader.PropertyToID("_ScreenTexture");
                public static int _ScreenSizePx = Shader.PropertyToID("_ScreenSizePx");
                public static int _LocalGIMaxDistance = Shader.PropertyToID("_LocalGIMaxDistance");
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(globalIlluminationTarget.id);
            }

            private int GetGroupCount(int textureDimension, uint groupSize)
            {
                return Mathf.CeilToInt((textureDimension + groupSize - 1) / groupSize);
            }
        }

        private bool DeviceSupportsComputeShaders()
        {
            const int _COMPUTE_SHADER_LEVEL = 45;
            if (SystemInfo.graphicsShaderLevel >= _COMPUTE_SHADER_LEVEL)
                return true;

            return false;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += Recreate;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += Recreate;
#endif
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += Recreate;
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= Recreate;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= Recreate;
#endif
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= Recreate;
        }

        private void Recreate(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.Scene next)
        {
            Create();
        }

        [System.Serializable]
        public class Settings
        {
            [Tooltip("Forward is compatible with Forward and Deferred Render Paths. Deferred is only compatible with the Deferred Render Path.")]
            public RenderPath renderPath = RenderPath.Deferred;
        }

        public enum RenderPath
        {
            Forward,
            Deferred
        }

        public Settings settings;

        private LocalGIRenderPass localGlobalIlluminationPass;

        public override void Create()
        {
            localGlobalIlluminationPass = new LocalGIRenderPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.renderPath == RenderPath.Deferred)
            {
                localGlobalIlluminationPass.renderPassEvent = RenderPassEvent.AfterRenderingDeferredLights;
                Shader.EnableKeyword("_LGI_USE_GBUFFER");
            }
            else
            {
                localGlobalIlluminationPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                Shader.DisableKeyword("_LGI_USE_GBUFFER");
            }

            if (!DeviceSupportsComputeShaders())
            {
                Debug.LogWarning("Local GI requires Compute Shader support.", this);
                return;
            }

            if (IsExcludedCameraType(renderingData.cameraData.camera.cameraType))
                return;

            if (!localGlobalIlluminationPass.LoadComputeShader())
                return;

            if (!renderingData.cameraData.postProcessEnabled)
                return;

            LocalGIHandler localGIHandler = FindObjectOfType<LocalGIHandler>();
            if (localGIHandler == null)
                return;

            localGlobalIlluminationPass.Setup(localGIHandler);
            renderer.EnqueuePass(localGlobalIlluminationPass);
        }

        private bool IsExcludedCameraType(CameraType type)
        {
            switch (type)
            {
                case CameraType.Preview:
                    return true;
                case CameraType.Reflection:
                    return true;
                default:
                    return false;
            }
        }
    }
}
