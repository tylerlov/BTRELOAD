using GraphProcessor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectDawn.Impostor
{
    public struct AllowAsyncCompilationScope : System.IDisposable
    {
#if UNITY_EDITOR
        bool m_CachedAllowAsyncCompilation;
#endif

        public AllowAsyncCompilationScope(bool allowAsyncCompilation)
        {
#if UNITY_EDITOR
            m_CachedAllowAsyncCompilation = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = allowAsyncCompilation;
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = m_CachedAllowAsyncCompilation;
#endif
        }
    }
}