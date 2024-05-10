using UnityEngine;

namespace OccaSoftware.LocalGI.Runtime
{
    /// <summary>
    /// The LocalGIHandler class is a MonoBehaviour that handles the generation and updating of an irradiance map for a given environment using a reflection probe.
    /// </summary>
    [ExecuteAlways]
    public class LocalGIHandler : MonoBehaviour
    {
        /// <summary>
        /// This is a private field of type EnvironmentData that stores the environment data for the irradiance map. It is lazily instantiated in the getter of the public EnvironmentData property.
        /// </summary>
        private EnvironmentData environmentData;

        /// <summary>
        /// This is a public getter-only property that returns the EnvironmentData field. It ensures that the field is not null by creating a new instance of the EnvironmentData class if it is null.
        /// </summary>
        public EnvironmentData EnvironmentData
        {
            get
            {
                if (environmentData == null)
                    environmentData = new EnvironmentData();

                return environmentData;
            }
        }

        /// <summary>
        /// This is a private field of type IrradianceData that stores the irradiance data for the irradiance map. It is lazily instantiated in the getter of the public IrradianceData property.
        /// </summary>
        private IrradianceData irradianceData;

        /// <summary>
        /// This is a public getter-only property that returns the IrradianceData field. It ensures that the field is not null by creating a new instance of the IrradianceData class if it is null.
        /// </summary>
        public IrradianceData IrradianceData
        {
            get
            {
                if (irradianceData == null)
                    irradianceData = new IrradianceData();

                return irradianceData;
            }
        }

        /// <summary>
        /// This variable sets the maximum distance for sampling reflections.
        /// </summary>
        [Min(0)]
        [Tooltip("Sets the maximum distance at which reflections will be sampled.")]
        public float maximumSampleDistance = 15f;

        /// <summary>
        /// This variable determines the maximum distance over which the irradiance map affects objects in the scene.
        /// </summary>
        [Min(0)]
        [Tooltip("Sets the maximum distance over which the irradiance map will have influence on objects in the scene.")]
        public float maximumInfluenceDistance = 30f;

        /// <summary>
        /// This variable adjusts the brightness of the irradiance map.
        /// </summary>
        [Min(0)]
        [Tooltip("Sets the overall brightness of the irradiance map.")]
        public float exposure = 1f;

        /// <summary>
        /// This variable specifies the layers to include in the irradiance map.
        /// </summary>
        [Tooltip("Determines which layers are included in the irradiance map.")]
        public LayerMask cullingMask = ~0;

        /// <summary>
        /// This variable specifies the clearing method for the reflection probe before rendering.
        /// </summary>
        [Tooltip(
            "Determines how the environment map probe is cleared before rendering. Use Black unless you have disabled Environment Lighting from other sources."
        )]
        public ClearFlagOptions clearFlags = ClearFlagOptions.Black;

        public enum ClearFlagOptions
        {
            Black = 2,
            Skybox = 1
        }

        private Camera cam;

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnEnable()
        {
            Setup();
            //UpdateShaderParams();
            //UpdateGlobalIllumination();
        }

        private void LateUpdate()
        {
            UpdateShaderParams();
            UpdateGlobalIllumination();
        }

        /// <summary>
        /// This method sets up the irradiance map/volume by initializing the required variables and components, and then updates the probe.
        /// </summary>
        private void Setup()
        {
            irradianceData = new IrradianceData();
            environmentData = new EnvironmentData();
        }

        private void Cleanup()
        {
            environmentData = null;
            irradianceData = null;
        }

        private void UpdateGlobalIllumination()
        {
            if (irradianceData == null)
                Setup();

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused)
            {
                return;
            }
#endif

            var faceToRender = Time.frameCount % 6;
            var faceMask = 1 << faceToRender;
            UpdateCubemap(faceMask);

            IrradianceData.IrradianceTexture.Initialize();
            IrradianceData.SetShaderParams(EnvironmentData.EnvironmentMap, exposure);
        }

        private void UpdateShaderParams()
        {
            Shader.SetGlobalVector(ShaderParams._LocalGIProbePosition, transform.position);
            Shader.SetGlobalFloat(ShaderParams._LocalGIMaxDistance, maximumInfluenceDistance);
            Shader.SetGlobalTexture(ShaderParams._DiffuseIrradianceData, IrradianceData.IrradianceTexture);
        }

        private static class ShaderParams
        {
            public static int _LocalGIProbePosition = Shader.PropertyToID("_LocalGIProbePosition");
            public static int _DiffuseIrradianceData = Shader.PropertyToID("_DiffuseIrradianceData");
            public static int _LocalGIMaxDistance = Shader.PropertyToID("_LocalGIMaxDistance");
        }

        void UpdateCubemap(int faceMask = 63)
        {
            if (!cam)
            {
                if (!TryGetComponent(out cam))
                    cam = gameObject.AddComponent<Camera>();

                cam.hideFlags = HideFlags.NotEditable;
                cam.nearClipPlane = 0.03f;
                cam.backgroundColor = Color.black;
                cam.enabled = false;
            }

            cam.farClipPlane = maximumSampleDistance;
            cam.clearFlags = (CameraClearFlags)clearFlags;
            cam.cullingMask = cullingMask.value;
            cam.RenderToCubemap(EnvironmentData.EnvironmentMap, faceMask);
        }

        private Material viewerMaterial;

        /// <summary>
        /// This method gets or creates the material used to visualize the irradiance map in the scene view.
        /// </summary>
        public Material ViewerMaterial
        {
            get
            {
                if (viewerMaterial == null)
                {
                    viewerMaterial = new Material(Shader.Find("Shader Graphs/DiffuseIrradianceViewer"));
                }
                return viewerMaterial;
            }
        }

        private Mesh sphere;

        /// <summary>
        /// This method gets or creates a sphere mesh used to visualize the irradiance map in the scene view.
        /// </summary>
        private Mesh Sphere
        {
            get
            {
                if (sphere == null)
                {
                    GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                    DestroyImmediate(gameObject);
                    sphere = mesh;
                }

                return sphere;
            }
        }

        /// <summary>
        /// This method draws a representation of the irradiance map/volume in the scene view using the viewer material and sphere mesh.
        /// </summary>
        private void OnDrawGizmos()
        {
            DrawIrradianceViewer();
        }

        private void OnDrawGizmosSelected()
        {
            DrawMaximumInfluenceDistance();
            DrawMaximumSampleDistance();
        }

        private void DrawIrradianceViewer()
        {
            Matrix4x4 m = new Matrix4x4();
            m.SetTRS(transform.position, Quaternion.identity, Vector3.one * 0.3f);
            ViewerMaterial.SetPass(0);
            Graphics.DrawMeshNow(Sphere, m);
        }

        private void DrawMaximumInfluenceDistance()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, maximumInfluenceDistance);
        }

        private void DrawMaximumSampleDistance()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maximumSampleDistance);
        }
    }
}
