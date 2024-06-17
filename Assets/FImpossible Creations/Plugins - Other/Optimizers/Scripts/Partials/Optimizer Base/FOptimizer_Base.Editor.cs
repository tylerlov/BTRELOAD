using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FOptimizing
{
    public abstract partial class FOptimizer_Base
    {
        #region Culling Components Methods


        /// <summary>
        /// Adding unassigned components which can be optimized to 'ToOptimize' list
        /// </summary>
        public virtual void AssignComponentsToOptimizeFrom(Component target)
        {
#if UNITY_EDITOR
            if (ToOptimize == null) ToOptimize = new List<FComponentLODsController>();

            // Checking if there is no other optimizer using this components for optimization
            List<FOptimizer_Base> childOptimizers = FindComponentsInAllChildren<FOptimizer_Base>(transform);

            manager = FOptimizers_Manager.Get; manager = null; // Casting Get() to generate optimizers manager

            TryAddLODControllerFor(LoadLODReference("Optimizers/Base/FLOD_Particle System Reference"), target, childOptimizers);
            TryAddLODControllerFor(LoadLODReference("Optimizers/Base/FLOD_Audio Source Reference"), target, childOptimizers);
            TryAddLODControllerFor(LoadLODReference("Optimizers/Base/FLOD_Nav Mesh Agent Reference"), target, childOptimizers);
            TryAddLODControllerFor(LoadLODReference("Optimizers/Base/FLOD_Renderer Reference"), target, childOptimizers);
            TryAddLODControllerFor(LoadLODReference("Optimizers/Base/FLOD_Light Reference"), target, childOptimizers);


            // Checking for extra unity components inside resources/optimizers/extra
            UnityEngine.Object[] custom = Resources.LoadAll("Optimizers/Extra/");
            FComponentLODsController controller;
            List<FOptimizer_Base> optimizers = FindComponentsInAllChildren<FOptimizer_Base>(transform);
            for (int i = 0; i < custom.Length; i++)
            {
                FLOD_Base lodType = custom[i] as FLOD_Base;
                if (lodType != null)
                {
                    controller = lodType.GenerateLODController(target, this);

                    if (controller != null)
                    {
                        if (!CheckIfAlreadyInUse(controller, optimizers)) AddToOptimize(controller);
                    }
                }
            }

#endif
        }

        /// <summary>
        /// Generating LOD controller for target component type and adding to optimizer
        /// </summary>
        protected void TryAddLODControllerFor(FLOD_Base lod, Component target, List<FOptimizer_Base> childOptims)
        {
#if UNITY_EDITOR
            if (lod == null) return;
            if (target == null) return;

            FComponentLODsController controller = lod.GenerateLODController(target, this);
            if (controller != null)
            {
                if (!CheckIfAlreadyInUse(controller, childOptims)) AddToOptimize(controller);
            }
#endif
        }

        /// <summary>
        /// Checking if there is no other optimizer using this components for optimization
        /// </summary>
        public bool CheckIfAlreadyInUse(FComponentLODsController generatedController, List<FOptimizer_Base> childOptims)
        {
            bool alreadyInUse = false;

            if (childOptims != null)
            {
                for (int i = 0; i < childOptims.Count; i++)
                {
                    if (alreadyInUse) break;

                    if (childOptims[i] != this)
                        if (childOptims[i].ToOptimize != null)
                            for (int c = 0; c < childOptims[i].ToOptimize.Count; c++)
                            {
                                if (childOptims[i].ToOptimize[c].Component == generatedController.Component)
                                {
                                    alreadyInUse = true;
                                    break;
                                }
                            }
                }
            }

            return alreadyInUse;
        }


        public virtual void AssignCustomComponentToOptimize(MonoBehaviour target)
        {
#if UNITY_EDITOR
            if (ToOptimize == null) ToOptimize = new List<FComponentLODsController>();
            if (target == null) return;
            if (target is FOptimizer_Base) return;
            if (target.GetType().IsSubclassOf(typeof(FOptimizer_Base))) return;

            if (!ContainsComponent(target))
            {
                // Checking if there is no other optimizer using this components for optimization
                List<FOptimizer_Base> optimizers = FindComponentsInAllChildren<FOptimizer_Base>(transform);

                FComponentLODsController controller;

                // Checking for custom components inside resources/optimizers/implementations
                UnityEngine.Object[] custom = Resources.LoadAll("Optimizers/Implementations/");
                for (int i = 0; i < custom.Length; i++)
                {
                    FLOD_Base lodType = custom[i] as FLOD_Base;
                    if (lodType != null)
                    {
                        controller = lodType.GenerateLODController(target, this);

                        if (controller != null)
                        {
                            if (!CheckIfAlreadyInUse(controller, optimizers)) AddToOptimize(controller);
                        }
                    }
                }

                // Checking for custom components inside resources/optimizers/custom
                custom = Resources.LoadAll("Optimizers/Custom/");
                for (int i = 0; i < custom.Length; i++)
                {
                    FLOD_Base lodType = custom[i] as FLOD_Base;
                    if (lodType != null)
                    {
                        controller = lodType.GenerateLODController(target, this);

                        if (controller != null)
                        {
                            if (!CheckIfAlreadyInUse(controller, optimizers)) AddToOptimize(controller);
                        }
                    }
                }

                controller = LoadLODReference("Optimizers/Base/FLOD_Mono Behaviour Reference").GenerateLODController(target, this);

                if (controller != null)
                {
                    if (!CheckIfAlreadyInUse(controller, optimizers)) AddToOptimize(controller);
                }
            }

#endif
        }


        /// <summary>
        /// Loading LOD Type reference from resources folder
        /// </summary>
        /// <param name="resourcesPath"> ex: Optimizers/FLOD_Mono Behaviour Reference</param>
        public FLOD_Base LoadLODReference(string resourcesPath)
        {
            FLOD_Base reference = Resources.Load<FLOD_Base>(resourcesPath);
            if (reference == null) Debug.LogError("[OPTIMIZERS CRITICAL ERROR] There are no references for base LOD Types, you removed them from resources folder???");
            return reference;
        }


        /// <summary>
        /// Searching through whole 'target' for components to optimize and adding them to 'ToOptimize' list if new are found
        /// </summary>
        public virtual void AssignComponentsToBeOptimizedFromAllChildren(GameObject target, bool searchForCustom = false)
        {
            RefreshToOptimizeList();

            if (!searchForCustom)
            {
                foreach (var c in target.GetComponentsInChildren<Transform>(true)) AssignComponentsToOptimizeFrom(c);
            }
            else
            {
                foreach (var c in target.GetComponentsInChildren<Transform>(true))
                    foreach (var m in c.gameObject.GetComponents<MonoBehaviour>()) AssignCustomComponentToOptimize(m);
            }
        }


        /// <summary>
        /// Checking if component is already in 'ToOptimize' list
        /// </summary>
        public bool ContainsComponent(Component component)
        {
            for (int i = ToOptimize.Count - 1; i >= 0; i--)
            {
                if (ToOptimize == null) { ToOptimize.RemoveAt(i); continue; }
                if (ToOptimize[i].Component == component) return true;
            }

            return false;
        }


        /// <summary>
        /// Removing null references
        /// </summary>
        public void RefreshToOptimizeList()
        {
            for (int i = ToOptimize.Count - 1; i >= 0; i--)
                if (ToOptimize[i] == null) ToOptimize.RemoveAt(i);
        }

        #endregion


        #region Project Assets Related Methods


        public bool IsPrefabed()
        {
            #region Unity Version Conditional

#if UNITY_EDITOR

#if UNITY_2018_3_OR_NEWER
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this)) return true;
#else
            if (UnityEditor.PrefabUtility.GetPrefabParent(this)) return true;
#endif

            return false;
#else
            return false;
#endif

            #endregion
        }


        /// <summary>
        /// Refreshing reference variables on nearest LOD level for every optimized object
        /// </summary>
        protected virtual void RefreshInitialSettingsForOptimized()
        {
            RefreshDistances();

            for (int i = ToOptimize.Count - 1; i >= 0; i--)
            {
                if (ToOptimize == null) { ToOptimize.RemoveAt(i); continue; }
                ToOptimize[i].OnStart();
            }
        }


        public void RemoveFromToOptimizeAt(int i)
        {
#if UNITY_EDITOR
            if (i < ToOptimize.Count)
            {
                if (!ToOptimize[i].UsingShared)
                {
                    if (ToOptimize[i].LODSet)
                        FOptimizers_LODTransport.RemoveLODControllerSubAssets(ToOptimize[i]);
                }

                ToOptimize.RemoveAt(i);
            }
#endif
        }


        public void RemoveAllComponentsFromToOptimize()
        {
#if UNITY_EDITOR

            if (ToOptimize == null) return;

            for (int i = ToOptimize.Count - 1; i >= 0; i--)
            {
                if (!ToOptimize[i].UsingShared)
                {
                    if (ToOptimize[i].LODSet)
                        FOptimizers_LODTransport.RemoveLODControllerSubAssets(ToOptimize[i]);
                }

                ToOptimize.RemoveAt(i);
            }
#endif
        }


        /// <summary>
        /// Adding and refreshing added component to optimize list
        /// </summary>
        private FComponentLODsController AddToOptimize(FComponentLODsController lod)
        {
#if UNITY_EDITOR
            ToOptimize.Add(lod);
            lod.GenerateLODParameters();
            return lod;
#else
            return null;
#endif
        }


        /// <summary>
        /// Resetting LODs settings when LOD levels count changed
        /// </summary>
        protected virtual void ResetLODs()
        {
#if UNITY_EDITOR
            for (int i = 0; i < ToOptimize.Count; i++)
            {
                ToOptimize[i].GenerateLODParameters();
            }

            if (ToOptimize.Count > 0)
                if (ToOptimize[0].LODSet != null)
                    if (LODLevels != ToOptimize[0].LODSet.LevelOfDetailSets.Count - 2) HiddenCullAt = LODLevels;
#endif
        }


        protected virtual void OnActivationChange(bool active)
        {
            if (OptimizingMethod == FEOptimizingMethod.TriggerBased)
            {
                if (!active)
                {
                    // Disconnecting triggers from deactivated object to be able of detecting whem camera will again catch max distance
                    if (triggersContainer.transform.parent != null) triggersContainer.transform.SetParent(null, true);
                }
                else
                    if (triggersContainer.transform.parent == null) triggersContainer.transform.SetParent(transform, true);
            }
        }


        #endregion


        #region Utilities


        public void CheckForNullsToOptimize()
        {
            if (ToOptimize == null) return;
            for (int i = ToOptimize.Count - 1; i >= 0; i--)
            {
                if (ToOptimize[i] == null)
                    ToOptimize.RemoveAt(i);
                else
                {
                    if (ToOptimize[i].Component == null)
                        ToOptimize.RemoveAt(i);
                }
            }
        }


        protected virtual void OnDestroy()
        {
            DisposeDynamicOptimizer();
            CleanCullingGroup();
            if (!isQuitting) if (!FOptimizers_Manager.AppIsQuitting) FOptimizers_Manager.Get.UnRegisterOptimizer(this);
        }

        private bool isQuitting = false;
        private void OnApplicationQuit() { isQuitting = true; CleanCullingGroup(); }

        /// <summary>
        /// When destroying component in prefab file then cleaning referenes stored inside
        /// </summary>
        public void CleanAsset()
        {
#if UNITY_EDITOR
            for (int i = 0; i < ToOptimize.Count; i++)
                if (!ToOptimize[i].UsingShared)
                    FOptimizers_LODTransport.RemoveLODControllerSubAssets(ToOptimize[i]);

            //FOptimizers_LODTransport.ClearPrefabFromUnusedOptimizersSubAssets(gameObject);

            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        private List<T> FindComponentsInAllChildren<T>(Transform transformToSearchIn) where T : Component
        {
            List<T> components = new List<T>();
            foreach (Transform child in transformToSearchIn.GetComponentsInChildren<Transform>(true))
            {
                T component = child.GetComponent<T>();
                if (component) components.Add(component);
            }

            return components;
        }

        #endregion


        #region Editor Stuff

        /// <summary> Flag for editor usage, triggering SaveAssets() if sub assets was saved  during OnValidate() </summary>
        internal bool Editor_WasSaving = false;
        /// <summary> Flag for editor usage, tagging from enabling inspector window that object is inside isolated scene </summary>
        [HideInInspector]
        public bool Editor_InIsolatedScene = false;

        [HideInInspector]
        public bool Editor_JustCreated = true;

        /// <summary>
        /// Method called when component is added to any game object
        /// </summary>
        protected void OptimizerReset()
        {
#if UNITY_EDITOR
            Camera main = Camera.main;
            if (main) MaxDistance = main.farClipPlane * 0.9f;

            //AssignComponentsToOptimizeFrom(gameObject.transform);
            if (ToOptimize == null) ToOptimize = new List<FComponentLODsController>();

            if (ToOptimize.Count == 0) AssignComponentsToBeOptimizedFromAllChildren(gameObject);
            if (ToOptimize.Count == 0) AssignComponentsToBeOptimizedFromAllChildren(gameObject, true);

            DrawDeactivateToggle = true;
#endif
        }


        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                OptimizerOnValidate();
            }
