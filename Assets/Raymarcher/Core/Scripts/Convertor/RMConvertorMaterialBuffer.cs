using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Raymarcher.RendererData;
using Raymarcher.Materials;

namespace Raymarcher.Convertor
{
    using static RMConvertorCommon;

    public static class RMConvertorMaterialBuffer
    {
#if UNITY_EDITOR

        // Symbol & method constants unique for material buffer convertor
        private const string MaterialBuffer_PerObjCoreMethodParams = "(in Ray ray, half4 sdfRGBA, in int sdfMaterialType, in int sdfMaterialInstance)";
        private const string MaterialBuffer_PerObjMethodName = "RM_PerObjRenderMaterials";
        private const string MaterialBuffer_PerObjCoreMethodHead = "half4 " + MaterialBuffer_PerObjMethodName + MaterialBuffer_PerObjCoreMethodParams + MAnewLine + "{" + MAnewLine;
        private const string MaterialBuffer_PerObjPostCoreMethodHead = "half4 RM_PerObjPostRenderMaterials" + MaterialBuffer_PerObjCoreMethodParams + MAnewLine + "{" + MAnewLine;

        private const string MaterialBuffer_GlobalCoreMethodParams = "(in Ray ray, half4 sdfRGBA, in int temp0, in int temp1)";
        private const string MaterialBuffer_GlobalMethodName = "RM_GlobalRenderMaterials";
        private const string MaterialBuffer_GlobalCoreMethodHead = "half4 " + MaterialBuffer_GlobalMethodName + MaterialBuffer_GlobalCoreMethodParams + MAnewLine + "{" + MAnewLine;
        private const string MaterialBuffer_GlobalPostCoreMethodHead = "half4 RM_GlobalPostRenderMaterials" + MaterialBuffer_GlobalCoreMethodParams + MAnewLine + "{" + MAnewLine;

        private const string MaterialBuffer_CoreMethodContentReturn = MAnewLine + MAspace + "return sdfRGBA;" + MAnewLine + "}" + MAnewLine;

        private const string MaterialBuffer_ShaderFeautre = "#pragma shader_feature_fragment ";

        private const string MaterialBuffer_Commentary_PerObjRenderMaterials = "// ------------------- PER-OBJECT MATERIAL RENDERER ----------------------";
        private const string MaterialBuffer_Commentary_PerObjPostRenderMaterials = "// ------------------- PER-OBJECT POST MATERIAL RENDERER ----------------------";
        private const string MaterialBuffer_Commentary_GlobalRenderMaterials = "// ------------------- GLOBAL MATERIAL RENDERER ----------------------";
        private const string MaterialBuffer_Commentary_GlobalPostRenderMaterials = "// ------------------- GLOBAL POST MATERIAL RENDERER ----------------------";
        private const string MaterialBuffer_Commentary_End = "// -----------------------------------------------------------------------";

        private const string MaterialBuffer_GlobalStage = "_Global";

        private const string MaterialBuffer_SdfRef = "sdfRef";

        // Compile-modifiable macros
        /// <summary>
        /// Add this to your method in your HLSL file to direct the compiler for a global stage.
        /// Eg: float4 CalculateReflections#RAYMARCHER-METHOD-MODIFIABLE#(...)
        /// Will be compiled (if its a global stage) to: float4 CalculateReflections_Global(...). If it's not global stage: float4 CalculateReflections(...)
        /// </summary>
        private const string MaterialBuffer_DynamicMacro_MethodModifiable = "#RAYMARCHER-METHOD-MODIFIABLE#";
        /// <summary>
        /// Add this to your method in your HLSL file to direct the compiler whether to use PerObjMaterialRenderer or GlobalMaterialRenderer.
        /// Eg:
        /// sdfRGBA += float4(#RAYMARCHER-METHOD-RENDER-POINTER#(r, color, sdfMaterialType, sdfMaterialInstance).rgb, 0) * data.reflectionIntensity;
        /// will be compiled to GlobalMaterialRenderer if it's global stage. Otherwise the compiler will replace this macro with per-object mat renderer.
        /// </summary>
        private const string MaterialBuffer_DynamicMacro_RenderMaterialsPointer = "#RAYMARCHER-METHOD-RENDER-POINTER#";

