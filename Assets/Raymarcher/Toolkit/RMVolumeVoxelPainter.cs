using System;

using UnityEngine;

using Raymarcher.Objects.Volumes;
using Raymarcher.Utilities;

namespace Raymarcher.Toolkit
{
    using static RMVolumeUtils;
    using static RMTextureUtils;

    public sealed class RMVolumeVoxelPainter : IDisposable
    {
        // Privates

        private int csKernelID;
        private int csThreadGroupWorker;
        private ComputeShader targetCS;

        // Properties

        public bool IsInitialized { get; private set; }
        public int CurrentVolumeResolution { get; private set; }
        public CommonVolumeResolution CurrentCommonVolumeResolution { get; private set; }

        public RMSdf_Tex3DVolumeBox TargetTex3DVolumeBox { get; private set; }
        public RenderTexture WorkingVolumeCanvas3D { get; private set; }
        public Texture3D InitialVolumeCanvas3D { get; private set; }

        // Constants

        private const string COMPUTE_NAME = "RMTex3DVoxelPainterCompute";
        private const string COMPUTE_KERNEL_NAME = "VolumeVoxelPainter";
        private const string COMPUTE_TEX3D = "Tex3DInput";
        private const string COMPUTE_COORDS = "BrushCoords";
        private const string COMPUTE_RADIUS = "BrushRadius";
        private const string COMPUTE_SMOOTHNESS = "BrushSmoothness";
        private const string COMPUTE_INTENS = "BrushIntensity";
        private const string COMPUTE_MATEXCP = "MaterialException";
        private const string COMPUTE_MATCOUNT = "MaterialCount";
        private const string COMPUTE_MATEXCPINC = "IncludeExcept";
        private const string COMPUTE_MAT = "MaterialValue";
        private const string COMPUTE_EXTRA = "ExtraValue";
        private const string COMPUTE_EXTRAINC = "IncludeExtra";
        private const string COMPUTE_TEXRES = "TexRes";

        private const int THREAD_GROUPS = 8;

        public RMVolumeVoxelPainter(CommonVolumeResolution targetCommonVolumeResolution, RMSdf_Tex3DVolumeBox targetTex3DVolumeBox, Texture3D initialVolumeCanvas3D = null)
            => Initialize(targetCommonVolumeResolution, targetTex3DVolumeBox, initialVolumeCanvas3D);

        public struct MaterialData
        {
            public readonly int materialInstanceIndex;
            public readonly int materialTotalCount;
            public readonly VoxelException voxelException;

            public MaterialData(int materialInstanceIndex, int materialTotalCount, VoxelException voxelException = default)
            {
                this.materialInstanceIndex = materialInstanceIndex;
                this.materialTotalCount = materialTotalCount;
                this.voxelException = voxelException;
            }
        }

        public struct VoxelException
        {
            public readonly int maxMaterialInstanceIndex;
            public readonly bool useVoxelException;
            public VoxelException(int maxMaterialInstanceIndex)
            {
                this.maxMaterialInstanceIndex = maxMaterialInstanceIndex;
                useVoxelException = maxMaterialInstanceIndex > 0;
            }
        }

        public void Dispose()
        {
            if (WorkingVolumeCanvas3D != null)
            {
                WorkingVolumeCanvas3D.Release();
                WorkingVolumeCanvas3D = null;
            }

            if (targetCS != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(targetCS);
                else
                    UnityEngine.Object.DestroyImmediate(targetCS);
                targetCS = null;
            }

            TargetTex3DVolumeBox = null;
            InitialVolumeCanvas3D = null;

            IsInitialized = false;
        }

        public void Initialize(CommonVolumeResolution targetCommonVolumeResolution, RMSdf_Tex3DVolumeBox targetTex3DVolumeBox, Texture3D initialVolumeCanvas3D = null)
        {
            if (IsInitialized)
                Dispose();

            TargetTex3DVolumeBox = targetTex3DVolumeBox;
            if (TargetTex3DVolumeBox == null)
            {
                RMDebug.Debug(this, $"Couldn't initialize the voxel-painting canvas 3D. Input parameter '{nameof(targetTex3DVolumeBox)}' is null!", true);
                return;
            }

            CurrentCommonVolumeResolution = targetCommonVolumeResolution;
            CurrentVolumeResolution = GetCommonVolumeResolution(CurrentCommonVolumeResolution);

            if (initialVolumeCanvas3D != null)
            {
                if (!CompareTex3DDimensions(initialVolumeCanvas3D, CurrentVolumeResolution))
                    return;
                InitialVolumeCanvas3D = initialVolumeCanvas3D;
                WorkingVolumeCanvas3D = ConvertTexture3DToRenderTexture3D(InitialVolumeCanvas3D);
            }
            else
            {
                WorkingVolumeCanvas3D = CreateDynamic3DRenderTexture(CurrentVolumeResolution, targetTex3DVolumeBox.name + "_Canvas3D");
            }

            ComputeShader shaderResource = Resources.Load<ComputeShader>(COMPUTE_NAME);
            if (shaderResource == null)
            {
                RMDebug.Debug(this, "Couldn't find a compute shader for modifying a 3D render texture while initializing the volume painter canvas", true);
                Dispose();
                return;
            }

            targetCS = UnityEngine.Object.Instantiate(shaderResource);
            targetCS.name = COMPUTE_NAME;

            csKernelID = targetCS.FindKernel(COMPUTE_KERNEL_NAME);
            csThreadGroupWorker = CurrentVolumeResolution / THREAD_GROUPS;

            TargetTex3DVolumeBox.VolumeTexture = WorkingVolumeCanvas3D;

            targetCS.SetTexture(csKernelID, COMPUTE_TEX3D, WorkingVolumeCanvas3D);
            targetCS.SetInt(COMPUTE_TEXRES, CurrentVolumeResolution);

            IsInitialized = true;
        }