#endif

            // Game Time Feature (not just editor)
            if (wasDisabled)
            {
                ApplyLastEvent();
                wasDisabled = false;
            }
        }


        // Game Time Feature (not just editor)
        protected bool wasDisabled = false;

        /// <summary>
        /// Applying last LOD event
        /// </summary>
        private void ApplyLastEvent()
        {
            if (OptimizingMethod == FEOptimizingMethod.Dynamic)
            {
                OutOfCameraView = false;
                DynamicLODUpdate((FEOptimizingDistance)CurrentDynamicDistanceCategory, lastDynamicDistance);
            }
            else
            {
                if (OptimizingMethod == FEOptimizingMethod.Effective) if (CurrentDynamicDistanceCategory != null) DynamicLODUpdate((FEOptimizingDistance)CurrentDynamicDistanceCategory, lastDynamicDistance);

                CullingGroupStateChanged(lastEvent);
            }
        }

        internal bool WasAskingForStatic = false;

        /// <summary>
        /// Executed every when component added and every change inside inspector window
        /// </summary>
        public void OptimizerOnValidate()
        {
#if UNITY_EDITOR

            OnValidateStart();

            if (Application.isPlaying) return;

            if (AutoDistance) SetAutoDistance();

            OnValidateRefreshComponents();
            OnValidateUpdateToOptimize();
            OnValidateCheckForStatic();

#endif
        }

        protected void OnValidateStart()
        {
            if (LODLevels <= 0) LODLevels = 2;
            if (LODLevels > 8) LODLevels = 8;

            if (DetectionRadius < 0f) DetectionRadius = 0f;
        }

        protected void OnValidateRefreshComponents()
        {
            if (ToOptimize != null)
                RefreshToOptimizeList();
            else
                AssignComponentsToOptimizeFrom(gameObject.transform);
        }

        protected void OnValidateUpdateToOptimize()
        {
            if (preLODLevels != LODLevels) ResetLODs();

#if UNITY_EDITOR
            if (ToOptimize != null)
                for (int i = 0; i < ToOptimize.Count; i++)
                    if (ToOptimize[i].RootReference)
                        ToOptimize[i].OnValidate();
#endif


            preLODLevels = LODLevels;

#if UNITY_EDITOR
            if (Editor_WasSaving) FOptimizers_LODTransport.OnValidateEnds(this);
#endif
        }


        /// <summary>
        /// Setting auto MaxDistance value basing on detection shape size
        /// </summary>
        public void SetAutoDistance(float multiplier = 1f)
        {
            switch (OptimizingMethod)
            {
                case FEOptimizingMethod.Static:
                case FEOptimizingMethod.Effective:

                    MaxDistance = DetectionRadius * 550f;

                    MaxDistance *= GetScaler(transform);

                    if (FOptimizers_Manager.MainCamera)
                        if (MaxDistance > FOptimizers_Manager.MainCamera.farClipPlane) MaxDistance = FOptimizers_Manager.MainCamera.farClipPlane;

                    MaxDistance *= multiplier;

                    break;

                case FEOptimizingMethod.Dynamic:
                case FEOptimizingMethod.TriggerBased:

                    MaxDistance = DetectionBounds.magnitude * 166f;

                    MaxDistance *= GetScaler(transform);

                    if (FOptimizers_Manager.MainCamera)
                        if (MaxDistance > FOptimizers_Manager.MainCamera.farClipPlane) MaxDistance = FOptimizers_Manager.MainCamera.farClipPlane;

                    MaxDistance *= multiplier;

                    break;
            }
        }


        protected void OnValidateCheckForStatic()
        {
#if UNITY_EDITOR

            if (gameObject.isStatic)
            {
                if (FEditor_OneShotLog.CanDrawLog("OptimAskStatic"))
                {
                    if (OptimizingMethod != FEOptimizingMethod.Static)
                    {
                        if (PlayerPrefs.GetInt("Optim_SetStatic", 0) == 1)
                        {
                            OptimizingMethod = FEOptimizingMethod.Static;
                        }
                        else
                        {
                            if (!WasAskingForStatic)
                            {
                                if (UnityEditor.EditorUtility.DisplayDialog("Static Game Object Detected", "Your object is marked as static, can I change optimizing method to 'Static'?", "Yes", "No"))
                                {
                                    OptimizingMethod = FEOptimizingMethod.Static;

                                    if (PlayerPrefs.GetInt("Optim_SetStatic", 0) == 0 || FEditor_OneShotLog.CanDrawLog("Optim_SetStatic"))
                                    {
                                        FEditor_OneShotLog.CanDrawLog("Optim_SetStatic");

                                        if (UnityEditor.EditorUtility.DisplayDialog("Can it be done automatically?", "Can I enable 'Static' optimization method every time static game object is detected?", "Yes do it for me everytime", "No I want to do it by myself"))
                                            PlayerPrefs.SetInt("Optim_SetStatic", 1);
                                        else
                                            PlayerPrefs.SetInt("Optim_SetStatic", -1);
                                    }
                                }

                                WasAskingForStatic = true;
                            }

                        }
                    }
                }
            }
            else // Not static game object
            {
                if (OptimizingMethod == FEOptimizingMethod.Static) // With static optimization method
                {
                    if (PlayerPrefs.GetInt("Optim_SetStatic", 0) == 1) // If we have enabled auto static then we resume to effective
                    {
                        OptimizingMethod = FEOptimizingMethod.Effective;
                    }
                    else // If we didn't set auto static setting then we do nothing
                    {
                    }
                }
            }

#endif
        }

        /// <summary>
        /// (Editor Usage) Syncing LOD Count with LOD Sets counts
        /// </summary>
        public void SyncWithReferences()
        {
            #region Prefab Syncing

            if (ToOptimize.Count > 0)
                if (ToOptimize[0].LODSet != null)
                    if (ToOptimize[0].LODSet.LevelOfDetailSets != null)
                        if (ToOptimize[0].LODSet.LevelOfDetailSets.Count > 0)
                            if (ToOptimize[0].LODSet.LevelOfDetailSets.Count - 2 != LODLevels)
                            {
                                LODLevels = ToOptimize[0].LODSet.LevelOfDetailSets.Count - 2;
                                preLODLevels = LODLevels;
                            }

            #endregion
        }


        public void EditorUpdate()
        {
#if UNITY_EDITOR
            if (LODLevels <= 0) LODLevels = 2;
            if (LODLevels > 8) LODLevels = 8;

            MinMaxDistance.y = MaxDistance;
            EditorResetLODValues();
#endif
        }


        public void EditorResetLODValues()
        {
#if UNITY_EDITOR
            if (LODPercent == null) LODPercent = new List<float>();

            if (LODLevels != LODPercent.Count)
            {
                float pow = Mathf.Lerp(1f, 1.65f, Mathf.InverseLerp(1, 7, LODLevels));

                LODPercent = new List<float>();
                for (int i = 0; i < LODLevels; i++)
                {
                    float percent = 0f;
                    percent = .05f + Mathf.Pow((float)(i + 1) / (float)(LODLevels + 1), pow);
                    LODPercent.Add(percent);
                }

                LODPercent[LODLevels - 1] = 1f;
            }
#endif
        }


        #endregion


        #region Color Utility


        /// <summary> Colors identifying certain LOD levels </summary>
        public static readonly Color[] lODColors =
        {
            new Color(0.2231376f, 0.8011768f, 0.1619608f, 1.0f),
            new Color(0.2070592f, 0.6333336f, 0.7556864f, 1.0f),
            new Color(0.1592160f, 0.5578432f, 0.3435296f, 1.0f),
            new Color(0.1333336f, 0.400000f, 0.7982352f, 1.0f),
            new Color(0.3827448f, 0.2886272f, 0.5239216f, 1.0f),
            new Color(0.8000000f, 0.4423528f, 0.0000000f, 1.0f),
            new Color(0.4886272f, 0.1078432f, 0.801960f, 1.0f),
            new Color(0.7749016f, 0.6368624f, 0.0250984f, 1.0f)
        };

        public static readonly Color culledLODColor = new Color(.4f, 0f, 0f, .5f);


        #endregion
    }
}