        // Cached writers
        private static readonly StringBuilder WRAPPER_TopMethodDefinitions = new StringBuilder();
        private static readonly StringBuilder WRAPPER_PostMethodDefinitions = new StringBuilder();
        private static readonly StringBuilder WRAPPER_PostGlobalMethodDefinitions = new StringBuilder();
        private static readonly StringBuilder WRAPPER_CoreContent = new StringBuilder();

        // Value: Global Keyword (exact)
        private static readonly HashSet<string> materialRegister_GlobalKeywords
            = new HashSet<string>();
        // Key: Uniform Name, Value: Uniform Type (converted)
        private static readonly Dictionary<string, string> materialRegister_GlobalUniforms
            = new Dictionary<string, string>();
        // Key: Container Type, Value: Container Uniforms Name, Uniform Type (converted)
        private static readonly Dictionary<string, Dictionary<string, string>> materialRegister_StructContainers
            = new Dictionary<string, Dictionary<string, string>>();
        // Key: Container Type, Type: Container Instance Name
        private static readonly Dictionary<string, string> materialRegister_StructContainerInstances
            = new Dictionary<string, string>();
        // Value: Texture Name (exact)
        private static readonly HashSet<string> materialRegister_TextureContainers
            = new HashSet<string>();
        // Value: Other Methods (exact)
        private static readonly HashSet<RMMaterialIdentifier.MaterialMethodContainer> materialRegister_OtherMethods
            = new HashSet<RMMaterialIdentifier.MaterialMethodContainer>();
        // Key: dataContainerNamePerInstanceVariable, Value: Main Method (exact)
        private static readonly Dictionary<string, (bool isUsedInScene, int materialIndex, RMMaterialIdentifier.MaterialMethodContainer theMethod)> materialRegister_MainMethods
            = new Dictionary<string, (bool isUsedInScene, int materialIndex, RMMaterialIdentifier.MaterialMethodContainer theMethod)>();
        // Key: dataContainerNamePerInstanceVariable, Value: Main Method (exact)
        private static readonly Dictionary<string, GlobalMethodWrapper> materialRegister_MainMethodsGlobal
            = new Dictionary<string, GlobalMethodWrapper>();
        // Key: dataContainerNamePerInstanceVariable, Value: Post Method (exact)
        private static readonly Dictionary<string, (bool isUsedInScene, int materialIndex, RMMaterialIdentifier.MaterialMethodContainer theMethod)> materialRegister_PostMethods
            = new Dictionary<string, (bool isUsedInScene, int materialIndex, RMMaterialIdentifier.MaterialMethodContainer theMethod)>();
        // Key: dataContainerNamePerInstanceVariable, Value: Post Method (exact)
        private static readonly Dictionary<string, GlobalMethodWrapper> materialRegister_PostMethodsGlobal
            = new Dictionary<string, GlobalMethodWrapper>();

        private static int[] materialRegister_CountOfMaterialInstancesPerType;
        private static string[] materialRegister_NameOfMaterialContainerInstancesPerType;

        private struct GlobalMethodWrapper
        {
            public int materialIndex;
            public RMMaterialIdentifier.MaterialMethodContainer methodContainer;

            public GlobalMethodWrapper(RMMaterialIdentifier.MaterialMethodContainer methodContainer, int materialIndex)
            {
                this.methodContainer = methodContainer;
                this.materialIndex = materialIndex;
            }
        }

