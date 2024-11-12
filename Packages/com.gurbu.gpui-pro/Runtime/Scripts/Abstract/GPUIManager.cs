// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Jobs;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GPUInstancerPro
{
    public abstract class GPUIManager : MonoBehaviour, IGPUIDisposable
    {
        #region Serialized Properties
        /// <summary>
        /// Determines if this manager should persist between scenes
        /// </summary>
        [SerializeField]
        public bool isDontDestroyOnLoad;
        /// <summary>
        /// Contains prototypes which are used for obtaining mesh and material information and profile data for rendering.
        /// Prototypes can be created from prefabs or GPUILODGroupData or simply a mesh and materials.
        /// </summary>
        [SerializeField]
        protected GPUIPrototype[] _prototypes;
        [SerializeField]
        public GPUIProfile defaultProfile;
        [SerializeField]
        public bool isEnableDefaultRenderingWhenDisabled = true;
        #endregion Serialized Properties

        #region Runtime Properties
        /// <summary>
        /// Contains the list of unique keys for each renderer
        /// </summary>
        [NonSerialized]
        protected int[] _runtimeRenderKeys;
        public bool IsInitialized { get; private set; }
        [NonSerialized]
        protected JobHandle _dependentJob;
        [NonSerialized]
        private bool _loggedPrototypeValidationError;
        [NonSerialized]
        public int errorCode;
        [NonSerialized]
        public UnityEngine.Events.UnityAction errorFixAction;
        [NonSerialized]
        private static readonly Type[] _prefabRendererTypes = new Type[] { typeof(MeshRenderer), typeof(BillboardRenderer) };
        private const int ERROR_CODE_ADDITION = 200;
        #endregion Runtime Properties

        #region Editor Properties

#if UNITY_EDITOR
        public GPUIRenderingSystem.GPUIManagerUIStoredValues editor_UIStoredValues
        {
            get
            {
                if (GPUIRenderingSystem.editor_managerUIStoredValues == null)
                    GPUIRenderingSystem.editor_managerUIStoredValues = new();
                if (GPUIRenderingSystem.editor_managerUIStoredValues.TryGetValue(this, out var managerUIStoredValues))
                    return managerUIStoredValues;
                managerUIStoredValues = new();
                GPUIRenderingSystem.editor_managerUIStoredValues.Add(this, managerUIStoredValues);
                return managerUIStoredValues;
            }
        }

        public List<int> editor_selectedPrototypeIndexes 
        {
            get
            {
                return editor_UIStoredValues.selectedPrototypeIndexes;
            }
            set
            {
                editor_UIStoredValues.selectedPrototypeIndexes = value;
            }
        }

        [SerializeField]
        private bool editor_isRenderInEditMode = true;

        [SerializeField]
        public bool editor_isTextMode = false;

        [SerializeField]
        public bool editor_isRollbackRuntimeProfileChanges = false;
#endif

        #endregion Editor Properties

        #region MonoBehaviour Methods

        protected virtual void Awake()
        {
            if (!GPUIRuntimeSettings.Instance.IsSupportedPlatform())
            {
                enabled = false;
                return;
            }
        }

        protected virtual void Start()
        {
            CheckPrototypeChanges();
            if (Application.isPlaying && isDontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!GPUIRuntimeSettings.Instance.IsSupportedPlatform())
            {
                enabled = false;
                return;
            }
            if (Application.isPlaying && editor_isRollbackRuntimeProfileChanges && _prototypes != null)
            {
                foreach (var prototype in _prototypes)
                {
                    if (prototype.profile != null)
                        GPUIRenderingSystem.Editor_CacheProfile(prototype.profile);
                }
            }
            if (Application.isPlaying || IsRenderInEditMode())
            {
#endif
                if (!IsInitialized)
                    Initialize();
#if UNITY_EDITOR
            }
#endif
        }

        protected virtual void OnDisable()
        {
            Dispose();
        }

        protected virtual void Update()
        {
        }

        protected virtual void LateUpdate()
        {
        }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            defaultProfile = GetDefaultProfile();
        }
#endif

        #endregion MonoBehaviour Methods

        #region Initialize/Dispose

        public virtual bool IsValid(bool logError)
        {
            errorCode = 0;
            errorFixAction = null;
            return true;
        }

        public virtual void Initialize()
        {
            //Debug.Log(this.name + " initialized...");
            Dispose();
            CheckPrototypeChanges();
            IsValid(Application.isPlaying);
#if UNITY_EDITOR
            if (!Application.isPlaying && !IsRenderInEditMode())
                return;
#endif
            GPUIRenderingSystem.AddActiveManager(this);
            for (int i = 0; i < _prototypes.Length; i++)
                RegisterRenderer(i);
            IsInitialized = true;
        }

        public virtual void ReleaseBuffers() { }

        public virtual void Dispose()
        {
            _dependentJob.Complete();
            if (!IsInitialized) return;
            IsInitialized = false;
            ReleaseBuffers();
            GPUIRenderingSystem.RemoveActiveManager(this);
            if (_runtimeRenderKeys != null)
            {
                for (int i = 0; i < _runtimeRenderKeys.Length; i++)
                    DisposeRenderer(i);
                _runtimeRenderKeys = null;
            }
        }

        public virtual void OnPrototypeEnabledStatusChanged(int prototypeIndex, bool isEnabled)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !IsRenderInEditMode())
                return;
