using System;
using System.Collections.Generic;

using UnityEngine;

using Raymarcher.Objects;
using Raymarcher.Convertor;
using Raymarcher.Utilities;
using Raymarcher.Constants;

namespace Raymarcher.RendererData
{
    using static RMConstants;

    [Serializable]
    public sealed class RMCoreRenderMasterMapping : IRMRenderMasterDependency, IRMComputeBuffer
    {
        // Serialized fields

        [SerializeField] private RMRenderMaster renderMaster;
        [SerializeField] private List<RMSdfObjectBase> sceneSdfObjectBufferContainer = new List<RMSdfObjectBase>();

        [SerializeField] private List<Texture2D> cachedQualityTextures;
        [SerializeField] private Texture2DArray generatedQualityTexturesArray;

        [SerializeField] private int compiledSdfObjectCount = 0;
        [SerializeField] private SDFObjectQuality[] sdfObjectQualityContainer;
        [SerializeField] private SDFObjectStandard[] sdfObjectStandardContainer;
        [SerializeField] private SDFObjectPerformant[] sdfObjectPerformantContainer;

        [SerializeField] private Matrix4x4[] sdfObjectStandardUnpackedContainer;
        [SerializeField] private Vector4[] sdfObjectPerformantUnpackedContainer;

        [Serializable]
        private struct SDFObjectQuality
        {
            public Matrix4x4 modelData;
            // [0][1][2] = transform (pos,rot,scale)
            // [3] = color rgb
            public Vector4 textureData;
            // x = texture index from tex2D list
            // y = texture uniform tiling
            // z = texture opacity
            public Vector4 textureScale;
            // xyz = tex scale xyz
        }

        [Serializable]
        private struct SDFObjectStandard
        {
            public Matrix4x4 modelData;
            // [0][1][2] = transform (pos,rot,scale)
            // [3].x = hue
        }

        [Serializable]
        private struct SDFObjectPerformant
        {
            public Vector4 modelData;
            // xyz = position
            // w = hue
        }

        // Properties

        /// <summary>
        /// Reference to the base render master
        /// </summary>
        public RMRenderMaster RenderMaster => renderMaster;
        /// <summary>
        /// List of the existing Raymarcher objects
        /// </summary>
        public IReadOnlyList<RMSdfObjectBase> SceneSdfObjectContainer => sceneSdfObjectBufferContainer;

        public ComputeBuffer RuntimeComputeBuffer { get; set; }

#if UNITY_EDITOR

        public void AddSdfObjectToContainer(RMSdfObjectBase sdfObject)
        {
            if (!sceneSdfObjectBufferContainer.Contains(sdfObject))
            {
                sceneSdfObjectBufferContainer.Add(sdfObject);
                RenderMaster.SetRecompilationRequired(true);
            }
        }

        public void RemoveSdfObjectFromContainer(RMSdfObjectBase sdfObject)
        {
            if (sceneSdfObjectBufferContainer.Contains(sdfObject))
            {
                sceneSdfObjectBufferContainer.Remove(sdfObject);
                RenderMaster.SetRecompilationRequired(true);
            }
        }

        public void DestroyCurrentlyMappedSdfObjects()
        {
            if (sceneSdfObjectBufferContainer.Count == 0)
                return;

            for (int i = 0; i < sceneSdfObjectBufferContainer.Count; i++)
            {
                if (sceneSdfObjectBufferContainer[i])
                    UnityEngine.Object.DestroyImmediate(sceneSdfObjectBufferContainer[i].gameObject);
            }
            sceneSdfObjectBufferContainer.Clear();
        }

        public void DestroyAllMappedSdfObjectsInTheScene()
        {
            DestroyCurrentlyMappedSdfObjects();

            RMSdfObjectBase[] allObjects = UnityEngine.Object.FindObjectsOfType(typeof(RMSdfObjectBase), true) as RMSdfObjectBase[];
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(allObjects[i].gameObject);
            }

            RenderMaster.SetRecompilationRequired(true);
        }

