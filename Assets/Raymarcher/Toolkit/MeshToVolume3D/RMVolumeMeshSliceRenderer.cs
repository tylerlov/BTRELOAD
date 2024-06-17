using UnityEngine;

using Raymarcher.Attributes;
using Raymarcher.Utilities;

namespace Raymarcher.Toolkit
{
    using static RMAttributes;
    using static RMVolumeUtils;

    [ExecuteAlways]
    public sealed class RMVolumeMeshSliceRenderer : MonoBehaviour
    {
        [Header("Camera Settings")]
        public bool uniformCamera = false;
        [ShowIf("uniformCamera", 0)] public float cameraFar = 10;
        [ShowIf("uniformCamera", 0)] public float cameraSize = 2;
        [ShowIf("uniformCamera", 1)] public float uniformCameraViewport = 2;

        public LayerMask renderingLayerMask = ~0;
        [Required] public Material targetMaterial;

        [Header("Slice Settings")]
        public bool useCommonVolumeResolution = true;
        [ShowIf("useCommonVolumeResolution", 1)] public CommonVolumeResolution volumeResolution = CommonVolumeResolution.x64;
        [ShowIf("useCommonVolumeResolution", 0), Range(8, 512)] public int countOfSlices = 32;
        [ShowIf("useCommonVolumeResolution", 0), Range(8, 512)] public int sliceResolution = 100;
        [Range(0.01f, 1f)] public float sliceWidth = 0.01f;

#if UNITY_EDITOR
        [Header("Adjustments (Experimental)")]
        [SerializeField] private bool adjustToView = false;
        [SerializeField, ShowIf("adjustToView", 1), Range(0f, 180f)] private float adjustDerivateX = 2;
        [SerializeField, ShowIf("adjustToView", 1), Range(0f, 180f)] private float adjustDerivateY = 2;

        [Header("Debug")]
        [SerializeField] private bool previewSlice = false;
        [SerializeField, ShowIf("previewSlice", 1), Range(0.0f, 1f)] private float sliceProgression = 0.5f;
#endif

        [SerializeField, HideInInspector] private Camera renderCamera;
        [SerializeField, HideInInspector] private RenderTexture renderCameraOutputRT;

        public Camera RenderCamera => renderCamera;
        public RenderTexture RenderCameraOutput => renderCameraOutputRT;

#if UNITY_EDITOR
        public bool IsRenderingSlices { get; set; }

        public const string PROPERTY_PREVIEW = "_PreviewSlice";
        public const string PROPERTY_SLICE = "_Slice";
        public const string PROPERTY_SLICE_WIDTH = "_SliceWidth";
        public const string PROPERTY_MIN = "_Min";
        public const string PROPERTY_MAX = "_Max";
        public const string PROPERTY_ADJUST = "_AdjustToView";
        public const string PROPERTY_ADJUST_DERIVATEX = "_AdjustDerivateX";
        public const string PROPERTY_ADJUST_DERIVATEY = "_AdjustDerivateY";
#endif

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_TOOLKIT_PATH + "Texture 3D Renderer (Slicer)")]
        private static void CreateExtraInEditor()
        {
            GameObject go = new GameObject(nameof(RMVolumeMeshSliceRenderer));
            go.AddComponent<RMVolumeMeshSliceRenderer>();
            UnityEditor.Selection.activeObject = go;
        }

        private void Reset()
        {
            InitializeTex3DRenderer();
        }

        private void Update()
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if(uniformCamera)
            {
                cameraFar = uniformCameraViewport;
                cameraSize = uniformCameraViewport / 2f;
            }

            sliceResolution = Mathf.Clamp(sliceResolution, 8, 512 - countOfSlices / 2);

            UpdateTex3DRenderer();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 pos = Vector3.zero;
            Vector3 size = Vector3.one * cameraSize * 2f;
            size.z = cameraFar;
            pos.z = size.z - size.z / 2f;
            Gizmos.DrawWireCube(pos, size);
        }

        private void InitializeTex3DRenderer()
        {
            renderCameraOutputRT = new RenderTexture(512, 512, 0);

            renderCamera = GetComponent<Camera>();
            if (!renderCamera)
                renderCamera = gameObject.AddComponent<Camera>();

            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.black;
            renderCamera.cullingMask = renderingLayerMask;
            renderCamera.orthographic = true;
            renderCamera.nearClipPlane = 0.01f;
            renderCamera.targetTexture = renderCameraOutputRT;
            renderCamera.useOcclusionCulling = false;
            renderCamera.hideFlags = HideFlags.HideInInspector;

            UpdateTex3DRenderer();
        }

        private void UpdateTex3DRenderer()
        {
            if (IsRenderingSlices)
                return;

            if (!renderCamera)
                return;

            if(renderCamera.targetTexture != renderCameraOutputRT)
                renderCamera.targetTexture = renderCameraOutputRT;

            renderCamera.farClipPlane = cameraFar;
            renderCamera.orthographicSize = cameraSize;
            renderCamera.cullingMask = renderingLayerMask;

            if (targetMaterial)
            {
                targetMaterial.SetFloat(PROPERTY_ADJUST, adjustToView ? 1 : 0);
                targetMaterial.SetFloat(PROPERTY_ADJUST_DERIVATEX, adjustDerivateX);
                targetMaterial.SetFloat(PROPERTY_ADJUST_DERIVATEY, adjustDerivateY);

                targetMaterial.SetFloat(PROPERTY_PREVIEW, previewSlice ? 1 : 0);
                targetMaterial.SetFloat(PROPERTY_SLICE, sliceProgression);
                targetMaterial.SetFloat(PROPERTY_SLICE_WIDTH, sliceWidth);
                targetMaterial.SetVector(PROPERTY_MIN, transform.position - (transform.up * cameraSize) - (transform.right * cameraSize));
                targetMaterial.SetVector(PROPERTY_MAX, transform.position + (transform.forward * cameraFar) + (transform.up * cameraSize) + (transform.right * cameraSize));
            }
        }
#endif
    }
}