#endif
            if (!IsInitialized)
                return;

            if (isEnabled && _runtimeRenderKeys[prototypeIndex] == 0)
                OnPrototypeEnabled(prototypeIndex);
            else if(!isEnabled && _runtimeRenderKeys[prototypeIndex] != 0)
                OnPrototypeDisabled(prototypeIndex);
        }

        protected virtual void OnPrototypeEnabled(int prototypeIndex)
        {
            RegisterRenderer(prototypeIndex);
        }

        protected virtual void OnPrototypeDisabled(int prototypeIndex)
        {
            DisposeRenderer(prototypeIndex);
        }

#if UNITY_EDITOR
        public bool IsRenderInEditMode()
        {
            return editor_isRenderInEditMode && CanRenderInEditMode();
        }

        public virtual bool CanRenderInEditMode()
        {
            return true;
        }
#endif

        #endregion Initialize/Dispose

        #region Renderer Methods

        protected virtual bool RegisterRenderer(int prototypeIndex)
        {
            GPUIPrototype prototype = _prototypes[prototypeIndex];

            if (prototype == null)
            {
                Debug.LogError("Prototype at index: " + prototypeIndex + " is null.", this);
                return false;
            }

            if (!prototype.IsValid(true) || !prototype.isEnabled)
                return false;

            if (GPUIRenderingSystem.RegisterRenderer(this, prototype, out int renderKey, GetRendererGroupID(prototypeIndex), GetTransformBufferType(prototypeIndex), GetShaderKeywords(prototypeIndex)))
            {
                _runtimeRenderKeys[prototypeIndex] = renderKey;
                return true;
            }
            return false;
        }

        protected virtual void DisposeRenderer(int prototypeIndex)
        {
            if (_runtimeRenderKeys == null || _runtimeRenderKeys.Length <= prototypeIndex)
                return;
            int renderKey = _runtimeRenderKeys[prototypeIndex];
            if (renderKey != 0)
                GPUIRenderingSystem.DisposeRenderer(renderKey);
            _runtimeRenderKeys[prototypeIndex] = 0;
        }

        internal void OnRenderSourceDisposed(int runtimeRenderKey)
        {
            int prototypeIndex = GetPrototypeIndex(runtimeRenderKey);
            if (prototypeIndex >= 0)
            {
                int renderKey = _runtimeRenderKeys[prototypeIndex];
                if (renderKey != 0)
                {
                    _runtimeRenderKeys[prototypeIndex] = 0;
                    DisposeRenderer(prototypeIndex);
                }
            }
        }

        protected virtual void DisposeAllRenderers()
        {
            if (_runtimeRenderKeys == null)
                return;
            for (int i = 0; i < _runtimeRenderKeys.Length; i++)
            {
                int renderKey = _runtimeRenderKeys[i];
                if (renderKey != 0)
                    GPUIRenderingSystem.DisposeRenderer(renderKey);
                _runtimeRenderKeys[i] = 0;
            }
        }

        public int GetRenderKey(int prototypeIndex)
        {
            if (!IsInitialized || _runtimeRenderKeys == null || _runtimeRenderKeys.Length < prototypeIndex || prototypeIndex < 0)
                return 0;
            return _runtimeRenderKeys[prototypeIndex];
        }

        public int GetPrototypeIndex(int renderKey)
        {
            if (_runtimeRenderKeys != null)
            {
                for (int i = 0; i < _runtimeRenderKeys.Length; i++)
                {
                    if (_runtimeRenderKeys[i] == renderKey)
                        return i;
                }
            }
            return -1;
        }

        public int GetPrototypeIndex(GameObject prefabObject)
        {
            if (prefabObject == null)
                return -1;
            if (_prototypes != null)
            {
                for (int i = 0; i < _prototypes.Length; i++)
                {
                    if (_prototypes[i].prototypeType == GPUIPrototypeType.Prefab && _prototypes[i].prefabObject == prefabObject)
                        return i;
                }
            }
            return -1;
        }

        public int GetPrototypeIndex(GPUILODGroupData lgd)
        {
            if (lgd == null)
                return -1;
            if (_prototypes != null)
            {
                for (int i = 0; i < _prototypes.Length; i++)
                {
                    if (_prototypes[i].prototypeType == GPUIPrototypeType.LODGroupData && _prototypes[i].gpuiLODGroupData == lgd)
                        return i;
                }
            }
            return -1;
        }

        public virtual GPUIProfile GetDefaultProfile()
        {
            if (defaultProfile != null)
                return defaultProfile;
            return GPUIProfile.DefaultProfile;
        }

        public virtual int GetRendererGroupID(int prototypeIndex)
        {
            return 0;
        }

        public virtual GPUITransformBufferType GetTransformBufferType(int prototypeIndex)
        {
            return GPUITransformBufferType.Default;
        }

        public virtual List<string> GetShaderKeywords(int prototypeIndex)
        {
            return null;
        }

        public virtual Type[] GetPrefabRendererTypes()
        {
            return _prefabRendererTypes;
        }

        #endregion Renderer Methods

        #region Prototype Changes

        public virtual void CheckPrototypeChanges()
        {
            ClearNullPrototypes();
            SynchronizeData();
        }

        protected virtual bool ValidatePrototype(int prototypeIndex)
        {
            GPUIPrototype prototype = _prototypes[prototypeIndex];
            if (!prototype.IsValid(Application.isPlaying))
                return false;
            if (prototype.prototypeType == GPUIPrototypeType.Prefab)
            {
                if (!ValidatePrefabPrototype(prototype, GetPrefabRendererTypes()))
                    return false;
            }
            return true;
        }

        public static bool ValidatePrefabPrototype(GPUIPrototype prototype, Type[] rendererTypes)
        {
            if (prototype.prefabObject.TryGetComponent(out LODGroup lodGroup))
            {
                LOD[] lods = lodGroup.GetLODs();
                for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
                {
                    bool hasRenderer = false;
                    if (lods[lodIndex].renderers != null)
                    {
                        foreach (Renderer renderer in lods[lodIndex].renderers)
                        {
                            if (renderer != null)
                            {
                                if (rendererTypes.Contains(renderer.GetType()))
                                {
                                    hasRenderer = true;
                                    if (renderer is not BillboardRenderer && renderer.sharedMaterials.Contains(null))
                                    {
                                        prototype.errorCode = ERROR_CODE_ADDITION * 10 + 4; // missing material
                                        return false;
                                    }
                                    if ((renderer is MeshRenderer meshRenderer) && (!meshRenderer.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null))
                                    {
                                        prototype.errorCode = ERROR_CODE_ADDITION * 10 + 5; // missing mesh
                                        return false;
                                    }
                                    if (renderer is not BillboardRenderer)
                                    {
                                        foreach (var mat in renderer.sharedMaterials)
                                        {
                                            if (mat.shader == null)
                                            {
                                                prototype.errorCode = ERROR_CODE_ADDITION * 10 + 6; // missing shader
                                                return false;
                                            }
#if UNITY_EDITOR
                                            GPUIRenderingSystem.InitializeRenderingSystem();
                                            if (!GPUIRenderingSystem.Instance.MaterialProvider.TryGetReplacementMaterial(mat, null, null, out _))
                                                GPUIRenderingSystem.editor_UpdateMethod?.Invoke();
                                            if (GPUIRenderingSystem.Instance.MaterialProvider.IsFailedShaderConversion(mat.shader, null))
                                            {
                                                prototype.errorCode = ERROR_CODE_ADDITION * 10 + 7; // failed shader conversion
                                                return false;
                                            }
#endif
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!hasRenderer)
                    {
                        prototype.errorCode = ERROR_CODE_ADDITION * 10 + 2; // One or more LODs on the LOD Group component do not have any Mesh Renderers.
                        return false;
                    }
                }
            }
            else
            {
                if (prototype.prefabObject.GetComponentInChildren<LODGroup>(true) != null)
                {
                    prototype.errorCode = ERROR_CODE_ADDITION * 10 + 1; // Can not have LOD Group component on child GameObjects
                    return false;
                }
                MeshRenderer[] meshRenderers = prototype.prefabObject.GetComponentsInChildren<MeshRenderer>();
                if (meshRenderers == null || meshRenderers.Length == 0)
                {
                    prototype.errorCode = ERROR_CODE_ADDITION * 10 + 3; // Prefab does not have any Mesh Renderers.
                    return false;
                }
                foreach (var meshRenderer in meshRenderers)
                {
                    if (meshRenderer.sharedMaterials == null || meshRenderer.sharedMaterials.Contains(null))
                    {
                        prototype.errorCode = ERROR_CODE_ADDITION * 10 + 4; // missing material
                        return false;
                    }
                    if (!meshRenderer.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
                    {
                        prototype.errorCode = ERROR_CODE_ADDITION * 10 + 5; // missing mesh
                        return false;
                    }
                    foreach (var mat in meshRenderer.sharedMaterials)
                    {
                        if (mat.shader == null)
                        {
                            prototype.errorCode = ERROR_CODE_ADDITION * 10 + 6; // missing shader
                            return false;
                        }
#if UNITY_EDITOR
                        GPUIRenderingSystem.InitializeRenderingSystem();
                        if (!GPUIRenderingSystem.Instance.MaterialProvider.TryGetReplacementMaterial(mat, null, null, out _))
                            GPUIRenderingSystem.editor_UpdateMethod?.Invoke();
                        if (GPUIRenderingSystem.Instance.MaterialProvider.IsFailedShaderConversion(mat.shader, null))
                        {
                            prototype.errorCode = ERROR_CODE_ADDITION * 10 + 7; // failed shader conversion
                            return false;
                        }
#endif
                    }
                }
            }
            return true;
        }

        protected virtual void SynchronizeData()
        {
            int length = _prototypes.Length;
            if (_runtimeRenderKeys == null)
                _runtimeRenderKeys = new int[length];
            else if (_runtimeRenderKeys.Length != length)
                Array.Resize(ref _runtimeRenderKeys, length);
        }

        protected virtual void ClearNullPrototypes()
        {
            if (_prototypes == null)
                _prototypes = new GPUIPrototype[0];
            int length = _prototypes.Length;

            // Remove prototype when prefab reference is lost
            for (int i = 0; i < length; i++)
            {
                GPUIPrototype p = _prototypes[i];
                if (p.prototypeType == GPUIPrototypeType.Prefab)
                {
                    if (p.prefabObject == null)
                    {
                        RemovePrototypeAtIndex(i);
                        return;
                    }
                }
            }

            if (_runtimeRenderKeys == null)
                _runtimeRenderKeys = new int[length];

            bool isValid = true;
            for (int i = 0; i < _prototypes.Length; i++)
            {
                if (_prototypes[i] == null)
                {
                    RemovePrototypeAtIndex(i);
                    return;
                }
                isValid &= ValidatePrototype(i);
            }
            if (!isValid && Application.isPlaying && !_loggedPrototypeValidationError)
            {
                Debug.LogError("There are errors with the prototype setup, please check the prototypes on " + GPUIUtility.CamelToTitleCase(GetType().Name) + " for further information.", this.gameObject);
                _loggedPrototypeValidationError = true;
            }

#if UNITY_EDITOR
            length = _prototypes.Length;
            for (int i = 0; i < editor_selectedPrototypeIndexes.Count; i++)
            {
                if (editor_selectedPrototypeIndexes[i] >= length)
                    editor_selectedPrototypeIndexes.RemoveAt(i);
            }
#endif
        }

        /// <summary>
        /// Adds the prototype to the Manager.
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns>Prototype index. -1 when add operation fails.</returns>
        public virtual int AddPrototype(GPUIPrototype prototype)
        {
            if (!prototype.IsValid(true))
                return -1;

            if (_prototypes != null && _prototypes.Contains(prototype))
                return -1;

            int length = _prototypes.Length;
            Array.Resize(ref _prototypes, length + 1);
            _prototypes[length] = prototype;
            CheckPrototypeChanges();

            prototype.GenerateBillboard(false);

            if (IsInitialized)
                RegisterRenderer(length);

            return length;
        }

        /// <summary>
        /// Adds the given GameObject as a prototype to the Prefab Manager
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns>Prototype index. -1 when add operation fails.</returns>
        public int AddPrototype(GameObject prefab)
        {
            return AddPrototype(new GPUIPrototype(prefab, GetDefaultProfile()));
        }

        public virtual void RemovePrototypeAtIndex(int index)
        {
            if (IsInitialized)
            {
                DisposeRenderer(index);
                _runtimeRenderKeys = _runtimeRenderKeys.RemoveAtAndReturn(index);
            }
            _prototypes = _prototypes.RemoveAtAndReturn(index);
            CheckPrototypeChanges();
        }

        public virtual void RemoveAllPrototypes()
        {
            if (IsInitialized)
            {
                DisposeAllRenderers();
                _runtimeRenderKeys = new int[0];
            }
            _prototypes = new GPUIPrototype[0];
            CheckPrototypeChanges();
        }

        public virtual bool CanAddObjectAsPrototype(UnityEngine.Object obj)
        {
            if (obj is GameObject prefabObject)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var prefabType = PrefabUtility.GetPrefabAssetType(obj);
                    if (prefabType != PrefabAssetType.Regular && prefabType != PrefabAssetType.Variant && prefabType != PrefabAssetType.Model)
                        return false;
                }
#endif
                for (int i = 0; i < _prototypes.Length; i++)
                {
                    if (_prototypes[i].prefabObject == prefabObject)
                        return false;
                }
                return true;
            }
            else if (obj is GPUILODGroupData lodGroupData)
            {
                for (int i = 0; i < _prototypes.Length; i++)
                {
                    if (_prototypes[i].gpuiLODGroupData == lodGroupData)
                        return false;
                }
                return true;
            }
            return false;
        }

        public int GetPrototypeCount()
        {
            if (_prototypes == null)
                return 0;
            return _prototypes.Length;
        }

        public GPUIPrototype GetPrototype(int index)
        {
            if (_prototypes == null || _prototypes.Length <= index)
                return null;
            return _prototypes[index];
        }

        public virtual void OnPrototypePropertiesModified() { }

        #endregion Prototype Changes

        #region Getters/Setters
        public virtual int GetRegisteredInstanceCount(int prototypeIndex)
        {
            if (!IsInitialized || prototypeIndex < 0 || _runtimeRenderKeys == null || prototypeIndex >= _runtimeRenderKeys.Length) return 0;
            if (GPUIRenderingSystem.TryGetRenderSource(_runtimeRenderKeys[prototypeIndex], out GPUIRenderSource rs))
                return rs.instanceCount;
            return 0;
        }
        #endregion Getters/Setters

        #region Editor Methods
#if UNITY_EDITOR
        public virtual bool Editor_IsAllowEditPrototype(int prototypeType) => false;
#endif
        #endregion Editor Methods
    }
}