        public void PaintVoxel(Vector3 worldSpaceCoordinates, float brushRadius, float brushIntensity01 = 0.5f, float brushSmoothness01 = 1, bool erase = false,
            MaterialData materialData = default)
        {
            if (!IsInitialized)
            {
                RMDebug.Debug(this, $"'{nameof(RMVolumeVoxelPainter)}' is not initialized. Call '{nameof(Initialize)}' first before painting voxels");
                return;
            }
            if (!TargetTex3DVolumeBox)
            {
                RMDebug.Debug(this, $"'{nameof(TargetTex3DVolumeBox)}' is null!", true);
                return;
            }

            targetCS.SetFloat(COMPUTE_EXTRAINC, 0);
            SetCommonData(worldSpaceCoordinates, brushRadius, brushIntensity01, brushSmoothness01, erase, materialData);
        }

        public void PaintMaterialOnly(Vector3 worldSpaceCoordinates, MaterialData materialData, float brushRadius, float brushSmoothness01 = 1)
        {
            if (!IsInitialized)
            {
                RMDebug.Debug(this, $"'{nameof(RMVolumeVoxelPainter)}' is not initialized. Call '{nameof(Initialize)}' first before painting voxels");
                return;
            }
            if (!TargetTex3DVolumeBox)
            {
                RMDebug.Debug(this, $"'{nameof(TargetTex3DVolumeBox)}' is null!", true);
                return;
            }

            targetCS.SetFloat(COMPUTE_EXTRAINC, 0);
            SetCommonData(worldSpaceCoordinates, brushRadius, 0, brushSmoothness01, false, materialData);
        }

        private void SetCommonData(Vector3 worldSpaceCoordinates, float brushRadius, float brushIntensity01, float brushSmoothness01, bool erase, MaterialData materialData)
        {
            targetCS.SetVector(COMPUTE_COORDS, ConvertWorldToVolumeTextureSpace(worldSpaceCoordinates, TargetTex3DVolumeBox, CurrentVolumeResolution));
            targetCS.SetFloat(COMPUTE_RADIUS, Mathf.Abs(brushRadius));
            targetCS.SetFloat(COMPUTE_SMOOTHNESS, 1 - Mathf.Clamp01(brushSmoothness01));
            targetCS.SetFloat(COMPUTE_INTENS, Mathf.Clamp(erase ? -brushIntensity01 : brushIntensity01, -1, 1));
            float matCount = Mathf.Clamp(materialData.materialTotalCount, 0f, 99f);
            targetCS.SetFloat(COMPUTE_MAT, Mathf.Clamp01((float)Mathf.Clamp(materialData.materialInstanceIndex, 0f, 99f) / matCount));

            targetCS.SetFloat(COMPUTE_MATEXCPINC, materialData.voxelException.useVoxelException ? 1f : 0f);
            targetCS.SetFloat(COMPUTE_MATEXCP, Mathf.Clamp01(materialData.voxelException.maxMaterialInstanceIndex / (float)matCount));

            targetCS.Dispatch(csKernelID, csThreadGroupWorker, csThreadGroupWorker, csThreadGroupWorker);
        }

        public void PaintToExtraChannel(Vector3 worldSpaceCoordinates, float brushRadius, float channelValue01, float brushSmoothness01, MaterialData materialData)
        {
            if (!IsInitialized)
            {
                RMDebug.Debug(this, $"'{nameof(RMVolumeVoxelPainter)}' is not initialized. Call '{nameof(Initialize)}' first before painting voxels");
                return;
            }
            if (!TargetTex3DVolumeBox)
            {
                RMDebug.Debug(this, $"'{nameof(TargetTex3DVolumeBox)}' is null!", true);
                return;
            }

            targetCS.SetFloat(COMPUTE_EXTRAINC, 1);
            targetCS.SetFloat(COMPUTE_EXTRA, channelValue01);
            SetCommonData(worldSpaceCoordinates, brushRadius, 0, brushSmoothness01, false, materialData);
        }
    }
}