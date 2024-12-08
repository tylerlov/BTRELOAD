using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.Bloom.Runtime
{
  [ExecuteAlways]
  [AddComponentMenu("OccaSoftware/Bloom/Bloom")]
  [RequireComponent(typeof(Camera))]
  public class Bloom : MonoBehaviour
  {
    private Camera mainCamera;
    private RenderPass renderPass;
    public bool renderInSceneView = true;
    public InjectionPoint injectionPoint = InjectionPoint.BeforeRenderingPostProcessing;

    private void OnEnable()
    {
      SubscribeToSceneManagerEvents();
      RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    private void SubscribeToSceneManagerEvents()
    {
#if UNITY_EDITOR
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += Recreate;
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += Recreate;
#endif
      UnityEngine.SceneManagement.SceneManager.activeSceneChanged += Recreate;
    }

    private void UnsubscribeToSceneManagerEvents()
    {
#if UNITY_EDITOR
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= Recreate;
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= Recreate;
#endif
      UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= Recreate;
    }

    private void Recreate(
      UnityEngine.SceneManagement.Scene current,
      UnityEngine.SceneManagement.Scene next
    )
    {
      QueueForReset();
    }

    private void QueueForReset()
    {
      renderPass?.Dispose();
      renderPass = null;
    }

    private void Setup()
    {
      if (mainCamera == null)
      {
        mainCamera = GetComponent<Camera>();
      }

      if (renderPass == null)
      {
        renderPass = new RenderPass();
        renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
      }
    }

    private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
      Setup();

      // Exclude preview and reflection cameras
      if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
        return;

      if (camera.cameraType == CameraType.SceneView && !renderInSceneView)
      {
        return;
      }

      if (camera.cameraType != CameraType.SceneView && camera != mainCamera)
      {
        return;
      }
      renderPass.renderPassEvent = (RenderPassEvent)injectionPoint;
      camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(renderPass);
    }

    private void OnDisable()
    {
      RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
      UnsubscribeToSceneManagerEvents();
      renderPass?.Dispose();
      renderPass = null;
    }
  }

  public enum InjectionPoint
  {
    BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,
    BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
    AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
  }

  internal class RenderPass : ScriptableRenderPass
  {
    int[] downIds;
    int[] upIds;
    private ComputeShader shader;
    private RTHandle target;

    private static readonly string passName = "Bloom";
    private ProfilingSampler sampler;
    private static readonly string shaderFilename = "Bloom";
    private static uint[] kernelThreadGroupSizes = new uint[3] { 8, 8, 1 }; // x, y, z

    private static int downsampleKernel = 0;
    private static int upsampleKernel = 1;
    private static int mergeKernel = 2;
    private static int ghostKernel = 3;
    private static int chromaShiftKernel = 4;
    private static int thresholdKernel = 5;
    private static int blurKernel = 6;

    RTHandle[] downTextures;
    RTHandle[] upTextures;
    int maxIterationsLimit = 9;
    RTHandle source;
    Vector4[] size;
    RTHandle ghost;
    RTHandle chromaShift;
    RTHandle threshold;
    RTHandle blur;

    RenderTextureDescriptor baseDescriptor;

    public RenderPass()
    {
      downIds = new int[maxIterationsLimit];
      upIds = new int[maxIterationsLimit];
      downTextures = new RTHandle[maxIterationsLimit];
      upTextures = new RTHandle[maxIterationsLimit];
      size = new Vector4[maxIterationsLimit];
      for (int i = 0; i < maxIterationsLimit; i++)
      {
        downIds[i] = Shader.PropertyToID("_DownBloom" + i);
        upIds[i] = Shader.PropertyToID("_UpBloom" + i);
        downTextures[i] = RTHandles.Alloc(
          new RenderTargetIdentifier(downIds[i]),
          name: "_DownBloom" + i
        );
        upTextures[i] = RTHandles.Alloc(new RenderTargetIdentifier(upIds[i]), name: "_UpBloom" + i);
      }
      ghost = RTHandles.Alloc(new RenderTargetIdentifier("_Ghost"), name: "_Ghost");
      chromaShift = RTHandles.Alloc(
        new RenderTargetIdentifier("_ChromaShift"),
        name: "_ChromaShift"
      );
      blur = RTHandles.Alloc(new RenderTargetIdentifier("_Blur"), name: "_Blur");
      threshold = RTHandles.Alloc(new RenderTargetIdentifier("_Threshold"), name: "_Threshold");
      target = RTHandles.Alloc("_BloomTarget");
    }

    public bool LoadComputeShader()
    {
      if (shader != null)
        return true;

      shader = (ComputeShader)Resources.Load(shaderFilename);
      if (shader == null)
        return false;

      return true;
    }

    private static int GetGroupCount(int textureDimension, uint groupSize)
    {
      return Mathf.CeilToInt((textureDimension + groupSize - 1) / groupSize);
    }

    BloomOverride bloom;

    /// <summary>
    /// Get the Bloom component from the Volume Manager stack.
    /// </summary>
    /// <returns>If Bloom component is null or inactive, returns false.</returns>
    internal bool RegisterStackComponent()
    {
      bloom = VolumeManager.instance.stack.GetComponent<BloomOverride>();

      if (bloom == null)
        return false;

      return bloom.IsActive();
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
      LoadComputeShader();

      baseDescriptor = renderingData.cameraData.cameraTargetDescriptor;
      baseDescriptor.depthBufferBits = 0;
      baseDescriptor.enableRandomWrite = true;
      baseDescriptor.msaaSamples = 1;
      baseDescriptor.width = Mathf.Max(1, baseDescriptor.width);
      baseDescriptor.height = Mathf.Max(1, baseDescriptor.height);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
      if (shader == null)
        return;

      if (!RegisterStackComponent())
        return;

      CommandBuffer cmd = CommandBufferPool.Get(passName);

      using (new ProfilingScope(cmd, sampler))
      {
        source = renderingData.cameraData.renderer.cameraColorTargetHandle;

        var d = baseDescriptor;
        var halfRes = baseDescriptor;
        halfRes.width >>= 1;
        halfRes.height >>= 1;

        int groupsX,
          groupsY;

        RenderingUtils.ReAllocateIfNeeded(ref ghost, halfRes, name: "_Ghost");
        RenderingUtils.ReAllocateIfNeeded(ref chromaShift, halfRes, name: "_ChromaShift");
        RenderingUtils.ReAllocateIfNeeded(ref threshold, halfRes, name: "_Threshold");
        RenderingUtils.ReAllocateIfNeeded(ref blur, halfRes, name: "_Blur");
        RenderingUtils.ReAllocateIfNeeded(ref target, d, name: "_TargetBloom");
        for (int i = 0; i < maxIterationsLimit; i++)
        {
          RenderingUtils.ReAllocateIfNeeded(
            ref downTextures[i],
            d,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: downTextures[i].name
          );

          RenderingUtils.ReAllocateIfNeeded(
            ref upTextures[i],
            d,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: upTextures[i].name
          );

          size[i] = new Vector4(d.width, d.height, 1.0f / d.width, 1.0f / d.height);

          d.width = Mathf.Max(1, d.width >> 1);
          d.height = Mathf.Max(1, d.height >> 1);
        }

        cmd.SetComputeFloatParam(shader, "_InternalBlend", bloom.internalBlend.value);
        cmd.SetComputeFloatParam(shader, "_FinalBlend", bloom.finalBlend.value);

        cmd.SetComputeFloatParam(shader, "_ThresholdEdge", bloom.thresholdEdge.value);
        cmd.SetComputeFloatParam(shader, "_ThresholdRange", bloom.thresholdRange.value);

        cmd.SetComputeFloatParam(shader, "_GhostIntensity", bloom.ghostIntensity.value);
        cmd.SetComputeVectorParam(shader, "_GhostChromaSpread", bloom.ghostChromaSpread.value);
        cmd.SetComputeVectorParam(shader, "_GhostTint", bloom.ghostTint.value);

        cmd.SetComputeFloatParam(shader, "_HaloIntensity", bloom.haloIntensity.value);
        cmd.SetComputeFloatParam(shader, "_HaloFisheyeStrength", bloom.haloFisheyeStrength.value);
        cmd.SetComputeFloatParam(shader, "_HaloFisheyeWidth", bloom.haloFisheyeWidth.value);
        cmd.SetComputeVectorParam(shader, "_HaloChromaSpread", bloom.haloChromaSpread.value);
        cmd.SetComputeVectorParam(shader, "_HaloTint", bloom.haloTint.value);

        cmd.SetGlobalFloatArray("_GhostDistances", bloom.PackGhostSpreads());
        cmd.SetComputeVectorArrayParam(shader, "_GhostTints", bloom.PackGhostTints());

        Blitter.BlitCameraTexture(cmd, source, downTextures[0]);

        // Downsample
        for (int i = 1; i < bloom.bloomMaxIterations.value; i++)
        {
          // Set Compute...
          // set source tex + dimensions
          // set dest tex + dimensions

          cmd.SetComputeVectorParam(shader, "_SrcScreenSizePx", size[i - 1]);
          cmd.SetComputeVectorParam(shader, "_DstScreenSizePx", size[i]);
          groupsX = GetGroupCount((int)size[i].x, kernelThreadGroupSizes[0]);
          groupsY = GetGroupCount((int)size[i].y, kernelThreadGroupSizes[1]);

          cmd.SetComputeTextureParam(shader, downsampleKernel, "_Src", downTextures[i - 1]);
          cmd.SetComputeTextureParam(shader, downsampleKernel, "_DstWritable", downTextures[i]);
          cmd.DispatchCompute(shader, downsampleKernel, groupsX, groupsY, 1);
        }

        // Upsample
        for (int i = bloom.bloomMaxIterations.value - 2; i >= 0; i--)
        {
          // Set Compute...
          // set source tex + dimensions
          // set dest tex + dimensions

          var src =
            (i == bloom.bloomMaxIterations.value - 2) ? downTextures[i + 1] : upTextures[i + 1];
          var dst = upTextures[i];

          cmd.SetComputeVectorParam(shader, "_SrcScreenSizePx", size[i + 1]);
          cmd.SetComputeVectorParam(shader, "_DstScreenSizePx", size[i]);
          groupsX = GetGroupCount((int)size[i].x, kernelThreadGroupSizes[0]);
          groupsY = GetGroupCount((int)size[i].y, kernelThreadGroupSizes[1]);

          cmd.SetComputeTextureParam(shader, upsampleKernel, "_DownSrc", downTextures[i]);
          cmd.SetComputeTextureParam(shader, upsampleKernel, "_Src", src);
          cmd.SetComputeTextureParam(shader, upsampleKernel, "_DstWritable", dst);
          cmd.DispatchCompute(shader, upsampleKernel, groupsX, groupsY, 1);
        }

        cmd.SetComputeVectorParam(shader, "_DstScreenSizePx", size[1]);
        groupsX = GetGroupCount((int)size[1].x, kernelThreadGroupSizes[0]);
        groupsY = GetGroupCount((int)size[1].y, kernelThreadGroupSizes[1]);

        // Threshold
        cmd.SetComputeTextureParam(shader, thresholdKernel, "_Src", downTextures[1]);
        cmd.SetComputeTextureParam(shader, thresholdKernel, "_DstWritable", threshold);
        cmd.DispatchCompute(shader, thresholdKernel, groupsX, groupsY, 1);

        // Chroma
        cmd.SetComputeTextureParam(shader, chromaShiftKernel, "_Src", threshold);
        cmd.SetComputeTextureParam(shader, chromaShiftKernel, "_DstWritable", chromaShift);
        cmd.DispatchCompute(shader, chromaShiftKernel, groupsX, groupsY, 1);

        // Ghost
        cmd.SetComputeTextureParam(shader, ghostKernel, "_Src", chromaShift);
        cmd.SetComputeTextureParam(shader, ghostKernel, "_Threshold", threshold);
        cmd.SetComputeTextureParam(shader, ghostKernel, "_DstWritable", ghost);
        cmd.DispatchCompute(shader, ghostKernel, groupsX, groupsY, 1);

        // Blur
        cmd.SetComputeTextureParam(shader, blurKernel, "_Src", ghost);
        cmd.SetComputeTextureParam(shader, blurKernel, "_DstWritable", blur);
        cmd.DispatchCompute(shader, blurKernel, groupsX, groupsY, 1);

        // Merge
        cmd.SetComputeVectorParam(shader, "_DstScreenSizePx", size[0]);
        groupsX = GetGroupCount((int)size[0].x, kernelThreadGroupSizes[0]);
        groupsY = GetGroupCount((int)size[0].y, kernelThreadGroupSizes[1]);
        cmd.SetComputeTextureParam(shader, mergeKernel, "_Bloom", upTextures[0]);
        cmd.SetComputeTextureParam(shader, mergeKernel, "_Ghost", blur);
        cmd.SetComputeTextureParam(shader, mergeKernel, "_CameraColor", source);
        cmd.SetComputeTextureParam(shader, mergeKernel, "_DstWritable", target);
        cmd.DispatchCompute(shader, mergeKernel, groupsX, groupsY, 1);

        // To Screen
        Blitter.BlitCameraTexture(cmd, target, source);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
      }
    }

    public void Dispose()
    {
      foreach (RTHandle handle in downTextures)
      {
        handle?.Release();
      }
      foreach (RTHandle handle in upTextures)
      {
        handle?.Release();
      }

      ghost?.Release();
      chromaShift?.Release();
      threshold?.Release();
      blur?.Release();
      source?.Release();
    }
  }
}
