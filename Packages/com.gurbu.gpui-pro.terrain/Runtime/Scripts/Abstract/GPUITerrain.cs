// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.TerrainModule
{
    public abstract class GPUITerrain : MonoBehaviour, IEquatable<GPUITerrain>
    {
        #region Serialized Properties
        [SerializeField]
        protected Bounds _bounds;
        [SerializeField]
        internal Texture2D[] _bakedDetailTextures;
        [SerializeField]
        public bool isAutoFindTreeManager = true;
        [SerializeField]
        public bool isAutoFindDetailManager = true;
        [SerializeField]
        public GPUITerrainHolesSampleMode terrainHolesSampleMode = GPUITerrainHolesSampleMode.Initialization;
        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        private Transform _cachedTransform;
        [NonSerialized]
        private Vector3 _cachedPosition;
        [NonSerialized]
        private RenderTexture _heightmapTexture;
        [NonSerialized]
        protected TreeInstance[] _treeInstances;
        [NonSerialized]
        protected RenderTexture[] _detailDensityTextures;

        public GPUITreeManager TreeManager { get; private set; }
        public TreePrototype[] TreePrototypes { get; protected set; }
        internal int[] TreePrototypeIndexes { get; private set; }

        public GPUIDetailManager DetailManager { get; private set; }
        public DetailPrototype[] DetailPrototypes { get; protected set; }
        internal int[] DetailPrototypeIndexes { get; private set; }

        public bool IsInitialized { get; private set; }
        public bool IsDetailDensityTexturesLoaded { get; protected set; }

        protected static readonly TreeInstance[] _emptyTreeInstances = new TreeInstance[0];
        protected static RenderTexture dummyHolesTexture;

        #endregion Runtime Properties

        #region MonoBehaviour Methods
        protected virtual void Awake()
        {
            LoadTerrain();
        }

        protected virtual void OnEnable()
        {
            if (!IsInitialized)
                Initialize();
            if (DetailManager != null)
                DetailManager.RequireUpdate();
            if (TreeManager != null)
                TreeManager.RequireUpdate();
        }

        protected virtual void OnDisable()
        {
            Dispose();
        }
        #endregion MonoBehaviour Methods

        #region Initialize/Dispose

        /// <summary>
        /// Set terrain references and bounds
        /// </summary>
        public virtual void LoadTerrain()
        {
            if (_cachedTransform == null)
                _cachedTransform = transform;
            NotifyTransformChanges();
        }

        public virtual void LoadTerrainData()
        {
            LoadTerrain();
        }

        protected void Initialize()
        {
            Dispose();
            LoadTerrainData();
            CreateHeightmapTexture();
            IsInitialized = true;

            if (TreeManager != null)
            {
                if (!TreeManager.AddTerrain(this))
                    SetTreeManager(TreeManager); // if terrain is already added, reload data
            }
            else if (Application.isPlaying && isAutoFindTreeManager)
            {
                TreeManager = FindFirstObjectByType<GPUITreeManager>();
                if (TreeManager != null)
                {
                    if (!TreeManager.AddTerrain(this))
                        SetTreeManager(TreeManager); // if terrain is already added, reload data
                }
            }

            if (DetailManager != null)
            {
                if (!DetailManager.AddTerrain(this))
                    SetDetailManager(DetailManager); // if terrain is already added, reload data
            }
            else if (Application.isPlaying && isAutoFindDetailManager)
            {
                DetailManager = FindFirstObjectByType<GPUIDetailManager>();
                if (DetailManager != null)
                {
                    if (!DetailManager.AddTerrain(this))
                        SetDetailManager(DetailManager); // if terrain is already added, reload data
                }
            }
        }

        internal void Dispose()
        {
            IsInitialized = false;
            DisposeDetailDensityTextures();
            _treeInstances = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!gameObject.scene.isLoaded)
                    return;
                if (TreeManager != null)
                    TreeManager.RequireUpdate();
                if (DetailManager != null)
                    DetailManager.RequireUpdate();
                return;
            }
#endif
            if (TreeManager != null)
                TreeManager.RemoveTerrain(this);
            if (DetailManager != null)
                DetailManager.RemoveTerrain(this);

            if (dummyHolesTexture != null)
                dummyHolesTexture.DestroyRenderTexture();
        }

        protected void DisposeDetailDensityTextures()
        {
            IsDetailDensityTexturesLoaded = false;
            if (_detailDensityTextures != null)
            {
                for (int i = 0; i < _detailDensityTextures.Length; i++)
                    DisposeDetailDensityTexture(i);
                _detailDensityTextures = null;
            }
        }

        protected void ResizeDetailDensityTexturesArray(int detailCount)
        {
            if (_detailDensityTextures == null)
                _detailDensityTextures = new RenderTexture[detailCount];
            else if(detailCount > _detailDensityTextures.Length)
                Array.Resize(ref _detailDensityTextures, detailCount);
            else if (detailCount < _detailDensityTextures.Length)
            {
                for (int i = detailCount; i < _detailDensityTextures.Length; i++)
                    DisposeDetailDensityTexture(i);
                Array.Resize(ref _detailDensityTextures, detailCount);
            }
        }

        protected void DisposeDetailDensityTexture(int index)
        {
            RenderTexture rt = _detailDensityTextures[index];
            if (rt != null && rt.name.EndsWith("_GPUIDL"))
                GPUITextureUtility.DestroyRenderTexture(rt);
        }

        internal virtual void SetTerrainDetailObjectDistance(float value) { }

        internal virtual void SetTerrainTreeDistance(float value) { }

        #region Create Heightmap and Detail Textures

        protected void CreateHeightmapTexture()
        {
            _heightmapTexture = LoadHeightmapTexture();
        }

        protected abstract RenderTexture LoadHeightmapTexture();

        public void CreateDetailTextures(bool forceUpdate = false)
        {
            LoadDetailDensityTextures(forceUpdate);
            IsDetailDensityTexturesLoaded = true;
        }

        protected virtual void LoadDetailDensityTextures(bool forceUpdate = false)
        {
            DisposeDetailDensityTextures();
            int detailCount = DetailPrototypes == null ? 0 : DetailPrototypes.Length;
            if (detailCount == 0)
                return;
            int detailResolution = GetDetailResolution();

            _detailDensityTextures = new RenderTexture[detailCount];

            if (_bakedDetailTextures == null)
                _bakedDetailTextures = new Texture2D[detailCount];
            else if (_bakedDetailTextures.Length != detailCount)
                Array.Resize(ref _bakedDetailTextures, detailCount);

            string terrainName = name;
            for (int i = 0; i < detailCount; i++)
            {
                _detailDensityTextures[i] = GPUITerrainUtility.CreateDetailRenderTexture(detailResolution, terrainName + "_GPUIDL" + i);
                if (_bakedDetailTextures[i] != null)
                    Graphics.Blit(_bakedDetailTextures[i], _detailDensityTextures[i]);
            }

            if (DetailManager != null)
                DetailManager.RequireUpdate();
        }

        protected abstract int GetDetailResolution();

        #endregion Create Heightmap and Detail Textures

        public virtual void RemoveTreePrototypeAtIndex(int index) { }

        public virtual void RemoveDetailPrototypeAtIndex(int index) { }

