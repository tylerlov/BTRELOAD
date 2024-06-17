using UnityEngine;

namespace Raymarcher.RendererData
{
    public interface IRMRenderMasterDependency
    {
        public RMRenderMaster RenderMaster { get; }

#if UNITY_EDITOR
        public void SetupDependency(in RMRenderMaster renderMaster);

        public void DisposeDependency();
#endif

        public void UpdateDependency(in Material raymarcherSessionMaterial);
    }
}