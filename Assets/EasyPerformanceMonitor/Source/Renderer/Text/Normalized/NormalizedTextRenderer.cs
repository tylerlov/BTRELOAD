// Microsoft
using System;
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;
using System.Text.RegularExpressions;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Text renderer implementation for a normalized graph displaying the min, max, and mean values as text of the subscribed 
    /// performance providers on the same GameObject. The displayed min, max, and mean values are normalized to a percentage 
    /// value based on the sum of all mean values.
    /// </summary>
    /// <remarks>
    /// The <see cref="NormalizedTextRenderer"/> class extends the functionality of <see cref="ATextRenderer<PerformanceData>"/> to provide
    /// text rendering for visualizing normalized performance metrics from subscribed providers on a graph. It displays 
    /// mean values alongside their normalized percentage values, allowing users to gauge the relative contribution of each 
    /// provider to the overall performance.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class NormalizedTextRenderer : ATextRenderer<PerformanceData>
    {
        /// <summary>
        /// The text pattern used to render the value and unit. The 0 represents the value and the # represents the unit.
        /// Possible patterns are: 0, 0.0, 0.00, 0.000, ... or 0#, 0.0#, 0.00#, 0.000#, ...
        /// </summary>
        [SerializeField]
        private String pattern = "0.0#";

        /// <summary>
        /// The text pattern used to render the value and unit. The 0 represents the value and the # represents the unit.
        /// Possible patterns are: 0, 0.0, 0.00, 0.000, ... or 0#, 0.0#, 0.00#, 0.000#, ...
        /// </summary>
        public String Pattern { get => this.pattern; }

        /// <summary>
        /// A calculated pattern used to render the value and unit.
        /// </summary>
        private String renderPattern = "{0:0.0}{1}";

        /// <summary>
        /// Stores the mean values received from performance providers.
        /// </summary>
        private float[] meanValues;

        /// <summary>
        /// Stores UI Text components associated with mean values.
        /// </summary>
        [SerializeField]
        private List<UnityEngine.UI.Text> uiMeanTexts = new List<UnityEngine.UI.Text>();

        /// <summary>
        /// Stores UI Text components associated with normalized percentage values.
        /// </summary>
        [SerializeField]
        private List<UnityEngine.UI.Text> uiPercentTexts = new List<UnityEngine.UI.Text>();

        /// <summary>
        /// Initializes the text renderer, subscribes to performance providers, and sets up initial values.
        /// </summary>
        protected override void Awake()
        {
            // Call the base implementation.
            base.Awake();

            // Initialize the mean values.
            this.meanValues = new float[this.Provider.Count];

            // Refresh the render pattern.
            this.RefreshRenderPattern();
        }

        /// <summary>
        /// Called when a new performance data value is received.
        /// Updates the displayed values and UI texts.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        public override void OnNext(PerformanceData _Next)
        {
            // Get the performance data.
            PerformanceData var_PerformanceData = (PerformanceData)_Next;

            // Get the sender of the performance data.
            IPerformanceProvider var_Sender = (IPerformanceProvider)_Next.Sender;

            // Get the index of the performance provider.
            int var_Index = this.Provider.IndexOf(var_Sender);

            // Check if the performance provider is registered.
            if (var_Index < 0)
            {
                // Performance provider not found.
                return;
            }

            // Assign scaled values.
            this.meanValues[var_Index] = this.ScaleValue(var_PerformanceData.MeanValue, var_Sender, out String var_MeanSuffix);

            // Refresh the ui mean texts.
            if (this.uiMeanTexts.Count > var_Index && this.uiMeanTexts[var_Index] != null)
                this.uiMeanTexts[var_Index].text = String.Format(this.renderPattern, this.meanValues[var_Index], var_MeanSuffix);

            // Calculate the normalized value.
            float var_SummedMeanValue = 0.0f;

            for(int i = 0; i < this.meanValues.Length; i++)
            {
                var_SummedMeanValue += this.meanValues[i];
            }

            float var_NormalizedValue = this.meanValues[var_Index] / var_SummedMeanValue;

            // Refresh the ui percent texts.
            if (this.uiPercentTexts.Count > var_Index && this.uiPercentTexts[var_Index] != null)
            {
                this.uiPercentTexts[var_Index].text = String.Format("{0:0.0}%", var_NormalizedValue * 100);
            }
        }

        /// <summary>
        /// Scales the provided value based on the scale factor and count of suffixes.
        /// Also returns the corresponding suffix.
        /// If the 'Scale' property is false, the value will not be scaled.
        /// </summary>
        /// <param name="_Value">The value to scale.</param>
        /// <param name="_Provider">The performance provider associated with the value.</param>
        /// <param name="_Suffix">The suffix indicating the scale factor.</param>
        /// <returns>The scaled value.</returns>
        private float ScaleValue(float _Value, IPerformanceProvider _Provider, out String _Suffix)
        {
            // Scale to the maximum of passed scale suffixes.
            if (this.Scale && _Provider.IsScaleAble)
            {
                // Find the scale suffix.
                int var_ScaleSuffixIndex = 0;
                while (var_ScaleSuffixIndex < _Provider.ScaleSuffixes.Length - 1 && _Value > Mathf.Pow(_Provider.ScaleFactor, var_ScaleSuffixIndex + 1))
                {
                    var_ScaleSuffixIndex++;
                }

                // Set the suffix.
                _Suffix = _Provider.ScaleSuffixes[var_ScaleSuffixIndex];

                // Scale the value.
                return _Value / Mathf.Pow(_Provider.ScaleFactor, var_ScaleSuffixIndex);
            }

            // Set the suffix.
            _Suffix = _Provider.Unit;

            // Do not scale the value.
            return _Value;
        }

        /// <summary>
        /// Refresh the rendering value and unit pattern, based on the user assigned pattern.
        /// </summary>
        public override void RefreshText()
        {
            // Call the base method.
            base.RefreshText();

            // Refresh the render pattern.
            this.RefreshRenderPattern();
        }

        /// <summary>
        /// Refresh the render pattern.
        /// </summary>
        private void RefreshRenderPattern()
        {
            // Refresh the render pattern.
            this.renderPattern = this.Pattern;

            // Regex pattern for finding the value pattern (0 or 0.0 or 0.00 ...).
            String var_ValuePattern = @"(\d\.\d+)|\d+";

            // Match the value pattern.
            Match var_ValueMatch = Regex.Match(this.renderPattern, var_ValuePattern);

            if (var_ValueMatch.Success)
            {
                this.renderPattern = this.renderPattern.Replace(var_ValueMatch.Value, "{0:" + var_ValueMatch.Value + "}");
            }

            // Replace the unit pattern.
            this.renderPattern = this.renderPattern.Replace("#", "{1}");
        }
    }
}
