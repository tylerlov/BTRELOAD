// Perfect Culling (C) 2021 Patrick König
//

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Koenigz.PerfectCulling
{
    public static class PerfectCullingUtil
    {
        public static string FormatNumber(int number)
        {
            if (number >= 100000000) {
                return (number / 1000000f).ToString("0.#M");
            }
            else if (number >= 1000000) {
                return (number / 1000000f).ToString("0.##M");
            }
            else if (number >= 100000) {
                return (number / 1000f).ToString("0.#k");
            }
            else if (number >= 10000) {
                return (number / 1000f).ToString("0.##k");
            }

            return number.ToString("#,0");
        }
        
        // Allows to customize the behaviour easily. For instance could assign it to a different layer, etc. instead.
        public static void ToggleRenderer(Renderer r, bool visible, bool forceNullCheck, ShadowCastingMode defaultShadowCastingMode)
        {
            if (forceNullCheck && r == null)
            {
                // We forced null checks for a reason and are aware. No need to output anything.
                return;
            }
            
#if UNITY_EDITOR
            if (r == null)
            {
                // This is an unexpected null renderer. We want to log an error.
                
                Debug.LogError("Encountered null renderer.");
                
                return;
            }
#endif
            
#pragma warning disable 162
            switch (PerfectCullingConstants.ToggleRenderMode)
            {
                case PerfectCullingRenderToggleMode.ToggleRendererComponent:
                    r.enabled = visible;
                    break;
                
                case PerfectCullingRenderToggleMode.ToggleShadowcastMode:
                    if (defaultShadowCastingMode == ShadowCastingMode.Off || defaultShadowCastingMode == ShadowCastingMode.TwoSided)
                    {
                        // We don't care about shadows so we might as well disable the entire Renderer.
                        
#if UNITY_2019_3_OR_NEWER
                        goto case PerfectCullingRenderToggleMode.ToggleForceRenderingOff;
#else
                        goto case PerfectCullingRenderToggleMode.ToggleRendererComponent;
#endif
                    }
                    
                    r.shadowCastingMode = visible ? defaultShadowCastingMode : ShadowCastingMode.ShadowsOnly;
                    break;

                case PerfectCullingRenderToggleMode.ToggleForceRenderingOff:
#if !UNITY_2019_3_OR_NEWER
                    // Unsupported before Unity 2019. This is the next best thing we can do (performance wise).
                    // I don't like that this happens silently but printing a warning when it is unsupported doesn't help either.
                    goto case PerfectCullingRenderToggleMode.ToggleShadowcastMode;
#else
                    r.forceRenderingOff = !visible;
#endif
                    break;
                
                default:
                    throw new System.InvalidOperationException();
                    break;
            }
#pragma warning restore 162
        }
    }
}