using UnityEngine;

namespace Raymarcher.Toolkit
{
    public sealed class RMVolumeRendererPD : MonoBehaviour
    {
        [Space]
        [SerializeField] private float volumeSize = 1;
        [SerializeField] private float nearClipOffset = 0;
        [SerializeField] private LayerMask volumeLayerMask = ~0;
        [Space]
        [SerializeField] private Camera topCameraRender;
        [SerializeField] private Camera rightCameraRender;
        [SerializeField] private Camera frontCameraRender;

        public float VolumeSize { get => volumeSize; set { volumeSize = value; RefreshVolumeRenderer(); } }
        public int VolumeResolution => volumeResolution;
        public bool VolumeFilteringBilinear => volumeFilteringBilinear;

        public RenderTexture RTTop => topRT;
        public RenderTexture RTRight => rightRT;
        public RenderTexture RTFront => frontRT;    

        [SerializeField, HideInInspector] private RenderTexture topRT;
        [SerializeField, HideInInspector] private RenderTexture rightRT;
        [SerializeField, HideInInspector] private RenderTexture frontRT;

        [SerializeField, HideInInspector] private int volumeResolution = 500;
        [SerializeField, HideInInspector] private bool volumeFilteringBilinear = true;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_TOOLKIT_PATH + "Perspective Driven (PD) Volume Renderer")]
        private static void CreateExtraInEditor()
        {
            GameObject go = new GameObject(nameof(RMVolumeRendererPD));
            var vo = go.AddComponent<RMVolumeRendererPD>();
            vo.SetupVolumeRenderer();
            UnityEditor.Selection.activeObject = go;
        }
#endif

        public void SetupVolumeRenderer()
        {
            if(topCameraRender != null)
                DestroyTarget(topCameraRender.gameObject);
            if (rightCameraRender != null)
                DestroyTarget(rightCameraRender.gameObject);
            if (frontCameraRender != null)
                DestroyTarget(frontCameraRender.gameObject);
           
            topCameraRender = rightCameraRender = frontCameraRender = null;

            if (RTTop)
                RTTop.Release();
            if(RTRight)
                RTRight.Release();
            if(RTFront)
                RTFront.Release();

            topRT = rightRT = frontRT = null;

            CreateRT(ref topRT, "RT_TOP");
            CreateRT(ref rightRT, "RT_RIGHT");
            CreateRT(ref frontRT, "RT_FRONT");

            RefreshCamera(ref topCameraRender, topRT, "VOLUME_CAM_TOP");
            RefreshCamera(ref rightCameraRender, rightRT, "VOLUME_CAM_RIGHT");
            RefreshCamera(ref frontCameraRender, frontRT, "VOLUME_CAM_FRONT");

            RefreshVolumeRenderer();

            void CreateRT(ref RenderTexture rt, string rtName)
            {
                rt = new RenderTexture(volumeResolution, volumeResolution, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm);
                rt.name = rtName;
            }
            void DestroyTarget(GameObject gm)
            {
                if (Application.isPlaying)
                    Destroy(gm);
                else
                    DestroyImmediate(gm);
            }

            void RefreshCamera(ref Camera container, RenderTexture targetRT, string trgName)
            {
                container = new GameObject(trgName).AddComponent<Camera>();

                container.clearFlags = CameraClearFlags.SolidColor;
                container.backgroundColor = Color.black;
                container.orthographic = true;
                container.depth = 0;
                container.useOcclusionCulling = false;
                container.targetTexture = targetRT;

                container.transform.parent = transform;
            }
        }

        public void SetVolumeResolution(int resolution)
        {
            volumeResolution = Mathf.Clamp(resolution, 10, 8000);

            SetSize(RTTop);
            SetSize(RTRight);
            SetSize(RTFront);

            void SetSize(RenderTexture rt)
            {
                rt.Release();
                rt.width = rt.height = volumeResolution;
                rt.Create();
            }
        }

        public void SetVolumeFiltering(bool bilinear)
        {
            volumeFilteringBilinear = bilinear;

            SetSize(RTTop);
            SetSize(RTRight);
            SetSize(RTFront);

            void SetSize(RenderTexture rt)
            {
                rt.Release();
                rt.filterMode = bilinear ? FilterMode.Bilinear : FilterMode.Point;
                rt.Create();
            }
        }

        public void RefreshVolumeRenderer()
        {
            if (topCameraRender == null || rightCameraRender == null || frontCameraRender == null)
                return;

            transform.rotation = Quaternion.identity;
            float doubleVolume = volumeSize * 2f;

            Transform tempTrans = topCameraRender.transform;
            Vector3 tempPos = Vector3.zero;
            tempPos.y = volumeSize;
            tempTrans.localPosition = tempPos;
            tempTrans.localRotation = Quaternion.Euler(90, 0, 0);
            topCameraRender.farClipPlane = doubleVolume;
            topCameraRender.orthographicSize = volumeSize;
            topCameraRender.nearClipPlane = -volumeSize + nearClipOffset;

            tempTrans = frontCameraRender.transform;
            tempPos = Vector3.zero;
            tempPos.z = -volumeSize;
            tempTrans.localPosition = tempPos;
            tempTrans.localRotation = Quaternion.identity;
            frontCameraRender.farClipPlane = doubleVolume;
            frontCameraRender.orthographicSize = volumeSize;
            frontCameraRender.nearClipPlane = -volumeSize + nearClipOffset;

            tempTrans = rightCameraRender.transform;
            tempPos = Vector3.zero;
            tempPos.x = volumeSize;
            tempTrans.localPosition = tempPos;
            tempTrans.localRotation = Quaternion.Euler(0, -90, 0);
            rightCameraRender.farClipPlane = doubleVolume;
            rightCameraRender.orthographicSize = volumeSize;
            rightCameraRender.nearClipPlane = -volumeSize + nearClipOffset;

            topCameraRender.cullingMask = frontCameraRender.cullingMask = rightCameraRender.cullingMask = volumeLayerMask;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshVolumeRenderer();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * volumeSize * 2f);
        }
#endif
    }
}