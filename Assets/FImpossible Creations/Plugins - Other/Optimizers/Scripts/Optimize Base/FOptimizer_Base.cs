using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Base class with methods to support culling groups and other methods useful for optimizer classes.
    /// > Defining how many LODs should be used and on what distances
    /// > Handling detecting distance ranges and object visibility
    /// > Supporting different algorithms for detecting object visibility and distance
    /// > Handling adding new components to be optimized
    /// </summary>
    public abstract partial class FOptimizer_Base : MonoBehaviour
    {
        // ---------------- Variables ---------------- \\

        [HideInInspector]
        /// <summary> List of objects to optimize by script </summary>
        public List<FComponentLODsController> ToOptimize;


        #region Culling Detection Public Variables

        [Range(1, 8)]
        [Tooltip("Level of detail (LOD) steps to configure optimization levels")]
        public int LODLevels = 2;

        [SerializeField]
        [HideInInspector]
        protected int preLODLevels = 1;

        [Tooltip("Max distance from main camera.\nWhen exceed object will be culled")]
        //[FD_HR(1, 7)]
        public float MaxDistance = 100f;

        [Tooltip("[Static] - For models which aren't moving far from initial position or just stays in one place (method is using only CullingGroups - Very Effective for 'Cull if not see')\n\n[Dynamic] - For objects which are moving in scene's world. If object is moving very fast, use 'UpdateBost' slider in Optimizers Manager but using EFFECTIVE method more recommended in such situtation. Dynamic method can response with some delay when there are thousands of active objects to optimize.\n\n[EFFECTIVE] - Connecting features of static method and dynamic, the most resposible method when you have very mobile objects and you need quick detection if object is seen by camera\n\n[Trigger Based] Using trigger colliders to define distance levels (experimental)")]
        public FEOptimizingMethod OptimizingMethod = FEOptimizingMethod.Effective;

        [FPD_DrawTexture("FIMSpace/FOptimizing/Opt_CullHelp", 128, 20, 120, 165)]
        [Tooltip("[Toggled] Changing LOD state to cull (or hidden) if camera is looking away from detection sphere/bounds\n\n[Untoggled] Only max distance will cull this object")]
        public bool CullIfNotSee = true;
        [Space(2)]
        //[FPD_Indent(1, 138, 5)]
        [Tooltip("CullIfNotSee: Radius of detecting object visibility for camera view (frustum - CullingGroups)")]
        public float DetectionRadius = 3f;
        [Space(2)]
        //[FPD_Indent(1, 138, 5)]
        [Tooltip("CullIfNotSee: Bounding Box for detecting object visibility for camera view (frustum)")]
        public Vector3 DetectionBounds = Vector3.one;
        //[FD_HR(1, 10)]
        //[FPD_Width(120)]
        //[Tooltip("Giving possibility to use SetHidden(bool) through code\n\nIt will apply another LOD level to provide more LOD possibilities.\n\nCan be used for example when you detecting that objects are not visible through walls, then you can trigger Optimizer.SetHidden(true);")]
        [HideInInspector]
        /// <summary> Set it to true at override void Reset() if you coding custom class and want to have "Hidden" LOD option always visible </summary>
        public bool Hideable = false;

        //[FD_HR(1, 10)]
        [Tooltip("Offsetting center of detection sphere/bounds")]
        public Vector3 DetectionOffset = Vector3.zero;

        [Range(0f, 1f)]
        [Tooltip("Alpha for debug spheres etc. visible in scene view when object with Optimizer is selected and Optimizer is unfolded")]
        public float GizmosAlpha = 1f;

        #endregion


        [Range(0f, 3f)]
        [Tooltip("How long (in seconds) should take transition between LOD levels (if transitioning for optimized component is supported)")]
        public float FadeDuration = 0f;

        [Tooltip("Displaying options to assign shared settings files to components LODs.\n(Untoggling will not disable using shared settings, just viewing them)")]
        public bool DrawSharedSettingsOptions = false;

        [Tooltip("If at 'Culled' LOD state game object should be deactivated (after transition)\n\nWARNING: Deactivating whole game object is highly time comsuming for unity when you do it on multiple objects during one game frame\nif you use optimizers on many objects and experience lags during rotating camera then try not deactivating game object but just components inside 'To Optimize' list!")]
        public bool DeactivateObject = false;


        [HideInInspector]
        public Vector2 MinMaxDistance = new Vector2(0, 1000);
        [HideInInspector]
        public List<float> LODPercent;

        //[HideInInspector]
        /// <summary> Reference position for editor to calculate helper distance from camera to this object </summary>
        protected Vector3 distancePoint = Vector3.zero;

        [HideInInspector]
        public bool AutoDistance = false;
        [HideInInspector]
        public bool DrawAutoDistanceToggle = true;

        [HideInInspector]
        public int HiddenCullAt = -1;

        [HideInInspector]
        public int LimitLODLevels = 0;

        protected bool drawDetectionSphere = true;
        protected float moveTreshold;


        #region LOD and Cull Variables

        [HideInInspector]
        public bool UnlockFirstLOD = false;

        public bool OutOfDistance { get; protected set; }
        public bool OutOfCameraView { get; protected set; }
        public float[] DistanceLevels { get; protected set; }
        public int CurrentLODLevel { get; protected set; }
        public int CurrentDistanceLODLevel { get; protected set; }
        public bool IsCulled { get; protected set; }
        public bool IsHidden { get; protected set; }
        public bool FarAway { get; protected set; }

        protected bool WasOutOfCameraView;
        protected bool WasHidden;

        protected bool doFirstCull = true; // Initial cull state change - no transitioning flag
        public Transform TargetCamera { get; protected set; }

        #endregion


        #region Transition Info

        public int TransitionNextLOD { get; internal set; }
        public float TransitionPercent { get; internal set; }

        [HideInInspector]
        public bool DrawGeneratedPrefabInfo = false;

        [HideInInspector]
        public bool DrawDeactivateToggle = true;

        #endregion


        // ---------------- Methods ---------------- \\

        protected virtual void Start()
        {
#if UNITY_EDITOR
            if (ToOptimize.Count == 0) Debug.LogWarning("[Optimizers] There is no object to optimize! (" + name + ")");
#endif

            bool removed = false;
            for (int i = ToOptimize.Count - 1; i >= 0; i--)
                if (ToOptimize[i].Component == null)
                {
                    ToOptimize.RemoveAt(i);
                    removed = true;
                }

            if (removed) Debug.LogWarning("[OPTIMIZERS] Optimizer had saved objects to optimize which are not existing anymore!");

            StartVariablesRefresh();
            RefreshInitialSettingsForOptimized();

            // Triggering correct initialization methods
            switch (OptimizingMethod)
            {
                case FEOptimizingMethod.Static:
                    InitStaticOptimizer();
                    break;

                case FEOptimizingMethod.Dynamic:
                    InitDynamicOptimizer(true);
                    break;

                case FEOptimizingMethod.Effective:
                    InitEffectiveOptimizer();
                    break;

                case FEOptimizingMethod.TriggerBased:
                    InitTriggerOptimizer();
                    break;
            }

            moveTreshold = (DetectionRadius * transform.lossyScale.x) / 100f;
            if (FOptimizers_Manager.Get) moveTreshold *= (1f - FOptimizers_Manager.Get.UpdateBoost * 0.999f);
            //initialized = true;
        }


        #region LOD Parameters Related Methods etc.

        /// <summary>
        /// Initializing component with default variables values for correct starting operations
        /// </summary>
        protected virtual void StartVariablesRefresh()
        {
            manager = null;

            CurrentDynamicDistanceCategory = null;

            DynamicListIndex = 0;

            TransitionNextLOD = 0;
            TransitionPercent = -1f;

            ContainerGeneratedID = FOptimizers_CullingContainer.GetId(GetDistanceMeasures());

            IsCulled = false;
            IsHidden = false;
        }

        public virtual float[] GetDistanceMeasures()
        {
            EditorResetLODValues();

            float[] lods = new float[LODPercent.Count];
            for (int i = 0; i < LODPercent.Count; i++) lods[i] = Mathf.Lerp(MinMaxDistance.x, MinMaxDistance.y, LODPercent[i]);
            return lods;
        }

        protected virtual void InitBaseCullingVariables(Camera targetCamera)
        {
            OutOfDistance = true;
            OutOfCameraView = true;
            WasOutOfCameraView = false;
            IsHidden = false;
            WasHidden = false;
            CurrentLODLevel = 0;
            CurrentDistanceLODLevel = 0;

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null)
            {
                if (FEditor_OneShotLog.CanDrawLog("optC", 16)) Debug.LogWarning("[OPTIMIZERS] There is no main camera on scene!");
            }
            else
                this.TargetCamera = targetCamera.transform;
        }


        /// <summary>
        /// Refreshing visibility state when something changed in optimizer system for this object
        /// </summary>
        protected void RefreshVisibilityState(int targetLODLevel)
        {
            if (enabled == false) return;

            bool shouldBeCulled = false;
            bool fastCull = false;
            bool hide = false;

            CurrentDistanceLODLevel = targetLODLevel;

            if (OutOfDistance)
            {
                shouldBeCulled = true;
            }
            else
            {
                // TODO MAYBE: Checking without else? Then transition will end when we go out of distance and look away
                if (CullIfNotSee) if (OutOfCameraView) fastCull = true;
                if (!fastCull) if (IsHidden) fastCull = true;

                if (fastCull) // Out of camera view or setted to hidden
                {
                    if (HiddenCullAt < 0)
                    {
                        shouldBeCulled = true;
                    }
                    else
                        if (targetLODLevel < HiddenCullAt + 1)
                    {
                        targetLODLevel = LODLevels + 1;
                        hide = true;
                    }
                    else
                    {
                        shouldBeCulled = true;
                    }
                }
                else // In camera range or unhidden and not out of distance
                {
                    if (WasOutOfCameraView) fastCull = true;
                }
            }


            if (!shouldBeCulled)
            {
                if (!IsHidden && WasHidden) fastCull = true;
            }

            if (fastCull)
            {
                if (TransitionPercent >= 0f)
                {
                UnityEngine.Debug.Log("fastc");
                    FOptimizers_Manager.Get.EndTransition(this);
                }
            }

            if (IsCulled && shouldBeCulled)
            { }
            else
            {
                // Executing culling procedures basing on computed behviour from culling event
                if (doFirstCull) // Instant setting LOD levels when object initializes
                {
                    if (shouldBeCulled)
                    {
                        ChangeLODLevelTo(LODLevels);
                    }
                    else
                        ChangeLODLevelTo(targetLODLevel);

                    doFirstCull = false;
                }
                else // Culling state changes during playmode
                {
                    if (CullIfNotSee) // If should check for deactivate when object is not visible in camera frustum
                    {
                        if (fastCull) // Instant change to LOD level when camera looking away or just looked on object by frustum
                        {
                            if (shouldBeCulled) // Culling when camera looking away
                            {
                                SetCulled(true);
                            }
                            else // Unculling when camera sees object (only frustum)
                            {
                                // If there is no transition
                                if (TransitionPercent < 0f || hide) ChangeLODLevelTo(targetLODLevel); // Hidden change of LOD level
                                if (!OutOfDistance) SetCulled(false, true); // If we aren't out of allowed distance we can uncull
                            }
                        }
                        else // Normal change or transitiong to LOD level
                        {
                            if (shouldBeCulled)
                            {
                                if (FadeDuration > 0f)
                                {
                                    if (!OutOfDistance)
                                        TransitionOrSetLODLevel(targetLODLevel);
                                    else
                                        TransitionOrSetLODLevel(LODLevels);
                                }
                                else
                                    TransitionOrSetLODLevel(LODLevels);
                            }
                            else
                            {
                                if (FadeDuration <= 0f)
                                {
                                    SetLODLevel(targetLODLevel);
                                    SetCulled(false);
                                }
                                else
                                {
                                    TransitionOrSetLODLevel(targetLODLevel);
                                    SetCulled(false, false);
                                }
                            }
                        }
                    }
                    else // If object should be culled only when is out of distance, no matter where camera is looking
                    {
                        if (shouldBeCulled)
                            TransitionOrSetLODLevel(LODLevels);
                        else
                        {
                            TransitionOrSetLODLevel(targetLODLevel);
                            SetCulled(false);
                        }
                    }
                }
            }

            WasOutOfCameraView = OutOfCameraView;
            WasHidden = IsHidden;
        }


        /// <summary>
        /// Setting new LOD level for optimized components or triggering transition if speed value is greater than 0
        /// </summary>
        protected virtual void TransitionOrSetLODLevel(int lodLevel)
        {
            if (FadeDuration <= 0f)
            {
                SetLODLevel(lodLevel); // No transitioning, just assigning parameters
                UnityEngine.Debug.Log("just set");
            }
            else
            {
                if (lodLevel != CurrentLODLevel || IsCulled || TransitionPercent != -1)
                {
                    if (lodLevel > LODLevels) // Transition to culling
                    {
                        FOptimizers_Manager.Get.TransitionTo(this, LODLevels, FadeDuration);
                        UnityEngine.Debug.Log("tr");
                    }
                    else // Transition to other LOD level
                    {
                        FOptimizers_Manager.Get.TransitionTo(this, lodLevel, FadeDuration);
                        UnityEngine.Debug.Log("tr2");
                    }
                }
            }
        }


        /// <summary>
        /// Setting object as hidden, applying hidden LOD settings or culled if range for culling is defined.
        /// Use this method for example when you detect through custom coding if object is behind the wall or so.
        /// </summary>
        public void SetHidden(bool hide)
        {
            if (hide != IsHidden)
            {
#if UNITY_EDITOR
                if (hide)
                    FOptimizers_Manager.HiddenObjects++;
                else
                    FOptimizers_Manager.HiddenObjects--;
#endif
                IsHidden = hide;
                RefreshVisibilityState(CurrentDistanceLODLevel);
            }
        }

        /// <summary>
        /// Culling object or unculling - applying current distance LOD level
        /// If it's distance based culling, not camera frustum dictated, then we do transition if enabled
        /// </summary>
        internal virtual void SetCulled(bool culled = true, bool apply = true)
        {
            if (culled) if (IsCulled == culled) return;

            IsCulled = culled;

            if (culled) // Culling object
            {
                for (int i = 0; i < ToOptimize.Count; i++)
                {
                    ToOptimize[i].ApplyLODLevelSettings(ToOptimize[i].GetCullingLOD());
                }

                if (DeactivateObject)
                {
                    OnActivationChange(false);
                    gameObject.SetActive(false);
                }
            }
            else // Making object visible
            {
                if (DeactivateObject) if (!gameObject.activeInHierarchy)
                    {
                        OnActivationChange(true);
                        gameObject.SetActive(true);
                    }

                if (apply)
                    for (int i = 0; i < ToOptimize.Count; i++)
                        ToOptimize[i].ApplyLODLevelSettings(ToOptimize[i].GetCurrentLOD());
            }
        }


        /// <summary>
        /// Applying LOD settings for optimized components
        /// </summary>
        internal virtual void SetLODLevel(int lodLevel)
        {
            UnityEngine.Debug.Log("set " + lodLevel);
            if (lodLevel == LODLevels) // Culling UnityEngine.Object
            {
                SetCulled(true);
            }
            else // Setting defined LOD level to know which settings should be applied when object is visible
            {
                CurrentLODLevel = lodLevel;
                for (int i = 0; i < ToOptimize.Count; i++)
                {
                    ToOptimize[i].SetCurrentLODLevel(CurrentLODLevel);
                }
            }
        }


        /// <summary>
        /// Applying LOD settings for optimized components
        /// </summary>
        internal virtual void ChangeLODLevelTo(int lodLevel)
        {
            CurrentLODLevel = Mathf.Min(lodLevel, LODLevels + 2);

            for (int i = 0; i < ToOptimize.Count; i++)
            {
                ToOptimize[i].SetCurrentLODLevel(CurrentLODLevel);
                ToOptimize[i].ApplyLODLevelSettings(ToOptimize[i].GetCurrentLOD());
            }

            bool cullIt = false;

            if (lodLevel >= LODLevels)
            {
                if (lodLevel == LODLevels + 1) cullIt = false;
                else
                    cullIt = true;
            }

            if (cullIt)
                CullOrUncullObject(true);
            else
                CullOrUncullObject(false);
        }


        /// <summary>
        /// Culling object or unculling - applying current distance LOD level
        /// If it's distance based culling, not camera frustum dictated, then we do transition if enabled
        /// </summary>
        internal virtual void CullOrUncullObject(bool cull = true)
        {
            if (IsCulled == cull) return;
            IsCulled = cull;

            if (cull)
            {
                if (DeactivateObject) if (gameObject.activeInHierarchy)
                    {
                        OnActivationChange(false);
                        gameObject.SetActive(false);
                    }
            }
            else
            {
                if (DeactivateObject) if (!gameObject.activeInHierarchy)
                    {
                        OnActivationChange(true);
                        gameObject.SetActive(true);
                    }
            }
        }

        #endregion


        #region Utilities


        public void RefreshCamera(Camera camera)
        {
            if (camera == null) return;

            TargetCamera = camera.transform;

            if (OwnerContainer == null)
                if (CullingGroup != null)
                {
                    CullingGroup.targetCamera = camera;
                    CullingGroup.SetDistanceReferencePoint(TargetCamera);
                }
        }


        /// <summary>
        /// Getting reference world position of this optimizer object
        /// </summary>
        public virtual Vector3 GetReferencePosition()
        {
            //if (!initialized) return Vector3.zero;

#if UNITY_EDITOR
            if (!Application.isPlaying) return transform.position + transform.TransformVector(DetectionOffset);
#endif
            if (OptimizingMethod == FEOptimizingMethod.Static) if (visibilitySpheres != null) return visibilitySpheres[0].position;

            return transform.position + transform.TransformVector(DetectionOffset);
        }

        public virtual float GetReferenceDistance()
        {
            //if (!initialized) return 0f;

            if (OptimizingMethod == FEOptimizingMethod.Static || OptimizingMethod == FEOptimizingMethod.Effective)
            {
                float distance = Vector3.Distance(GetReferencePosition(), TargetCamera.position);
                if (distance < mainVisibilitySphere.radius) distance = 0f; else distance -= mainVisibilitySphere.radius;
                return distance;
            }

            return Vector3.Distance(PreviousPosition, LastDynamicCheckCameraPosition);
        }


        /// <summary>
        /// Getting additional radius from detection sphere radius or other for distance detection
        /// </summary>
        public float GetAddRadius()
        {
            if (OptimizingMethod == FEOptimizingMethod.Static || OptimizingMethod == FEOptimizingMethod.Effective)
                return DetectionRadius * transform.lossyScale.x;
            else
                return 0f;
        }


        /// <summary>
        /// Executed every when component added and every change inside inspector window
        /// </summary>
        public virtual void OnValidate()
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                if (ToOptimize != null)
                {
                    bool generated = false;
                    for (int i = 0; i < ToOptimize.Count; i++)
                    {
                        if (ToOptimize[i].LODSet == null)
                        {
                            ToOptimize[i].GenerateLODParameters();
                            generated = true;
                        }
                    }

                    if (generated)
                    {
                        Debug.LogWarning("[OPTIMIZERS EDITOR] LOD Settings generated from scratch for " + name + ". Did you copy and paste objects through scenes? Unity is not able to remember LOD settings for not prefabed objects and to objects without shared settings between scenes like that :/ \n(without prefabing or saving shared settings this settings are scene assets, no object assets)");
                    }
                }
            }

            OptimizerOnValidate();
#endif
        }

        /// <summary>
        /// Method called when component is added to any game object
        /// </summary>
        protected virtual void Reset()
        {
#if UNITY_EDITOR
            OptimizerReset();
#endif
        }

        #endregion


    }
}
