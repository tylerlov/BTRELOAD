// Microsoft
using System.Reflection;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Renders multiple colored graphs stacked on top of each other, with automatic normalization of the passed data.
    /// The renderer utilizes performance data received from subscribed providers on the same GameObject.
    /// </summary>
    /// <remarks>
    /// The <see cref="NormalizedMultiGraphRenderer"/> extends the functionality of <see cref="ColoredMultiGraphRenderer"/>
    /// to include normalization of the passed data. It stacks multiple graphs vertically, ensuring that the data is
    /// normalized before rendering. This renderer subscribes to performance providers and automatically manages the
    /// rendering of multiple colored graphs, with each graph's values normalized for accurate visualization.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class NormalizedMultiGraphRenderer : ColoredMultiGraphRenderer
    {
        /// <summary>
        /// Flattens the 2D array of values into a 1D array and normalizes the values.
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
                    // Sum up all values of the current column.
                    float var_SummedValues = 0f;

                    for (int k = 0; k < _Array.GetLength(0); k++)
                    {
                        var_SummedValues += _Array[k, j];
                    }

                    var_SummedValues = var_SummedValues == 0f ? 1f : var_SummedValues;

                    // Normalize the value.
                    float var_NormalizedValue = _Array[i, j] / var_SummedValues;

                    // Make sure it is at least 2% of the total height to be visible.
                    var_NormalizedValue = var_NormalizedValue < 0.02f ? 0.02f : var_NormalizedValue;

                    // Store the normalized value.
                    var_FlattenedArray[i * _Array.GetLength(1) + j] = var_NormalizedValue;
                }
            }

            // Return the flattened array.
            return var_FlattenedArray;
        }

        /// <summary>
        /// Flattens and passes the value array to the shader for graph rendering.
        /// </summary>
        protected override void UpdateGraph()
        {
            // Pass the graph values to the shader.
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, this.FlattenAndScaleArray(this.Values));
        }
    }
}
