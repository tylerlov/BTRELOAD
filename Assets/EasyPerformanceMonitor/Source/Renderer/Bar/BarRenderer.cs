// Microsoft
using System;
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Observer;
using GUPS.EasyPerformanceMonitor.Provider;
using GUPS.EasyPerformanceMonitor.Platform;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Implementation of the IBarRenderer interface for rendering <see cref="PerformanceData"/> as a bar.
    /// </summary>
    /// <remarks>
    /// The <see cref="BarRenderer"/> provides the functionality for rendering <see cref="PerformanceData"/> as a bar with a percentage value.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class BarRenderer : MonoBehaviour, IBarRenderer
    {
        /// <summary>
        /// Gets the list of data providers associated with the renderer.
        /// </summary>
        /// <remarks>This list is populated by finding all components implementing the IProvider interface on this GameObject.</remarks>
        public List<IProvider> Provider { get; private set; }

        /// <summary>
        /// Stores the unsubscriber for the data provider.
        /// </summary>
        /// <remarks>This list is used to manage subscriptions and unsubscribe from data providers during disposal.</remarks>
        private List<IDisposable> unsubscriber;

        /// <summary>
        /// The minimum value serving as the lower bound for the performance data. Index 0: Desktop, Index 1: Mobile, Index 2: Console.
        /// </summary>
        [SerializeField]
        private float[] lowerBoundValues;

        /// <summary>
        /// The lower bound value for the active platform.
        /// </summary>
        private float lowerBoundValueActivePlatform;

        /// <summary>
        /// The maximum value serving as the upper bound for the performance data. Index 0: Desktop, Index 1: Mobile, Index 2: Console.
        /// </summary>
        [SerializeField]
        private float[] upperBoundValues;

        /// <summary>
        /// The lower bound value for the active platform.
        /// </summary>
        private float upperBoundValueActivePlatform;

        /// <summary>
        /// The slider used to display the value of the performance data.   
        /// </summary>
        [SerializeField]
        private UnityEngine.UI.Slider uiValueSlider;

        /// <summary>
        /// The text used to display the percentage value of the performance data.
        /// </summary>
        [SerializeField]
        public UnityEngine.UI.Text uiValuePercentageText;

        /// <summary>
        /// Initializes the bar renderer, subscribes to data providers.
        /// </summary>
        protected virtual void Awake()
        {
            // Find all providers on this game object.
            this.Provider = new List<IProvider>(this.GetComponents<IProvider>());

            // Initialize the unsubscriber list.
            this.unsubscriber = new List<IDisposable>();

            // Subscribe to all providers, that provide data as TProvidedData.
            foreach (IProvider var_Provider in this.Provider)
            {
                // Check if the provider provides the correct data type.
                if (typeof(PerformanceData).IsAssignableFrom(var_Provider.ProvidedDataType))
                {
                    this.unsubscriber.Add(var_Provider.Subscribe(this));
                }
            }
        }

        /// <summary>
        /// Selects the current thresholds for the active platform.
        /// </summary>
        protected virtual void Start()
        {
            // Select the current boundaries.
            EPlatform var_Platform = PlatformHelper.GetCurrentPlatform();

            switch (var_Platform)
            {
                case EPlatform.Desktop:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[0];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[0];
                    break;

                case EPlatform.Mobile:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[1];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[1];
                    break;

                case EPlatform.Console:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[2];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[2];
                    break;

                default:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[0];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[0];
                    break;
            }
        }

        /// <summary>
        /// Called when a new performance data value is received.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        public void OnNext(PerformanceData _Next)
        {
            // Calculate the value.
            float var_Value = (_Next.Value - this.lowerBoundValueActivePlatform) / (this.upperBoundValueActivePlatform - this.lowerBoundValueActivePlatform);

            // Cap the value.
            var_Value = Mathf.Max(0.0f, Mathf.Min(1.0f, var_Value));

            // Calculate the percentage value.
            float var_PercentageValue = var_Value * 100.0f;

            // Set the slider value - Set the value to a minimum of 0.1 to avoid the slider being invisible.
            if (this.uiValueSlider != null)
                this.uiValueSlider.value = Mathf.Max(0.1f, var_Value);

            // Set the percentage value.
            if(this.uiValuePercentageText != null)
                this.uiValuePercentageText.text = string.Format("{0:0.0}%", var_PercentageValue);
        }

        /// <summary>
        /// Called when a new performance data value is received.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        void IObserver<IProvidedData>.OnNext(IProvidedData _Next)
        {
            if (_Next is PerformanceData)
            {
                this.OnNext((PerformanceData)_Next);
            }
        }

        /// <summary>
        /// Called when an error is encountered.
        /// </summary>
        /// <param name="_Error">The encountered error.</param>
        public virtual void OnError(Exception _Error)
        {
            // Does nothing.
        }

        /// <summary>
        /// Called when data observation is completed.
        /// </summary>
        public virtual void OnCompleted()
        {
            // Does nothing.
        }

        /// <summary>
        /// Disposes of the renderer by unsubscribing from all data providers.
        /// </summary>
        public virtual void Dispose()
        {
            // Unsubscribe from all data providers.
            foreach (IDisposable var_Unsubscriber in this.unsubscriber)
            {
                var_Unsubscriber.Dispose();
            }
        }

        /// <summary>
        /// Called when the GameObject is destroyed, unsubscribes from all data providers.
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Unsubscribe from all data providers.
            foreach (IDisposable var_Unsubscriber in this.unsubscriber)
            {
                var_Unsubscriber.Dispose();
            }
        }

        /// <summary>
        /// Refresh the bar renderer on editor value changed by the user.
        /// </summary>
        public void RefreshBar()
        {
            // Select the current boundaries.
            EPlatform var_Platform = PlatformHelper.GetCurrentPlatform();

            switch (var_Platform)
            {
                case EPlatform.Desktop:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[0];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[0];
                    break;

                case EPlatform.Mobile:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[1];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[1];
                    break;

                case EPlatform.Console:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[2];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[2];
                    break;

                default:
                    this.lowerBoundValueActivePlatform = this.lowerBoundValues[0];
                    this.upperBoundValueActivePlatform = this.upperBoundValues[0];
                    break;
            }
        }
    }
}
