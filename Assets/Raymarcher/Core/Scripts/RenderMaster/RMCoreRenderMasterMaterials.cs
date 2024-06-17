using System;
using System.Collections.Generic;

using UnityEngine;

using Raymarcher.Objects;
using Raymarcher.Materials;
using Raymarcher.Convertor;

namespace Raymarcher.RendererData
{
    [Serializable]
    public sealed class RMCoreRenderMasterMaterials : IRMRenderMasterDependency
    {
        // Fields

        [SerializeReference] private List<RMMaterialDataBuffer> materialDataBuffers = new List<RMMaterialDataBuffer>();

        [SerializeField] private List<RMMaterialBase> globalMaterialInstances = new List<RMMaterialBase>();
        [SerializeField] private List<RMMaterialBase> registeredGlobalMaterialInstances;

        [SerializeField] private List<string> filteredCommonMaterialTypes = new List<string>();
        [SerializeField] private List<string> filteredMaterialFamilies = new List<string>();

        [SerializeField] private RMRenderMaster renderMaster;

        // Properties

        public RMRenderMaster RenderMaster => renderMaster;

        /// <summary>
        /// List of the currently compiled material types
        /// </summary>
        public IReadOnlyList<RMMaterialDataBuffer> MaterialDataBuffers => materialDataBuffers;
        /// <summary>
        /// List of the global material instances
        /// </summary>
        public IReadOnlyList<RMMaterialBase> GlobalMaterialInstances => globalMaterialInstances;
        /// <summary>
        /// List of the currently registered global material instances
        /// </summary>
        public IReadOnlyList<RMMaterialBase> RegisteredGlobalMaterialInstances => registeredGlobalMaterialInstances;

        public bool HasMaterialDataBuffers => materialDataBuffers.Count > 0;
        public bool HasGlobalMaterialInstances => globalMaterialInstances.Count > 0;
        public bool HasRegisteredGlobalMaterialInstances => registeredGlobalMaterialInstances != null && registeredGlobalMaterialInstances.Count > 0;
        public List<string> FilteredCommonMaterialTypes => filteredCommonMaterialTypes;

#if UNITY_EDITOR

        public void RegisterAndDispatchGlobalMaterials()
        {
            if (registeredGlobalMaterialInstances != null && registeredGlobalMaterialInstances.Count > 0)
                foreach (var instance in registeredGlobalMaterialInstances)
                {
                    if (instance != null)
                        instance.DisposeMaterialInstance();
                }

            if (registeredGlobalMaterialInstances != null)
                registeredGlobalMaterialInstances.Clear();

            if (GlobalMaterialInstances == null || GlobalMaterialInstances.Count == 0)
            {
                ObjectMaterialChanged(null);
                return;
            }

            if(registeredGlobalMaterialInstances == null)
                registeredGlobalMaterialInstances = new List<RMMaterialBase>();

            foreach (var globalMatInstance in GlobalMaterialInstances)
            {
                if (globalMatInstance == null)
                    continue;
                if(!globalMatInstance.IsCompatibleWithSelectedPlatform(RenderMaster.CompiledTargetPlatform))
                {
                    UnityEditor.EditorUtility.DisplayDialog("Warning", $"Couldn't register material '{globalMatInstance.name}' as Global, because it is not compatible with the selected target platform '{RenderMaster.CompiledTargetPlatform}'." +
                            $" Please use a different material type compatible with the selected target platform.", "OK");
                    continue;
                }
                globalMatInstance.SetThisMaterialInstanceAsGlobal(true);
                ObjectMaterialChanged(globalMatInstance);
                registeredGlobalMaterialInstances.Add(globalMatInstance);
            }

            RenderMaster.RecompileTarget(false);
        }

