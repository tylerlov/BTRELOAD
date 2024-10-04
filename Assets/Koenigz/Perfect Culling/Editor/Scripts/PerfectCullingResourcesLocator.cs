// Perfect Culling (C) 2021 Patrick König
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Koenigz.PerfectCulling
{
    public class PerfectCullingResourcesLocator : ScriptableObject
    {
        private static PerfectCullingResourcesLocator m_instance;

        public static PerfectCullingResourcesLocator Instance
        {
            get
            {
                if (m_instance == null)
                {
                    //PerfectCullingResourcesLocator[] tmp = Resources.LoadAll<PerfectCullingResourcesLocator>(PerfectCullingConstants.ResourcesFolder);
                    List<PerfectCullingResourcesLocator> tmp = PerfectCullingEditorUtil.LoadAssets<PerfectCullingResourcesLocator>();

                    //if (tmp.Length == 0)
                    if (tmp.Count == 0)
                    {
                        return null;
                    }
                    
                    m_instance = tmp[0];
                }

                return m_instance;
            }
        }
        
        [Header("Internally used references. Please do not modify!")]
        public ComputeShader PointExtractorComputeShader;
        public Material UnlitTagMaterial;
        [SerializeField] private UnityEngine.Object NativeLib;
        [SerializeField] private UnityEngine.Object NativeVulkanLib;

        public PerfectCullingSettings Settings;
        public PerfectCullingColorTable ColorTable;

        // HACK: Lookup workarounds for Unity 2022+ because the reference appears to get lost...
        public UnityEngine.Object LookupNativeLib()
        {
#if UNITY_EDITOR
            if (NativeLib == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("pc_renderer");

                foreach (var guid in guids)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                    if (assetPath.ToLower().EndsWith("pc_renderer.dll"))
                    {
                        NativeLib = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        UnityEditor.EditorUtility.SetDirty(this);
                        break;
                    }
                }
            }
#endif
            return NativeLib;
        }
        
        public UnityEngine.Object LookupNativeVulkanLib()
        {
#if UNITY_EDITOR
            if (NativeVulkanLib == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("pc_renderer_vulkan");

                foreach (var guid in guids)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                    if (assetPath.ToLower().EndsWith("pc_renderer_vulkan.dll"))
                    {
                        NativeVulkanLib = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        UnityEditor.EditorUtility.SetDirty(this);
                        break;
                    }
                }
            }
#endif
            return NativeVulkanLib;
        }
    }
}