#endregion Initialize/Dispose

        #region Update Methods

        internal void GenerateVegetation(GPUIDetailPrototypeData detailPrototypeData, GraphicsBuffer transformBuffer, GPUIDataBuffer<GPUICounterData> counterBuffer, Vector3 cameraPos, float detailObjectDistance, Texture2D healthyDryNoiseTexture, int[] sizeAndIndexes)
        {
            if (!IsInitialized) return;

            if (_heightmapTexture == null)
            {
                CreateHeightmapTexture();
                if (_heightmapTexture == null)
                    return;
            }

            if (!IsDetailDensityTexturesLoaded)
                CreateDetailTextures();

            if (_detailDensityTextures == null) return;

            Vector3 terrainPos = GetPosition();
            if (!IsTerrainWithinViewDistance(terrainPos, cameraPos, detailObjectDistance)) return;

            if (DetailPrototypeIndexes == null)
                DetermineDetailPrototypeIndexes(DetailManager);

            int prototypeIndex = sizeAndIndexes[1];
            int subSettingCount = detailPrototypeData.GetSubSettingCount();

            ComputeShader CS_VegetationGenerator = GPUITerrainConstants.CS_VegetationGenerator;
            for (int terrainPrototypeIndex = 0; terrainPrototypeIndex < DetailPrototypeIndexes.Length && terrainPrototypeIndex < _detailDensityTextures.Length; terrainPrototypeIndex++)
            {
                int managerPrototypeIndex = DetailPrototypeIndexes[terrainPrototypeIndex];
                if (managerPrototypeIndex % GPUIDetailManager.DETAIL_SUB_SETTING_DIVIDER != prototypeIndex) continue;
                if (!detailPrototypeData.TryGetParameterBufferIndex(out sizeAndIndexes[2])) continue;
                int subSettingIndex = managerPrototypeIndex / GPUIDetailManager.DETAIL_SUB_SETTING_DIVIDER;
                if (subSettingCount <= subSettingIndex || !detailPrototypeData.GetSubSettings(subSettingIndex).TryGetParameterBufferIndex(out sizeAndIndexes[3]))
                {
                    Debug.LogError("Can not find Detail Prototype Sub Setting parameter buffer index.");
                    continue;
                }

                RenderTexture detailTexture = _detailDensityTextures[terrainPrototypeIndex];
                if (detailTexture == null) continue;

                int detailTextureWidth = detailTexture.width;
                if (detailPrototypeData.isUseDensityReduction && detailPrototypeData.densityReduceDistance < detailObjectDistance)
                    CS_VegetationGenerator.EnableKeyword(GPUITerrainConstants.Kw_GPUI_DETAIL_DENSITY_REDUCE);
                else
                    CS_VegetationGenerator.DisableKeyword(GPUITerrainConstants.Kw_GPUI_DETAIL_DENSITY_REDUCE);

                if (terrainHolesSampleMode == GPUITerrainHolesSampleMode.Runtime)
                {
                    CS_VegetationGenerator.EnableKeyword(GPUITerrainConstants.Kw_GPUI_TERRAIN_HOLES);
                    CS_VegetationGenerator.SetTexture(0, GPUITerrainConstants.PROP_terrainHoleTexture, GetHolesTexture());
                }
                else
                    CS_VegetationGenerator.DisableKeyword(GPUITerrainConstants.Kw_GPUI_TERRAIN_HOLES);

                CS_VegetationGenerator.SetBuffer(0, GPUIConstants.PROP_gpuiTransformBuffer, transformBuffer);
                CS_VegetationGenerator.SetBuffer(0, GPUITerrainConstants.PROP_detailCounterBuffer, counterBuffer);

                CS_VegetationGenerator.SetBuffer(0, GPUIConstants.PROP_parameterBuffer, GPUIRenderingSystem.Instance.ParameterBuffer);
                CS_VegetationGenerator.SetTexture(0, GPUITerrainConstants.PROP_terrainDetailTexture, detailTexture);
                CS_VegetationGenerator.SetTexture(0, GPUITerrainConstants.PROP_heightmapTexture, _heightmapTexture);

                CS_VegetationGenerator.SetInt(GPUITerrainConstants.PROP_detailTextureSize, detailTextureWidth);
                CS_VegetationGenerator.SetInt(GPUITerrainConstants.PROP_heightmapTextureSize, _heightmapTexture.width);
                CS_VegetationGenerator.SetVector(GPUITerrainConstants.PROP_startPosition, terrainPos);
                CS_VegetationGenerator.SetVector(GPUITerrainConstants.PROP_terrainSize, GetSize());
                CS_VegetationGenerator.SetInts(GPUIConstants.PROP_sizeAndIndexes, sizeAndIndexes);

                CS_VegetationGenerator.SetVector(GPUITerrainConstants.PROP_cameraPos, cameraPos);
                CS_VegetationGenerator.SetFloat(GPUITerrainConstants.PROP_density, GetDetailDensity(terrainPrototypeIndex));
                CS_VegetationGenerator.SetFloat(GPUITerrainConstants.PROP_detailObjectDistance, detailObjectDistance);

                CS_VegetationGenerator.SetTexture(0, GPUITerrainConstants.PROP_healthyDryNoiseTexture, healthyDryNoiseTexture);

                CS_VegetationGenerator.DispatchXZ(0, detailTextureWidth, detailTextureWidth);
            }
        }

        private bool IsTerrainWithinViewDistance(Vector3 terrainPos, Vector3 cameraPos, float detailObjectDistance)
        {
            Bounds b = _bounds;
            b.center += terrainPos;
            if (!b.Contains(cameraPos) && Mathf.Sqrt(b.SqrDistance(cameraPos)) > detailObjectDistance)
                return false;

            return true;
        }

        public bool IsTerrainWithinViewDistance(Vector3 cameraPos, float viewDistance)
        {
            return IsTerrainWithinViewDistance(GetPosition(), cameraPos, viewDistance);
        }

        public void NotifyTransformChanges()
        {
            if (_cachedPosition != _cachedTransform.position)
            {
                _cachedPosition = _cachedTransform.position;
                if (TreeManager != null)
                    TreeManager.RequireUpdate();
                if (DetailManager != null)
                    DetailManager.RequireUpdate();
            }
        }

        protected virtual void LoadTreeInstances() { }

        private void ConvertToGPUITreeData(GPUITreeManager treeManager)
        {
            TreeInstance[] treeData = GetTreeInstances();
            if (IsUnorderedTreePrototypeIndexes(treeManager))
            {
                for (int i = 0; i < treeData.Length; i++)
                {
                    TreeInstance treeInstance = treeData[i];
                    treeInstance.prototypeIndex = TreePrototypeIndexes[treeInstance.prototypeIndex];
                    treeData[i] = treeInstance;
                }
            }
            if (treeManager._enableTreeInstanceColors)
            {
                for (int i = 0; i < treeData.Length; i++)
                {
                    TreeInstance treeInstance = treeData[i];
                    Color color = treeInstance.color;
                    treeInstance.color = DecodeFloatRGBA(color);
                    treeData[i] = treeInstance;
                }
            }
        }

        private static readonly Vector4 _kDecodeDot = new Vector4(1.0f, 1 / 255.0f, 1 / 65025.0f, 1 / 16581375.0f);
        private static Color32 DecodeFloatRGBA(Vector4 enc)
        {
            float result = Vector4.Dot(enc, _kDecodeDot);
            byte[] bytes = BitConverter.GetBytes(result);
            return new Color32(bytes[0], bytes[1], bytes[2], bytes[3]);
        }

        internal void SetTreeManager(GPUITreeManager treeManager)
        {
            Profiler.BeginSample("GPUITerrain.SetTreeManager");
            if (TreeManager != null && TreeManager != treeManager) // To avoid adding the same terrain on multiple managers
                TreeManager.RemoveTerrain(this);
            TreeManager = treeManager;

            SetTerrainTreeDistance(0);
            DetermineTreePrototypeIndexes(treeManager);

            LoadTreeInstances();
            ConvertToGPUITreeData(treeManager);
            Profiler.EndSample();
        }

        internal void SetDetailManager(GPUIDetailManager detailManager)
        {
            Profiler.BeginSample("GPUITerrain.SetDetailManager");
            if (DetailManager != null && DetailManager != detailManager) // To avoid adding the same terrain on multiple managers
                DetailManager.RemoveTerrain(this);
            DetailManager = detailManager;

            SetTerrainDetailObjectDistance(0);
            DetermineDetailPrototypeIndexes(detailManager);
            Profiler.EndSample();
        }

        internal void RemoveTreeManager()
        {
            if (TreeManager != null && (!Application.isPlaying || TreeManager.isEnableDefaultRenderingWhenDisabled))
                SetTerrainTreeDistance(GetTerrainTreeDistance());
            TreeManager = null;
            _treeInstances = null;
        }

        internal void RemoveDetailManager()
        {
            if (DetailManager != null && (!Application.isPlaying || DetailManager.isEnableDefaultRenderingWhenDisabled))
                SetTerrainDetailObjectDistance(DetailManager.detailObjectDistance);
            DetailManager = null;
        }

        #endregion Update Methods

        #region Getters / Setters

        internal void DetermineTreePrototypeIndexes(GPUITreeManager treeManager)
        {
            if (TreePrototypes == null)
            {
                if (TreePrototypeIndexes == null || TreePrototypeIndexes.Length != 0)
                    TreePrototypeIndexes = new int[0];
                return;
            }
            if (TreePrototypeIndexes == null || TreePrototypeIndexes.Length != TreePrototypes.Length)
                TreePrototypeIndexes = new int[TreePrototypes.Length];

            if (treeManager == null) return;
            for (int i = 0; i < TreePrototypes.Length; i++)
                TreePrototypeIndexes[i] = treeManager.DetermineTreePrototypeIndex(TreePrototypes[i]);
        }

        internal void DetermineDetailPrototypeIndexes(GPUIDetailManager detailManager)
        {
            if (DetailPrototypes == null)
            {
                DetailPrototypeIndexes = new int[0];
                return;
            }
            if (DetailPrototypeIndexes == null || DetailPrototypeIndexes.Length != DetailPrototypes.Length)
                DetailPrototypeIndexes = new int[DetailPrototypes.Length];

            if (detailManager == null) return;
            for (int i = 0; i < DetailPrototypes.Length; i++)
                DetailPrototypeIndexes[i] = detailManager.DetermineDetailPrototypeIndex(DetailPrototypes[i]);
        }

        protected bool IsUnorderedTreePrototypeIndexes(GPUITreeManager treeManager)
        {
            if (TreePrototypeIndexes == null)
                DetermineTreePrototypeIndexes(treeManager);
            for (int i = 0; i < TreePrototypeIndexes.Length; i++)
            {
                if (i != TreePrototypeIndexes[i])
                    return true;
            }
            return false;
        }

        public Bounds GetBounds()
        {
            Bounds b = _bounds;
            b.center += GetPosition();
            return b;
        }

        public virtual float GetTerrainTreeDistance()
        {
            return 5000f;
        }

        public RenderTexture GetHeightmapTexture()
        {
            if (_heightmapTexture == null)
                CreateHeightmapTexture();
            return _heightmapTexture;
        }

        public void SetHeightmapTexture(RenderTexture heightmapTexture)
        {
            _heightmapTexture = heightmapTexture;
        }

        public abstract int GetHeightmapResolution();

        public virtual Vector3 GetPosition()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (_cachedTransform == null)
                    _cachedTransform = transform;
                return _cachedTransform.position;
            }