        public void RecompileSdfObjectBuffer()
        {
            RMConvertorSdfObjectBuffer.ConvertAndWriteToSdfObjectBuffer(this);

            compiledSdfObjectCount = 0;
            bool qualityRender = RenderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality;

            if (qualityRender)
            {
                if (cachedQualityTextures == null)
                    cachedQualityTextures = new List<Texture2D>();
                else
                    cachedQualityTextures.Clear();
            }

            if (SceneSdfObjectContainer == null)
                return;

            for (int i = 0; i < SceneSdfObjectContainer.Count; i++)
            {
                var obj = SceneSdfObjectContainer[i];
                if (!obj)
                {
                    RMDebug.Debug(GetType(), $"Sdf Object at index '{i}' in the mapping master is null for unexpected reason. Please check the mapping list properly", true);
                    continue;
                }
                obj.SdfBufferRecompiled();

                if(qualityRender)
                {
                    if(obj.QualityRenderData.objectTexture && !cachedQualityTextures.Contains(obj.QualityRenderData.objectTexture))
                        cachedQualityTextures.Add(obj.QualityRenderData.objectTexture);
                }
            }

            compiledSdfObjectCount = SceneSdfObjectContainer.Count;
            if (qualityRender)
                generatedQualityTexturesArray = RMTextureUtils.GenerateTextureArray(cachedQualityTextures);

            CreateComputeBuffer();
        }

        public string GetSdfUniqueIdentifier(RMSdfObjectBase sdfObject)
        {
            if (SceneSdfObjectContainer.Count == 0)
                return "1";

            int i = 0;
            foreach (RMSdfObjectBase sdfObj in SceneSdfObjectContainer)
            {
                if (sdfObj == sdfObject)
                    return RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_ObjInstance + i.ToString();
                i++;
            }
            return "1";
        }

#endif

        public void SetDirtyModifiersWithSharedContainers()
        {
            for (int i = 0; i < sceneSdfObjectBufferContainer.Count; i++)
            {
                if (!sceneSdfObjectBufferContainer[i].HasModifiers)
                    continue;
                for (int x = 0; x < sceneSdfObjectBufferContainer[i].Modifiers.Count; x++)
                    sceneSdfObjectBufferContainer[i].Modifiers[x].PassSharedContainerDataToModifier();
            }
        }

        public void CreateComputeBuffer()
        {
            switch (RenderMaster.RenderingData.CompiledRenderType)
            {
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                    sdfObjectQualityContainer = new SDFObjectQuality[compiledSdfObjectCount];
                    break;
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                    if (RenderMaster.UnpackDataContainer)
                        sdfObjectStandardUnpackedContainer = new Matrix4x4[compiledSdfObjectCount];
                    else
                        sdfObjectStandardContainer = new SDFObjectStandard[compiledSdfObjectCount];
                    break;
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                    if (RenderMaster.UnpackDataContainer)
                        sdfObjectPerformantUnpackedContainer = new Vector4[compiledSdfObjectCount];
                    else
                        sdfObjectPerformantContainer = new SDFObjectPerformant[compiledSdfObjectCount];
                    break;
            }
            if(!RenderMaster.UnpackDataContainer)
                IRMComputeBufferExtensions.CreateComputeBuffer(this);
        }

        public void ReleaseComputeBuffer()
        {
            IRMComputeBufferExtensions.ReleaseComputeBuffer(this);
        }

