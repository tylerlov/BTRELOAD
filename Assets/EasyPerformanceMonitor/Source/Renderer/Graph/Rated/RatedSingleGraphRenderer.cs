// Microsoft
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Platform;
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Renders a single rated graph based on the performance data passed by the subscribed performance providers 
    /// on the same GameObject. The displayed data in the graph will be colored based on the thresholds.
    /// </summary>
    [Obfuscation(Exclude = true)]
    public class RatedSingleGraphRenderer : ASingleGraphRenderer
    {
        /// <summary>
        /// Indicates whether higher values are considered good.
        /// </summary>
        [SerializeField]
        private bool highIsGood = true;

        /// <summary>
        /// Gets a value indicating whether higher values are considered good.
        /// </summary>
        public bool HighIsGood { get => highIsGood; }

        /// <summary>
        /// Gets the list of colors for each section.
        /// </summary>
        [SerializeField]
        private List<Color> colors = new List<Color>();

        /// <summary>
        /// Gets the list of colors for each section.
        /// </summary>
        public List<Color> GoodColor { get => colors; }

        /// <summary>
        /// Gets the threshold for each section for the desktop graph.
        /// </summary>
        [SerializeField]
        private List<float> desktopThresholds;

        /// <summary>
        /// Gets the threshold for each section for the desktop graph.
        /// </summary>
        public List<float> DesktopThresholds { get => desktopThresholds; }

        /// <summary>
        /// Gets the threshold for each section for the mobile graph.
        /// </summary>
        [SerializeField]
        private List<float> mobileThresholds;

        /// <summary>
        /// Gets the threshold for each section for the mobile graph.
        /// </summary>
        public List<float> MobileThresholds { get => mobileThresholds; }

        /// <summary>
        /// Gets the threshold for each section for the console graph.
        /// </summary>
        [SerializeField]
        private List<float> consoleThresholds;

        /// <summary>
        /// Gets the threshold for each section for the console graph.
        /// </summary>
        public List<float> ConsoleThresholds { get => consoleThresholds; }

        /// <summary>
        /// Stores the current threshold for each section for either desktop, mobile, or console.
        /// </summary>
        private float[] currentThresholds;

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
        /// The property identifier for indicating whether higher values are considered good in the shader.
        /// </summary>
        public static readonly int HighIsGoodPropertyId = Shader.PropertyToID("_HighIsGood");

        /// <summary>
        /// The property identifier for the list of thresholds in the shader.
        /// </summary>
        public static readonly int ThresholdsPropertyId = Shader.PropertyToID("_Thresholds");

        /// <summary>
        /// The property identifier for the list of colors in the shader.
        /// </summary>
        public static readonly int ColorsPropertyId = Shader.PropertyToID("_Colors");

        /// <summary>
        /// The property identifier for count of colors in the shader.
        /// </summary>
        public static readonly int ColorCountPropertyId = Shader.PropertyToID("_ColorCount");

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
            this.Target.material.SetFloat(RatedSingleGraphRenderer.HighIsGoodPropertyId, this.highIsGood ? 1.0f : 0.0f);

            // Select the current thresholds.
            EPlatform var_Platform = PlatformHelper.GetCurrentPlatform();

            switch (var_Platform)
            {
                case EPlatform.Desktop:
                    this.currentThresholds = this.desktopThresholds.ToArray();
                    break;

                case EPlatform.Mobile:
                    this.currentThresholds = this.mobileThresholds.ToArray();
                    break;

                case EPlatform.Console:
                    this.currentThresholds = this.consoleThresholds.ToArray();
                    break;

                default:
                    this.currentThresholds = this.desktopThresholds.ToArray();
                    break;
            }

            this.Target.material.SetFloatArray(RatedSingleGraphRenderer.ThresholdsPropertyId, this.currentThresholds);

            this.Target.material.SetColorArray(RatedSingleGraphRenderer.ColorsPropertyId, this.colors);
            this.Target.material.SetFloat(RatedSingleGraphRenderer.ColorCountPropertyId, this.colors.Count);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void RefreshGraph()
        {
            // Call the base method.
            base.RefreshGraph();

            // Pass the graph properties to the shader.
            this.Target.material.SetFloat(RatedSingleGraphRenderer.HighIsGoodPropertyId, this.highIsGood ? 1.0f : 0.0f);

            // Select the current thresholds.
            EPlatform var_Platform = PlatformHelper.GetCurrentPlatform();

            switch (var_Platform)
            {
                case EPlatform.Desktop:
                    this.currentThresholds = this.desktopThresholds.ToArray();
                    break;

                case EPlatform.Mobile:
                    this.currentThresholds = this.mobileThresholds.ToArray();
                    break;

                case EPlatform.Console:
                    this.currentThresholds = this.consoleThresholds.ToArray();
                    break;

                default:
                    this.currentThresholds = this.desktopThresholds.ToArray();
                    break;
            }

            this.Target.material.SetFloatArray(RatedSingleGraphRenderer.ThresholdsPropertyId, this.currentThresholds);

            this.Target.material.SetColorArray(RatedSingleGraphRenderer.ColorsPropertyId, this.colors);
            this.Target.material.SetFloat(RatedSingleGraphRenderer.ColorCountPropertyId, this.colors.Count);

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

            // Update the legend.
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

            // Scale the thresholds.
            float[] var_ScaledThresholds = new float[this.currentThresholds.Length];
            for (int i = 0; i < this.currentThresholds.Length; i++)
            {
                var_ScaledThresholds[i] = this.currentThresholds[i] / this.maxValue;
            }

            // Pass the thresholds to the shader.
            this.Target.material.SetFloatArray(RatedSingleGraphRenderer.ThresholdsPropertyId, var_ScaledThresholds);

            // Pass the graph values to the shader.
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, var_NormalizedValues);
        }

        /// <summary>
        /// Update the legend image based on the mean value.
        /// </summary>
        private void UpdateLegend()
        {
            // Check if there is a legend image.
            if (this.LegendImage == null)
            {
                return;
            }

            // Based on the mean value, select the color.
            Color var_Color = this.colors[0];

            // If high is good, check if the mean value is above the warning threshold.
            if (this.highIsGood)
            {
                if (this.meanValue > this.currentThresholds[0])
                {
                    var_Color = this.colors[0];
                }
                else if (this.meanValue > this.currentThresholds[1])
                {
                    var_Color = this.colors[1];
                }
                else
                {
                    var_Color = this.colors[2];
                }
            }
            // If low is good, check if the mean value is below the warning threshold.
            else
            {
                if (this.meanValue > this.currentThresholds[1])
                {
                    var_Color = this.colors[2];
                }
                else if (this.meanValue > this.currentThresholds[0])
                {
                    var_Color = this.colors[1];
                }
                else
                {
                    var_Color = this.colors[0];
                }
            }

            // Update the legend image.
            this.LegendImage.color = var_Color;
        }
    }
}
