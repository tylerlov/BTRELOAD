// Microsoft
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Renders colored multiple graphs (stacked or next to each other) based on performance data received from subscribed
    /// performance providers on the same GameObject. The colors of individual graphs are customizable, and the renderer
    /// automatically manages the display of legends associated with each graph.
    /// </summary>
    /// <remarks>
    /// The <see cref="ColoredMultiGraphRenderer"/> extends the functionality of <see cref="AMultiGraphRenderer"/> to include
    /// the ability to customize the colors of individual graphs. It subscribes to performance providers, dynamically adjusts
    /// the rendering for multiple graphs, and updates the display of legend images. The colors for each graph can be set
    /// through the 'Colors' property.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class ColoredMultiGraphRenderer : AMultiGraphRenderer
    {
        /// <summary>
        /// Stores the colors for each graph.
        /// </summary>
        [SerializeField]
        private List<Color> colors = new List<Color>();

        /// <summary>
        /// Stores the colors for each graph.
        /// </summary>
        public List<Color> Colors { get => this.colors; }

        /// <summary>
        /// The property identifier for the graph colors in the shader.
        /// </summary>
        public static readonly int GraphColorsPropertyId = Shader.PropertyToID("_GraphColors");

        /// <summary>
        /// The property identifier for the count of graphs in the shader.
        /// </summary>
        public static readonly int GraphCountPropertyId = Shader.PropertyToID("_GraphCount");

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnInitializeGraph(Shader _Shader)
        {
            // Call the base method.
            base.OnInitializeGraph(_Shader);

            // Pass the graph properties to the shader.
            this.Target.material.SetColorArray(ColoredMultiGraphRenderer.GraphColorsPropertyId, this.colors);
            this.Target.material.SetFloat(ColoredMultiGraphRenderer.GraphCountPropertyId, this.Provider.Count);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void RefreshGraph()
        {
            // Call the base method.
            base.RefreshGraph();

            // Pass the graph properties to the shader.
            this.Target.material.SetColorArray(ColoredMultiGraphRenderer.GraphColorsPropertyId, this.colors);
        }

        /// <summary>
        /// Called when a new performance data value is received.
        /// Updates the displayed values and legend images.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        public override void OnNext(PerformanceData _Next)
        {
            // Call the base method.
            base.OnNext(_Next);

            // Update the legend images.
            this.UpdateLegends();
        }

        /// <summary>
        /// Update the legend images.
        /// </summary>
        private void UpdateLegends()
        {
            // Iterate over all legend images.
            for (int i = 0; i < this.LegendImages.Count; i++)
            {
                // Set the color of the legend image.
                this.LegendImages[i].color = this.colors[i];
            }
        }
    }
}
