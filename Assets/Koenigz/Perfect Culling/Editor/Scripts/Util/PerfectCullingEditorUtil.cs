// Perfect Culling (C) 2021 Patrick König
//

#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Koenigz.PerfectCulling
{
    public static class PerfectCullingEditorUtil
    {
        public static bool SaveModifiedScenesIfUserWantsTo(Scene[] scenes)
        {           
#pragma warning disable CS0162
            if (PerfectCullingConstants.DisableConfirmationDialogs)
            {
                PerfectCullingLogger.Log("Saving scenes.");
                
                UnityEditor.SceneManagement.EditorSceneManager.SaveScenes(scenes);
                
                return true;
            }
#pragma warning restore CS0162
            
            return UnityEditor.SceneManagement.EditorSceneManager.SaveModifiedScenesIfUserWantsTo(scenes);
        }
        
        public static bool DisplayDialog(string title, string message, string ok, string cancel)
        {
#pragma warning disable CS0162
            if (PerfectCullingConstants.DisableConfirmationDialogs)
            {
                PerfectCullingLogger.Log($"Dialog: '{title}'");

                return true;
            }
#pragma warning restore CS0162
            
            return UnityEditor.EditorUtility.DisplayDialog(title, message, ok, cancel);
        }
        
        public static bool DisplayDialog(string title, string message, string ok)
        {
#pragma warning disable CS0162
            if (PerfectCullingConstants.DisableConfirmationDialogs)
            {
                PerfectCullingLogger.Log($"Dialog: '{title}'");
                
                return true;
            }
#pragma warning restore CS0162
            
            return UnityEditor.EditorUtility.DisplayDialog(title, message, ok);
        }
        
        public static string FormatSeconds(double seconds)
        {
            System.TimeSpan ts = System.TimeSpan.FromSeconds(seconds);

            return ts.ToString(@"hh\:mm\:ss");
        }
        
        public static HashSet<PerfectCullingBakeGroup> CreateBakeGroupsForRenderers(List<Renderer> inputRenderers, System.Func<Renderer, bool> filter, PerfectCullingBakingBehaviour attachedBakingBehaviour = null, bool ignoreDisabledLodGroups = true)
        {
            // Filter all unsupported renderers
            inputRenderers.RemoveAll(rend => filter != null && !filter.Invoke(rend));

            // We are going to manipulate this. So we clone it just in case. We also use a Hashset to prevent double insertions.
            HashSet<Renderer> sceneRenderers = new HashSet<Renderer>(inputRenderers);

            // Find all the objects in the scene
            HashSet<LODGroup> lodGroups = new HashSet<LODGroup>(UnityEngine.Object.FindObjectsOfType<LODGroup>());
            HashSet<PerfectCullingMonoGroup> perfectCullingGroups = new HashSet<PerfectCullingMonoGroup>(FindMonoGroupsForBakingBehaviour(attachedBakingBehaviour));

            lodGroups.RemoveWhere((lodGroup) =>
            {
                if (ignoreDisabledLodGroups && !lodGroup.enabled)
                {
                    return true;
                }
                
                foreach (LOD lod in lodGroup.GetLODs())
                {
                    foreach (Renderer r in lod.renderers)
                    {
                        if (sceneRenderers.Contains(r))
                        {
                            // Keep this LODGroup. We selected its renderers.
                            return false;
                        }
                    }
                }
                
                return true;
            });
            
            // Our output list
            HashSet<PerfectCullingBakeGroup> result = new HashSet<PerfectCullingBakeGroup>(new PerfectCullingBakeGroupComparer());

            // We are going to remove already processed elements from our renderers list.
            //
            // Priority goes as follow:
            // - PerfectCullingGroup
            // - LODGroup
            // - Anything else

            // PerfectCullingGroup
            foreach (PerfectCullingMonoGroup perfectCullingGroup in perfectCullingGroups)
            {
                foreach (Renderer renderer in perfectCullingGroup.Renderers)
                {
                    sceneRenderers.Remove(renderer);
                }
                
                result.Add(new PerfectCullingBakeGroup()
                {
                    renderers = perfectCullingGroup.Renderers.ToArray(), // Renderers filters null renderers automatically
                    unityBehaviours = perfectCullingGroup.UnityBehaviours.ToArray(), // UnityBehaviours filters null renderers automatically
                    groupType = PerfectCullingBakeGroup.GroupType.User
                });
            }

            // LODGroup
            foreach (LODGroup lodGroup in lodGroups)
            {
                HashSet<Renderer> renderersInLodGroup = new HashSet<Renderer>();
                    
                foreach (LOD lod in lodGroup.GetLODs())
                {
                    foreach (Renderer renderer in lod.renderers)
                    {
                        if (renderer == null)
                        {
                            PerfectCullingLogger.LogWarning($"Found null renderer in LODGroup: {lodGroup.name}. Selecting the LODGroup might remove the invalid renderer(s) for you.", lodGroup.gameObject);
                            
                            continue;
                        }
                        
                        renderersInLodGroup.Add(renderer);
                        sceneRenderers.Remove(renderer);
                    }
                }
                
                if (renderersInLodGroup.Count == 0)
                {
                    continue;
                }
                
                result.Add(new PerfectCullingBakeGroup()
                {
                    renderers = renderersInLodGroup.ToArray(),
                    groupType = PerfectCullingBakeGroup.GroupType.LOD
                });
            }
            
            // Remaining renderers
            foreach (Renderer renderer in sceneRenderers)
            {
                result.Add(new PerfectCullingBakeGroup()
                {
                    renderers = new Renderer[] { renderer },
                    groupType = PerfectCullingBakeGroup.GroupType.Other
                });
            }
            
            return result;
        }

        public static PerfectCullingMonoGroup[] FindMonoGroupsForBakingBehaviour(PerfectCullingBakingBehaviour attachedBakingBehaviour)
        {
            return UnityEngine.Object.FindObjectsOfType<PerfectCullingMonoGroup>().Where((group) =>
            {
                if (group.restrictToBehaviours.Length == 0 || attachedBakingBehaviour == null)
                {
                    return true;
                }

                return System.Array.IndexOf(group.restrictToBehaviours, attachedBakingBehaviour) >= 0;
            }).ToArray();
        }

        public static float FindValidDivisorCloseToUserProvided(float userProvidedDivisor, float volumeSize)
        {
            float bestFit = 0;
            
            for (float i = 0; i < volumeSize; i += 1f / 4f)
            {
                if (volumeSize % i == 0)
                {
                    if ((bestFit <= 0) ||
                        Mathf.Abs(i - userProvidedDivisor) < Mathf.Abs(bestFit - userProvidedDivisor))
                    {
                        bestFit = i;
                    }
                }
            }

            if (bestFit <= 0)
            {
                return volumeSize;
            }

            return bestFit;
        }

        // Keywords that we use to assume the material should be transparent.
        private static readonly string[] transparentShaderKeywordHints = new string[]
        {
            "_ALPHATEST_ON",
            "ALPHACLIPPING_ON"
        };

        public const byte OPAQUE_RENDER_COLOR = 0;
        public const byte TRANSPARENT_RENDER_COLOR = 128;

            
        public static bool IsMaterialTransparent(Material mat)
        {
#pragma warning disable 162
            if (mat == null)
            {
                return false;
            }
            
            if (!PerfectCullingSettings.Instance.renderTransparency)
            {
                return false;
            }

            // Check whether this material is forced to render transparent or opaque.
            string nameLower = mat.name.ToLower();

            if (nameLower.Contains("pc_trans"))
            {
                return true;
            }
            
            if (nameLower.Contains("pc_opaque"))
            {
                return false;
            }
            
            // It's pretty hard to detect transparent materials. Lets use some shader keywords as a hint.

            foreach (var keyword in transparentShaderKeywordHints)
            {
                if (mat.IsKeywordEnabled(keyword))
                {
                    return true;
                }
            }
            
            return mat.renderQueue >= 2450;
#pragma warning restore 162
        }

        public static bool TryGetAssetBakeSize(PerfectCullingBakeData bakeData, out float bakeSizeMb)
        {  
            bakeSizeMb = 0f;
            
            if (bakeData == null)
            {
                return false;
            }
            
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(bakeData);
            
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            System.IO.FileInfo fi;

            try
            {
                fi = new FileInfo(assetPath);

                if (!fi.Exists)
                {
                    return false;
                }
            }
            catch (System.Exception)
            {
                return false;
            }

            bakeSizeMb = (float) fi.Length * 1e-6f;

            return true;
        }
        
        public static List<T> LoadAssets<T>() 
            where T : UnityEngine.Object
        {
            string filter = $"t:{typeof(T).Name}";
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets(filter);

            List<T> assets = new List<T>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                
                assets.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path));
            }

            return assets;
        }
    }
}

#endif