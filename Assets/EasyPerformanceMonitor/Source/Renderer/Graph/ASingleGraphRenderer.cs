// Microsoft
using System.Reflection;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Abstract implementation of a graph renderer displaying a single graph on a Unity UI Image component
    /// using data provided by a performance provider on the same GameObject.
    /// </summary>
    /// <remarks>
    /// The <see cref="ASingleGraphRenderer"/> class extends the functionality of a basic graph renderer
    /// to support rendering a single graph.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public abstract class ASingleGraphRenderer : AGraphRenderer
    {
        /// <summary>
        /// Gets or sets the Unity UI Image used to display the legend color for the single graph in the renderer.
        /// </summary>
        [SerializeField]
        private UnityEngine.UI.Image legendImage;

        /// <summary>
        /// Gets or sets the Unity UI Image used to display the legend color for the single graph in the renderer.
        /// </summary>
        public UnityEngine.UI.Image LegendImage { get => this.legendImage; }
    }
}