        public static void ConvertAndWriteToMaterialBuffer(RMRenderMaster renderMaster)
        {
            string fullBufferPath = GetAssetRelativetPath(renderMaster, true) + generatedCodeMaterialsPath + renderMaster.RegisteredSessionName + ".cginc";

            if (!File.Exists(fullBufferPath))
            {
                RMDebug.Debug(renderMaster, $"Couldn't convert to shading language and write to material buffer on a path '{fullBufferPath}'", true);
                return;
            }

            CleanConvertorGarbage();

            RMCoreRenderMasterMaterials materials = renderMaster.MasterMaterials;

            bool hasMaterialBuffers = materials.HasMaterialDataBuffers;
            bool hasMaterialBuffersGlobal = materials.HasGlobalMaterialInstances;

            if (hasMaterialBuffers)
            {
                if (renderMaster.UnpackDataContainer)
                {
                    materialRegister_CountOfMaterialInstancesPerType = new int[materials.MaterialDataBuffers.Count];
                    materialRegister_NameOfMaterialContainerInstancesPerType = new string[materials.MaterialDataBuffers.Count];
                }
                int counter = 0;
                foreach (RMMaterialDataBuffer materialDataBuffer in materials.MaterialDataBuffers)
                {
                    if (materialDataBuffer.SceneObjectsAreUsingSomeInstances || (hasMaterialBuffersGlobal && !materialDataBuffer.SceneObjectsAreUsingSomeInstances))
                    {
                        if (!RegisterMaterialBuffer(materialDataBuffer, materials, counter))
                            goto Finisher;
                    }
                    counter++;
                }
            }
            if (hasMaterialBuffersGlobal)
                foreach (RMMaterialDataBuffer materialDataBuffer in materials.MaterialDataBuffers)
                    RegisterGlobalMaterialBuffer(materialDataBuffer, materials);

            WriteRegisteredMaterialBuffers(renderMaster.UnpackDataContainer);

            WRAPPER_CoreContent.Append(WRAPPER_TopMethodDefinitions);

        Finisher:
            WritePerObjMaterialBuffer(materials);
            WriteGlobalMaterialBuffer(materials);

            File.WriteAllText(fullBufferPath, WRAPPER_CoreContent.ToString());
            CleanConvertorGarbage();
            AssetDatabase.Refresh();
        }

        private static bool RegisterMaterialBuffer(RMMaterialDataBuffer materialBuffer, RMCoreRenderMasterMaterials materials, int iterationIndex)
        {
            RMMaterialIdentifier identifier = materialBuffer.MaterialIdentifier;
            bool unpackedDataContainers = materials.RenderMaster.UnpackDataContainer;

            if(identifier == null)
            {
                RMDebug.Debug(typeof(RMConvertorMaterialBuffer), $"Material data buffer of type '{materialBuffer.GetType().Name}' has no identifier", true);
                return false;
            }

            if (identifier.MaterialUniformFieldsPerInstance == null)
            {
                RMDebug.Debug(typeof(RMConvertorMaterialBuffer), $"Material data buffer of type '{identifier.MaterialTypeName}' has no uniform fields", true);
                return false;
            }

            if (unpackedDataContainers)
            {
                materialRegister_CountOfMaterialInstancesPerType[iterationIndex] = materialBuffer.MaterialInstances.Count;
                materialRegister_NameOfMaterialContainerInstancesPerType[iterationIndex] = identifier.MaterialDataContainerTypePerInstance;
            }

            // Global keywords
            if (materialBuffer.MaterialGlobalKeywordsArray != null)
                foreach(var keyword in materialBuffer.MaterialGlobalKeywordsArray)
                {
                    if(!materialRegister_GlobalKeywords.Contains(keyword.keyword))
                        materialRegister_GlobalKeywords.Add(keyword.keyword);
                }
            // Global uniforms
            if(identifier.MaterialUniformFieldsGlobal != null)
                foreach(var uniform in identifier.MaterialUniformFieldsGlobal)
                {
                    if(unpackedDataContainers && uniform.uniformType == RMMaterialIdentifier.MaterialUniformType.NestedContainer)
                    {
                        RMDebug.Debug(typeof(RMConvertorMaterialBuffer), $"Material data buffer of type '{identifier.MaterialTypeName}' contains NestedContainer uniform type, which is prohibited while using the 'unpacked data container' directive. Please choose a different uniform type.", true);
                        return false;
                    }
                    if(!materialRegister_GlobalUniforms.ContainsKey(uniform.uniformName))
                        materialRegister_GlobalUniforms.Add(uniform.uniformName, ConvertUniformType(uniform));
                }

            // Structured container
            WriteStructuredContainerNested(identifier, unpackedDataContainers, out bool isValid);
            if (!isValid)
                return false;

            // Structured container instance
            if (!materialRegister_StructContainerInstances.ContainsKey(identifier.MaterialDataContainerTypePerInstance))
                materialRegister_StructContainerInstances.Add(identifier.MaterialDataContainerTypePerInstance, identifier.MaterialDataContainerTypePerInstance + RMMaterialDataBuffer.MATERIAL_DATACONTAINER_TYPE_INSTANCE);

            if (!materialRegister_TextureContainers.Contains(materialBuffer.MaterialTexturesPerInstanceInstanceVariable))
                materialRegister_TextureContainers.Add(materialBuffer.MaterialTexturesPerInstanceInstanceVariable);

            // Other methods
            if(identifier.MaterialOtherMethods != null)
                foreach (var method in identifier.MaterialOtherMethods)
                {
                    if (!materialRegister_OtherMethods.Contains(method))
                        materialRegister_OtherMethods.Add(method);
                }

            // Main methods
            var mainMethod = identifier.MaterialMainMethod;
            if (!materialRegister_MainMethods.ContainsKey(materialBuffer.MaterialDataContainerNamePerInstanceVariable))
                materialRegister_MainMethods.Add(materialBuffer.MaterialDataContainerNamePerInstanceVariable, 
                    (materialBuffer.SceneObjectsAreUsingSomeInstances, materials.GetMaterialTypeAndInstanceIndex(materialBuffer.MaterialInstances[0]).typeIndex, mainMethod));

            // Post method
            var postMethod = identifier.PostMaterialMainMethod;
            if (postMethod != null)
            {
                var postMethodNew = (RMMaterialIdentifier.MaterialMethodContainer)postMethod;
                if(!materialRegister_PostMethods.ContainsKey(materialBuffer.MaterialDataContainerNamePerInstanceVariable))
                    materialRegister_PostMethods.Add(materialBuffer.MaterialDataContainerNamePerInstanceVariable, 
                        (materialBuffer.SceneObjectsAreUsingSomeInstances, materials.GetMaterialTypeAndInstanceIndex(materialBuffer.MaterialInstances[0]).typeIndex, postMethodNew));
            }
            return true;
        }

