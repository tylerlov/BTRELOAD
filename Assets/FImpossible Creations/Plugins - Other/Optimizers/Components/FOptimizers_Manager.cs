using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Manager class to support optimizng objects without CullingGroups api or support Effective technique.
    /// This calss also handles transitioning feature for LOD levels.
    /// </summary>
    [AddComponentMenu("FImpossible Creations/Optimizers/System/Optimizers Manager")]
    public partial class FOptimizers_Manager : MonoBehaviour, UnityEngine.EventSystems.IDropHandler, IFHierarchyIcon
    {
        [Tooltip("(DontDestroyOnLoad - untoggled just for package examples purpose!)\n\nWith this option enabled, manager will be never destroyed, even during changing scenes. This one manager can be used as only manager in whole game time")]
        //[FPD_Width(135)]
        public bool ExistThroughScenes = true;

        //[Tooltip("Put here LOD file for certain type of component, then when you drag your component to Optimizer box under 'To Optimize' / 'Assigning new components tab' there will not be added MonoBehaviour LOD settings but your custom one.\n\n(LOD must implement CheckForComponent() method)")]
        //public FLOD_Base[] CustomComponentsDefinition;


        #region List of built in references

        //[HideInInspector]
        //public FLOD_Light LightReference;
        //[HideInInspector]
        //public FLOD_AudioSource AudioSourceReference;
        //[HideInInspector]
        //public FLOD_MonoBehaviour MonoBehReference;
        //[HideInInspector]
        //public FLOD_NavMeshAgent NavMeshAgentReference;
        //[HideInInspector]
        //public FLOD_ParticleSystem ParticleSystemReference;
        //[HideInInspector]
        //public FLOD_Renderer RendererReference;
        //[HideInInspector]
        //public FLOD_Terrain TerrainReference;

        #endregion


        #region Static Flags

        public static bool DrawGizmos = false;

        #endregion


        #region Get manager stuff and initialization

        #region Construction

        public string EditorIconPath { get { return "FIMSpace/FOptimizing/Optimizers Manager Icon"; } }
        public void OnDrop(UnityEngine.EventSystems.PointerEventData data) { }

        private static FOptimizers_Manager _get;
        public static FOptimizers_Manager Get
        {
            get
            {
                if (_get == null) GenerateOptimizersManager();
                if (_get == null) return FindObjectOfType<FOptimizers_Manager>();
                return _get;
            }
            private set { _get = value; }
        }

        #endregion

        public static bool Exists
        {
            get
            {
                if (_get == null) { FOptimizers_Manager man = FindObjectOfType<FOptimizers_Manager>(); man.SetGet(); }
                return _get != null;
            }
        }

        [Tooltip("Main rendering camera reference")]
        public Camera TargetCamera;
        private static Camera _mainCam;
        public static Camera MainCamera
        {
            get { if (_mainCam == null) GetMainCamera(); return _mainCam; }
            private set { _mainCam = value; }
        }

        private Vector3 previousCameraPositionMoveTrigger;

        /// <summary> Lists for each FEOptimizingDistance level </summary>
        private List<List<FOptimizer_Base>> dynamicLists;

        private static void GenerateOptimizersManager()
        {
            //if (!Application.isPlaying) return;
            FOptimizers_Manager manager = FindObjectOfType<FOptimizers_Manager>();

            if (!manager)
            {
                GameObject managerObject = new GameObject("Generated Optimizers Manager");
                managerObject.transform.SetAsFirstSibling();
                manager = managerObject.AddComponent<FOptimizers_Manager>();
            }

            _get = manager;
            Get = manager;
            Get.Init();
        }

        private static void GetMainCamera()
        {
            bool was = _mainCam != null;

            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();

                if (mainCamera)
                    Debug.LogWarning("[OPTIMIZERS] There is no object with 'MainCamera' Tag!");
                else
                    if ( FEditor_OneShotLog.CanDrawLog("OptNoCamera", 10)) Debug.LogWarning("[OPTIMIZERS] There is no camera on the scene!");
            }

            _mainCam = mainCamera;
            Get.TargetCamera = mainCamera;

            if (!was) SetNewMainCamera(mainCamera);
        }

        public void SetGet()
        {
            FOptimizers_Manager manager = FindObjectOfType<FOptimizers_Manager>();
            bool destroyed = false;
            if (manager) if (manager != this)
                {

                    if (Application.isPlaying)
                    {
                        Debug.LogError("[OPTIMIZERS] There can't be two Optimizers Managers at the same time! I'm removing new one!");
                        Destroy(this);
                        destroyed = true;
                    }
                    else
                    {
                        Debug.LogError("[OPTIMIZERS EDITOR] There can't be two Optimizers Managers at the same time! I'm removing previous one!");
                        DestroyImmediate(manager);
                        destroyed = true;
                    }
                }

            if (!destroyed)
            {
                if (_get != null) if (_get != this)
                    {

                        if (Application.isPlaying)
                        {
                            Debug.LogError("[OPTIMIZERS] There can't be two Optimizers Managers at the same time! I'm removing new one!");
                            Destroy(this);
                        }
                        else
                        {
                            Debug.LogError("[OPTIMIZERS EDITOR] There can't be two Optimizers Managers at the same time! I'm removing previous one!");
                            DestroyImmediate(_get);
                        }

                        return;
                    }
            }
            else
                return;

            Get = this;
        }

        private bool existThroughScenes = false;
        private bool initialized = false;

        #region Utilities


        /// <summary>
        /// Setting new camera as main for optimizers system.
        /// Method will refresh target camera for all existing optimizers components on scene.
        /// </summary>
        public static void SetNewMainCamera(Camera camera)
        {
            if (camera == null) return;

            MainCamera = camera;
            //foreach (FOptimizer_Base optim in FindObjectsOfType<FOptimizer_Base>()) optim.RefreshCamera(camera);
            foreach (FOptimizer_Base optim in Get.notContainedStaticOptimizers) optim.RefreshCamera(camera);
            foreach (FOptimizer_Base optim in Get.notContainedDynamicOptimizers) optim.RefreshCamera(camera);
            foreach (FOptimizer_Base optim in Get.notContainedEffectiveOptimizers) optim.RefreshCamera(camera);
            foreach (FOptimizer_Base optim in Get.notContainedTriggerOptimizers) optim.RefreshCamera(camera);

            SetNewMainCameraForContainers(camera);
        }


        /// <summary>
        /// Setting new camera only for objects added to culling containers (much quicker, execute if you use only static/dynamic/effective method and basic/detection optimizer)
        /// </summary>
        public static void SetNewMainCameraForContainers(Camera camera)
        {
            MainCamera = camera;

            if (Get.CullingContainersIDSpecific != null)
                foreach (var item in Get.CullingContainersIDSpecific)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        item.Value[i].SetNewCamera(camera);
                    }
                }
        }


        /// <summary>
        /// If you need to turn off/on optimizers works.
        /// </summary>
        public static void SwitchOptimizersOnOrOff(bool on = true, bool unhideAll = true)
        {
            if (Get)
            {
                Get.enabled = on;

                if (unhideAll)
                {
                    foreach (FOptimizer_Base optim in FindObjectsOfType<FOptimizer_Base>())
                    {
                        if (optim.CullingGroup != null) optim.CullingGroup.enabled = on;
                        optim.SetLODLevel(0);
                    }
                }
                else
                {
                    foreach (FOptimizer_Base optim in FindObjectsOfType<FOptimizer_Base>())
                    {
                        if (optim.CullingGroup != null) optim.CullingGroup.enabled = on;
                    }
                }
            }
        }


        private static int GetDistanceTypesCount()
        {
            return System.Enum.GetValues(typeof(FEOptimizingDistance)).Length;
        }


        #endregion


        #endregion


        private void Awake() { if (!Application.isPlaying) { SetGet(); return; } Init(); }

        private void Start() { Init(); }

        private void Reset()
        {
            GetMainCamera();
            if (MainCamera) WorldScale = (float)System.Math.Round(MainCamera.farClipPlane / 520f, 2);
        }

        public void Init()
        {
            if (initialized) return;

            SetGet();

            if (Application.isPlaying)
            {
                if (ExistThroughScenes) { DontDestroyOnLoad(gameObject); existThroughScenes = true; }

                dynamicLists = new List<List<FOptimizer_Base>>();
                CullingContainersIDSpecific = new Dictionary<int, FOptimizers_CullingContainersList>();
                //if (MainCamera) previousCameraRotationMoveTrigger = MainCamera.transform.rotation; else previousCameraRotationMoveTrigger = Quaternion.identity;

                initialized = true;

                GenerateClocks();
                RefreshDistances();
                RunDynamicClocks();
            }
        }

        private void Update()
        {
            if (!existThroughScenes) if (ExistThroughScenes) DontDestroyOnLoad(gameObject);

            if (TargetCamera == null)
            {
                GetMainCamera();
                SetNewMainCamera(TargetCamera);
                if (TargetCamera != null) Debug.Log("[OPTIMIZERS] New Camera detected and assigned! " + TargetCamera.name);
            }
            else
            {
                if (TargetCamera != MainCamera)
                {
                    SetNewMainCamera(TargetCamera);
                    Debug.Log("[OPTIMIZERS] New Camera detected and assigned! " + TargetCamera.name);
                }

                TransitionsUpdate();
                DynamicUpdate();
            }
        }


        public void OnValidate()
        {
            if (TargetCamera != null) if (TargetCamera != MainCamera) MainCamera = TargetCamera;

            if (WorldScale <= 0f) WorldScale = 0.1f;
            if (!Advanced) MoveTreshold = WorldScale / (150f * (1f + UpdateBoost));
            RefreshDistances();
            if (!Advanced) Debugging = false;

            TargetCamera = MainCamera;
        }


        public static bool AppIsQuitting = false;
        void OnApplicationQuit()
        {
            AppIsQuitting = true;
        }

        // Rest of the code inside partial classes
    }

}