#endif
            return _cachedPosition;
        }

        public virtual bool IsBakedDetailTextures()
        {
            return true;
        }

        public int GetTerrainTreePrototypeIndex(int managerPrototypeIndex)
        {
            if (TreePrototypeIndexes == null)
                DetermineTreePrototypeIndexes(TreeManager);

            for (int i = 0; i < TreePrototypeIndexes.Length; i++)
            {
                if (TreePrototypeIndexes[i] == managerPrototypeIndex)
                    return i;
            }
            return -1;
        }

        public int GetTerrainDetailPrototypeIndex(int managerPrototypeIndex)
        {
            if (DetailPrototypeIndexes == null)
                DetermineDetailPrototypeIndexes(DetailManager);

            for (int i = 0; i < DetailPrototypeIndexes.Length; i++)
            {
                if (DetailPrototypeIndexes[i] % GPUIDetailManager.DETAIL_SUB_SETTING_DIVIDER == managerPrototypeIndex)
                    return i;
            }
            return -1;
        }

        public virtual Vector3 GetSize()
        {
            return _bounds.size;
        }

        public virtual float GetDetailDensity(int prototypeIndex)
        {
            return Mathf.Pow(2f, 16f) / Mathf.Pow(GetDetailResolution(), 2f);
        }

        public int GetDetailTextureCount()
        {
            if (_detailDensityTextures == null)
                return 0;
            return _detailDensityTextures.Length;
        }

        public RenderTexture GetDetailDensityTexture(int index)
        {
            if (_detailDensityTextures == null || index < 0 || _detailDensityTextures.Length <= index)
                return null;
            return _detailDensityTextures[index];
        }

        public Texture2D GetBakedDetailTexture(int index)
        {
            if (_bakedDetailTextures == null || index < 0 || _bakedDetailTextures.Length <= index)
                return null;
            return _bakedDetailTextures[index];
        }

        public virtual void SetBakedDetailTexture(int index, Texture2D texture)
        {
            if (DetailPrototypes == null)
            {
                Debug.LogError("Detail prototypes are not set.");
                return;
            }
            if (_bakedDetailTextures == null)
                _bakedDetailTextures = new Texture2D[DetailPrototypes.Length];
            if (index < 0 || index > _bakedDetailTextures.Length)
            {
                Debug.LogError("SetBakedDetailTexture error: given index [" + index + "] is out of bounds. Detail prototype count: " + _bakedDetailTextures.Length);
                return;
            }
            _bakedDetailTextures[index] = texture;
            if (IsDetailDensityTexturesLoaded)
                CreateDetailTextures();
        }

        public virtual void SetDetailDensityTexture(int index, RenderTexture renderTexture)
        {
            if (DetailPrototypes == null)
            {
                Debug.LogError("Detail prototypes are not set.");
                return;
            }
            if (!IsDetailDensityTexturesLoaded)
                CreateDetailTextures();
            if (_detailDensityTextures[index] != null)
                _detailDensityTextures[index].Release();
            _detailDensityTextures[index] = renderTexture;
        }

        public TreeInstance[] GetTreeInstances(bool reloadTreeInstances = false)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !isActiveAndEnabled)
                return _emptyTreeInstances;
