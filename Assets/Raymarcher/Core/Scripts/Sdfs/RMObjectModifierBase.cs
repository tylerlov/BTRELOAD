using System;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Raymarcher.Convertor;

namespace Raymarcher.Objects.Modifiers
{
    /// <summary>
    /// Base identifier for all modifier SDF entities.
    /// Inherit from this base class to define custom modifiers for Raymarcher SDF objects.
    /// </summary>
    [Serializable]
    [ExecuteAlways]
    public abstract class RMObjectModifierBase : MonoBehaviour, ISDFEntity
    {
#if UNITY_EDITOR
        // These constants are defined by the compiler within each modifier. Utilize these constants to modify the SDF in your custom modifier.
        protected const string VARCONST_POSITION = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_Position;
        protected const string VARCONST_SDF = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_Sdf;
        protected const string VARCONST_MATERIAL_INSTANCE = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_MaterialInstance;
        protected const string VARCONST_MATERIAL_TYPE = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_MaterialType;
        protected const string VARCONST_COLOR = RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_Color;
#else
        protected const string VARCONST_POSITION = "";
        protected const string VARCONST_SDF = "";
        protected const string VARCONST_MATERIAL_INSTANCE = "";
        protected const string VARCONST_MATERIAL_TYPE = "";
        protected const string VARCONST_COLOR = "";
#endif

        // Serialized fields

        [SerializeField] private RMObjectModifierSharedContainer sharedContainer;
        [SerializeField, HideInInspector] private RMObjectModifierSharedContainer cachedSharedContainer;

        // Serialized privates

        [SerializeField, HideInInspector] private RMSdfObjectBase sdfTarget;

        // Properties

        /// <summary>
        /// Target sdf object of this modifier
        /// </summary>
        public RMSdfObjectBase SdfTarget => sdfTarget;
        /// <summary>
        /// Shared modifier container (if any)
        /// </summary>
        public RMObjectModifierSharedContainer SharedModifierContainer => sharedContainer;

        protected virtual void Awake()
        {
#if UNITY_EDITOR
            if (!SdfTarget)
                sdfTarget = GetComponent<RMSdfObjectBase>();
            if (!SdfTarget)
            {
                RMDebug.Debug(this, $"The '{nameof(RMObjectModifierBase)}' requires '{nameof(RMSdfObjectBase)}' component!", true);
                return;
            }
            SdfTarget.AddModifier(this);
#endif
        }

        protected virtual void OnDestroy()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!(Time.frameCount != 0 && Time.renderedFrameCount != 0))
                    return;
            }
            if (SdfTarget)
                SdfTarget.RemoveModifier(this);
#endif
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!ModifierSupportsSharedContainer)
                return;
            if(cachedSharedContainer != SharedModifierContainer)
            {
                SdfTarget.MappingMaster.RenderMaster.SetRecompilationRequired(true);
                return;
            }
            if (!SharedModifierContainer)
                return;

            PassModifierDataToSharedContainer();
        }
#endif

        protected bool TryToGetSharedModifierContainerType<T>(out T output) where T : class
        {
            output = null;
            if (SharedModifierContainer == null)
                return false;
            if (!(SharedModifierContainer.SharedContainerInstance is T type))
                return false;
            output = type;
            return true;
        }

        #region Implementation methods & properties

        /// <summary>
        /// Invoked when the SdfObjectBuffer is recompiled
        /// </summary>
        public virtual void SdfBufferRecompiled()
        {
            if(ModifierSupportsSharedContainer)
                cachedSharedContainer = sharedContainer;
        }

        /// <summary>
        /// Specify a method name for this modifier. This method will be called and represent this modifier formula
        /// </summary>
        public abstract string SdfMethodName { get; }

        /// <summary>
        /// Specify an array of uniform fields for this modifier. These variables will be used and modified at runtime in the formula
        /// </summary>
        public abstract ISDFEntity.SDFUniformField[] SdfUniformFields { get; }

        /// <summary>
        /// Specify a method body for this modifier. This defines the actual behavior of the modifier formula
        /// </summary>
        public abstract string SdfMethodBody { get; }

        /// <summary>
        /// Specify an optional method extension for this modifier. Use this to inline all helper and utility methods at the top of the declaration list (optional)
        /// </summary>
        public virtual string SdfMethodExtension { get; } = "";

        /// <summary>
        /// Set Float/Vector/Color/Texture values in the raymarcher session material with the given iterationIndex
        /// </summary>
        /// <param name="raymarcherSessionMaterial">The current raymarcher scene material</param>
        /// <param name="iterationIndex">The current iteration index (combine this with your property name)</param>
        public abstract void PushSdfEntityToShader(in Material raymarcherSessionMaterial, in string iterationIndex);

        public enum InlineMode { PostSdfInstance, SdfInstancePosition, PostSdfBuffer }

        /// <summary>
        /// Select the appropriate inline mode for this modifier. Inform the compiler whether the SDF modifier should be inlined to the SDF position, placed below the SDF declaration, or positioned after the object buffer declaration.
        /// For more information about inline mode, refer to the official documentation on Raymarcher SDF modifiers.
        /// </summary>
        public virtual InlineMode ModifierInlineMode() => InlineMode.PostSdfInstance;

        /// <summary>
        /// If the modifier supports shared containers, create a boxed reference directive to the target shared container type
        /// </summary>
        public virtual object CreateSharedModifierContainer { get; } = null;

        /// <summary>
        /// Define whether this modifier supports shared modifier containers
        /// </summary>
        public virtual bool ModifierSupportsSharedContainer { get; } = false;

        /// <summary>
        /// Pass modifier data to the shared container object
        /// </summary>
        public virtual void PassModifierDataToSharedContainer() 
        {
            if (ModifierSupportsSharedContainer)
                SdfTarget.MappingMaster.SetDirtyModifiersWithSharedContainers();
        }

        /// <summary>
        /// Pass shared container object's data to the modifier
        /// </summary>
        public virtual void PassSharedContainerDataToModifier() { }

        #endregion
    }
}