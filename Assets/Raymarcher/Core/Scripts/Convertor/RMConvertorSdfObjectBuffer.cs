using System.Collections.Generic;
using System.IO;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Raymarcher.Objects;
using Raymarcher.Objects.Modifiers;
using Raymarcher.Constants;
using Raymarcher.RendererData;

namespace Raymarcher.Convertor
{
    using static RMConvertorCommon;
    using static RMConstants.CommonBuildTimeConstants;

    public static class RMConvertorSdfObjectBuffer
    {
#if UNITY_EDITOR

        // Symbol & method constants unique for object buffer convertor
        // Variables
        private const string SdfObjectBuffer_GroupInstance = RMSdfObjBuffer_VariableConstant_ObjInstance + "Group";
        private const string SdfObjectBuffer_ModifierGroupInstance = RMSdfObjBuffer_VariableConstant_ObjInstance + "ModifierGroup";
        private const string SdfObjectBuffer_SdfInstancesContainer = "SdfInstancesContainer";
        private const string SdfObjectBuffer_PrecomputedLocalPos = "localPos";
        private const string SdfObjectBuffer_ModelData = RM_COMMON_SDFOBJBUFFER_ModelData;
        private const string SdfObjectBuffer_TextureData = "textureData";
        private const string SdfObjectBuffer_TextureScale = "textureScale";

        // Function definitions
        private const string SdfObjectBuffer_CoreMethodHead = "SdfObjectBuffer(in float3 p)";
        private static readonly string SdfObjectBuffer_CoreMethodEmptyCase =
            $"{MAnewLine}{MAbc}{MAnewLine}{MAspace}" +
            $"return 1{MAe}{MAnewLine}{MAbce}";
        // Boolean Operations
        private const string SdfObjectBuffer_Smoothing4 = "GroupSmoothUnion" + MAb;

        // Internal Shader Vars
        private const string SdfObjectBuffer_GlobalSdfObjSmoothness = RMConstants.CommonRendererProperties.GlobalSdfObjectSmoothness;

#endif

        public const string RMSdfObjBuffer_VariableConstant_ObjInstance = "obj";
        public const string RMSdfObjBuffer_VariableConstant_ModelInstance = RMSdfObjBuffer_VariableConstant_ObjInstance + "Model";
        public const string RMSdfObjBuffer_VariableConstant_Position = "p";
        public const string RMSdfObjBuffer_VariableConstant_Sdf = "sdf";
        public const string RMSdfObjBuffer_VariableConstant_Color = "color";
        public const string RMSdfObjBuffer_VariableConstant_MaterialType = "materialType";
        public const string RMSdfObjBuffer_VariableConstant_MaterialInstance = "materialInstance";
        public const string RMSdfObjBuffer_VariableConstant_Result = "result";
        public const string RMSdfObjBuffer_VariableConstant_SdfInstances = RMConstants.CommonBuildTimeConstants.RM_COMMON_SDFOBJBUFFER_SdfInstances;
        public const string RMSdfObjBuffer_VariableConstant_SamplerPack = "RaymarcherSamplerPackTextures";
        public const string SdfObjectBuffer_VariableConstant_SharedModifierID = "_shared";

#if UNITY_EDITOR

        private const string SdfObjectBuffer_TransformationCall = "RM_TRANS" + MAb;
        private const string SdfObjectBuffer_SampleTex = "RM_SAMPLE_TEX" + MAb;

        #region Dynamic wrappers

        private static readonly StringBuilder WRAPPER_TopMethodDefinitions = new StringBuilder();
        private static readonly StringBuilder WRAPPER_TopPostMethodDefinitions = new StringBuilder();
        private static readonly StringBuilder WRAPPER_TopVariableDeclarations = new StringBuilder();
        private static readonly StringBuilder WRAPPER_CoreContent = new StringBuilder();
        private static readonly StringBuilder WRAPPER_BaseWrapper = new StringBuilder();

        private struct MethodWrapper
        {
            public string methodName;
            public ISDFEntity.SDFUniformField[] methodParams;
            public string methodBody;
            public string methodExtension;
            public bool isModifierMethod;
            public RMObjectModifierBase.InlineMode methodInlineMode;

            public MethodWrapper(string methodName, ISDFEntity.SDFUniformField[] methodParams, string methodBody, string methodExtension, bool isModifierMethod,
                RMObjectModifierBase.InlineMode methodInlineMode = RMObjectModifierBase.InlineMode.PostSdfInstance)
            {
                this.methodName = methodName;
                this.methodParams = methodParams;
                this.methodBody = methodBody;
                this.isModifierMethod = isModifierMethod;
                this.methodInlineMode = methodInlineMode;
                this.methodExtension = methodExtension;
            }
        }

        private struct PostSDFModifierWrapper
        {
            public string line;
            public List<string> modifiedSdfObject;
        }

        #endregion

