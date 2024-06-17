using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Raymarcher.Convertor;
using Raymarcher.Objects.Modifiers;
using Raymarcher.Materials;
using Raymarcher.RendererData;

namespace Raymarcher.Objects
{
    /// <summary>
    /// Base class for all objects representing a signed-distance field in Raymarcher.
    /// Inherit from this class to create a custom SDF object with a unique SDF formula.
    /// </summary>
    [Serializable]
    [ExecuteInEditMode]
    public abstract class RMSdfObjectBase : MonoBehaviour, ISDFEntity
    {
#if UNITY_EDITOR
        // These constants are defined by the compiler within each sdf object. Utilize these constants to modify the SDF in your custom formula.
        protected const string VARCONST_RESULT = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_Result;
        protected const string VARCONST_POSITION = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_Position;
        protected const string VARCONST_COLOR = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_Color;
#else
        protected const string VARCONST_RESULT = "";
        protected const string VARCONST_POSITION = "";
        protected const string VARCONST_COLOR = "";
#endif

        // Serialized privates

        [SerializeField, HideInInspector] private RMMaterialBase objectMaterial;
        [SerializeField, Range(-360, 360), HideInInspector] private float hueShift = 0;
        [SerializeField, HideInInspector] private SdfQualityRenderData qualityRenderData = new SdfQualityRenderData(); 

        [SerializeField, HideInInspector] private RMMaterialBase cachedObjectMaterial;
        [SerializeField, HideInInspector] private Texture2D cachedObjectTexture;

        [SerializeField, HideInInspector] private RMRenderMaster renderMaster;
        [SerializeField, HideInInspector] private List<RMObjectModifierBase> modifiers;
        [SerializeField, HideInInspector] private int cachedModifierCount = 0;

        // Properties

        /// <summary>
        /// Does the object have some sdf modifiers?
        /// </summary>
        public bool HasModifiers => modifiers != null && modifiers.Count > 0;
        /// <summary>
        /// Currently cached sdf object modifiers
        /// </summary>
        public IReadOnlyList<RMObjectModifierBase> Modifiers => modifiers;
        /// <summary>
        /// Reference shortcut to the current render master
        /// </summary>
        public RMRenderMaster RenderMaster => renderMaster;
        /// <summary>
        /// Reference shortcut to the current object buffer
        /// </summary>
        public RMCoreRenderMasterMapping MappingMaster => renderMaster == null ? null : renderMaster.MappingMaster;
        /// <summary>
        /// Current material instance on the object
        /// </summary>
        public RMMaterialBase ObjectMaterial => objectMaterial;
        /// <summary>
        /// Current quality render data on the object
        /// </summary>
        public SdfQualityRenderData QualityRenderData => qualityRenderData;
        /// <summary>
        /// Current hue shift on the object (not active when the render type is set to Quality)
        /// </summary>
        public float HueShift { get => hueShift; set => hueShift = value; }

        [Serializable]
        public sealed class SdfQualityRenderData
        {
            public Color objectColor = Color.white;
            [Space]
            public Texture2D objectTexture;
            public float textureTiling = 1f;
            [Range(1.0e-4f, 1)] public float textureScaleX = 1f;
            [Range(1.0e-4f, 1)] public float textureScaleY = 1f;
            [Range(1.0e-4f, 1)] public float textureScaleZ = 1f;
            [Range(0, 1)] public float textureOpacity = 1f;
        }


        #region Editor core handling

        [ContextMenu("> REGISTER OBJECT MANUALLY")]
        protected virtual void Awake()
        {
#if UNITY_EDITOR
            if (MappingMaster == null)
                renderMaster = FindObjectOfType<RMRenderMaster>();

            if(renderMaster == null)
            {
                RMDebug.Debug(this, $"'{nameof(RMRenderMaster)}' is missing in the scene! It's not possible to create Sdf Objects without a valid render master", true);
                DestroyImmediate(this);
                return;
            }

            MappingMaster.AddSdfObjectToContainer(this);
#endif
        }

        protected virtual void OnDestroy()
        {
#if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            if (MappingMaster == null)
                return;

            MappingMaster.RemoveSdfObjectFromContainer(this);
            MappingMaster.RenderMaster.MasterMaterials.ObjectMaterialChanged(null);
#endif
        }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            if(renderMaster == null)
                Awake();
        }

        protected virtual void OnValidate()
        {
            if (MappingMaster == null)
                return;

            if (Modifiers == null)
                MappingMaster.RenderMaster.SetRecompilationRequired(true);
            else if (cachedModifierCount != Modifiers.Count)
                MappingMaster.RenderMaster.SetRecompilationRequired(true);

            if (cachedObjectMaterial != ObjectMaterial)
                AlterMaterialInstance(objectMaterial, ref cachedObjectMaterial);

            if (MappingMaster.RenderMaster.RenderingData.CompiledRenderType != RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality)
                return;
            if(cachedObjectTexture != QualityRenderData.objectTexture)
                MappingMaster.RenderMaster.SetRecompilationRequired(true);
        }

