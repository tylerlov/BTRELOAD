using System.IO;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Raymarcher.Convertor
{
    public static class RMConvertorExtensions
    {
        public static string CheckPrecision(this string str, RMRenderMaster renderMaster)
        {
            if (renderMaster.RenderingData.CompiledRenderType == RendererData.RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant)
                return str.Replace("float", "half");
            else
                return str;
        }
    }

    public static class RMConvertorCommon
    {

#if UNITY_EDITOR

        public static readonly string sep = Path.DirectorySeparatorChar.ToString();

        // Global convertor shortcuts
        //
        public const string MAe = ";";         // Macro for Line End ;
        public const string MAc = ",";         // Macro for Coma ,
        public const string MAb = "(";         // Macro for Starting Bracket )
        public const string MAbe = ")";        // Macro for Ending Bracket )
        public const string MAbs = "[";         // Macro for Starting Bracket ]
        public const string MAbse = "]";        // Macro for Ending Bracket ]
        public const string MAq = " = ";       // Macro for Equation =
        public const string MAs = " ";         // Macro for small space
        public const string MAspace = "    ";  // Macro for large space
        public const string MAnewLine = "\n";  // Macro for NewLine
        public const string MAbc = "{";        // Macro for Starting Curvy Bracket {
        public const string MAbce = "}";       // Macro for Ending Curvy Bracket }
        //
        public const string Data_Uniform = "uniform ";
        public const string Data_Matrix4x4 = "float4x4 ";
        public const string Data_Matrix4x4Half = "half4x4 ";
        public const string Data_Matrix2x4 = "float2x4 ";
        public const string Data_Vector4 = "float4 ";
        public const string Data_Vector4Half = "half4 ";
        public const string Data_Vector3 = "float3 ";
        public const string Data_Vector3Half = "half3 ";
        public const string Data_Vector2 = "float2 ";
        public const string Data_Vector2Half = "half2 ";
        public const string Data_Vector1 = "float ";
        public const string Data_Vector1Half = "half ";
        public const string Data_Sampler2DArray = "Texture2DArray ";
        public const string Data_SamplerState = "SamplerState ";
        public const string Data_Struct = "struct ";
        public const string Data_StructuredBuffer = "StructuredBuffer";
        //-------------------------------------------------------------


        public static readonly string CGPath = $"{sep}CG{sep}";

        public static readonly string renderMasterTemplateSource = $"{CGPath}CoreLibrary{sep}RMTemplate_BasePattern.txt";

        public static readonly string generatedCodeMainPath = $"{CGPath}GeneratedCode{sep}";
        public static readonly string generatedCodeRenderMastersPath = $"{generatedCodeMainPath}RenderMasters{sep}";
        public static readonly string generatedCodeMaterialsPath = $"{generatedCodeMainPath}MaterialBuffers{sep}RM_MaterialBuffer_";
        public static readonly string generatedCodeSdfBufferPath = $"{generatedCodeMainPath}SdfObjectBuffers{sep}RM_SdfObjectBuffer_";

        public const string baseRaymarcherShaderHead = "SESSION_NAME";
        public static readonly string basePatternSdfObjectBufferInclude = $"RM_SdfObjectBuffer_{baseRaymarcherShaderHead}.cginc";
        public static readonly string basePatternMaterialBufferInclude = $"RM_MaterialBuffer_{baseRaymarcherShaderHead}.cginc";

        public static string GetAssetRelativetPath(in MonoBehaviour sender, in bool getDirectoryName = false)
        {
            string assetPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(sender));
            string combinedAssetPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            combinedAssetPath = combinedAssetPath.Replace("/", sep).Replace("\\", sep);

            if (getDirectoryName)
                return Path.GetDirectoryName(combinedAssetPath);
            else
                return combinedAssetPath;
        }

#endif

    }
}