        public void ObjectMaterialChanged(RMMaterialBase newMaterial)
        {
            if(newMaterial == null)
            {
                for (int i = MaterialDataBuffers.Count - 1; i >= 0; i--)
                    RefreshMaterialTypeInScene(MaterialDataBuffers[i]);
                return;
            }

            RMMaterialDataBuffer foundBuffer = null;
            Type buff = newMaterial.MaterialCreateDataBufferInstance().GetType();
            for (int i = MaterialDataBuffers.Count - 1; i >= 0; i--)
            {
                var buffer = MaterialDataBuffers[i];
                if (!buffer.HasMaterialInstances)
                {
                    DisposeAndRemoveBuffer(buffer, false);
                    continue;
                }
                if (buffer.GetType() == buff)
                {
                    foundBuffer = buffer;
                    break;
                }
            }

            if (foundBuffer == null)
            {
                foundBuffer = newMaterial.MaterialCreateDataBufferInstance();
                foundBuffer.AddMaterialInstance(newMaterial);
                materialDataBuffers.Add(foundBuffer);

                renderMaster.SetRecompilationRequired(false);
                renderMaster.SetRecompilationRequired(true);
            }

            RefreshMaterialTypeInScene(foundBuffer);
        }

        public void RecompileMaterialBuffer()
        {
            Material targetMat = RenderMaster.RenderingData.RendererSessionMaterialSource;
            if (targetMat == null)
                return;

            for (int i = 0; i < materialDataBuffers.Count; i++)
                RefreshMaterialTypeInScene(materialDataBuffers[i]);

            FilterCommonMaterialTypes();

            RMConvertorMaterialBuffer.ConvertAndWriteToMaterialBuffer(renderMaster);

            foreach (var matType in MaterialDataBuffers)
            {
                if (matType == null)
                    continue;

                var identifier = matType.MaterialIdentifier;
                if (!FilteredCommonMaterialTypes.Contains(identifier.MaterialTypeName))
                    continue;

                if(matType.MaterialGlobalKeywordsArray != null && matType.MaterialGlobalKeywordsArray.Length > 0)
                    for (int x = 0; x < matType.MaterialGlobalKeywordsArray.Length; x++)
                    {
                        RMMaterialIdentifier.MaterialGlobalKeywords kwd = matType.MaterialGlobalKeywordsArray[x];
                        if (!kwd.enabled && targetMat.IsKeywordEnabled(kwd.keyword))
                            targetMat.DisableKeyword(kwd.keyword);
                        else if (kwd.enabled && !targetMat.IsKeywordEnabled(kwd.keyword))
                            targetMat.EnableKeyword(kwd.keyword);
                    }
            }
        }

        private void RefreshMaterialTypeInScene(RMMaterialDataBuffer buffer)
        {
            List<RMMaterialBase> materialInstances = new List<RMMaterialBase>();

            int materialTypeCount = 0;
            int objectsUsingThisMaterialType = 0;

            if (buffer.HasMaterialInstances)
            {
                string materialType = buffer.MaterialInstances[0].GetType().Name;

                foreach (RMSdfObjectBase obj in renderMaster.MappingMaster.SceneSdfObjectContainer)
                {
                    if(obj.HasModifiers)
                        foreach(var modifier in obj.Modifiers)
                        {
                            if(modifier is ISDFModifierMaterialHandler matHandler)
                            {
                                bool gotMaterials = false;
                                for (int i = 0; i < ISDFModifierMaterialHandlerExtensions.AVAILABLE_MATERIAL_CACHES; i++)
                                {
                                    RMMaterialBase materialinst = ISDFModifierMaterialHandlerExtensions.HandleMatIndex(matHandler, i);
                                    if (materialinst == null)
                                        continue;
                                    if (materialType != materialinst.GetType().Name)
                                        continue;
                                    if (!materialInstances.Contains(materialinst))
                                        materialInstances.Add(materialinst);
                                    objectsUsingThisMaterialType++;
                                    gotMaterials = true;
                                }
                                if(gotMaterials)
                                    materialTypeCount++;
                            }
                        }

                    if (!obj.ObjectMaterial)
                        continue;

                    if (materialType == obj.ObjectMaterial.GetType().Name)
                    {
                        if (obj.ObjectMaterial.IsGlobalMaterialInstance)
                            continue;

                        if (!materialInstances.Contains(obj.ObjectMaterial))
                            materialInstances.Add(obj.ObjectMaterial);

                        materialTypeCount++;
                        objectsUsingThisMaterialType++;
                    }
                }

                if (GlobalMaterialInstances != null && GlobalMaterialInstances.Count > 0)
                {
                    foreach (var instance in GlobalMaterialInstances)
                    {
                        if (instance.GetType().Name != materialType)
                            continue;

                        materialInstances.Add(instance);
                        materialTypeCount++;
                    }
                }
            }

            if (materialTypeCount == 0)
                DisposeAndRemoveBuffer(buffer, true);
            else
            {
                buffer.InitializeDataContainerPerInstance(materialInstances, objectsUsingThisMaterialType > 0, RenderMaster.UnpackDataContainer);
                if(!RenderMaster.UnpackDataContainer)
                    buffer.CreateComputeBuffer();
            }
        }