        public void AlterMaterialInstance(RMMaterialBase targetMat, ref RMMaterialBase cacheTargetMat)
        {
            if (targetMat)
            {
                if (targetMat.IsGlobalMaterialInstance)
                {
                    EditorUtility.DisplayDialog("Warning", $"Material '{targetMat.name}' is a global material and cannot be used for individual Raymarcher objects. " +
                        $"Remove this material from the 'Global Materials' list in the Raymarcher Master renderer and try again.", "OK");
                    objectMaterial = null;
                }
                else if (!targetMat.IsCompatibleWithSelectedPlatform(MappingMaster.RenderMaster.CompiledTargetPlatform))
                {
                    EditorUtility.DisplayDialog("Warning", $"Material '{targetMat.name}' is not compatible with the selected target platform '{MappingMaster.RenderMaster.CompiledTargetPlatform}'." +
                        $"Please use a different material type compatible with the selected target platform.", "OK");
                    objectMaterial = null;
                }
                else
                    DispatchChange(ref cacheTargetMat);
            }
            else
                DispatchChange(ref cacheTargetMat);

            void DispatchChange(ref RMMaterialBase cache)
            {
                MappingMaster.RenderMaster.MasterMaterials.ObjectMaterialChanged(targetMat);
                MappingMaster.RenderMaster.SetRecompilationRequired(true);
                cache = targetMat;
            }
        }

        protected static T CreateSDFObject<T>() where T : RMSdfObjectBase
        {
            Type type = typeof(T);
            GameObject newSdf = new GameObject(type.Name);
            var sdfType = newSdf.AddComponent(type) as T;

            Selection.activeGameObject = newSdf;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return sdfType;
            Camera sceneCam = sceneView.camera;
            if (sceneCam == null)
                return sdfType;

            newSdf.transform.position = sceneCam.transform.position + sceneCam.transform.forward * 8f;
            return sdfType;
        }

        public string GetMyIdentifierFromMappingMaster() => MappingMaster.GetSdfUniqueIdentifier(this);

        public (int typeIndex, int instanceIndex) GetMyMaterialTypeAndInstanceIndex()
        {
            if (!ObjectMaterial)
                return (-1, 0);

            return RenderMaster.MasterMaterials.GetMaterialTypeAndInstanceIndex(ObjectMaterial);
        }

        public void SetObjectMaterialNullEditor()
        {
            objectMaterial = null;
            OnValidate();
        }
#else
        public string GetMyIdentifierFromMappingMaster() => "";
#endif

        #endregion

#if UNITY_EDITOR
        #region Modifier handling

        public void AddModifier(RMObjectModifierBase modifier)
        {
            if(modifiers == null)
                modifiers = new List<RMObjectModifierBase>();
            if (modifiers.Contains(modifier))
                return;

            modifiers.Add(modifier);
            MappingMaster.RenderMaster.SetRecompilationRequired(true);
        }

        public void RemoveModifier(RMObjectModifierBase modifier)
        {
            if (modifiers == null)
                return;
            if (!modifiers.Contains(modifier))
                return;

            modifiers.Remove(modifier);
            RenderMaster.SetRecompilationRequired(true);
        }

        [ContextMenu("> REFRESH MODIFIERS")]
        public void RefreshModifiers()
        {
            modifiers = new List<RMObjectModifierBase>(GetComponents<RMObjectModifierBase>());
            MappingMaster.RenderMaster.SetRecompilationRequired(true);
        }

        #endregion
#endif

        public void PushModifiersToShader(in Material raymarcherSessionMaterial, in string iterationIndex)
        {
            if (Modifiers != null)
                for (int i = 0; i < Modifiers.Count; i++)
                {
                    if(Modifiers[i] != null)
                        Modifiers[i].PushSdfEntityToShader(raymarcherSessionMaterial, Modifiers[i].SharedModifierContainer
                            ? Modifiers[i].SharedModifierContainer.ShaderQueueID
                            : iterationIndex);
                }
        }

        #region Implementation methods & properties

        /// <summary>
        /// Invoked when the SdfObjectBuffer is recompiled (optional)
        /// </summary>
        public virtual void SdfBufferRecompiled()
        {
            if (Modifiers == null)
                return;
            for (int i = 0; i < Modifiers.Count; i++)
            {
                var modifier = Modifiers[i];
                if (modifier == null)
                {
                    modifiers.RemoveAt(i);
                    continue;
                }
                modifier.SdfBufferRecompiled();
            }
            cachedModifierCount = Modifiers.Count;
            cachedObjectTexture = QualityRenderData.objectTexture;
        }

        /// <summary>
        /// Specify a method name for this SDF. This method will be called and represent this SDF formula
        /// </summary>
        public abstract string SdfMethodName { get; }

        /// <summary>
        /// Specify an array of uniform fields for this SDF. These variables will be used and modified at runtime in the formula
        /// </summary>
        public abstract ISDFEntity.SDFUniformField[] SdfUniformFields { get; }

        /// <summary>
        /// Specify a method body for this SDF. This defines the actual behavior for this SDF formula
        /// </summary>
        public abstract string SdfMethodBody { get; }

        /// <summary>
        /// Specify an optional method extension for this SDF. Use this to inline all helper and utility methods at the top of the declaration list (optional)
        /// </summary>
        public virtual string SdfMethodExtension { get; }

        /// <summary>
        /// Set Float/Vector/Color/Texture values in the raymarcher session material with the given iterationIndex
        /// </summary>
        /// <param name="raymarcherSessionMaterial">The current raymarcher session material</param>
        /// <param name="iterationIndex">The current iteration index (combine this with your property name)</param>
        public abstract void PushSdfEntityToShader(in Material raymarcherSessionMaterial, in string iterationIndex);

        #endregion
    }
}