        private static void RegisterGlobalMaterialBuffer(RMMaterialDataBuffer materialBuffer, RMCoreRenderMasterMaterials materials)
        {
            RMMaterialIdentifier identifier = materialBuffer.MaterialIdentifier;

            if (identifier == null)
            {
                RMDebug.Debug(typeof(RMConvertorMaterialBuffer), $"Material data buffer of type '{materialBuffer.GetType().Name}' has no identifier", true);
                return;
            }

            foreach(RMMaterialBase material in materialBuffer.MaterialInstances)
            {
                if (!materials.GlobalMaterialInstances.Contains(material))
                    continue;

                var mainMethod = identifier.MaterialMainMethod;
                int materialIndex = materials.GetMaterialTypeAndInstanceIndex(materialBuffer.MaterialInstances[materialBuffer.MaterialInstances.Count - 1]).instanceIndex;

                if (!materialRegister_MainMethodsGlobal.ContainsKey(materialBuffer.MaterialDataContainerNamePerInstanceVariable))
                    materialRegister_MainMethodsGlobal.Add(materialBuffer.MaterialDataContainerNamePerInstanceVariable, new GlobalMethodWrapper(mainMethod, materialIndex));
                
                if (identifier.PostMaterialMainMethodGlobalSupported && !materialRegister_PostMethodsGlobal.ContainsKey(materialBuffer.MaterialDataContainerNamePerInstanceVariable))
                {
                    var postMethod = identifier.PostMaterialMainMethod;
                    if(postMethod != null)
                        materialRegister_PostMethodsGlobal.Add(materialBuffer.MaterialDataContainerNamePerInstanceVariable, new GlobalMethodWrapper(((RMMaterialIdentifier.MaterialMethodContainer)postMethod), materialIndex));
                }
            }
        }

        private static void WriteStructuredContainerNested(RMMaterialIdentifier identifier, bool unpackedDataContainers, out bool valid)
        {
            valid = true;

            if (materialRegister_StructContainers.ContainsKey(identifier.MaterialDataContainerTypePerInstance))
                return;

            Dictionary<string, string> uniformsPerInstance = new Dictionary<string, string>();

            foreach (var uniform in identifier.MaterialUniformFieldsPerInstance)
            {
                if(uniform.uniformType == RMMaterialIdentifier.MaterialUniformType.NestedContainer)
                {
                    if (unpackedDataContainers)
                    {
                        valid = false;
                        RMDebug.Debug(typeof(RMConvertorMaterialBuffer), $"Material data buffer of type '{identifier.MaterialTypeName}' contains NestedContainer uniform type, which is prohibited while using the 'unpacked data container' directive. Please choose a different uniform type.", true);
                        return;
                    }
                    if (uniform.nestedContainerParent == null)
                    {
                        valid = false;
                        RMDebug.Debug(typeof(RMConvertorMaterialBuffer), $"Nested container parent on material identifier '{identifier.MaterialDataContainerTypePerInstance}' is missing!", true);
                        return;
                    }
                    WriteStructuredContainerNested(uniform.nestedContainerParent, unpackedDataContainers, out valid);
                }
                uniformsPerInstance.Add(uniform.uniformName, ConvertUniformType(uniform));
            }

            materialRegister_StructContainers.Add(identifier.MaterialDataContainerTypePerInstance, uniformsPerInstance);
            return;
        }

