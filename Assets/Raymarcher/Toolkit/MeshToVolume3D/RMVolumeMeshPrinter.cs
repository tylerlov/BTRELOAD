using System.Collections;

using UnityEngine;

using Raymarcher.Attributes;
using Raymarcher.Utilities;
using Raymarcher.Objects.Volumes;
using Raymarcher.Objects.Modifiers;

namespace Raymarcher.Toolkit
{
    using static RMAttributes;
    using static RMTextureUtils;
    using static RMVolumeUtils;

    public sealed class RMVolumeMeshPrinter : MonoBehaviour
    {
        public enum PrintMode { Instant, Progressive };

        // Serialized & public

        [Space]
        [SerializeField, Required] private RMSdf_Tex3DVolumeBox targetTex3DVolumeBox;
        [SerializeField] private Transform getChildsFromRoot;
        [SerializeField] private MeshFilter[] targetMeshFilters;
        [Header("Print Settings")]
        public CommonVolumeResolution commonVolumeResolution = CommonVolumeResolution.x64;
        public Texture3D initialVolumeCanvas3D;
        [Space]
        public PrintMode printMode = PrintMode.Instant;
        [ShowIf("printMode", 1, setFieldToOppositeValueIfConditionNotMet: true)] public bool travelVertexToVertex = false;
        [ShowIf("travelVertexToVertex", 1, setFieldToOppositeValueIfConditionNotMet: true), Range(0.05f, 64f)] public float vertexTravelDurationInSeconds = 1.0f;
        [Header("Brush Settings")]
        [ShowIf("printMode", 0, false)] public bool radialBrushShape = true;
        public float brushRadius = 0.5f;
        [Range(0f, 1f)] public float brushIntensity01 = 1.0f;
        [Range(0f, 1f)] public float brushSmoothness01 = 1.0f;
        public bool eraseVoxels = false;
        [Header("Material Settings (Optional)")]
        [Range(0, 8)] public int selectedMaterialIndex;
        [Range(0, 8)] public int maxMaterialIndexInstance = 8;
        public RMModifier_VolumeMaterialCompositor materialCompositor;

        // Properties

        public RMSdf_Tex3DVolumeBox TargetTex3DVolumeBox => targetTex3DVolumeBox;
        public RMVolumeVoxelPainter VolumeVoxelPainter => volumeVoxelPainter;
        public MeshFilter[] TargetMeshFilters => targetMeshFilters;

        public bool IsPrinting { get; private set; }
        public float PrintingProgress { get; private set; }
        public Vector3 VirtualBrushWorldPosition { get; private set; }
        public RenderTexture WorkingVolumeCanvas => workingVolumeCanvas;

        // Privates

        private RMVolumeVoxelPainter volumeVoxelPainter;
        private Vector3[] loadedVertices;
        private int[] loadedIndices;

        private Coroutine printingCoroutine;
        private bool setupProperly = false;

        private RenderTexture workingVolumeCanvas;

        private RMVolumeVoxelPainter.MaterialData CreateMaterialData =>
            new RMVolumeVoxelPainter.MaterialData(selectedMaterialIndex, materialCompositor == null ? 1 : materialCompositor.MaterialFamilyTotalCount,
                new RMVolumeVoxelPainter.VoxelException(maxMaterialIndexInstance));


        // Constants

        private const string COMPUTE_NAME = "RMTex3DMeshPrinterCompute";
        private const string COMPUTE_KERNEL_NAME = "VolumeMeshPrinter";
        private const string COMPUTE_TEX3D = "Tex3DInput";
        private const string COMPUTE_VERTCOUNT = "VertexCount";
        private const string COMPUTE_VERTS = "Vertices";
        private const string COMPUTE_RADIUS = "BrushRadius";
        private const string COMPUTE_SMOOTHNESS = "BrushSmoothness";
        private const string COMPUTE_INTENS = "BrushIntensity";
        private const string COMPUTE_MAT = "MaterialValue";
        private const string COMPUTE_TEXRES = "TexRes";
        private const string COMPUTE_SHAPE = "BrushShape";

        private const int THREAD_GROUPS = 8;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_TOOLKIT_PATH + "Mesh Printer")]
        private static void CreateExtraInEditor()
        {
            GameObject go = new GameObject(nameof(RMVolumeMeshPrinter));
            go.AddComponent<RMVolumeMeshPrinter>();
            UnityEditor.Selection.activeObject = go;
        }
