using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine.Rendering;

namespace TND.FSR
{
    public class RemoveDefines : AssetPostprocessor
    {
        const string relativeFilePathWithTypeExtension = "The Naked Dev/FSR";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                           string[] movedAssets, string[] movedFromAssetPaths)
        {
            try
            {
                if (deletedAssets != null && deletedAssets.Contains($"Assets/{relativeFilePathWithTypeExtension}"))
                {
                    PipelineDefines.RemoveDefine("TND_FSR3");
                    PipelineDefines.RemoveDefine("TND_FSR1");
                    PipelineDefines.RemoveDefine("UNITY_BIRP");
                    PipelineDefines.RemoveDefine("UNITY_HDRP");
                    PipelineDefines.RemoveDefine("UNITY_URP");
                }
            }
            catch { }
        }
    }

    [InitializeOnLoad]
    public class PipelineDefines
    {
        enum PipelineType
        {
            BIRP,
            URP,
            HDRP
        }

        static PipelineDefines()
        {
            UpdateDefines();
        }

        /// <summary>
        /// Update the unity pipeline defines for URP
        /// </summary>
        static void UpdateDefines()
        {
            var pipeline = GetPipeline();

            if (pipeline == PipelineType.URP)
            {
                AddDefine("UNITY_URP");
            }
            else
            {
                RemoveDefine("UNITY_URP");
            }
            if (pipeline == PipelineType.HDRP)
            {
                AddDefine("UNITY_HDRP");
            }
            else
            {
                RemoveDefine("UNITY_HDRP");
            }
            if (pipeline == PipelineType.BIRP)
            {
                AddDefine("UNITY_BIRP");
            }
            else
            {
                RemoveDefine("UNITY_BIRP");
            }

            AddDefine("TND_FSR1");
            AddDefine("TND_FSR3");
        }

        static PipelineType GetPipeline()
        {
#if UNITY_2019_1_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                var srpType = GraphicsSettings.renderPipelineAsset.GetType().ToString();
                //HDRP
                if (srpType.Contains("HDRenderPipelineAsset"))
                {
                    return PipelineType.HDRP;
                }
                //URP
                else
                {
                    return PipelineType.URP;
                }
            }
#endif

            //BIRP
            return PipelineType.BIRP;
        }

        static void AddDefine(string define)
        {
            var definesList = GetDefines();
            if (!definesList.Contains(define))
            {
                definesList.Add(define);
                SetDefines(definesList);
            }
        }

        public static void RemoveDefine(string define)
        {
            var definesList = GetDefines();
            if (definesList.Contains(define))
            {
                definesList.Remove(define);
                SetDefines(definesList);
            }
        }

        public static List<string> GetDefines()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);

#if UNITY_2022_1_OR_NEWER
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
#endif
            return defines.Split(';').ToList();
        }

        public static void SetDefines(List<string> definesList)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            string defines = string.Join(";", definesList.ToArray());

#if UNITY_2022_1_OR_NEWER
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
#endif
        }
    }
}
