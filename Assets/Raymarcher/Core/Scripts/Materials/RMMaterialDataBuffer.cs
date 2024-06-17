using System;
using System.Collections.Generic;

using UnityEngine;

using Raymarcher.Utilities;

namespace Raymarcher.Materials
{
    /// <summary>
    /// Base class for a specific material data buffer.
    /// Inherit from this class to manage material instances in the scene.
    /// Written by Matej Vanco, November 2023.
    /// </summary>
    [Serializable]
    public abstract class RMMaterialDataBuffer : IRMComputeBuffer
    {
        public const string MATERIAL_DATACONTAINER_TYPE_INSTANCE = "Instance";
        public const string MATERIAL_TEXTURECONTAINER_TYPE_INSTANCE = "Textures";

        public RMMaterialDataBuffer(in RMMaterialIdentifier materialIdentifier)
        {
#if UNITY_EDITOR
            materialDataContainerNamePerInstanceVariable = materialIdentifier.MaterialDataContainerTypePerInstance + MATERIAL_DATACONTAINER_TYPE_INSTANCE;
            materialTexturesPerInstanceVariable = materialIdentifier.MaterialDataContainerTypePerInstance + MATERIAL_TEXTURECONTAINER_TYPE_INSTANCE;

            materialGlobalKeywords = materialIdentifier.ShaderKeywordFeaturesGlobal;

            this.materialIdentifier = materialIdentifier;
#endif
        }

        // Compute Buffer

        public ComputeBuffer RuntimeComputeBuffer { get; set; }

        // Serialized privates

        [SerializeField] private List<RMMaterialBase> materialInstances = new List<RMMaterialBase>();
        [SerializeField] private string materialDataContainerNamePerInstanceVariable;
        [SerializeField] private string materialTexturesPerInstanceVariable;

        [SerializeField] private Texture2DArray generatedMaterialTextureArray;
        [SerializeField] private List<Texture2D> materialTextures = new List<Texture2D>();

#if UNITY_EDITOR
        [SerializeField] private RMMaterialIdentifier.MaterialGlobalKeywords[] materialGlobalKeywords;
        [SerializeReference] private RMMaterialIdentifier materialIdentifier;
#endif

        [SerializeField] private bool sceneObjectsAreUsingSomeInstances;
        [SerializeField] private bool bufferIsInUse = true;

        // Properties

        public string MaterialDataContainerNamePerInstanceVariable => materialDataContainerNamePerInstanceVariable;
        public string MaterialTexturesPerInstanceInstanceVariable => materialTexturesPerInstanceVariable;
        public bool SceneObjectsAreUsingSomeInstances => sceneObjectsAreUsingSomeInstances;
        public bool BufferIsInUse => bufferIsInUse;
        public bool HasMaterialInstances => MaterialInstances.Count > 0;
        public IReadOnlyList<RMMaterialBase> MaterialInstances => materialInstances;
#if UNITY_EDITOR
        public RMMaterialIdentifier MaterialIdentifier => materialIdentifier;
        public RMMaterialIdentifier.MaterialGlobalKeywords[] MaterialGlobalKeywordsArray => materialGlobalKeywords;
#endif

        #region Internally managed

        public void CreateComputeBuffer() => IRMComputeBufferExtensions.CreateComputeBuffer(this);

        public void ReleaseComputeBuffer() => IRMComputeBufferExtensions.ReleaseComputeBuffer(this);

        public void PushComputeBuffer(in Material raymarcherMaterial)
        {
            IRMComputeBufferExtensions.CheckComputeBuffer(this);

            SyncMaterials(raymarcherMaterial);

            IRMComputeBufferExtensions.SetComputeBuffer(this, MaterialDataContainerNamePerInstanceVariable, raymarcherMaterial);
        }

        public void PushUnpackedDataOnly(Material raymarcherMaterial)
        {
            SyncMaterials(raymarcherMaterial);
            PushUnpackedDataToShader(raymarcherMaterial, MaterialInstances.Count);
        }