        private static void WriteRegisteredMaterialBuffers(bool unpackedDataContainers)
        {
            const string COMMENT_LINE = "// --------------------- ";

            WRAPPER_TopMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Global Keywords");
            foreach(var gKeyword in materialRegister_GlobalKeywords)
                WRAPPER_TopMethodDefinitions.AppendLine(MaterialBuffer_ShaderFeautre + gKeyword);

            WRAPPER_TopMethodDefinitions.AppendLine();

            WRAPPER_TopMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Global Uniforms");
            foreach (var gUniform in materialRegister_GlobalUniforms)
                WRAPPER_TopMethodDefinitions.AppendLine(Data_Uniform + gUniform.Value + MAs + gUniform.Key + MAe);

            WRAPPER_TopMethodDefinitions.AppendLine();

            WRAPPER_TopMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Structured/Unpacked Data Containers");
            foreach (var container in materialRegister_StructContainers)
            {
                WRAPPER_TopMethodDefinitions.Append(Data_Struct + container.Key + MAnewLine + MAbc + MAnewLine);

                foreach (var uniform in container.Value)
                    WRAPPER_TopMethodDefinitions.AppendLine(MAspace + uniform.Value + MAs + uniform.Key + MAe);

                WRAPPER_TopMethodDefinitions.AppendLine(MAbce + MAe);
                WRAPPER_TopMethodDefinitions.AppendLine();
            }
            if (!unpackedDataContainers)
            {
                foreach (var container in materialRegister_StructContainerInstances)
                {
                    WRAPPER_TopMethodDefinitions.AppendLine(Data_StructuredBuffer
                     + "<" + container.Key + "> " + container.Value + MAe);
                }
            }
            else
            {
                int counter = 0;
                foreach (var container in materialRegister_StructContainers)
                {
                    foreach (var uniform in container.Value)
                        WRAPPER_TopMethodDefinitions.AppendLine(MAspace + uniform.Value + MAs + uniform.Key 
                            + MAbs + materialRegister_CountOfMaterialInstancesPerType[counter].ToString() + MAbse 
                            + MAe);

                    WRAPPER_TopMethodDefinitions.AppendLine();
                    counter++;
                }
            }

            WRAPPER_TopMethodDefinitions.AppendLine();

            WRAPPER_TopMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Texture Containers");
            foreach(var tex in materialRegister_TextureContainers)
            {
                WRAPPER_TopMethodDefinitions.AppendLine(Data_Sampler2DArray + tex + MAe);
                WRAPPER_TopMethodDefinitions.AppendLine(Data_SamplerState + "sampler_" + tex + MAe);
            }

            WRAPPER_TopMethodDefinitions.AppendLine();

            WRAPPER_TopMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Method Contents");
            foreach(var method in materialRegister_OtherMethods)
                WRAPPER_TopMethodDefinitions.AppendLine(method.methodContent);

            WRAPPER_TopMethodDefinitions.AppendLine();

            WRAPPER_TopMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Core Method Contents");
            foreach (var method in materialRegister_MainMethods)
                WRAPPER_TopMethodDefinitions.AppendLine(method.Value.theMethod.methodContent);

            if (materialRegister_PostMethods.Count > 0)
            {
                WRAPPER_PostMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Post Method Contents");
                foreach (var method in materialRegister_PostMethods)
                    WRAPPER_PostMethodDefinitions.AppendLine(ConvertDefinedMacros(method.Value.theMethod.methodContent, false));
            }

            if (materialRegister_PostMethodsGlobal.Count > 0)
            {
                WRAPPER_PostGlobalMethodDefinitions.AppendLine(COMMENT_LINE + "Registered Post Method Global Contents");
                foreach (var method in materialRegister_PostMethodsGlobal)
                    WRAPPER_PostGlobalMethodDefinitions.AppendLine(ConvertDefinedMacros(method.Value.methodContainer.methodContent, true));
            }
        }

