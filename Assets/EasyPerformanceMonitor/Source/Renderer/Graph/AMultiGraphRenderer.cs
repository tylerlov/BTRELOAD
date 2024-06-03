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
    /// Abstract implementation of a graph renderer displaying multiple graphs on a Unity UI Image component 
    /// using data provided by a performance provider on the same GameObject.
    /// </summary>
    /// <remarks>
    /// The <see cref="AMultiGraphRenderer"/> class extends the functionality of a basic graph renderer
    /// to support rendering multiple graphs simultaneously. It provides options for rendering graphs as stacked
    /// or side by side.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public abstract class AMultiGraphRenderer : AGraphRenderer
    {
        /// <summary>
        /// Gets a value indicating whether the graph is rendered as a stacked graph or next to each other.
        /// </summary>
        [SerializeField]
        private bool isStacked = true;

        /// <summary>
        /// Gets a value indicating whether the graph is rendered as a stacked graph or next to each other.
        /// </summary>
        public bool IsStacked { get => this.isStacked; }

        /// <summary>
        /// Get a list of Unity UI Images used to display legend colors for each graph in the renderer.
        /// </summary>
        [SerializeField]
        private List<UnityEngine.UI.Image> legendImages = new List<UnityEngine.UI.Image>();

        /// <summary>
        /// Get a list of Unity UI Images used to display legend colors for each graph in the renderer.
        /// </summary>
        public List<UnityEngine.UI.Image> LegendImages { get => this.legendImages; }

        /// <summary>
        /// Stores the historical values for each performance provider.
        /// </summary>
        private float[,] values;

        /// <summary>
        /// Stores the historical values for each performance provider.
        /// </summary>
        protected float[,] Values { get => this.values; }

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        private float[] minValues;

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        protected float[] MinValues { get => this.minValues; }

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        private float[] maxValues;

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        protected float[] MaxValues { get => this.maxValues; }

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        private float[] meanValues;

        /// <summary>
        /// Gets the historical values for each performance provider.
        /// </summary>
        protected float[] MeanValues { get => this.meanValues; }

        /// <summary>
        /// The property identifier for indicating whether the graph is stacked in the shader.
        /// </summary>
        public static readonly int StackedPropertyId = Shader.PropertyToID("_Stacked");

        /// <summary>
        /// Initializes the rated graph renderer, subscribes to performance providers, and sets up initial values.
        /// </summary>
        protected override void Awake()
        {
            // Call the base method.
            base.Awake();

            // Initialize the values array.
            int var_Length = this.IsStacked ? this.GraphValues : this.GraphValues / this.Provider.Count;

            this.values = new float[this.Provider.Count, var_Length];

            // Initialize the min, max and mean values.
            this.minValues = new float[this.Provider.Count];
            this.maxValues = new float[this.Provider.Count];
            this.meanValues = new float[this.Provider.Count];
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnInitializeGraph(Shader _Shader)
        {
            // Call the base method.
            base.OnInitializeGraph(_Shader);

            // Pass the graph properties to the shader.
            this.Target.material.SetFloat(AMultiGraphRenderer.StackedPropertyId, this.IsStacked ? 1.0f : 0.0f);

            // Calculate the graph values.
            int var_GraphValues = this.GraphValues * (this.IsStacked ? this.Provider.Count : 1);

            // Find the maximum number of graph values for the current platform.
            EPlatform var_Platform = PlatformHelper.GetCurrentPlatform();

            if (var_Platform == EPlatform.Mobile)
            {
                var_GraphValues = Mathf.Min(var_GraphValues, AGraphRenderer.CMaxGraphValuesMobile);
            }
            else
            {
                var_GraphValues = Mathf.Min(var_GraphValues, AGraphRenderer.CMaxGraphValues);
            }

            // Pass the number of values to the graph shader.
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, new float[var_GraphValues]);
            this.Target.material.SetFloat(AGraphRenderer.ValueCountPropertyId, var_GraphValues);

            // Calculate the length of each graph.
            int var_Length = var_GraphValues / this.Provider.Count;

            // Create resized value array.
            float[,] var_ValueArray = new float[this.Provider.Count, var_Length];

            // Copy current values to new array.
            int var_Difference = var_Length - this.values.GetLength(1);
            int var_StartIndex = var_Length > this.values.GetLength(1) ? var_Difference : 0;
            for (int p = 0; p < this.Provider.Count; p++)
            {
                for (int i = var_StartIndex; i < var_Length; i++)
                {
                    var_ValueArray[p, i] = this.values[p, i - var_Difference];
                }
            }

            // Assign to the current value array.
            this.values = var_ValueArray;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void RefreshGraph()
        {
            // Call the base method.
            base.RefreshGraph();

            // Pass the graph properties to the shader.
            this.Target.material.SetFloat(AMultiGraphRenderer.StackedPropertyId, this.IsStacked ? 1.0f : 0.0f);

            // Calculate the graph values.
            int var_GraphValues = this.GraphValues * (this.IsStacked ? this.Provider.Count : 1);

            // Find the maximum number of graph values for the current platform.
            EPlatform var_Platform = PlatformHelper.GetCurrentPlatform();

            if (var_Platform == EPlatform.Mobile)
            {
                var_GraphValues = Mathf.Min(var_GraphValues, AGraphRenderer.CMaxGraphValuesMobile);
            }
            else
            {
                var_GraphValues = Mathf.Min(var_GraphValues, AGraphRenderer.CMaxGraphValues);
            }

            // Pass the number of values to the graph shader.
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, new float[var_GraphValues]);
            this.Target.material.SetFloat(AGraphRenderer.ValueCountPropertyId, var_GraphValues);

            // Calculate the length of each graph.
            int var_Length = var_GraphValues / this.Provider.Count;

            // Create resized value array.
            float[,] var_ValueArray = new float[this.Provider.Count, var_Length];

            // Copy current values to new array.
            int var_Difference = var_Length - this.values.GetLength(1);
            int var_StartIndex = var_Length > this.values.GetLength(1) ? var_Difference : 0;
            for (int p = 0; p < this.Provider.Count; p++)
            {
                for (int i = var_StartIndex; i < var_Length; i++)
                {
                    var_ValueArray[p, i] = this.values[p, i - var_Difference];
                }
            }

            // Assign to the current value array.
            this.values = var_ValueArray;
        }

        /// <summary>
        /// Called when a new performance data value is received.
        /// Updates the displayed values.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        public override void OnNext(PerformanceData _Next)
        {
            // Get the index of the performance provider.
            int var_Index = this.Provider.IndexOf(_Next.Sender);

            // Check if the performance provider is registered.
            if (var_Index < 0)
            {
                // Performance provider not found.
                return;
            }

            // Add the new value to the graph.
            this.AddValue(var_Index, (float) _Next.Value);

            // Update the graph.
            this.UpdateGraph();
        }

        /// <summary>
        /// Adds a new performance value to the historical values.
        /// </summary>
        /// <param name="_Index">The index of the performance provider.</param>
        /// <param name="_Value">The new performance value.</param>
        private void AddValue(int _Index, float _Value)
        {
            // Store the min and max values.
            float var_MinValue = float.MaxValue;
            float var_MaxValue = 0;

            // Store the mean value.
            float var_MeanValue = 0;
            int var_MeanCounter = 0;

            // Push new values to the end of the array and shift the array to the left.
            for (int i = 0; i < this.values.GetLength(1); i++)
            {
                // Push to the left.
                if (i < this.values.GetLength(1) - 1)
                {
                    this.values[_Index, i] = this.values[_Index, i + 1];
                }
                // Store the current value.
                else
                {
                    this.values[_Index, i] = _Value;
                }

                // Find the min and max value.
                if (this.values[_Index, i] < var_MinValue)
                {
                    var_MinValue = this.values[_Index, i];
                }
                if (this.values[_Index, i] > var_MaxValue)
                {
                    var_MaxValue = this.values[_Index, i];
                }

                // Increase the mean value if the value is greater than zero.
                if (this.values[_Index, i] > 0)
                {
                    var_MeanValue += this.values[_Index, i];
                    var_MeanCounter += 1;
                }
            }

            // Assign the min, mean, and max value.
            this.minValues[_Index] = var_MinValue;
            this.meanValues[_Index] = var_MeanCounter > 0 ? var_MeanValue / var_MeanCounter : 0;
            this.maxValues[_Index] = var_MaxValue;
        }

        /// <summary>
        /// Flattens the 2D array of values into a 1D array.
        /// </summary>
        /// <param name="_Array">The 2D array to flatten.</param>
        /// <returns>The flattened 1D array.</returns>
        private float[] FlattenAndScaleArray(float[,] _Array)
        {
            // Initialize the flattened array.
            float[] var_FlattenedArray = new float[_Array.GetLength(0) * _Array.GetLength(1)];

            // Flatten the array.
            for (int i = 0; i < _Array.GetLength(0); i++)
            {
                for (int j = 0; j < _Array.GetLength(1); j++)
                {
                    // Get the max value.
                    float var_MaxValue = this.maxValues[i];

                    var_MaxValue = var_MaxValue == 0f ? 1f : var_MaxValue;

                    var_FlattenedArray[i * _Array.GetLength(1) + j] = _Array[i, j] / var_MaxValue;
                }
            }

            // Return the flattened array.
            return var_FlattenedArray;
        }

        /// <summary>
        /// Flattens and passes the value array to the shader for graph rendering.
        /// </summary>
        protected virtual void UpdateGraph()
        {
            // Pass the graph values to the shader.
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, this.FlattenAndScaleArray(this.values));
        }
    }
}
