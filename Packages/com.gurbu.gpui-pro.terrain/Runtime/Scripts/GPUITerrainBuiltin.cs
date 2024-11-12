// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.TerrainModule
{
    /// <summary>
    /// This component is automatically attached to Terrains that are used with GPUI Managers
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Terrain))]
    [DefaultExecutionOrder(-200)]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted#GPUI_Terrain")]
    public class GPUITerrainBuiltin : GPUITerrain
    {
        #region Serialized Properties

        [SerializeField]
        internal float _terrainTreeDistance = 5000f;
        [SerializeField]
        internal bool _isBakedDetailTextures;
        [SerializeField]
        protected bool _isCustomBakedDetailTextures;
        [SerializeField]
        private Terrain _terrain;

        #endregion Serialized Properties

        #region Runtime Properties
        [NonSerialized]
        private DetailScatterMode _detailScatterMode;
#if UNITY_EDITOR
        [NonSerialized]
        internal float _latestDetailObjectDensity;
#endif
        #endregion Runtime Properties

        #region MonoBehaviour Methods
#if UNITY_EDITOR
        protected virtual void Reset()
        {
            if (!Application.isPlaying && !GetComponent<Terrain>())
            {
                Debug.LogError("Terrain Modifier components must be added to Terrains!");
                DestroyImmediate(this);
            }
            LoadTerrainData();
        }
#endif
        #endregion MonoBehaviour Methods

        #region Initialize/Dispose

        public override void LoadTerrain()
        {
            base.LoadTerrain();
            if (_terrain == null)
            {
                _terrain = GetComponent<Terrain>();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(this);
#endif
            }

            Vector3 size = _terrain.terrainData.size;
            Bounds bounds = new Bounds(size / 2f, size);
            if (_bounds != bounds)
            {
                _bounds = bounds;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(this);
#endif
            }
        }

        public override void LoadTerrainData()
        {
            base.LoadTerrainData();

            if (_terrain.terrainData != null)
            {
                TreePrototypes = _terrain.terrainData.treePrototypes;
                DetailPrototypes = _terrain.terrainData.detailPrototypes;
                _detailScatterMode = _terrain.terrainData.detailScatterMode;

                if (_terrain.treeDistance > 0)
                    _terrainTreeDistance = _terrain.treeDistance;
            }
        }

        internal override void SetTerrainDetailObjectDistance(float value)
        {
            base.SetTerrainDetailObjectDistance(value);

            if (_terrain == null) return;
            _terrain.detailObjectDistance = value;
        }

        internal override void SetTerrainTreeDistance(float value)
        {
            base.SetTerrainTreeDistance(value);

            if (_terrain == null) return;
            _terrain.treeDistance = value;
        }

        #region Create Heightmap and Detail Textures

        protected override RenderTexture LoadHeightmapTexture()
        {
            return _terrain.terrainData.heightmapTexture;
        }

        protected override void LoadDetailDensityTextures(bool forceUpdate = false)
        {
            if (_terrain == null || _terrain.terrainData == null)
                return;

            int detailCount = _terrain.terrainData.detailPrototypes.Length;
            if (forceUpdate || detailCount == 0 || !Application.isPlaying)
                DisposeDetailDensityTextures();

            if (detailCount == 0)
                return;

            Profiler.BeginSample("GPUITerrainBuiltin.LoadDetailDensityTextures");
            _detailScatterMode = _terrain.terrainData.detailScatterMode;
            int detailResolution = _terrain.terrainData.detailResolution;

            ResizeDetailDensityTexturesArray(detailCount);

            if (_isBakedDetailTextures)
            {
                if (_bakedDetailTextures == null)
                    _bakedDetailTextures = new Texture2D[detailCount];
                else if (_bakedDetailTextures.Length != detailCount)
                    Array.Resize(ref _bakedDetailTextures, detailCount);
            }

            for (int i = 0; i < detailCount; i++)
            {
                RenderTexture rt = _detailDensityTextures[i];
                if (rt != null)
                {
                    if (_isBakedDetailTextures && _isCustomBakedDetailTextures)
                        Graphics.Blit(_bakedDetailTextures[i], rt);
                    continue;
                }
                rt = GPUITerrainUtility.CreateDetailRenderTexture(detailResolution, _terrain.terrainData.name + "_GPUIDL" + i);
                _detailDensityTextures[i] = rt;

                if (_isBakedDetailTextures && _isCustomBakedDetailTextures)
                    Graphics.Blit(_bakedDetailTextures[i], rt);
                else
                    CaptureTerrainDetailsToRenderTexture(rt, i);
            }
            if (DetailManager != null)
                DetailManager.RequireUpdate();

            Profiler.EndSample();
        }

        private void CaptureTerrainDetailsToRenderTexture(RenderTexture rt, int detailLayer)
        {
            if (_isBakedDetailTextures && _isCustomBakedDetailTextures)
                return;
            Profiler.BeginSample("GPUITerrainBuiltin.CaptureTerrainDetailsToRenderTexture");
            GPUITerrainUtility.CaptureTerrainDetailToRenderTexture(_terrain, detailLayer, rt, terrainHolesSampleMode == GPUITerrainHolesSampleMode.Initialization);
            Profiler.EndSample();
        }

        public void SetDetailLayer(int layer, int[,] details)
        {
            int detailCount = _terrain.terrainData.detailPrototypes.Length;
            if (layer >= detailCount)
                return;
            ResizeDetailDensityTexturesArray(detailCount);
            int detailResolution = _terrain.terrainData.detailResolution;
            if (_detailDensityTextures[layer] == null)
                _detailDensityTextures[layer] = GPUITerrainUtility.CreateDetailRenderTexture(detailResolution, _terrain.terrainData.name + "_GPUIDL" + layer);

            GPUITerrainUtility.CaptureTerrainDetailToRenderTexture(_terrain, detailResolution, details, _detailDensityTextures[layer], terrainHolesSampleMode == GPUITerrainHolesSampleMode.Initialization);
            IsDetailDensityTexturesLoaded = true;
        }

        #endregion Create Heightmap and Detail Textures

        public override void RemoveTreePrototypeAtIndex(int index)
        {
            base.RemoveTreePrototypeAtIndex(index);

            int terrainPrototypeIndex = GetTerrainTreePrototypeIndex(index);
            if (terrainPrototypeIndex < 0)
                return;

            List<TreePrototype> newTreePrototypes = new List<TreePrototype>(_terrain.terrainData.treePrototypes);
            TreeInstance[] treeInstances = _terrain.terrainData.treeInstances;

            for (int i = 0; i < treeInstances.Length; i++)
            {
                TreeInstance treeInstance = treeInstances[i];
                if (treeInstance.prototypeIndex < terrainPrototypeIndex)
                    continue;
                else if (treeInstance.prototypeIndex == terrainPrototypeIndex)
                {
                    treeInstances = treeInstances.RemoveAtAndReturn(i);
                    i--;
                }
                else if (treeInstance.prototypeIndex > terrainPrototypeIndex)
                    treeInstances[i].prototypeIndex = treeInstance.prototypeIndex - 1;
            }

            if (newTreePrototypes.Count > terrainPrototypeIndex)
                newTreePrototypes.RemoveAt(terrainPrototypeIndex);

            _terrain.terrainData.treeInstances = treeInstances;
            _terrain.terrainData.treePrototypes = newTreePrototypes.ToArray();

            _terrain.terrainData.RefreshPrototypes();
        }

        public override void RemoveDetailPrototypeAtIndex(int index)
        {
            base.RemoveDetailPrototypeAtIndex(index);

            int terrainPrototypeIndex = GetTerrainDetailPrototypeIndex(index);
            if (terrainPrototypeIndex < 0)
                return;

            DetailPrototype[] terrainDetailPrototypes = _terrain.terrainData.detailPrototypes;
            List<DetailPrototype> newDetailPrototypes = new List<DetailPrototype>();
            List<int[,]> newDetailLayers = new List<int[,]>();

            for (int i = 0; i < terrainDetailPrototypes.Length; i++)
            {
                if (i != terrainPrototypeIndex)
                {
                    newDetailPrototypes.Add(terrainDetailPrototypes[i]);
                    newDetailLayers.Add(_terrain.terrainData.GetDetailLayer(0, 0, _terrain.terrainData.detailResolution, _terrain.terrainData.detailResolution, i));
                }
            }

            _terrain.terrainData.detailPrototypes = newDetailPrototypes.ToArray();
            for (int i = 0; i < newDetailLayers.Count; i++)
            {
                _terrain.terrainData.SetDetailLayer(0, 0, i, newDetailLayers[i]);
            }
            _terrain.terrainData.RefreshPrototypes();
        }

        #endregion Initialize/Dispose

        #region Update Methods

        /// <summary>
        /// Saves the runtime detail density changes to TerrainData detail layers
        /// </summary>
        [ContextMenu("Save Detail Density Changes")]
        public void SaveDetailChangesToTerrainData()
        {
            for (int i = 0; i < GetDetailTextureCount(); i++)
            {
                GPUITerrainUtility.UpdateTerrainDetailWithRenderTexture(_terrain, i, GetDetailDensityTexture(i));
            }
        }

        /// <summary>
        /// Resets the runtime detail density changes
        /// </summary>
        [ContextMenu("Reset Detail Density Changes")]
        public void ResetDetailChanges()
        {
            CreateDetailTextures();
            if (DetailManager != null)
                DetailManager.RequireUpdate();
        }

        protected override void LoadTreeInstances()
        {
            Profiler.BeginSample("GPUITerrainBuiltin.LoadTreeInstances");
            if (_terrain != null && _terrain.terrainData != null)
                _treeInstances = _terrain.terrainData.treeInstances;
            Profiler.EndSample();
        }

        #endregion Update Methods

        #region Getters / Setters

        public override int GetHeightmapResolution()
        {
            return _terrain.terrainData.heightmapResolution;
        }

        public override Vector3 GetSize()
        {
            return _terrain.terrainData.size;
        }

        public Terrain GetTerrain()
        {
            if (_terrain == null)
                LoadTerrain();
            return _terrain;
        }

        public override float GetTerrainTreeDistance()
        {
            return _terrainTreeDistance;
        }

        public override bool IsBakedDetailTextures()
        {
            return _isBakedDetailTextures;
        }

        public override float GetDetailDensity(int prototypeIndex)
        {
            DetailPrototype detailPrototype = DetailPrototypes[prototypeIndex];
            float prototypeDensity = detailPrototype.useDensityScaling ? _terrain.detailObjectDensity : 1f;
            if (_detailScatterMode == DetailScatterMode.CoverageMode)
            {
                int detailResolution = _terrain.terrainData.detailResolution;
                Vector3 terrainSize = GetSize();
                float densityDivider = (terrainSize.x / detailResolution) * (terrainSize.z / detailResolution);
                Bounds prototypeBounds = DetailManager.GetPrototypeBounds(DetailPrototypeIndexes[prototypeIndex] % GPUIDetailManager.DETAIL_SUB_SETTING_DIVIDER);
                float boundsMaxSize = math.max(math.max(prototypeBounds.size.x, prototypeBounds.size.z), 1f);
                return (prototypeDensity * densityDivider * math.pow(detailPrototype.density, 2f)) / (math.pow(detailPrototype.maxWidth, 2f) * math.pow(boundsMaxSize, 3f));
                //return (_latestDetailObjectDensity / (2f * Mathf.Pow(detailPrototype.maxWidth, 2f))) * densityDivider * Mathf.Pow(detailPrototype.density, 2f);
            }
            return prototypeDensity * 255f;
        }

        public override Color GetWavingGrassTint()
        {
            if (_terrain == null || _terrain.terrainData == null)
                return base.GetWavingGrassTint();

            return _terrain.terrainData.wavingGrassTint;
        }

        public override void AddTreePrototypeToTerrain(GameObject pickerGameObject, int overwriteIndex)
        {
            base.AddTreePrototypeToTerrain(pickerGameObject, overwriteIndex);

            TreePrototype[] treePrototypes = _terrain.terrainData.treePrototypes;
            if (overwriteIndex >= 0)
            {
                int prototypeIndex = GetTerrainTreePrototypeIndex(overwriteIndex);
                if (prototypeIndex >= 0 && prototypeIndex < treePrototypes.Length)
                {
                    treePrototypes[prototypeIndex].prefab = pickerGameObject;
                    _terrain.terrainData.treePrototypes = treePrototypes;
                    _terrain.terrainData.RefreshPrototypes();
                    return;
                }
            }
            else
            {
                List<TreePrototype> newTreePrototypes = new List<TreePrototype>(treePrototypes);
                newTreePrototypes.Add(new TreePrototype()
                {
                    prefab = pickerGameObject
                });
                _terrain.terrainData.treePrototypes = newTreePrototypes.ToArray();
                _terrain.terrainData.RefreshPrototypes();
            }

            DetermineTreePrototypeIndexes(TreeManager);
        }

        public override void AddDetailPrototypeToTerrain(UnityEngine.Object pickerObject, int overwriteIndex)
        {
            base.AddDetailPrototypeToTerrain(pickerObject, overwriteIndex);

            DetailPrototype[] detailPrototypes = _terrain.terrainData.detailPrototypes;

            if (pickerObject is Texture2D)
            {
                if (overwriteIndex >= 0)
                {
                    int prototypeIndex = GetTerrainDetailPrototypeIndex(overwriteIndex);
                    if (prototypeIndex >= 0 && prototypeIndex < detailPrototypes.Length)
                    {
                        detailPrototypes[prototypeIndex].prototype = null;
                        detailPrototypes[prototypeIndex].prototypeTexture = (Texture2D)pickerObject;
                        detailPrototypes[prototypeIndex].renderMode = DetailRenderMode.GrassBillboard;
                        detailPrototypes[prototypeIndex].usePrototypeMesh = false;
                        _terrain.terrainData.detailPrototypes = detailPrototypes;
                        _terrain.terrainData.RefreshPrototypes();
                    }
                }
                else
                {
                    List<DetailPrototype> newDetailPrototypes = new List<DetailPrototype>(detailPrototypes);
                    newDetailPrototypes.Add(new DetailPrototype()
                    {
                        usePrototypeMesh = false,
                        prototypeTexture = (Texture2D)pickerObject,
                        renderMode = DetailRenderMode.GrassBillboard,
                        noiseSeed = UnityEngine.Random.Range(100, 100000)
                    });
                    _terrain.terrainData.detailPrototypes = newDetailPrototypes.ToArray();
                    _terrain.terrainData.RefreshPrototypes();
                }
            }
            else if (pickerObject is GameObject pickerGameObject)
            {
                if (pickerGameObject.GetComponentInChildren<MeshRenderer>() == null)
                    return;

                if (overwriteIndex >= 0)
                {
                    int prototypeIndex = GetTerrainDetailPrototypeIndex(overwriteIndex);
                    if (prototypeIndex >= 0 && prototypeIndex < detailPrototypes.Length)
                    {
                        detailPrototypes[prototypeIndex].prototype = pickerGameObject;
                        detailPrototypes[prototypeIndex].prototypeTexture = null;
                        detailPrototypes[prototypeIndex].renderMode = DetailRenderMode.VertexLit;
                        detailPrototypes[prototypeIndex].usePrototypeMesh = true;
                        _terrain.terrainData.detailPrototypes = detailPrototypes;
                        _terrain.terrainData.RefreshPrototypes();
                    }
                }
                else
                {
                    List<DetailPrototype> newDetailPrototypes = new List<DetailPrototype>(detailPrototypes);
                    newDetailPrototypes.Add(new DetailPrototype()
                    {
                        usePrototypeMesh = true,
                        prototype = pickerGameObject.GetComponentInChildren<MeshRenderer>().gameObject,
                        renderMode = DetailRenderMode.VertexLit,
                        noiseSeed = UnityEngine.Random.Range(100, 100000),
                        healthyColor = Color.white,
                        dryColor = Color.white,
                        useInstancing = true
                    });

                    _terrain.terrainData.detailPrototypes = newDetailPrototypes.ToArray();
                    _terrain.terrainData.RefreshPrototypes();
                }
            }

            DetermineDetailPrototypeIndexes(DetailManager);
        }

        public override void SetBakedDetailTexture(int index, Texture2D texture)
        {
            base.SetBakedDetailTexture(index, texture);
            _isBakedDetailTextures = true;
            _isCustomBakedDetailTextures = true;
        }

        protected override int GetDetailResolution()
        {
            return _terrain.terrainData.detailResolution;
        }

        public override Texture GetHolesTexture()
        {
            return _terrain.terrainData.holesTexture;
        }

        #endregion Getters / Setters

        #region Editor Methods

#if UNITY_EDITOR
        [NonSerialized]
        private long _lastDetailChangeTicks;
        [NonSerialized]
        private long _lastTreeChangeTicks;
        [NonSerialized]
        private static readonly long _waitForTicks = 1000;
        [NonSerialized]
        private bool _isTreesBeingModified;
        [NonSerialized]
        private bool _isDetailsBeingModified;
        //[NonSerialized]
        //private bool _reloadAllDetails;
        //[NonSerialized]
        //private Type _paintDetailsToolType;
        //[NonSerialized]
        //private PropertyInfo _selectedDetailPropertyInfo;
        //[NonSerialized]
        //private UnityEngine.Object[] _paintDetailsTools;

        private void OnTerrainChanged(TerrainChangedFlags flags)
        {
            if (Application.isPlaying)
                return;
            //Debug.Log(flags);

            bool isFlushEverything = (flags & TerrainChangedFlags.FlushEverythingImmediately) != 0;
            if (isFlushEverything)
                LoadTerrainData();

            bool isHeightmapChanged = (flags & TerrainChangedFlags.Heightmap) != 0 || (flags & TerrainChangedFlags.HeightmapResolution) != 0;
            bool isHoles = (flags & TerrainChangedFlags.Holes) != 0;

            if (IsDetailDensityTexturesLoaded && DetailManager != null && (isFlushEverything || isHoles || (flags & TerrainChangedFlags.RemoveDirtyDetailsImmediately) != 0))
            {
                _lastDetailChangeTicks = DateTime.Now.Ticks;
                //if (isFlushEverything || isHoles)
                //    _reloadAllDetails = true;
                if (!_isDetailsBeingModified)
                {
                    _isDetailsBeingModified = true;
                    EditorApplication.update -= DelayedCaptureTerrainDetails;
                    EditorApplication.update += DelayedCaptureTerrainDetails;
                }
                //if (!_reloadAllDetails)
                //{
                //    RenderTexture dt = GetDetailDensityTexture(0);
                //    if (dt != null && dt.width != _terrain.terrainData.detailResolution)
                //        _reloadAllDetails = true;
                //}
            }

            if (TreeManager != null && (isFlushEverything || (flags & TerrainChangedFlags.TreeInstances) != 0))
            {
                _lastTreeChangeTicks = DateTime.Now.Ticks;
                if (!_isTreesBeingModified)
                {
                    _isTreesBeingModified = true;
                    EditorApplication.update -= DelayedCaptureTerrainTrees;
                    EditorApplication.update += DelayedCaptureTerrainTrees;
                }
            }

            if (isFlushEverything || isHeightmapChanged)
            {
                CreateHeightmapTexture();
                if (TreeManager != null && TreeManager.IsInitialized)
                    TreeManager.RequireUpdate();
                if (DetailManager != null && DetailManager.IsInitialized)
                    DetailManager.RequireUpdate();
            }
        }

        private void DelayedCaptureTerrainDetails()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= DelayedCaptureTerrainDetails;
                _isDetailsBeingModified = false;
                return;
            }
            if (DateTime.Now.Ticks - _lastDetailChangeTicks < _waitForTicks)
                return;

            try
            {
                // Paint Details Tool is not always reliable
                //if (_paintDetailsTools == null || _paintDetailsTools.Length == 0 || _selectedDetailPropertyInfo == null)
                //{
                //    _paintDetailsToolType = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.TerrainTools.PaintDetailsTool");
                //    _selectedDetailPropertyInfo = _paintDetailsToolType.GetProperty("selectedDetail");
                //    _paintDetailsTools = Resources.FindObjectsOfTypeAll(_paintDetailsToolType);
                //}
                //if (!_reloadAllDetails && _paintDetailsTools != null && _paintDetailsTools.Length == 1 && _selectedDetailPropertyInfo != null)
                //{
                //    int detailLayer = (int)_selectedDetailPropertyInfo.GetValue(_paintDetailsTools[0]);
                //    if (detailLayer < GetDetailTextureCount())
                //        UpdateDetailDensityTexture(GetDetailDensityTexture(detailLayer), detailLayer);
                //}
                //else
                    CreateDetailTextures(true);

                if (DetailManager != null)
                    DetailManager.RequireUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            _isDetailsBeingModified = false;
            //_reloadAllDetails = false;
            EditorApplication.update -= DelayedCaptureTerrainDetails;
        }

        private void DelayedCaptureTerrainTrees()
        {
            if (Application.isPlaying)
            {
                EditorApplication.update -= DelayedCaptureTerrainTrees;
                _isTreesBeingModified = false;
                return;
            }
            if (DateTime.Now.Ticks - _lastTreeChangeTicks < _waitForTicks)
                return;


            if (TreeManager != null)
            {
                SetTreeManager(TreeManager); // required to match the prototype indexes
                TreeManager.RequireUpdate();
            }

            _isTreesBeingModified = false;
            EditorApplication.update -= DelayedCaptureTerrainTrees;
        }

        public void Editor_EnableBakedDetailTextures()
        {
            _isBakedDetailTextures = true;
            _isCustomBakedDetailTextures = false;
            CreateDetailTextures(true);
        }

        public void Editor_DeleteBakedDetailTextures()
        {
            if (!EditorUtility.DisplayDialog("Delete Baked Density Textures", "Do you wish to delete the generated detail density textures?", "Yes", "No"))
                return;
            _isBakedDetailTextures = false;
            if (_bakedDetailTextures != null)
            {
                for (int i = 0; i < _bakedDetailTextures.Length; i++)
                {
                    if (_bakedDetailTextures[i] != null && AssetDatabase.Contains(_bakedDetailTextures[i]) && _bakedDetailTextures[i].name.Contains("_GPUIDL"))
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_bakedDetailTextures[i]));
                }
                _bakedDetailTextures = null;
            }
        }

        public void Editor_SaveDetailRenderTexturesToBakedTextures()
        {
            int detailCount = _terrain.terrainData.detailPrototypes.Length;
            for (int detailLayer = 0; detailLayer < detailCount; detailLayer++)
            {
                if (_isBakedDetailTextures && !Application.isPlaying)
                {
                    int detailResolution = _terrain.terrainData.detailResolution;
                    string folderPath = _terrain.terrainData.GetAssetFolderPath();
                    if (_bakedDetailTextures[detailLayer] != null && AssetDatabase.Contains(_bakedDetailTextures[detailLayer]) && _bakedDetailTextures[detailLayer].name.EndsWith("_GPUIDL"))
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_bakedDetailTextures[detailLayer]));
                    _bakedDetailTextures[detailLayer] = GPUITextureUtility.SaveRenderTextureToPNG(GetDetailDensityTexture(detailLayer), TextureFormat.R8, folderPath, TextureImporterType.Default, detailResolution, true, false, false);
                }
            }
        }
#endif //UNITY_EDITOR

        #endregion Editor Methods
    }
}