        public static void ConvertAndWriteToSdfObjectBuffer(in RMCoreRenderMasterMapping mapping)
        {
            string fullBufferPath = GetAssetRelativetPath(mapping.RenderMaster, true) + generatedCodeSdfBufferPath + mapping.RenderMaster.RegisteredSessionName + ".cginc";

            if (!File.Exists(fullBufferPath))
            {
                RMDebug.Debug(mapping.RenderMaster, $"Couldn't convert to shading language and write to sdf object buffer in path '{fullBufferPath}'", true);
                return;
            }

            if (mapping.SceneSdfObjectContainer == null || mapping.SceneSdfObjectContainer.Count == 0)
            {
                File.WriteAllText(fullBufferPath, ReturnCorrectRenderTypePrecision(mapping) + SdfObjectBuffer_CoreMethodHead.CheckPrecision(mapping.RenderMaster) + SdfObjectBuffer_CoreMethodEmptyCase);
                CleanConvertorGarbage();
                AssetDatabase.Refresh();
                return;
            }

            WRAPPER_BaseWrapper.AppendLine("// This code has been generated by the RMConvertor. Please do not attempt any changes");
            WRAPPER_BaseWrapper.AppendLine();
            WRAPPER_BaseWrapper.AppendLine();

            WRAPPER_TopMethodDefinitions.AppendLine("// SDF methods (sdf object sources & modifiers)");

            WRAPPER_TopVariableDeclarations.AppendLine("// SDF object variables");

            // Writing structured obj instance buffer
            WriteSdfContainer(mapping);
            
            WRAPPER_CoreContent.AppendLine(ReturnCorrectRenderTypePrecision(mapping) + SdfObjectBuffer_CoreMethodHead.CheckPrecision(mapping.RenderMaster));
            WRAPPER_CoreContent.AppendLine(MAbc);
            WRAPPER_CoreContent.AppendLine("// SDF object declarations");

            List<MethodWrapper> methodWrapper = new List<MethodWrapper>();
            List<PostSDFModifierWrapper> postSdfsModifierWrapper = new List<PostSDFModifierWrapper>();

            StringBuilder postInstanceModifierWrapper = new StringBuilder();
            HashSet<string> existingVariables = new HashSet<string>();
            Dictionary<string, HashSet<RMObjectModifierSharedContainer>> sharedModifierContainers = new Dictionary<string, HashSet<RMObjectModifierSharedContainer>>();
            
            // Reordering shared modifiers
            for (int x = 0; x < mapping.SceneSdfObjectContainer.Count; x++)
            {
                var obj = mapping.SceneSdfObjectContainer[x];
                if (!obj.HasModifiers)
                    continue;
                for (int z = 0; z < obj.Modifiers.Count; z++)
                {
                    var mod = obj.Modifiers[z];
                    if (!mod.ModifierSupportsSharedContainer)
                        continue;
                    if (!mod.SharedModifierContainer)
                        continue;

                    if (sharedModifierContainers.ContainsKey(mod.SharedModifierContainer.SharedContainerIdentifier))
                    {
                        if (!sharedModifierContainers[mod.SharedModifierContainer.SharedContainerIdentifier].Contains(mod.SharedModifierContainer))
                            sharedModifierContainers[mod.SharedModifierContainer.SharedContainerIdentifier].Add(mod.SharedModifierContainer);
                    }
                    else
                        sharedModifierContainers.Add(mod.SharedModifierContainer.SharedContainerIdentifier, new HashSet<RMObjectModifierSharedContainer>() { mod.SharedModifierContainer });
                }
            }
            foreach(var sharedContainers in sharedModifierContainers)
            {
                int counter = 0;
                foreach (var cont in sharedContainers.Value)
                    cont.SetShaderQueueID(SdfObjectBuffer_VariableConstant_SharedModifierID + (counter++).ToString());
            }

            // Declaration of top core variables & core sdf formulas
            int i = 0;
            foreach (RMSdfObjectBase sdfObj in mapping.SceneSdfObjectContainer)
            {
                string istr = i.ToString();
                string modifierPosition = "";
                postInstanceModifierWrapper.Clear();

                // Utilize & write sdf modifiers
                if (sdfObj.HasModifiers)
                {
                    foreach (RMObjectModifierBase modifier in sdfObj.Modifiers)
                    {
                        // Shared container evaluation
                        string hasSharedModifier = "";
                        if(modifier.ModifierSupportsSharedContainer && modifier.SharedModifierContainer != null && modifier.SharedModifierContainer.SharedContainerInstance != null)
                        {
                            if (sharedModifierContainers.ContainsKey(modifier.SharedModifierContainer.SharedContainerIdentifier))
                            {
                                foreach(var sharedContainer in sharedModifierContainers[modifier.SharedModifierContainer.SharedContainerIdentifier])
                                {
                                    if(sharedContainer == modifier.SharedModifierContainer)
                                    {
                                        hasSharedModifier = sharedContainer.ShaderQueueID;
                                        break;
                                    }
                                }
                            }
                        }

                        // Utilize modifier method
                        UtilizeMethod(new MethodWrapper(modifier.SdfMethodName, modifier.SdfUniformFields, modifier.SdfMethodBody, modifier.SdfMethodExtension, true, modifier.ModifierInlineMode()), methodWrapper);
                        UtilizeModifierVariables(istr, modifier.SdfUniformFields, existingVariables, hasSharedModifier, mapping.RenderMaster);

                        // Create modifier method call based on inline mode
                        switch (modifier.ModifierInlineMode())
                        {
                            case RMObjectModifierBase.InlineMode.PostSdfInstance:
                                postInstanceModifierWrapper.Append(CreateModifierPostSdfInstanceMethodCall(
                                    RMSdfObjBuffer_VariableConstant_ObjInstance + istr,
                                    istr,
                                    modifier.SdfMethodName,
                                    modifier.SdfUniformFields,
                                    mapping.RenderMaster));
                                break;

                            case RMObjectModifierBase.InlineMode.SdfInstancePosition:
                                modifierPosition = CreateModifierSdfInstancePositionMethodCall(
                                    istr,
                                    modifier.SdfMethodName,
                                    modifier.SdfUniformFields,
                                    modifierPosition,
                                    hasSharedModifier,
                                    mapping.RenderMaster);
                                break;

                            case RMObjectModifierBase.InlineMode.PostSdfBuffer:
                                PostSDFModifierWrapper postModifierInstance = new PostSDFModifierWrapper();
                                postModifierInstance.modifiedSdfObject = new List<string>();
                                postModifierInstance.line = CreateModifierPostSdfBufferMethodCall(
                                    postModifierInstance,
                                    postSdfsModifierWrapper.Count,
                                    RMSdfObjBuffer_VariableConstant_ObjInstance + istr,
                                    istr,
                                    modifier.SdfMethodName,
                                    modifier.SdfUniformFields,
                                    hasSharedModifier,
                                    mapping.RenderMaster);
                                postSdfsModifierWrapper.Add(postModifierInstance);
                                break;
                        }
                    }
                }

                // Write complete sdf variables + formulas in the core
                WriteFullSdf(istr, sdfObj, modifierPosition, mapping.RenderMaster);
                // Write post-sdf-instance (inline mode) modifier wrapper
                WRAPPER_CoreContent.Append(postInstanceModifierWrapper);

                // Utilize sdf method
                UtilizeMethod(new MethodWrapper(sdfObj.SdfMethodName, sdfObj.SdfUniformFields, sdfObj.SdfMethodBody, sdfObj.SdfMethodExtension, false), methodWrapper);

                i++;
            }

            // Write all methods
            WriteAllMethods(methodWrapper, mapping.RenderMaster);
            WritePostSDFBufferModifiers(postSdfsModifierWrapper);

            bool moreThanOne = i > 1;
            int groupCounter = 0;

            // Write smooth unions if more than one
            if (moreThanOne)
                WriteSmoothUnions(i, postSdfsModifierWrapper, mapping.RenderMaster, out groupCounter);

            // Finalizer
            string returnFunc;
            if (groupCounter > 0)
                returnFunc = SdfObjectBuffer_GroupInstance + (groupCounter - 1).ToString();
            else if (!moreThanOne)
                returnFunc = RMSdfObjBuffer_VariableConstant_ObjInstance + (i - 1).ToString();
            else
            {
                returnFunc = postSdfsModifierWrapper.Count == 1
                    ? SdfObjectBuffer_ModifierGroupInstance + groupCounter
                    : SdfObjectBuffer_GroupInstance + (groupCounter - 1).ToString();
            }

            WRAPPER_CoreContent.Append(MAspace + "return " + returnFunc + MAe);

            WRAPPER_BaseWrapper.Append(WRAPPER_TopMethodDefinitions);
            WRAPPER_BaseWrapper.AppendLine();
            WRAPPER_BaseWrapper.Append(WRAPPER_TopVariableDeclarations);
            WRAPPER_BaseWrapper.AppendLine();
            WRAPPER_BaseWrapper.Append(WRAPPER_CoreContent);
            WRAPPER_BaseWrapper.AppendLine(MAnewLine + MAbce);

            methodWrapper.Clear();
            postSdfsModifierWrapper.Clear();
            postInstanceModifierWrapper.Clear();
            existingVariables.Clear();
            sharedModifierContainers.Clear();

            File.WriteAllText(fullBufferPath, WRAPPER_BaseWrapper.ToString());
            CleanConvertorGarbage();

            AssetDatabase.Refresh();
        }

