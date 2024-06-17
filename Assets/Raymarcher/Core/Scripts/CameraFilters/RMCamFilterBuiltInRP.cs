using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Raymarcher.Constants;
using Raymarcher.Attributes;

namespace Raymarcher.CameraFilters
{
    using static RMCamFilterUtils;
    using static RMConstants;

    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class RMCamFilterBuiltInRP : MonoBehaviour
    {
        // Serialized fields

        [Header("Raymarcher Camera Filter (BuiltInRP)")]
        [SerializeField] private RMRenderMaster targetMaster;
        [SerializeField] private bool renderInSceneView = true;
        [SerializeField] private bool blitSceneColor = true;
        [SerializeField] private bool disableDepthIfUnused = true;
        [SerializeField] private bool syncEditorCamera = true;
        [Space]
        [SerializeField] private float projectorSize = 0;
        [Space]
        [SerializeField] private bool useDownsampleFeature = false;
        [SerializeField, RMAttributes.ShowIf("useDownsampleFeature", 1), Range(1f, 16f)] private float downsample = 1;
        [SerializeField, RMAttributes.ShowIf("useDownsampleFeature", 1), Range(0f, 1f)] private float sharpness = 0;

        // Privates

        [SerializeField, HideInInspector] private RenderTexture currentSceneColor;
        [SerializeField, HideInInspector] private Camera targetCamera;

        private RenderTexture downsampledRT;
        private Material DownsampleBlitMat
        {
            get
            {
                if(downsampleBlitMat == null)
                {
                    Shader shader = Resources.Load<Shader>(SHADER_NAME_DOWNSAMPLE_BUILT_IN);
                    if (shader == null)
                        RMDebug.Debug(this, $"Shader '{SHADER_NAME_DOWNSAMPLE_BUILT_IN}' couldn't be found in the Resources! Can't downsample the RM frame", true);
                    else
                        downsampleBlitMat = new Material(shader);
                }
                return downsampleBlitMat;
            }
        }
        [SerializeField, HideInInspector] private Material downsampleBlitMat;

        private const float MIN_DOWNSAMPLE = 1f;
        private const float MAX_DOWNSAMPLE = 16f;
        private const string SHADER_NAME_DOWNSAMPLE_BUILT_IN = "RMCamFilterBuiltInDownsampleBlit";
        private const string SHPROP_RM_FRAME = "_RaymarcherFrame";
        private const string SHPROP_SHARPNESS = "_Sharpness";

        // Properties

        public bool SceneViewEditorCamera { get; private set; }
        public RenderTexture CurrentSceneColor
        {
            get
            {
                if (currentSceneColor == null)
                    currentSceneColor = new RenderTexture(960, 540, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
                return currentSceneColor;
            }
        }
        public bool BlitSceneColor { get => blitSceneColor; set => blitSceneColor = value; }
        public bool DisableDepthIfUnused { get => disableDepthIfUnused; set => disableDepthIfUnused = value; }
        public float ProjectorSize { get => projectorSize; set => projectorSize = value; }

        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {		
            Graphics.Blit(source, destination);

            if(targetCamera == null)
            {
                RMDebug.Debug(this, $"Camera component is missing on the object '{name}'!", true);
                return;
            }

            if (SceneViewEditorCamera && !renderInSceneView)
                return;

            if (targetMaster == null)
                return;

            if (targetMaster.RenderingData.UseSceneDepth && targetCamera.depthTextureMode != DepthTextureMode.Depth)
                targetCamera.depthTextureMode = DepthTextureMode.Depth;
            else if(!targetMaster.RenderingData.UseSceneDepth && disableDepthIfUnused && targetCamera.depthTextureMode != DepthTextureMode.None)
                targetCamera.depthTextureMode = DepthTextureMode.None;

            if (!targetMaster.gameObject.activeSelf || !targetMaster.gameObject.activeInHierarchy || !targetMaster.enabled)
                return;

            Material m = targetMaster.RenderingData.RendererSessionMaterialSource;
            if (m == null)
                return;

            if (blitSceneColor)
                Graphics.Blit(source, CurrentSceneColor);
            else if (currentSceneColor != null)
                currentSceneColor = null;
            m.SetTexture(CommonRendererProperties.GrabSceneColor, currentSceneColor);

            Vector3[] outCorners = CalculateFrustum(targetCamera);

            Matrix4x4 matrix = targetCamera.cameraToWorldMatrix;
            m.SetMatrix(CommonRendererProperties.CamSpaceToWorldMatrix, matrix);

            if(useDownsampleFeature)
            {
                float ds = Mathf.Clamp(downsample, MIN_DOWNSAMPLE, MAX_DOWNSAMPLE);
                if (downsampledRT == null)
                    downsampledRT = new RenderTexture(destination.descriptor);
                downsampledRT.Release();
                downsampledRT.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
                downsampledRT.width = Mathf.FloorToInt(destination.width / ds);
                downsampledRT.height = Mathf.FloorToInt(destination.height / ds);
                downsampledRT.Create();

                RenderTexture.active = downsampledRT;
                GL.Clear(true, true, Color.clear);
            }
            else
                RenderTexture.active = destination;

            m.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            AdjustFrustumToProjector(ref outCorners, projectorSize, true);
            GL.MultMatrix(matrix);

            GL.End();
            GL.PopMatrix();

            if (useDownsampleFeature)
            {
                if (!DownsampleBlitMat)
                    Graphics.Blit(downsampledRT, destination);
                else
                {
                    DownsampleBlitMat.SetTexture(SHPROP_RM_FRAME, downsampledRT);
                    DownsampleBlitMat.SetFloat(SHPROP_SHARPNESS, sharpness);
                    Graphics.Blit(downsampledRT, destination, DownsampleBlitMat);
                }
            }

            if (SceneViewEditorCamera)
                return;

#if UNITY_EDITOR
            if(syncEditorCamera)
                UpdateEditor();
#endif
        }

#if UNITY_EDITOR
        private void Reset()
        {
            targetCamera = GetComponent<Camera>();
        }

        private Camera GetEditorCamera
        {
            get
            {
                SceneView sc = SceneView.currentDrawingSceneView;
                if (sc == null)
                {
                    sc = SceneView.lastActiveSceneView;
                    if (sc == null)
                        return null;
                }
                return sc.camera;
            }
        }

        private void UpdateEditor()
        {
            Camera cam = GetEditorCamera;
            if (cam == null)
                return;
            RMCamFilterBuiltInRP filter = cam.GetComponent<RMCamFilterBuiltInRP>();
            if (!filter)
                filter = cam.gameObject.AddComponent<RMCamFilterBuiltInRP>();
            filter.SceneViewEditorCamera = true;
            filter.renderInSceneView = renderInSceneView;
            filter.useDownsampleFeature = useDownsampleFeature;
            filter.downsample = downsample;
            filter.sharpness = sharpness;
            filter.SetupCamFilter(targetMaster, cam);
        }

        public void SetupCamFilter(RMRenderMaster master, Camera cam)
        {
            targetCamera = cam;
            targetMaster = master;
        }

        public void DisposeCamFilter()
        {
            Camera cam = GetEditorCamera;
            if (cam != null && cam.TryGetComponent<RMCamFilterBuiltInRP>(out var filter))
                DestroyImmediate(filter);
            DestroyImmediate(this);
        }
#endif

    }
}