        public void PushComputeBuffer(in Material raymarcherMaterial)
        {
            if(compiledSdfObjectCount != SceneSdfObjectContainer.Count)
            {
#if UNITY_EDITOR
                RenderMaster.SetRecompilationRequired(true);
#endif
                return;
            }
            
            if (SceneSdfObjectContainer.Count == 0)
                return;

            if(!RenderMaster.UnpackDataContainer)
                IRMComputeBufferExtensions.CheckComputeBuffer(this);

            if (RenderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality && generatedQualityTexturesArray != null)
                raymarcherMaterial.SetTexture(RMConvertorSdfObjectBuffer.RMSdfObjBuffer_VariableConstant_SamplerPack, generatedQualityTexturesArray);

            for (int i = 0; i < SceneSdfObjectContainer.Count; i++)
            {
                RMSdfObjectBase obj = SceneSdfObjectContainer[i];

                if (obj == null)
                {
#if UNITY_EDITOR
                    RenderMaster.SetRecompilationRequired(true);
#endif
                    continue;
                }

                string istr = i.ToString();
                Transform objTrans = obj.transform;

                switch(RenderMaster.RenderingData.CompiledRenderType)
                {
                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                        Matrix4x4 modelMatrix = Matrix4x4.TRS(objTrans.position, objTrans.rotation, !obj.gameObject.activeInHierarchy ? Vector3.one * 0.01f : objTrans.localScale).inverse;
                        modelMatrix.m30 = obj.QualityRenderData.objectColor.r;
                        modelMatrix.m31 = obj.QualityRenderData.objectColor.g;
                        modelMatrix.m32 = obj.QualityRenderData.objectColor.b;

                        SDFObjectQuality sdfQuality = new SDFObjectQuality();
                        sdfQuality.modelData = modelMatrix;
                        sdfQuality.textureData = new Vector4(
                            RMTextureUtils.GetInstanceTextureIndexFromCachedTextureList(obj.QualityRenderData.objectTexture, cachedQualityTextures),
                            obj.QualityRenderData.textureTiling,
                            obj.QualityRenderData.textureOpacity);
                        sdfQuality.textureScale = new Vector3(obj.QualityRenderData.textureScaleX, obj.QualityRenderData.textureScaleY, obj.QualityRenderData.textureScaleZ);
                        sdfObjectQualityContainer[i] = sdfQuality;
                        break;

                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                        modelMatrix = Matrix4x4.TRS(objTrans.position, objTrans.rotation, !obj.gameObject.activeInHierarchy ? Vector3.one * 0.01f : objTrans.localScale).inverse;

                        if (RenderMaster.UnpackDataContainer)
                        {
                            modelMatrix.m30 = obj.HueShift;
                            sdfObjectStandardUnpackedContainer[i] = modelMatrix;
                        }
                        else
                        {
                            SDFObjectStandard sdfStandard = new SDFObjectStandard();
                            modelMatrix.m30 = obj.HueShift;
                            sdfStandard.modelData = modelMatrix;
                            sdfObjectStandardContainer[i] = sdfStandard;
                        }
                        break;

                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                        Vector4 performantData = new Vector4();
                        performantData.x = objTrans.position.x;
                        performantData.y = objTrans.position.y;
                        performantData.z = objTrans.position.z;
                        performantData.w = obj.HueShift;

                        if (RenderMaster.UnpackDataContainer)
                            sdfObjectPerformantUnpackedContainer[i] = performantData;
                        else
                        {
                            SDFObjectPerformant sdfPerformant = new SDFObjectPerformant();
                            sdfPerformant.modelData = performantData;
                            sdfObjectPerformantContainer[i] = sdfPerformant;
                        }
                        break;
                }

                obj.PushSdfEntityToShader(raymarcherMaterial, istr);
                if (!RenderMaster.RecompilationRequiredSdfObjectBuffer)
                    obj.PushModifiersToShader(raymarcherMaterial, istr);
            }

            if(RenderMaster.UnpackDataContainer)
            {
                if(RenderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard)
                {
                    raymarcherMaterial.SetMatrixArray(CommonBuildTimeConstants.RM_COMMON_SDFOBJBUFFER_ModelData, sdfObjectStandardUnpackedContainer);
                }
                else
                {
                    raymarcherMaterial.SetVectorArray(CommonBuildTimeConstants.RM_COMMON_SDFOBJBUFFER_ModelData, sdfObjectPerformantUnpackedContainer);
                }
            }
            else
                IRMComputeBufferExtensions.SetComputeBuffer(this, CommonBuildTimeConstants.RM_COMMON_SDFOBJBUFFER_SdfInstances, raymarcherMaterial);
        }

        public (int dataLength, int strideSize) GetComputeBufferLengthAndStride()
        {
            return RenderMaster.RenderingData.CompiledRenderType switch
            {
                RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality => (sdfObjectQualityContainer.Length, sizeof(float) * 24),
                RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard => (sdfObjectStandardContainer.Length, sizeof(float) * 16),
                RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant => (sdfObjectPerformantContainer.Length, sizeof(float) * 4),
                _ => (sdfObjectStandardContainer.Length, sizeof(float) * 16),
            };
        }

        public Array GetComputeBufferDataContainer
        {
            get
            {
                return RenderMaster.RenderingData.CompiledRenderType switch
                {
                    RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality => sdfObjectQualityContainer,
                    RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard => sdfObjectStandardContainer,
                    RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant => sdfObjectPerformantContainer,
                    _ => sdfObjectStandardContainer,
                };
            }
        }

#if UNITY_EDITOR

        public void DisposeDependency()
        {
            DestroyAllMappedSdfObjectsInTheScene();

            cachedQualityTextures = null;
            generatedQualityTexturesArray = null;

            sdfObjectQualityContainer = null;
            sdfObjectStandardContainer = null;
            sdfObjectPerformantContainer = null;

            sdfObjectPerformantUnpackedContainer = null;
            sdfObjectStandardUnpackedContainer = null;

            renderMaster = null;

            ReleaseComputeBuffer();
        }

        public void SetupDependency(in RMRenderMaster renderMaster)
        {
            this.renderMaster = renderMaster;

            sceneSdfObjectBufferContainer = new List<RMSdfObjectBase>();

            RMConvertorSdfObjectBuffer.ConvertAndWriteToSdfObjectBuffer(this);
        }

#endif

        public void UpdateDependency(in Material raymarcherSessionMaterial)
        {
            PushComputeBuffer(raymarcherSessionMaterial);
        }
    }
}