        private static void WritePerObjMaterialBuffer(RMCoreRenderMasterMaterials rendererMaterials)
        {
            WRAPPER_CoreContent.AppendLine();
            if (!rendererMaterials.HasMaterialDataBuffers)
            {
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_PerObjRenderMaterials);
                WRAPPER_CoreContent.Append(MaterialBuffer_PerObjCoreMethodHead);
                WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);
                WRAPPER_CoreContent.AppendLine();
                WRAPPER_CoreContent.Append(WRAPPER_PostMethodDefinitions);
                WRAPPER_CoreContent.AppendLine();
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_PerObjPostRenderMaterials);
                WRAPPER_CoreContent.Append(MaterialBuffer_PerObjPostCoreMethodHead);
                WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);
                return;
            }

            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_PerObjRenderMaterials);
            WRAPPER_CoreContent.Append(MaterialBuffer_PerObjCoreMethodHead);
            FillMaterialMethods(false);
            WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);
            WRAPPER_CoreContent.AppendLine();
            WRAPPER_CoreContent.Append(WRAPPER_PostMethodDefinitions);
            WRAPPER_CoreContent.AppendLine();
            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_PerObjPostRenderMaterials);
            WRAPPER_CoreContent.Append(MaterialBuffer_PerObjPostCoreMethodHead);
            FillMaterialMethods(true);
            WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);

            void FillMaterialMethods(bool postMethod)
            {
                WRAPPER_CoreContent.AppendLine(MAspace + Data_Vector4Half + MaterialBuffer_SdfRef + MAq + "sdfRGBA" + MAe);

                var targetMethods = postMethod ? materialRegister_PostMethods : materialRegister_MainMethods;
                int counter = -1;
                foreach(var method in targetMethods)
                {
                    counter++;

                    if(!method.Value.isUsedInScene)
                        continue;

                    string preAppendix = "";
                    string appendix;
                    if (rendererMaterials.RenderMaster.UnpackDataContainer)
                    {
                        string containerType = materialRegister_NameOfMaterialContainerInstancesPerType[counter];
                        appendix = containerType + "_TempInstance";
                        preAppendix = MAspace + containerType + " " + appendix + MAq + MAbc;
                        var targetStrucContainer = materialRegister_StructContainers[containerType];
                        string sumup = "";
                        int c = 0;
                        foreach (var field in targetStrucContainer)
                        {
                            sumup += field.Key + MAbs + "sdfMaterialInstance" + MAbse;
                            if (c < targetStrucContainer.Count - 1)
                                sumup += MAc;
                            c++;
                        }
                        preAppendix += sumup + MAbce + MAe + MAnewLine;
                    }
                    else
                    {
                        appendix = method.Key + "[sdfMaterialInstance]";
                    }

                    WRAPPER_CoreContent.AppendLine(preAppendix + MAspace + $"sdfRGBA = sdfMaterialType != " + method.Value.materialIndex.ToString() + " ? sdfRGBA : " + method.Value.theMethod.methodName + MAb +
                        appendix + $", ray, {MaterialBuffer_SdfRef}" + MAbe + MAe);
                }
            }
        }

        private static void WriteGlobalMaterialBuffer(RMCoreRenderMasterMaterials rendererMaterials)
        {
            WRAPPER_CoreContent.AppendLine();

            if (!rendererMaterials.HasGlobalMaterialInstances)
            {
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_GlobalRenderMaterials);
                WRAPPER_CoreContent.Append(MaterialBuffer_GlobalCoreMethodHead);
                WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);
                WRAPPER_CoreContent.AppendLine();
                WRAPPER_CoreContent.Append(WRAPPER_PostGlobalMethodDefinitions);
                WRAPPER_CoreContent.AppendLine();
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_GlobalPostRenderMaterials);
                WRAPPER_CoreContent.Append(MaterialBuffer_GlobalPostCoreMethodHead);
                WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
                WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);
                return;
            }

            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_GlobalRenderMaterials);
            WRAPPER_CoreContent.Append(MaterialBuffer_GlobalCoreMethodHead);
            FillMaterialMethods(false);
            WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);
            WRAPPER_CoreContent.AppendLine();
            WRAPPER_CoreContent.Append(WRAPPER_PostGlobalMethodDefinitions);
            WRAPPER_CoreContent.AppendLine();
            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_GlobalPostRenderMaterials);
            WRAPPER_CoreContent.Append(MaterialBuffer_GlobalPostCoreMethodHead);
            FillMaterialMethods(true);
            WRAPPER_CoreContent.Append(MaterialBuffer_CoreMethodContentReturn);
            WRAPPER_CoreContent.AppendLine(MaterialBuffer_Commentary_End);

            void FillMaterialMethods(bool postMethod)
            {
                var targetMethods = postMethod ? materialRegister_PostMethodsGlobal : materialRegister_MainMethodsGlobal;
                int counter = -1;
                foreach (var method in targetMethods)
                {
                    string methodName = method.Value.methodContainer.methodName;
                    string containerNamePerIns = method.Key;

                    counter++;

                    string preAppendix = "";
                    string appendix;
                    if (rendererMaterials.RenderMaster.UnpackDataContainer)
                    {
                        string containerType = materialRegister_NameOfMaterialContainerInstancesPerType[counter];
                        appendix = containerType + "_TempInstance";
                        preAppendix = MAspace + containerType + " " + appendix + MAq + MAbc;
                        var targetStrucContainer = materialRegister_StructContainers[containerType];
                        string sumup = "";
                        int c = 0;
                        foreach (var field in targetStrucContainer)
                        {
                            sumup += field.Key + MAbs + method.Value.materialIndex + MAbse;
                            if (c < targetStrucContainer.Count - 1)
                                sumup += MAc;
                            c++;
                        }
                        preAppendix += sumup + MAbce + MAe + MAnewLine;
                    }
                    else
                    {
                        appendix = containerNamePerIns + MAbs + method.Value.materialIndex.ToString() + MAbse;
                    }

                    WRAPPER_CoreContent.AppendLine(preAppendix + MAspace + "sdfRGBA = " + methodName + (postMethod ? MaterialBuffer_GlobalStage : "") +
                        MAb + appendix + MAc + " ray, sdfRGBA)" + MAe);
                }
            }
        }

        #region Helpers

        private static string ConvertUniformType(RMMaterialIdentifier.MaterialUniformField inputUniform)
        {
            string uniform;

            if (inputUniform.uniformType == RMMaterialIdentifier.MaterialUniformType.NestedContainer)
                uniform = inputUniform.nestedContainerParent.MaterialDataContainerTypePerInstance;
            else
            {
                uniform = inputUniform.uniformType.ToString().ToLower();
                if (inputUniform.lowPrecision)
                    uniform = uniform.Replace("float", "half");
            }

            return uniform;
        }

        private static string ConvertDefinedMacros(string input, bool globalMaterialStage = false)
        {
            if (input.Contains(MaterialBuffer_DynamicMacro_MethodModifiable))
                input = input.Replace(MaterialBuffer_DynamicMacro_MethodModifiable, globalMaterialStage ? MaterialBuffer_GlobalStage : "");
            if(input.Contains(MaterialBuffer_DynamicMacro_RenderMaterialsPointer))
                input = input.Replace(MaterialBuffer_DynamicMacro_RenderMaterialsPointer, globalMaterialStage ? MaterialBuffer_GlobalMethodName : MaterialBuffer_PerObjMethodName);
            return input;
        }

        private static void CleanConvertorGarbage()
        {
            WRAPPER_CoreContent.Clear();
            WRAPPER_TopMethodDefinitions.Clear();
            WRAPPER_PostMethodDefinitions.Clear();
            WRAPPER_PostGlobalMethodDefinitions.Clear();

            materialRegister_GlobalKeywords.Clear();
            materialRegister_GlobalUniforms.Clear();
            materialRegister_StructContainers.Clear();
            materialRegister_StructContainerInstances.Clear();
            materialRegister_MainMethods.Clear();
            materialRegister_MainMethodsGlobal.Clear();
            materialRegister_OtherMethods.Clear();
            materialRegister_PostMethods.Clear();
            materialRegister_PostMethodsGlobal.Clear();
            materialRegister_TextureContainers.Clear();

            materialRegister_CountOfMaterialInstancesPerType = null;
            materialRegister_NameOfMaterialContainerInstancesPerType = null;
        }

        #endregion

#endif
    }
}