        private void DisposeAndRemoveBuffer(RMMaterialDataBuffer buffer, bool recompileRequired)
        {
            buffer.DisposeDependency();
            materialDataBuffers.Remove(buffer);
            if (recompileRequired)
            {
                renderMaster.SetRecompilationRequired(false);
                renderMaster.SetRecompilationRequired(true);
            }
        }

        private void FilterCommonMaterialTypes()
        {
            if (filteredCommonMaterialTypes == null)
                filteredCommonMaterialTypes = new List<string>();
            else
                filteredCommonMaterialTypes.Clear();

            if (filteredMaterialFamilies == null)
                filteredMaterialFamilies = new List<string>();
            else
                filteredMaterialFamilies.Clear();

            foreach (var matBufferA in materialDataBuffers)
            {
                if (filteredCommonMaterialTypes.Contains(matBufferA.MaterialIdentifier.MaterialTypeName))
                    continue;

                matBufferA.SetBufferUsage(false);

                if (string.IsNullOrEmpty(matBufferA.MaterialIdentifier.MaterialFamilyName))
                {
                    matBufferA.SetBufferUsage(true);
                    filteredCommonMaterialTypes.Add(matBufferA.MaterialIdentifier.MaterialTypeName);
                    continue;
                }

                if (filteredMaterialFamilies.Contains(matBufferA.MaterialIdentifier.MaterialFamilyName))
                {
                    matBufferA.SetBufferUsage(false);
                    continue;
                }

                RMMaterialDataBuffer targetNestedBuffer = matBufferA;
                RMMaterialIdentifier targetNestedIdentifier = targetNestedBuffer.MaterialIdentifier;

                int maxInheritation = InheritationCounter(targetNestedIdentifier.GetType());
                int kwdA = matBufferA.MaterialGlobalKeywordsArray != null ? matBufferA.MaterialGlobalKeywordsArray.Length : 0;
                bool dontAdd = false;
                foreach(var matBufferB in materialDataBuffers)
                {
                    if (matBufferA.MaterialIdentifier.MaterialFamilyName != matBufferB.MaterialIdentifier.MaterialFamilyName)
                        continue;

                    int currentInheritation = InheritationCounter(matBufferB.MaterialIdentifier.GetType());
                    int kwdB = matBufferB.MaterialGlobalKeywordsArray != null ? matBufferB.MaterialGlobalKeywordsArray.Length : 0;
                    if (currentInheritation == maxInheritation && kwdA < kwdB)
                    {
                        dontAdd = true;
                        break;
                    }

                    if (maxInheritation < currentInheritation)
                    {
                        maxInheritation = currentInheritation;
                        targetNestedIdentifier = matBufferB.MaterialIdentifier;
                        targetNestedBuffer = matBufferB;
                    }

                    matBufferA.SetBufferUsage(false);
                }

                if (dontAdd)
                    continue;

                targetNestedBuffer.SetBufferUsage(true);
                filteredCommonMaterialTypes.Add(targetNestedIdentifier.MaterialTypeName);
                filteredMaterialFamilies.Add(targetNestedIdentifier.MaterialFamilyName);
            }

            static int InheritationCounter(in Type currentType)
            {
                const int TIMEOUT = 99;
                int i = 0;
                Type iterType = currentType.BaseType;
                while(iterType != typeof(RMMaterialIdentifier) && i < TIMEOUT && iterType != null)
                {
                    iterType = iterType.BaseType;
                    i++;
                }
                return i;
            }
        }


