using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace OccaSoftware.LocalGI.Runtime
{
    /// <summary>
    /// Represents the data needed for rendering the environment dffuse irradiance map.
    /// </summary>
    public class EnvironmentData
    {
        private RenderTexture environmentMap;

        /// <summary>
        /// Gets the environment map that is used to render reflections.
        /// </summary>
        public RenderTexture EnvironmentMap
        {
            get
            {
                if (environmentMap == null)
                {
                    // Create a new environment map if it doesn't already exist
                    environmentMap = new RenderTexture(
                        Common.ProbeResolution,
                        Common.ProbeResolution,
                        0,
                        DefaultFormat.HDR
                    );
                    environmentMap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
                    environmentMap.anisoLevel = 0;
                    environmentMap.antiAliasing = 1;
                    environmentMap.autoGenerateMips = false;
                    environmentMap.filterMode = FilterMode.Bilinear;
                    environmentMap.wrapMode = TextureWrapMode.Repeat;
                    environmentMap.hideFlags = HideFlags.HideAndDontSave;
                    environmentMap.Create();

                    // Clear the environment map to black
                    RenderTexture activeRenderTexture = RenderTexture.active;
                    RenderTexture.active = environmentMap;
                    GL.Clear(true, true, Color.black);
                    RenderTexture.active = activeRenderTexture;
                }

                return environmentMap;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentData"/> class.
        /// </summary>
        public EnvironmentData() { }
    }
}
