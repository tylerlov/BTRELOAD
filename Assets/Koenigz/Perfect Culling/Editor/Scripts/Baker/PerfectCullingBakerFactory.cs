// Perfect Culling (C) 2021 Patrick König
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Koenigz.PerfectCulling
{
    public class PerfectCullingBakerFactory
    {
        public static PerfectCullingBaker CreateBaker(PerfectCullingBakeSettings bakeSettings)
        {
#if !UNITY_EDITOR_WIN
            return new PerfectCullingBakerUnity(bakeSettings);
#else
            if (PerfectCullingSettings.Instance.useUnityForRendering)
            {
                return new PerfectCullingBakerUnity(bakeSettings);
            } 
            
            if (PerfectCullingSettings.Instance.useNativeVulkanForRendering)
            {

                if (PerfectCullingResourcesLocator.Instance.LookupNativeVulkanLib() == null)
                {
                    Debug.LogError("Missing NativeVulkanLib. You can fix it by manually assigning \"pc_renderer_vulkan\" it in the PerfectCullingResourcesLocator ScriptableObject.", PerfectCullingResourcesLocator.Instance);

                    return null;
                }

                return new PerfectCullingBakerNativeVulkanWin64(bakeSettings);
            }
            
            if (PerfectCullingResourcesLocator.Instance.LookupNativeLib() == null)
            {
                Debug.LogError("Missing NativeLib. You can fix it by manually assigning \"pc_renderer\" it in the PerfectCullingResourcesLocator ScriptableObject.", PerfectCullingResourcesLocator.Instance);

                return null;
            }

            return new PerfectCullingBakerNativeWin64(bakeSettings);
#endif
        }
    }
}