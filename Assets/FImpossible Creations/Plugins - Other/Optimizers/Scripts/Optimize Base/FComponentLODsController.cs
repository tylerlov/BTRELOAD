using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.FOptimizing
{
    /// <summary>
    /// FM: Class which is helping holding settings and references for one optimized component.
    /// > Containing reference to target optimized component from scene/prefab
    /// > Handling applying changes to target optimized component in playmode
    /// > Handling drawing editor windows elements for optimization settings etc.
    /// > Containing ref to LOD settings of target component
    /// </summary>
    [System.Serializable]
    public partial class FComponentLODsController
    {
        public FOptimizer_LODSettings LODSet;
        /// <summary> If LOD Set is assigned from asset </summary>
        [SerializeField]
        private FOptimizer_LODSettings sharedLODSet;
        /// <summary> If LOD Set for this component is individual </summary>
        [SerializeField]
        private FOptimizer_LODSettings uniqueLODSet;

        /// <summary> Optimizer which is optimizing target component </summary>
        [SerializeField]
        private FOptimizer_Base optimizer;

        /// <summary> Component choosed to be optimized </summary>
        public Component Component;

        /// <summary> Reference to FLOD_Base but with unclamped ranges pointing to true settings values of the component which is generated when game starts </summary>
        public FLOD_Base InitialSettings { get; protected set; }

        //[SerializeField]
        //[HideInInspector]
        //private FLOD_Base _referenceLOD;
        //public FLOD_Base ReferenceLOD { get { if (!_referenceLOD) return _referenceLOD; if (LODSet.LevelOfDetailSets.Count > 0) return _referenceLOD = LODSet.LevelOfDetailSets[0]; return _referenceLOD; } protected set { _referenceLOD = value; } }
        public FLOD_Base ReferenceLOD { get { if (LODSet.LevelOfDetailSets.Count > 0) return LODSet.LevelOfDetailSets[0]; return null; } }

        [HideInInspector]
        /// <summary> Reference to target type of component to optimize, it needs to be saved as asset inside project to make it work with prefabs </summary>
        public FLOD_Base RootReference; // { get; protected set; }

        /// <summary> Making first LOD properties view as unactive inside inspector window </summary>
        [SerializeField]
        [HideInInspector]
        protected bool lockFirstLOD = true;

#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        private string editorHeader = "";

        [SerializeField]
        [HideInInspector]
        private bool drawProperties = true;
#endif

        public int LODLevelsCount { get { return optimizer.LODLevels; } }
        public int CurrentLODLevel { get; private set; }
        [HideInInspector]
        public bool UsingShared = false;// { get; private set; }

        /// <summary> Can be used for custom coding </summary>
        internal int Version = 0;

#if UNITY_2018_3_OR_NEWER
        /// <summary> Flag used in Unity Version 2018.3+ to create prefab with LOD settings from scene </summary>
        public int nullTry = 0;
#endif

        #region Initialization


        public FComponentLODsController(FOptimizer_Base sourceOptimizer, Component toOptimize, string header = "", FLOD_Base rootReference = null) //: this(sourceOptimizer, toOptimize, header)
        {
            optimizer = sourceOptimizer;
            Component = toOptimize;
#if UNITY_EDITOR
            editorHeader = header;
#endif
            RootReference = rootReference;
        }


        /// <summary>
        /// Refreshing values of LOD parameters to be the same like target optimized component values
        /// </summary>
        public void OnStart()
        {
            if (!RootReference) return;
            if (InitialSettings == null) InitialSettings = RootReference.GetLODInstance();
            InitialSettings.SetSameValuesAsComponent(Component);
        }

        #endregion


        #region Playmode LOD Settings Applying Operations

        internal void SetCurrentLODLevel(int currentLODLevel)
        {
            CurrentLODLevel = currentLODLevel;
            if (currentLODLevel >= LODSet.LevelOfDetailSets.Count) CurrentLODLevel = LODSet.LevelOfDetailSets.Count - 1;
        }

        internal void ApplyLODLevelSettings(FLOD_Base currentLOD)
        {
            if (currentLOD == null)
            {
                if (RootReference == null)
                {
                    Debug.LogError("[OPTIMIZERS] CRITICAL ERROR: There is no root reference in Optimizer's LOD Controller! (" + optimizer + ") " + "Try adding Optimizers Manager again to the scene or import newest version from the Asset Store!");
                }

                Debug.LogError("[OPTIMIZERS] Target LOD is NULL! (" + optimizer.name + " - " + RootReference.name + ")");
                return;
            }

            CurrentLODLevel = GetLODIndex(currentLOD);
            if (IsTransitioningOrOther()) CurrentLODLevel = -1;
            currentLOD.ApplySettingsToComponent(Component, InitialSettings);

        }

        internal FLOD_Base GetCurrentLOD()
        {
            return LODSet.LevelOfDetailSets[CurrentLODLevel];
        }

        internal FLOD_Base GetCullingLOD()
        {
            return LODSet.LevelOfDetailSets[LODSet.LevelOfDetailSets.Count - 2];
        }

        internal FLOD_Base GetHiddenLOD()
        {
            return LODSet.LevelOfDetailSets[LODSet.LevelOfDetailSets.Count - 1];
        }

        #endregion


        #region Utilities

        public int GetLODIndex(FLOD_Base lod)
        {
            for (int i = 0; i < LODSet.LevelOfDetailSets.Count; i++)
            {
                if (LODSet.LevelOfDetailSets[i] == lod) return i;
            }

            return -1;
        }

        public bool IsTransitioningOrOther()
        {
            if (CurrentLODLevel >= 0 && CurrentLODLevel <= LODSet.LevelOfDetailSets.Count) return false; else return true;
        }

        public FOptimizer_Base GetOptimizer()
        {
            return optimizer;
        }

        #endregion

    }
}
