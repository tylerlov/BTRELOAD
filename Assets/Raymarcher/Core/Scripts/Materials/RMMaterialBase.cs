using UnityEngine;

namespace Raymarcher.Materials
{
    public static class RMMaterialExtensions
    {
        public static bool IsCompatibleWithSelectedPlatform(this RMMaterialBase material, RMRenderMaster.TargetPlatform targetPlatform)
        {
            return !(targetPlatform != RMRenderMaster.TargetPlatform.PCConsole && targetPlatform != RMRenderMaster.TargetPlatform.PCVR && !material.UnpackedDataContainersSupported);
        }
    }

    /// <summary>
    /// Base class for a specific material container.
    /// Inherit from this class to define all the required material fields for creating a custom material in Raymarcher.
    /// Written by Matej Vanco, November 2023.
    /// </summary>
    public abstract class RMMaterialBase : ScriptableObject
    {
        [Space]
        [SerializeField, Attributes.RMAttributes.ReadOnly] private bool isGlobalMaterial;

        public bool displayGlobalFeatureDependencies = true;

        public bool IsGlobalMaterialInstance => isGlobalMaterial;

        /// <summary>
        /// Once the material instance is set as global, it can't be used for individual objects in a scene
        /// </summary>
        public void SetThisMaterialInstanceAsGlobal(bool setAsGlobal)
            => isGlobalMaterial = setAsGlobal;

        public void DisposeMaterialInstance()
        {
            isGlobalMaterial = false;
        }


        /// <summary>
        /// Invoked by the compiler when the material instance has been changed in the scene.
        /// </summary>
        /// <returns>Must return a specific materialDataBuffer type with its identifier</returns>
        public abstract RMMaterialDataBuffer MaterialCreateDataBufferInstance();

        /// <summary>
        /// Array of available textures for this material instance
        /// </summary>
        public virtual Texture2D[] MaterialTexturesPerInstance { get; } = null;

        /// <summary>
        /// (Optional) Define whether the material type supports unpacked data containers. In another words: Are the mobile & WebGL platforms supported?
        /// If yes, this will require further modifications in the target material Data Buffer and there are certain limitations creating materials with unpacked containers.
        /// Please refer to the main documentation.
        /// </summary>
        public virtual bool UnpackedDataContainersSupported { get; } = false;
    }
}