        private static string ReturnCorrectRenderTypePrecision(RMCoreRenderMasterMapping mappingMaster)
            => ReturnCorrectRenderTypePrecision(mappingMaster.RenderMaster);

        private static string ReturnCorrectRenderTypePrecision(RMRenderMaster renderMaster)
        {
            switch (renderMaster.RenderingData.CompiledRenderType)
            {
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                    return Data_Matrix2x4;
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                    return Data_Vector4;
                default:
                    return Data_Vector2Half;
            }
        }

        // Sdfs

        private static void WriteSdfContainer(RMCoreRenderMasterMapping mapping)
        {
            if (mapping.RenderMaster.UnpackDataContainer == false)
            {
                WRAPPER_TopVariableDeclarations.AppendLine(Data_Struct + SdfObjectBuffer_SdfInstancesContainer);
                WRAPPER_TopVariableDeclarations.AppendLine(MAbc);

                switch (mapping.RenderMaster.RenderingData.CompiledRenderType)
                {
                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Matrix4x4Half + SdfObjectBuffer_ModelData + MAe);
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Vector4Half + SdfObjectBuffer_TextureData + MAe);
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Vector4Half + SdfObjectBuffer_TextureScale + MAe);
                        break;

                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Matrix4x4Half + SdfObjectBuffer_ModelData + MAe);
                        break;

                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Vector4Half + SdfObjectBuffer_ModelData + MAe);
                        break;
                }

                WRAPPER_TopVariableDeclarations.AppendLine(MAbce + MAe);
                WRAPPER_TopVariableDeclarations.AppendLine(Data_StructuredBuffer + "<" + SdfObjectBuffer_SdfInstancesContainer + "> " + RMSdfObjBuffer_VariableConstant_SdfInstances + MAe);
            }
            else
            {
                string count = mapping.SceneSdfObjectContainer.Count.ToString();
                switch (mapping.RenderMaster.RenderingData.CompiledRenderType)
                {
                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Matrix4x4Half + SdfObjectBuffer_ModelData + MAbs + count + MAbse + MAe);
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Vector4Half + SdfObjectBuffer_TextureData + MAbs + count + MAbse + MAe);
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Vector4Half + SdfObjectBuffer_TextureScale + MAbs + count + MAbse + MAe);
                        break;

                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Matrix4x4Half + SdfObjectBuffer_ModelData + MAbs + count + MAbse + MAe);
                        break;

                    case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Vector4Half + SdfObjectBuffer_ModelData + MAbs + count + MAbse + MAe);
                        break;
                }
            }
        }

        private static void WriteFullSdf(string currentIndex, RMSdfObjectBase sdfObj, string sdfPositionModifier, RMRenderMaster renderMaster)
        {
            string formula = MAspace;
            string temp;
            bool unpackData = renderMaster.UnpackDataContainer;

            switch (renderMaster.RenderingData.CompiledRenderType)
            {
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                    // local pos
                    if (currentIndex == "0")
                        formula += $"{Data_Vector3}";
                    // unpacked/structured model data
                    temp = !unpackData ? $"{RMSdfObjBuffer_VariableConstant_SdfInstances}[{currentIndex}].{SdfObjectBuffer_ModelData}" : $"{SdfObjectBuffer_ModelData}[{currentIndex}]";
                    formula += $"{SdfObjectBuffer_PrecomputedLocalPos}{MAq}{SdfObjectBuffer_TransformationCall}" +
                            $"{RMSdfObjBuffer_VariableConstant_Position}{MAc}{temp}{MAbe}{MAe}{MAnewLine}{MAspace}";
                    // sdf
                    formula += $"{Data_Matrix2x4}{RMSdfObjBuffer_VariableConstant_ObjInstance}{currentIndex}{MAq}{Data_Matrix2x4}{MAb}";
                    // sdf - pos
                    formula += $"{sdfObj.SdfMethodName}{MAb}{(string.IsNullOrEmpty(sdfPositionModifier) ? SdfObjectBuffer_PrecomputedLocalPos : sdfPositionModifier)}{MAc}";
                    // sdf - texture data
                    temp = !unpackData ? $"{RMSdfObjBuffer_VariableConstant_SdfInstances}[{currentIndex}].{SdfObjectBuffer_TextureData}" : $"{SdfObjectBuffer_TextureData}[{currentIndex}]";
                    formula += $"{SdfObjectBuffer_SampleTex}{SdfObjectBuffer_PrecomputedLocalPos}{MAc}{temp}";
                    // sdf - texture scale
                    temp = !unpackData ? $"{RMSdfObjBuffer_VariableConstant_SdfInstances}[{currentIndex}].{SdfObjectBuffer_TextureScale}" : $"{SdfObjectBuffer_TextureScale}[{currentIndex}]";
                    formula += $"{MAc}{temp}{MAc}";
                    // sdf - model
                    temp = !unpackData ? $"{RMSdfObjBuffer_VariableConstant_SdfInstances}[{currentIndex}].{SdfObjectBuffer_ModelData}" : $"{SdfObjectBuffer_ModelData}[{currentIndex}]";
                    formula += $"{temp}[3]{MAbe}";
                    foreach (ISDFEntity.SDFUniformField shaderField in sdfObj.SdfUniformFields)
                    {
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Uniform + ConvertUniformType(shaderField, renderMaster) + MAs + shaderField.fieldName + currentIndex + MAe);
                        formula += MAc + shaderField.fieldName + currentIndex;
                    }
                    (int typeIndex, int instanceIndex) materialIndexes = sdfObj.GetMyMaterialTypeAndInstanceIndex();
                    formula += $"{MAbe}{MAc}{materialIndexes.typeIndex}{MAc}{materialIndexes.instanceIndex}{MAc}0{MAc}0{MAbe}{MAe}";
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                    formula += $"{Data_Vector4}{RMSdfObjBuffer_VariableConstant_ObjInstance}{currentIndex}{MAq}{Data_Vector4}{MAb}";
                    // sdf
                    temp = !unpackData ? $"{RMSdfObjBuffer_VariableConstant_SdfInstances}[{currentIndex}].{SdfObjectBuffer_ModelData}" : $"{SdfObjectBuffer_ModelData}[{currentIndex}]";
                    formula += $"{sdfObj.SdfMethodName}{MAb}{(string.IsNullOrEmpty(sdfPositionModifier) ? SdfObjectBuffer_TransformationCall + RMSdfObjBuffer_VariableConstant_Position + MAc + temp + MAbe : sdfPositionModifier)}{MAc}";
                    // sdf - model
                    formula += $"{temp}[3].x";
                    foreach (ISDFEntity.SDFUniformField shaderField in sdfObj.SdfUniformFields)
                    {
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Uniform + ConvertUniformType(shaderField, renderMaster) + MAs + shaderField.fieldName + currentIndex + MAe);
                        formula += MAc + shaderField.fieldName + currentIndex;
                    }
                    materialIndexes = sdfObj.GetMyMaterialTypeAndInstanceIndex();
                    formula += $"{MAbe}{MAc}{materialIndexes.typeIndex}{MAc}{materialIndexes.instanceIndex}{MAbe}{MAe}";
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                    formula += $"{Data_Vector2Half}{RMSdfObjBuffer_VariableConstant_ObjInstance}{currentIndex}{MAq}{Data_Vector2Half}{MAb}";
                    // sdf
                    temp = !unpackData ? $"{RMSdfObjBuffer_VariableConstant_SdfInstances}[{currentIndex}].{SdfObjectBuffer_ModelData}" : $"{SdfObjectBuffer_ModelData}[{currentIndex}]";
                    formula += $"{sdfObj.SdfMethodName}{MAb}{(string.IsNullOrEmpty(sdfPositionModifier) ? SdfObjectBuffer_TransformationCall + RMSdfObjBuffer_VariableConstant_Position + MAc + temp + ".xyz" + MAbe : sdfPositionModifier)}{MAc}";
                    formula += $"{temp}.w";
                    foreach (ISDFEntity.SDFUniformField shaderField in sdfObj.SdfUniformFields)
                    {
                        WRAPPER_TopVariableDeclarations.AppendLine(Data_Uniform + ConvertUniformType(shaderField, renderMaster) + MAs + shaderField.fieldName + currentIndex + MAe);
                        formula += MAc + shaderField.fieldName + currentIndex;
                    }
                    formula += $"{MAbe}{MAbe}{MAe}";
                    break;
            }

            WRAPPER_CoreContent.AppendLine(formula);
        }

        // Methods

        private static void UtilizeMethod(MethodWrapper entryMethod, List<MethodWrapper> methodWrapper)
        {
            bool found = false;
            foreach(MethodWrapper method in methodWrapper)
            {
                if(method.methodName == entryMethod.methodName)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                methodWrapper.Add(entryMethod);
        }

        private static void WriteAllMethods(List<MethodWrapper> methodWrapper, RMRenderMaster renderMaster)
        {
            for (int z = 0; z < methodWrapper.Count; z++)
            {
                MethodWrapper methodIns = methodWrapper[z];

                string dataType = Data_Vector2;
                string methodParams = "";

                if (methodIns.isModifierMethod)
                {
                    switch (methodIns.methodInlineMode)
                    {
                        case RMObjectModifierBase.InlineMode.SdfInstancePosition:
                            if (renderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant)
                            {
                                dataType = Data_Vector3Half;
                                methodParams = $"{Data_Vector3Half}{RMSdfObjBuffer_VariableConstant_Position}";
                            }
                            else
                            {
                                dataType = Data_Vector3;
                                methodParams = $"{Data_Vector3}{RMSdfObjBuffer_VariableConstant_Position}";
                            }
                            break;

                        case RMObjectModifierBase.InlineMode.PostSdfBuffer:
                            dataType = ReturnCorrectRenderTypePrecision(renderMaster);
                            if (renderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality)
                                methodParams = $"{Data_Vector1}{RMSdfObjBuffer_VariableConstant_Sdf}{MAc} {Data_Vector3Half}{RMSdfObjBuffer_VariableConstant_Color}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialType}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialInstance}";
                            else if (renderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard)
                                methodParams = $"{Data_Vector1}{RMSdfObjBuffer_VariableConstant_Sdf}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Color}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialType}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialInstance}";
                            else
                                methodParams = $"{Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Sdf}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Color}";
                            break;

                        case RMObjectModifierBase.InlineMode.PostSdfInstance:
                            dataType = ReturnCorrectRenderTypePrecision(renderMaster);
                            if (renderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality)
                                methodParams = $"{Data_Vector1}{RMSdfObjBuffer_VariableConstant_Sdf}{MAc} {Data_Vector3}{RMSdfObjBuffer_VariableConstant_Position}{MAc} {Data_Vector3Half}{RMSdfObjBuffer_VariableConstant_Color}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialType}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialInstance}";
                            else if (renderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard)
                                methodParams = $"{Data_Vector1}{RMSdfObjBuffer_VariableConstant_Sdf}{MAc} {Data_Vector3}{RMSdfObjBuffer_VariableConstant_Position}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Color}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialType}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_MaterialInstance}";
                            else
                                methodParams = $"{Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Sdf}{MAc} {Data_Vector3}{RMSdfObjBuffer_VariableConstant_Position}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Color}";
                            break;
                    }
                }
                else
                {
                    switch(renderMaster.RenderingData.CompiledRenderType)
                    {
                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                            dataType = Data_Vector4;
                            methodParams = $"{Data_Vector3}{RMSdfObjBuffer_VariableConstant_Position}{MAc} {Data_Vector3Half}{RMSdfObjBuffer_VariableConstant_Color}";
                            break;
                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                            dataType = Data_Vector2;
                            methodParams = $"{Data_Vector3}{RMSdfObjBuffer_VariableConstant_Position}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Color}";
                            break;
                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                            dataType = Data_Vector2Half;
                            methodParams = $"{Data_Vector3Half}{RMSdfObjBuffer_VariableConstant_Position}{MAc} {Data_Vector1Half}{RMSdfObjBuffer_VariableConstant_Color}";
                            break;
                    }
                }

                string methodHeader = dataType + methodIns.methodName + MAb + methodParams;

                for (int g = 0; g < methodIns.methodParams.Length; g++)
                {
                    var methodParam = methodIns.methodParams[g];
                    methodHeader += MAc + MAs + ConvertUniformType(methodParam, renderMaster) + MAs +
                        (string.IsNullOrEmpty(methodParam.methodParameterName) ? methodParam.fieldName : methodParam.methodParameterName);
                }

                methodHeader += MAbe;

                if (!string.IsNullOrWhiteSpace(methodIns.methodExtension))
                    WRAPPER_TopMethodDefinitions.AppendLine(methodIns.methodExtension);

                WRAPPER_TopMethodDefinitions.AppendLine(methodHeader);
                WRAPPER_TopMethodDefinitions.AppendLine(MAbc);

                if (!methodIns.isModifierMethod)
                    WRAPPER_TopMethodDefinitions.AppendLine(renderMaster.RenderingData.CompiledRenderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant
                        ? Data_Vector1Half + RMSdfObjBuffer_VariableConstant_Result + MAe
                        : Data_Vector1 + RMSdfObjBuffer_VariableConstant_Result + MAe);

                WRAPPER_TopMethodDefinitions.AppendLine(methodIns.methodBody);

                if(!methodIns.isModifierMethod)
                {
                    switch(renderMaster.RenderingData.CompiledRenderType)
                    {
                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                            WRAPPER_TopMethodDefinitions.AppendLine("return " + Data_Vector4 + MAb + RMSdfObjBuffer_VariableConstant_Result + MAc + RMSdfObjBuffer_VariableConstant_Color + MAbe + MAe);
                            break;
                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                            WRAPPER_TopMethodDefinitions.AppendLine("return " + Data_Vector2 + MAb + RMSdfObjBuffer_VariableConstant_Result + MAc + RMSdfObjBuffer_VariableConstant_Color + MAbe + MAe);
                            break;
                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                            WRAPPER_TopMethodDefinitions.AppendLine("return " + Data_Vector2Half + MAb + RMSdfObjBuffer_VariableConstant_Result + MAc + RMSdfObjBuffer_VariableConstant_Color + MAbe + MAe);
                            break;
                    }
                }
                else
                {
                    if (methodIns.methodInlineMode == RMObjectModifierBase.InlineMode.SdfInstancePosition)
                        WRAPPER_TopMethodDefinitions.AppendLine("return " + RMSdfObjBuffer_VariableConstant_Position + MAe);
                    else
                    {
                        switch (renderMaster.RenderingData.CompiledRenderType)
                        {
                            case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                                WRAPPER_TopMethodDefinitions.AppendLine("return " + Data_Matrix2x4 + MAb + RMSdfObjBuffer_VariableConstant_Sdf + MAc + RMSdfObjBuffer_VariableConstant_Color + MAc + RMSdfObjBuffer_VariableConstant_MaterialType + MAc + RMSdfObjBuffer_VariableConstant_MaterialInstance + MAc + "0,0" + MAbe + MAe);
                                break;
                            case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                                WRAPPER_TopMethodDefinitions.AppendLine("return " + Data_Vector4 + MAb + RMSdfObjBuffer_VariableConstant_Sdf + MAc + RMSdfObjBuffer_VariableConstant_Color + MAc + RMSdfObjBuffer_VariableConstant_MaterialType + MAc + RMSdfObjBuffer_VariableConstant_MaterialInstance + MAbe + MAe);
                                break;
                            case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                                WRAPPER_TopMethodDefinitions.AppendLine("return " + Data_Vector2Half + MAb + RMSdfObjBuffer_VariableConstant_Sdf + MAc + RMSdfObjBuffer_VariableConstant_Color + MAbe + MAe);
                                break;
                        }
                    }
                }

                WRAPPER_TopMethodDefinitions.AppendLine(MAbce + MAnewLine);
            }
        }

        // Modifiers

        private static void UtilizeModifierVariables(string index, ISDFEntity.SDFUniformField[] modifierMethodParams, HashSet<string> existingVariables, string hasSharedContainer, RMRenderMaster renderMaster)
        {
            for (int i = 0; i < modifierMethodParams.Length; i++)
            {
                ISDFEntity.SDFUniformField field = modifierMethodParams[i];
                if (field.dontCreateUniformVariable)
                    continue;
                string fieldName = field.fieldName + (!string.IsNullOrEmpty(hasSharedContainer) ? hasSharedContainer : index);

                if (existingVariables.Contains(fieldName))
                    continue;
                existingVariables.Add(fieldName);
                WRAPPER_TopVariableDeclarations.AppendLine(Data_Uniform + ConvertUniformType(field, renderMaster) + MAs + fieldName + MAe);
            }
        }

        private static void WritePostSDFBufferModifiers(in List<PostSDFModifierWrapper> afterSdfsModifierWrapper)
        {
            WRAPPER_CoreContent.AppendLine("// SDF modifier groups");
            foreach (PostSDFModifierWrapper str in afterSdfsModifierWrapper)
                WRAPPER_CoreContent.AppendLine(str.line);
            WRAPPER_CoreContent.AppendLine();
        }

        private static string CreateModifierPostSdfInstanceMethodCall(string currentObj, string index, string modifierMethodName, ISDFEntity.SDFUniformField[] modifierMethodParams, RMRenderMaster renderMaster)
        {
            string formula = MAspace + currentObj + MAq + modifierMethodName + MAb;
            switch(renderMaster.RenderingData.CompiledRenderType)
            {
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                    formula += $"{currentObj}[0].x{MAc}{RMSdfObjBuffer_VariableConstant_Position}{MAc}{currentObj}[0].yzw{MAc}{currentObj}[1].x{MAc}{currentObj}[1].y";
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                    formula += $"{currentObj}.x{MAc}{RMSdfObjBuffer_VariableConstant_Position}{MAc}{currentObj}.y{MAc}{currentObj}.z{MAc}{currentObj}.w";
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                    formula += $"{currentObj}.x{MAc}{RMSdfObjBuffer_VariableConstant_Position}{MAc}{currentObj}.y";
                    break;
            }

            for (int i = 0; i < modifierMethodParams.Length; i++)
            {
                ISDFEntity.SDFUniformField field = modifierMethodParams[i];
                formula += MAc + field.fieldName + (field.dontCreateUniformVariable ? "" : index);
            }
            return formula + MAbe + MAe + MAnewLine;
        }

        private static string CreateModifierPostSdfBufferMethodCall(
            PostSDFModifierWrapper postSdfWrapperIns,
            int countOfPostModifiers,
            string currentObj,
            string index,
            string modifierMethodName,
            ISDFEntity.SDFUniformField[] modifierMethodParams,
            string hasSharedModifier,
            RMRenderMaster renderMaster)
        {
            postSdfWrapperIns.modifiedSdfObject.Add(currentObj);

            string formula = MAspace + ReturnCorrectRenderTypePrecision(renderMaster) + SdfObjectBuffer_ModifierGroupInstance + countOfPostModifiers.ToString() + MAq + modifierMethodName + MAb;
            switch (renderMaster.RenderingData.CompiledRenderType)
            {
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                    formula += $"{currentObj}[0].x{MAc}{currentObj}[0].yzw{MAc}{currentObj}[1].x{MAc}{currentObj}[1].y";
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                    formula += $"{currentObj}.x{MAc}{currentObj}.y{MAc}{currentObj}.z{MAc}{currentObj}.w";
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                    formula += $"{currentObj}.x{MAc}{currentObj}.y";
                    break;
            }

            for (int i = 0; i < modifierMethodParams.Length; i++)
            {
                ISDFEntity.SDFUniformField field = modifierMethodParams[i];
                string fname = field.fieldName + (field.dontCreateUniformVariable || !string.IsNullOrEmpty(hasSharedModifier) ? hasSharedModifier : index);
                formula += MAc + fname;
                if (field.uniformFieldHoldsOtherSdfObjectData)
                    postSdfWrapperIns.modifiedSdfObject.Add(fname);
            }
            return formula + MAbe + MAe;
        }

        private static string CreateModifierSdfInstancePositionMethodCall(string index, string modifierMethodName, ISDFEntity.SDFUniformField[] modifierMethodParams, string currentModifiedPosition, string hasSharedModifier, RMRenderMaster renderMaster)
        {
            string nonModifierMethodFormula = SdfObjectBuffer_TransformationCall;
            string temp = !renderMaster.UnpackDataContainer ? $"{RMSdfObjBuffer_VariableConstant_SdfInstances}[{index}].{SdfObjectBuffer_ModelData}" : $"{SdfObjectBuffer_ModelData}[{index}]";

            switch (renderMaster.RenderingData.CompiledRenderType)
            {
                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                    nonModifierMethodFormula = SdfObjectBuffer_PrecomputedLocalPos;
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                    nonModifierMethodFormula += $"{RMSdfObjBuffer_VariableConstant_Position}{MAc}{temp}{MAbe}";
                    break;

                case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                    nonModifierMethodFormula += $"{RMSdfObjBuffer_VariableConstant_Position}{MAc}{temp}.xyz{MAbe}";
                    break;
            }

            string modifiedPos = string.IsNullOrEmpty(currentModifiedPosition) ?
                nonModifierMethodFormula : currentModifiedPosition;

            string formula = modifierMethodName + MAb + modifiedPos;
            for (int i = 0; i < modifierMethodParams.Length; i++)
            {
                ISDFEntity.SDFUniformField field = modifierMethodParams[i];
                formula += MAc + field.fieldName + (field.dontCreateUniformVariable ? "" : !string.IsNullOrEmpty(hasSharedModifier) ? hasSharedModifier : index);
            }
            formula += MAbe;
            return formula;
        }

        // Mixing

        private static void WriteSmoothUnions(int objCount, List<PostSDFModifierWrapper> postSdfsModifierWrapper, RMRenderMaster renderMaster, out int groupCounter)
        {
            WRAPPER_CoreContent.AppendLine("// Smooth Unions");

            List<string> remainingSdfs = new List<string>();

            // Filter remaining sdfs from modifier groups
            for (int i = 0; i < objCount; i++)
            {
                string objname = RMSdfObjBuffer_VariableConstant_ObjInstance + i.ToString();

                bool found = false;
                foreach (PostSDFModifierWrapper postSdfModifierWrapper in postSdfsModifierWrapper)
                {
                    foreach (string obj in postSdfModifierWrapper.modifiedSdfObject)
                    {
                        if (obj == objname)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
                if (found)
                    continue;
                remainingSdfs.Add(objname);
            }

            groupCounter = 0;

            string dataType = ReturnCorrectRenderTypePrecision(renderMaster);

            // Append modifier groups first
            for (int x = 0; x < postSdfsModifierWrapper.Count - 1; x++)
            {
                string xstr = x.ToString();
                string xmstr = (x - 1).ToString();
                string xpstr = (x + 1).ToString();

                string from = x == 0 ? SdfObjectBuffer_ModifierGroupInstance + xstr : SdfObjectBuffer_GroupInstance + xmstr;
                string to = SdfObjectBuffer_ModifierGroupInstance + xpstr;

                WRAPPER_CoreContent.AppendLine(MAspace + dataType + SdfObjectBuffer_GroupInstance + x.ToString() + MAq +
                   SdfObjectBuffer_Smoothing4 + from + MAc + to + MAc + SdfObjectBuffer_GlobalSdfObjSmoothness + MAbe + MAe);

                groupCounter++;
            }

            // Finish object groups and alter remaining sdfs
            for (int x = 0; x < remainingSdfs.Count; x++)
            {
                string gstr = groupCounter.ToString();
                string gmstr = (groupCounter - 1).ToString();

                string from;
                string to;

                if (x == 0)
                {
                    if (postSdfsModifierWrapper.Count == 1)
                    {
                        from = SdfObjectBuffer_ModifierGroupInstance + gstr;
                        to = remainingSdfs[x];
                    }
                    else if (postSdfsModifierWrapper.Count > 1)
                    {
                        from = SdfObjectBuffer_GroupInstance + gmstr;
                        to = remainingSdfs[x];
                    }
                    else
                    {
                        from = remainingSdfs[x];
                        to = remainingSdfs[++x];
                    }
                }
                else
                {
                    from = SdfObjectBuffer_GroupInstance + gmstr;
                    to = remainingSdfs[x];
                }

                WRAPPER_CoreContent.AppendLine(MAspace + dataType + SdfObjectBuffer_GroupInstance + gstr + MAq +
                    SdfObjectBuffer_Smoothing4 + from + MAc + to + MAc + SdfObjectBuffer_GlobalSdfObjSmoothness + MAbe + MAe);

                groupCounter++;
            }
        }


        private static string ConvertUniformType(ISDFEntity.SDFUniformField inputUniform, RMRenderMaster renderMaster)
        {
            string uniform = inputUniform.uniformType.ToString();

            if (inputUniform.uniformType == ISDFEntity.SDFUniformType.Sampler2D)
                uniform = uniform.Replace("Sampler2D", "sampler2D");
            else if (inputUniform.uniformType == ISDFEntity.SDFUniformType.Sampler3D)
                uniform = uniform.Replace("Sampler3D", "sampler3D");
            else if (inputUniform.uniformType == ISDFEntity.SDFUniformType.DefineByRenderType)
                uniform = ReturnCorrectRenderTypePrecision(renderMaster);
            else
            {
                uniform = uniform.ToLower();
                if (inputUniform.lowPrecision)
                    uniform = uniform.Replace("float", "half");
            }
            return uniform;
        }

        private static void CleanConvertorGarbage()
        {
            WRAPPER_BaseWrapper.Clear();
            WRAPPER_CoreContent.Clear();
            WRAPPER_TopVariableDeclarations.Clear();
            WRAPPER_TopMethodDefinitions.Clear();
            WRAPPER_TopPostMethodDefinitions.Clear();
        }

#endif

    }
}