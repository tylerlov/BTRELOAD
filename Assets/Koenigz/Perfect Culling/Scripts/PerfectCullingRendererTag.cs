// Perfect Culling (C) 2021 Patrick König
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Koenigz.PerfectCulling
{
    [RequireComponent(typeof(Renderer))]
    public class PerfectCullingRendererTag : MonoBehaviour
    {
        public bool ExcludeRendererFromBake
        { 
            get => excludeRendererFromBake;
            
            set
            {
                excludeRendererFromBake = value;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        public bool RenderDoubleSided 
        {
            get => renderDoubleSided;
            
            set
            {
                renderDoubleSided = value;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        public EBakeRenderMode ForcedBakeRenderMode
        { 
            get => forcedBakeRenderMode;
            
            set
            {
                forcedBakeRenderMode = value;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        [SerializeField] private bool excludeRendererFromBake = false;
        [SerializeField] private bool renderDoubleSided = false;

        [SerializeField] private EBakeRenderMode forcedBakeRenderMode = EBakeRenderMode.None;
    }
}