#endif

        public void UpdateMeshPrinter(RMVolumeVoxelPainter newTargetVoxelPainter, Transform getChildsFromRoot)
        {
            this.getChildsFromRoot = getChildsFromRoot;
            volumeVoxelPainter = newTargetVoxelPainter;
            StopAndRelease();
        }

        public void UpdateMeshPrinter(RMVolumeVoxelPainter newTargetVoxelPainter, MeshFilter[] newTargetMeshFilters)
        {
            getChildsFromRoot = null;
            targetMeshFilters = newTargetMeshFilters;
            volumeVoxelPainter = newTargetVoxelPainter;
            StopAndRelease();
        }

        public void PrintTargetMeshesToTex3DVolume(bool printAdditive)
        {
            StopAndRelease();
			
			if(targetTex3DVolumeBox == null)
            {
                RMDebug.Debug(this, "Can't print a model. Target Tex3DVolumeBox is null or empty!", true);
                return;
            }

            if (getChildsFromRoot != null)
                targetMeshFilters = getChildsFromRoot.GetComponentsInChildren<MeshFilter>(true);
            if (targetMeshFilters == null || targetMeshFilters.Length == 0)
            {
                RMDebug.Debug(this, "Can't print a model. Target mesh renderers are null or empty");
                return;
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (targetMeshFilters.Length != 1)
            {
                CombineInstance[] combine = new CombineInstance[targetMeshFilters.Length];

                int i = 0;
                while (i < targetMeshFilters.Length)
                {
                    if (targetMeshFilters[i] == null || targetMeshFilters[i].sharedMesh == null)
                    {
                        RMDebug.Debug(this, $"Can't print to a volume texture 3D. One of the mesh renderers at index '{i}' has null reference or empty shared mesh!", true);
                        return;
                    }
                    combine[i].mesh = targetMeshFilters[i].sharedMesh;
                    combine[i].transform = targetMeshFilters[i].transform.localToWorldMatrix;
                    i++;
                }

                mesh.CombineMeshes(combine);
                loadedVertices = mesh.vertices;
            }
            else
            {
                if (targetMeshFilters[0] == null || targetMeshFilters[0].sharedMesh == null)
                {
                    RMDebug.Debug(this, $"Can't print to a volume texture 3D. One of the mesh renderers at index '0' has null reference or empty shared mesh!", true);
                    return;
                }
                mesh = targetMeshFilters[0].sharedMesh;
                loadedVertices = mesh.vertices;
                for (int i = 0; i < loadedVertices.Length; i++)
                    loadedVertices[i] = targetMeshFilters[0].transform.TransformPoint(loadedVertices[i]);
            }

            loadedIndices = mesh.triangles;

            setupProperly = true;

            if(Application.isPlaying)
                printingCoroutine = StartCoroutine(IEPrintModel(printAdditive));
        }

        private void OnDestroy()
        {
            StopAndRelease();
            DisposeSensitiveResources();
        }

        public void StopAndRelease()
        {
            if (printingCoroutine != null)
                StopCoroutine(printingCoroutine);
            printingCoroutine = null;
            loadedVertices = null;
            loadedIndices = null;
            setupProperly = false;
            IsPrinting = false;
            PrintingProgress = 0;
        }

        public void DisposeSensitiveResources()
        {
            volumeVoxelPainter?.Dispose();
            volumeVoxelPainter = null;
            SetWorkingCanvas();
            workingVolumeCanvas = null;
        }

        public void SetWorkingCanvas(RenderTexture newWorkingCanvas = null)
        {
            if (workingVolumeCanvas)
                workingVolumeCanvas.Release();
            workingVolumeCanvas = newWorkingCanvas;
        }

        public IEnumerator IEPrintModel(bool printAdditive)
        {
            if(!setupProperly)
            {
                RMDebug.Debug(this, "Couldn't print a model into volume Tex3D");
                StopAndRelease();
                yield break;
            }

            if(printMode == PrintMode.Instant)
            {
                if (!printAdditive)
                    SetWorkingCanvas();
                PrintInstantly();
                StopAndRelease();
                yield break;
            }

            if (volumeVoxelPainter == null)
                volumeVoxelPainter = new RMVolumeVoxelPainter(commonVolumeResolution, targetTex3DVolumeBox, initialVolumeCanvas3D);
            else if (!printAdditive)
                volumeVoxelPainter.Initialize(commonVolumeResolution, targetTex3DVolumeBox, initialVolumeCanvas3D);

            if (!volumeVoxelPainter.IsInitialized)
            {
                StopAndRelease();
                yield break;
            }

            vertexTravelDurationInSeconds = Mathf.Max(vertexTravelDurationInSeconds, 0.05f);

            VirtualBrushWorldPosition = loadedVertices[loadedIndices[0]];
            IsPrinting = true;

            int currentIndiceIndex = 0;
            int totalIndices = loadedIndices.Length;
            while (currentIndiceIndex < totalIndices)
            {
                if (loadedIndices == null)
                    yield break;

                Vector3 currentPos = VirtualBrushWorldPosition;
                Vector3 currentTarget = loadedVertices[loadedIndices[currentIndiceIndex]];
                if (!travelVertexToVertex)
                {
                    VirtualBrushWorldPosition = currentTarget;
                    volumeVoxelPainter.PaintVoxel(VirtualBrushWorldPosition, brushRadius, brushIntensity01, brushSmoothness01, eraseVoxels, CreateMaterialData);
                    yield return null;
                }
                else
                {
                    float t = 0;
                    while (t < vertexTravelDurationInSeconds)
                    {
                        t += Time.deltaTime;
                        yield return null;
                        VirtualBrushWorldPosition = Vector3.Lerp(currentPos, currentTarget, t / vertexTravelDurationInSeconds);
                        volumeVoxelPainter.PaintVoxel(VirtualBrushWorldPosition, brushRadius, brushIntensity01, brushSmoothness01, eraseVoxels, CreateMaterialData);
                    }
                }
                currentIndiceIndex++;
                if(loadedIndices != null)
                    PrintingProgress = (float)currentIndiceIndex / loadedIndices.Length;
            }

            StopAndRelease();
        }

        private void PrintInstantly()
        {
            int resolution = GetCommonVolumeResolution(commonVolumeResolution);

            if (workingVolumeCanvas == null)
            {
                if (initialVolumeCanvas3D != null)
                {
                    if (!CompareTex3DDimensions(initialVolumeCanvas3D, resolution))
                        return;
                    workingVolumeCanvas = ConvertTexture3DToRenderTexture3D(initialVolumeCanvas3D);
                }
                else
                {
                    workingVolumeCanvas = CreateDynamic3DRenderTexture(resolution, targetTex3DVolumeBox.name + "_Canvas3D");
                }
            }

            ComputeShader shaderResource = Resources.Load<ComputeShader>(COMPUTE_NAME);
            if (shaderResource == null)
            {
                RMDebug.Debug(this, "Couldn't find a compute shader for modifying a 3D render texture", true);
                return;
            }

            ComputeShader targetCS = Instantiate(shaderResource);
            targetCS.name = COMPUTE_NAME;

            int csKernelID = targetCS.FindKernel(COMPUTE_KERNEL_NAME);
            int csThreadGroupWorker = resolution / THREAD_GROUPS;

            if(TargetTex3DVolumeBox.VolumeTexture != workingVolumeCanvas)
                TargetTex3DVolumeBox.VolumeTexture = workingVolumeCanvas;

            targetCS.SetTexture(csKernelID, COMPUTE_TEX3D, workingVolumeCanvas);
            targetCS.SetInt(COMPUTE_TEXRES, resolution);

            ComputeBuffer cb = new ComputeBuffer(loadedVertices.Length, sizeof(float) * 3);
            for (int i = 0; i < loadedVertices.Length; i++)
                loadedVertices[i] = ConvertWorldToVolumeTextureSpace(loadedVertices[i], TargetTex3DVolumeBox, resolution);

            cb.SetData(loadedVertices);

            targetCS.SetInt(COMPUTE_SHAPE, radialBrushShape ? 0 : 1);
            targetCS.SetFloat(COMPUTE_RADIUS, Mathf.Abs(brushRadius));
            targetCS.SetFloat(COMPUTE_SMOOTHNESS, 1 - Mathf.Clamp01(brushSmoothness01));
            targetCS.SetFloat(COMPUTE_INTENS, Mathf.Clamp(eraseVoxels ? -brushIntensity01 : brushIntensity01, -1, 1));
            float matIndex = (float)Mathf.Clamp(selectedMaterialIndex, 0f, 99f) / Mathf.Clamp(materialCompositor != null ? materialCompositor.MaterialFamilyTotalCount : 1, 0f, 99f);
            targetCS.SetFloat(COMPUTE_MAT, Mathf.Clamp01(matIndex));

            targetCS.SetInt(COMPUTE_VERTCOUNT, loadedVertices.Length);

            targetCS.SetBuffer(csKernelID, COMPUTE_VERTS, cb);

            targetCS.Dispatch(csKernelID, csThreadGroupWorker, csThreadGroupWorker, csThreadGroupWorker);

            cb.Release();

            if (Application.isPlaying)
                Destroy(targetCS);
            else
                DestroyImmediate(targetCS);
        }
    }
}