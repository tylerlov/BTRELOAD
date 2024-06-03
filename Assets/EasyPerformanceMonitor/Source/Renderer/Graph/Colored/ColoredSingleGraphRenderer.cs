// Microsoft
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Renders a colored graph based on performance data received from subscribed performance providers on the same GameObject.
    /// </summary>
    /// <remarks>
    /// The <see cref="ColoredSingleGraphRenderer"/> extends the functionality of <see cref="ASingleGraphRenderer"/> to include
    /// color customization for the rendered graph. It subscribes to performance providers, maintains historical values, and
    /// updates the displayed graph and legend image accordingly. The graph color can be configured through the 'Color' property.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class ColoredSingleGraphRenderer : ASingleGraphRenderer
    {
        /// <summary>
        /// Gets the color for the graph.
        /// </summary>
        [SerializeField]
        private Color color;

        /// <summary>
        /// Gets the color for the graph.
        /// </summary>
        public Color Color { get => color; }

        /// <summary>
        /// Stores the historical values for each performance provider.
        /// </summary>
        private float[] values;

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        private float minValue;

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        private float maxValue;

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        private float meanValue;

        /// <summary>
        /// The property identifier the color in the shader.
        /// </summary>
        public static readonly int ColorPropertyId = Shader.PropertyToID("_GraphColor");

        /// <summary>
        /// Initializes the rated graph renderer, subscribes to performance providers, and sets up initial values.
        /// </summary>
        protected override void Awake()
        {
            // Call the base method.
            base.Awake();

            // Initialize the values array.
            this.values = new float[this.GraphValues];
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnInitializeGraph(Shader _Shader)
        {
            // Call the base method.
            base.OnInitializeGraph(_Shader);

            // Pass the graph properties to the shader.
            this.Target.material.SetColor(ColoredSingleGraphRenderer.ColorPropertyId, this.color);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void RefreshGraph()
        {
            // Call the base method.
            base.RefreshGraph();

            // Pass the graph properties to the shader.
            this.Target.material.SetColor(ColoredSingleGraphRenderer.ColorPropertyId, this.color);

            // Create resized value array.
            float[] var_ValueArray = new float[this.GraphValues];

            // Copy current values to new array.
            int var_Difference = this.GraphValues - this.values.Length;
            int var_StartIndex = this.GraphValues > this.values.Length ? var_Difference : 0;
            for (int i = var_StartIndex; i < this.GraphValues; i++)
            {
                var_ValueArray[i] = this.values[i - var_Difference];
            }

            // Assign to the current value array.
            this.values = var_ValueArray;
        }

        /// <summary>
        /// Called when a new performance data value is received.
        /// Updates the displayed values and legend image.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        public override void OnNext(PerformanceData _Next)
        {
            // Add the new value to the graph.
            this.AddValue((float) _Next.Value);

            // Update the graph.
            this.UpdateGraph();

            // Update the legend image.
            this.UpdateLegend();
        }

        /// <summary>
        /// Adds a new performance value to the historical values.
        /// </summary>
        /// <param name="_Value">The new performance value.</param>
        private void AddValue(float _Value)
        {
            // Store the min and max values.
            float var_MinValue = float.MaxValue;
            float var_MaxValue = 0;

            // Store the mean value.
            float var_MeanValue = 0;
            int var_MeanCounter = 0;

            // Calculate the graph values.
            int var_GraphValues = this.GraphValues;

            // Push new values to the end of the array and shift the array to the left.
            for (int i = 0; i < var_GraphValues; i++)
            {
                // Push to the left.
                if (i < var_GraphValues - 1)
                {
                    this.values[i] = this.values[i + 1];
                }
                // Store the current value.
                else
                {
                    this.values[i] = _Value;
                }

                // Find the min and max value.
                if (this.values[i] < var_MinValue)
                {
                    var_MinValue = this.values[i];
                }
                if (this.values[i] > var_MaxValue)
                {
                    var_MaxValue = this.values[i];
                }

                // Increase the mean value if the value is greater than zero.
                if (this.values[i] > 0)
                {
                    var_MeanValue += this.values[i];
                    var_MeanCounter += 1;
                }
            }

            // Assign the min, mean, and max value.
            this.minValue = var_MinValue;
            this.meanValue = var_MeanCounter > 0 ? var_MeanValue / var_MeanCounter : 0;
            this.maxValue = var_MaxValue;
        }

        /// <summary>
        /// Passes the value array to the shader for graph rendering.
        /// </summary>
        private void UpdateGraph()
        {
            // Normalize values to the range [0, 1].
            float[] var_NormalizedValues = new float[this.values.Length];

            for (int i = 0; i < this.values.Length; i++)
            {
                var_NormalizedValues[i] = this.values[i] / this.maxValue;
            }

            // Pass the graph values to the shader.
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, var_NormalizedValues);
        }

        /// <summary>
        /// Update the legend image based.
        /// </summary>
        private void UpdateLegend()
        {
            // Check if there is a legend image.
            if (this.LegendImage == null)
            {
                return;
            }

            // Select the color.
            Color var_Color = this.color;

            // Update the legend image.
            this.LegendImage.color = var_Color;
        }
    }
}