        private void SyncMaterials(Material raymarcherMaterial)
        {
            for (int i = 0; i < materialInstances.Count; i++)
            {
                if (materialInstances[i] == null)
                    continue;
                SyncDataContainerPerInstanceWithMaterialInstance(i, materialInstances[i]);
                if (generatedMaterialTextureArray != null)
                    raymarcherMaterial.SetTexture(MaterialTexturesPerInstanceInstanceVariable, generatedMaterialTextureArray);
            }

            if (BufferIsInUse)
                PushGlobalDataContainerToShader(raymarcherMaterial);
        }

#if UNITY_EDITOR

        public void SetBufferUsage(bool isInUse)
            => bufferIsInUse = isInUse;

        public void AddMaterialInstance(RMMaterialBase materialInstance)
            => materialInstances.Add(materialInstance);

        public void DisposeDependency()
        {
            if (MaterialInstances.Count > 0)
                for (int i = 0; i < materialInstances.Count; i++)
                {
                    if(materialInstances[i] != null)
                        materialInstances[i].DisposeMaterialInstance();
                }
            ReleaseComputeBuffer();
            materialInstances.Clear();
            materialTextures.Clear();
            generatedMaterialTextureArray = null;
            materialGlobalKeywords = null;
        }

#endif

        #endregion

        public int GetInstanceTextureIndexFromCachedArray(Texture2D existingTextureEntry)
            => RMTextureUtils.GetInstanceTextureIndexFromCachedTextureList(existingTextureEntry, materialTextures);

        /// <summary>
        /// Specify the correct data container length and compute buffer stride size. Any mistakes will result in error messages.
        /// https://developer.nvidia.com/content/understanding-structured-buffer-performance
        /// </summary>
        public abstract (int dataLength, int strideSize) GetComputeBufferLengthAndStride();

        /// <summary>
        /// Pass your custom data container array to the compute buffer for blitting
        /// </summary>
        public abstract Array GetComputeBufferDataContainer { get; }

        /// <summary>
        /// Initialize your custom data container based on the list of current material instances. This method is invoked by the compiler when the material instances have been changed
        /// </summary>
        public virtual void InitializeDataContainerPerInstance(in IReadOnlyList<RMMaterialBase> materialInstances, in bool sceneObjectsAreUsingSomeInstances, in bool unpackedDataContainerDirective)
        {
#if UNITY_EDITOR

            this.sceneObjectsAreUsingSomeInstances = sceneObjectsAreUsingSomeInstances;

            this.materialInstances.Clear();
            this.materialInstances.AddRange(materialInstances);

            if(MaterialIdentifier.MaterialIsUsingTexturesPerInstance)
            {
                materialTextures.Clear();
                for (int i = 0; i < materialInstances.Count; i++)
                {
                    if (materialInstances[i] == null)
                        continue;
                    Texture2D[] textureInstances = materialInstances[i].MaterialTexturesPerInstance;
                    if (textureInstances == null || textureInstances.Length == 0)
                        continue;
                    for (int x = 0; x < textureInstances.Length; x++)
                    {
                        if (!materialTextures.Contains(textureInstances[x]))
                            materialTextures.Add(textureInstances[x]);
                    }
                }
                generatedMaterialTextureArray = RMTextureUtils.GenerateTextureArray(materialTextures);
            }
#endif
        }

        /// <summary>
        /// Synchronize your custom data container with the given material instance at the specified iteration index. Use the iteration index to set an element in your data container array
        /// </summary>
        public abstract void SyncDataContainerPerInstanceWithMaterialInstance(in int iterationIndex, in RMMaterialBase materialInstance);

        public virtual void PushUnpackedDataToShader(in Material raymarcherSessionMaterial, in int actualCountOfMaterialInstances) { }

        /// <summary>
        /// Pass your custom global data to the render master scoped to the scene
        /// </summary>
        public virtual void PushGlobalDataContainerToShader(in Material raymarcherSceneMaterial) { }
    }
}