#endif
            if (reloadTreeInstances)
                LoadTreeInstances();
            if (_treeInstances == null)
                return _emptyTreeInstances;
            return _treeInstances;
        }

        public void SetTreeInstances(TreeInstance[] treeInstances)
        {
            _treeInstances = treeInstances;
            if (TreeManager != null)
            {
                ConvertToGPUITreeData(TreeManager);
                TreeManager.RequireUpdate();
            }
        }

        public virtual Color GetWavingGrassTint()
        {
            return Color.white;
        }

        public virtual void AddTreePrototypeToTerrain(GameObject pickerGameObject, int overwriteIndex) { }

        public virtual void AddDetailPrototypeToTerrain(UnityEngine.Object pickerObject, int overwriteIndex) { }

        public string GetTreePrototypeIndexesToString()
        {
            if (TreePrototypeIndexes == null)
                return null;
            string result = "";
            for (int i = 0; i < TreePrototypeIndexes.Length; i++)
            {
                if (i > 0)
                    result += ", ";
                result += TreePrototypeIndexes[i];
            }
            return result;
        }

        public string GetDetailPrototypeIndexesToString()
        {
            if (DetailPrototypeIndexes == null)
                return null;
            string result = "";
            for (int i = 0; i < DetailPrototypeIndexes.Length; i++)
            {
                if (i > 0)
                    result += ", ";
                result += (DetailPrototypeIndexes[i] % GPUIDetailManager.DETAIL_SUB_SETTING_DIVIDER) + "[" + (DetailPrototypeIndexes[i] / GPUIDetailManager.DETAIL_SUB_SETTING_DIVIDER) + "]";
            }
            return result;
        }

        public bool Equals(GPUITerrain other)
        {
            if (other == null) return false;
            return GetInstanceID() == other.GetInstanceID();
        }

        public override bool Equals(object obj)
        {
            if (obj is GPUITerrain other)
                return Equals(other);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return GetInstanceID();
        }

        public virtual Texture GetHolesTexture()
        {
            if (dummyHolesTexture == null)
            {
                dummyHolesTexture = new RenderTexture(1, 1, 0, GPUIRuntimeSettings.Instance.API_HAS_GUARANTEED_R8_SUPPORT ? RenderTextureFormat.R8 : RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
                {
                    isPowerOfTwo = false,
                    enableRandomWrite = true,
                    filterMode = FilterMode.Point,
                    useMipMap = false,
                    autoGenerateMips = false,
                };
                dummyHolesTexture.Create();
                Texture2D dummy2D = new Texture2D(1, 1);
                dummy2D.SetPixel(0, 0, Color.white);
                Graphics.Blit(dummy2D, dummyHolesTexture);
                dummy2D.DestroyGeneric();
            }
            return dummyHolesTexture;
        }

        #endregion Getters / Setters

        public enum GPUITerrainHolesSampleMode
        {
            /// <summary>
            /// Only sample terrain holes texture during initialization
            /// </summary>
            Initialization = 0,
            /// <summary>
            /// Sample terrain holes texture every time the detail instances are regenerated
            /// </summary>
            Runtime = 1,
            /// <summary>
            /// Never sample terrain holes texture
            /// </summary>
            None = 2,
        }
    }
}