        public void SetupDependency(in RMRenderMaster renderMaster)
        {
            this.renderMaster = renderMaster;
            RMConvertorMaterialBuffer.ConvertAndWriteToMaterialBuffer(renderMaster);
        }
        
        public void SetMaterialDataBuffers(IReadOnlyList<RMMaterialDataBuffer> dataBuffers)
        {
            if (materialDataBuffers.Count == dataBuffers.Count)
                for (int i = 0; i < dataBuffers.Count; i++)
                {
                    materialDataBuffers[i].ReleaseComputeBuffer();
                    materialDataBuffers[i] = dataBuffers[i];
                }
        }

        public void DisposeDependency()
        {
            filteredCommonMaterialTypes = null;

            for (int i = 0; i < MaterialDataBuffers.Count; i++)
                MaterialDataBuffers[i].DisposeDependency();
            materialDataBuffers = null;

            if (registeredGlobalMaterialInstances != null)
                for (int i = 0; i < registeredGlobalMaterialInstances.Count; i++)
                {
                    if (registeredGlobalMaterialInstances[i])
                        registeredGlobalMaterialInstances[i].DisposeMaterialInstance();
                }

            globalMaterialInstances = null;
            registeredGlobalMaterialInstances = null;
        }

#endif

        public void UpdateDependency(in Material raymarcherSessionMaterial)
        {
            if (MaterialDataBuffers.Count != 0)
                for (int i = 0; i < MaterialDataBuffers.Count; i++)
                {
                    if(MaterialDataBuffers[i] == null)
                    {
                        RMDebug.Debug(RenderMaster, $"Material data buffer at index '{i}' was null. Raymarcher will automatically remove an empty buffer.");
                        materialDataBuffers.RemoveAt(i);
                        return;
                    }
                    if(!RenderMaster.UnpackDataContainer)
                        MaterialDataBuffers[i].PushComputeBuffer(raymarcherSessionMaterial);
                    else
                        MaterialDataBuffers[i].PushUnpackedDataOnly(raymarcherSessionMaterial);
                }
        }

        public void ReleaseComputeBuffer()
        {
            if(materialDataBuffers != null)
                for (int i = 0; i < MaterialDataBuffers.Count; i++)
                    MaterialDataBuffers[i].ReleaseComputeBuffer();
        }

        public (int typeIndex, int instanceIndex) GetMaterialTypeAndInstanceIndex(RMMaterialBase inputMaterial)
        {
            if (!HasMaterialDataBuffers || !inputMaterial)
                return (-1, -1);

            for (int i = 0; i < MaterialDataBuffers.Count; i++)
            {
                if (MaterialDataBuffers[i] == null)
                    continue;
                if (!MaterialDataBuffers[i].HasMaterialInstances)
                    continue;

                if (MaterialDataBuffers[i].MaterialInstances[0].GetType() == inputMaterial.GetType())
                {
                    for (int x = 0; x < MaterialDataBuffers[i].MaterialInstances.Count; x++)
                    {
                        if (MaterialDataBuffers[i].MaterialInstances[x] == inputMaterial)
                            return (i, x);
                    }
                }
            }
            return (-1, -1);
        }

        public int GetCountOfMaterialsInBuffer(RMMaterialBase inputMaterial)
        {
            if (!HasMaterialDataBuffers || !inputMaterial)
                return 0;

            for (int i = 0; i < MaterialDataBuffers.Count; i++)
            {
                if (MaterialDataBuffers[i] == null)
                    continue;
                if (!MaterialDataBuffers[i].HasMaterialInstances)
                    continue;

                if (MaterialDataBuffers[i].MaterialInstances[0].GetType() == inputMaterial.GetType())
                    return MaterialDataBuffers[i].MaterialInstances.Count;
            }
            return 0;
        }
    }
}