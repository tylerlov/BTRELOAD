using System.IO;

using Raymarcher.RendererData;

namespace Raymarcher.Convertor
{
    using static RMConvertorCommon;

    public static class RMConvertorCore
    {
#if UNITY_EDITOR

        private const string EXTENSION_SHADER = ".shader";
        private const string EXTENSION_INCLUDE = ".cginc";
        private const string EXTENSION_META = ".meta";

        public const string baseRaymarcherShaderPatternHead = "RaymarcherRenderer_" + baseRaymarcherShaderHead;
        public const string basePatternCGSTART = "CGPROGRAM";
        public const string basePatternCGEND = "ENDCG";
        public const string basePatternHLSLSTART = "HLSLPROGRAM";
        public const string basePatternHLSLEND = "ENDHLSL";
        public const string basePatternIncludeMacro = "#include";
        public const string basePatternDefPipelineType = "#define RAYMARCHER_PIPELINE";
        public const string basePatternDefRenderType = "#define RAYMARCHER_TYPE";
        public const string basePatternMacroBlending = "Cull Off ZWrite Off ZTest Always";
        public const string basePatternDefLightCount = "#define RAYMARCHER_LIGHT_COUNT #N#";

        public static bool CreateNewRaymarcherInstance(RMRenderMaster renderMaster, string rmSessionName, out string exception)
        {
            string basePath = GetAssetRelativetPath(renderMaster, true);
            exception = "";

            try
            {
                // Shader Master
                string path = basePath + generatedCodeRenderMastersPath + baseRaymarcherShaderPatternHead.Replace(baseRaymarcherShaderHead, rmSessionName) + EXTENSION_SHADER;
                if (!File.Exists(path))
                    File.Create(path).Dispose();

                RefreshExistingRaymarcherInstance(renderMaster, rmSessionName);

                // Sdf Buffer
                path = basePath + generatedCodeSdfBufferPath + rmSessionName + EXTENSION_INCLUDE;
                if (!File.Exists(path))
                    File.Create(path).Dispose();

                // Materials
                path = basePath + generatedCodeMaterialsPath + rmSessionName + EXTENSION_INCLUDE;
                if (!File.Exists(path))
                    File.Create(path).Dispose();

                return true;
            }
            catch (IOException e)
            {
                exception = e.Message;
                return false;
            }
        }

        public static void RemoveExistingRaymarcherInstance(RMRenderMaster renderMaster, string rmSessionName)
        {
            string fullCorePath = GetAssetRelativetPath(renderMaster, true);

            try
            {
                // Shader Master
                DeleteFile(fullCorePath + generatedCodeRenderMastersPath + baseRaymarcherShaderPatternHead.Replace(baseRaymarcherShaderHead, rmSessionName), false);
                // Sdf Buffer
                DeleteFile(fullCorePath + generatedCodeSdfBufferPath + rmSessionName, true);
                // Materials
                DeleteFile(fullCorePath + generatedCodeMaterialsPath + rmSessionName, true);

                static void DeleteFile(string pathWithoutExtension, bool isInclude)
                {
                    string mainExtension = isInclude ? EXTENSION_INCLUDE : EXTENSION_SHADER;
                    string p = pathWithoutExtension + mainExtension;
                    if (File.Exists(p))
                        File.Delete(p);
                    p += EXTENSION_META;
                    if (File.Exists(p))
                        File.Delete(p);
                }
            }
            catch (IOException e)
            {
                RMDebug.Debug(typeof(RMConvertorCore), "Couldn't remove the current raymarcher instance. Exception: " + e.Message);
                return;
            }
        }

        public static void RefreshExistingRaymarcherInstance(RMRenderMaster renderMaster, string rmSessionName)
        {
            string basePath = GetAssetRelativetPath(renderMaster, true);
            string generatedShadersPath = basePath + generatedCodeRenderMastersPath;
            string basePatternShaderFile = generatedShadersPath + baseRaymarcherShaderPatternHead.Replace(baseRaymarcherShaderHead, rmSessionName) + EXTENSION_SHADER;

            string[] basePatternContentLines = File.ReadAllLines(basePath + renderMasterTemplateSource);

            FileStream fstream = new FileStream(basePatternShaderFile, FileMode.Create);
            StreamWriter fwriter = new StreamWriter(fstream);

            for (int i = 0; i < basePatternContentLines.Length; i++)
            {
                string currentLine = basePatternContentLines[i];

                if (i == 0)
                {
                    fwriter.WriteLine(currentLine.Replace(baseRaymarcherShaderHead, rmSessionName));
                    continue;
                }

                if (currentLine.Contains(basePatternCGSTART) && renderMaster.CompiledTargetPipeline == RMRenderMaster.TargetPipeline.HDRP)
                    currentLine = basePatternHLSLSTART;
                if (currentLine.Contains(basePatternCGEND) && renderMaster.CompiledTargetPipeline == RMRenderMaster.TargetPipeline.HDRP)
                    currentLine = basePatternHLSLEND;

                if (currentLine.Contains(basePatternDefRenderType))
                {
                    switch (renderMaster.RenderingData.CompiledRenderType)
                    {
                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality:
                            currentLine += "_QUALITY";
                            break;

                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Standard:
                            currentLine += "_STANDARD";
                            break;

                        case RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant:
                            currentLine += "_PERFORMANT";
                            break;
                    }
                }

                if (currentLine.Contains(basePatternDefPipelineType))
                {
                    switch (renderMaster.CompiledTargetPipeline)
                    {
                        case RMRenderMaster.TargetPipeline.BuiltIn:
                            currentLine += "_BUILTIN";
                            break;

                        case RMRenderMaster.TargetPipeline.URP:
                            currentLine += "_URP";
                            break;

                        case RMRenderMaster.TargetPipeline.HDRP:
                            currentLine += "_HDRP";
                            break;
                    }
                }

                if (currentLine.Contains(basePatternIncludeMacro))
                    currentLine = currentLine.Replace("\\", sep).Replace("/", sep);

                if (currentLine.Contains(basePatternSdfObjectBufferInclude))
                    currentLine = currentLine.Replace(baseRaymarcherShaderHead, rmSessionName);
                if (currentLine.Contains(basePatternMaterialBufferInclude))
                    currentLine = currentLine.Replace(baseRaymarcherShaderHead, rmSessionName);

                if (currentLine.Contains(basePatternDefLightCount))
                {
                    if (renderMaster.MasterLights.UseAdditionalLights && renderMaster.MasterLights.AdditionalLightsCollection.Count > 0)
                        currentLine = currentLine.Replace("#N#", (renderMaster.MasterLights.AdditionalLightsCollection.Count * 3).ToString());
                    else
                        currentLine = "// No additional lights detected. RAYMARCHER_LIGHT_COUNT has been removed";
                }

                fwriter.WriteLine(currentLine);
            }

            fwriter.Dispose();
            fstream.Dispose();
        }

#endif
    }
}