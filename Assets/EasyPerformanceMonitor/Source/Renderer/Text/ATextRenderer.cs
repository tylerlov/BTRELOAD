// Microsoft
using System;
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Observer;
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Abstract implementation of a text renderer that subscribes to data providers on the same GameObject.
    /// </summary>
    /// <remarks>
    /// The <see cref="ATextRenderer<TProvidedData>"/> class serves as the foundation for implementing text renderers
    /// that visualize data obtained from subscribed <see cref="IProvider"/> instances.
    /// It provides functionality for automatic subscription to data providers on the same GameObject,
    /// and dynamic rendering of textual information based on real-time data metrics.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public abstract class ATextRenderer<TProvidedData> : MonoBehaviour, ITextRenderer
        where TProvidedData : IProvidedData
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
        /// Determines whether to scale the displayed values.
        /// </summary>
        /// <remarks>Serialized field to expose the option in the Unity editor.</remarks>
        [SerializeField]
        private bool scale = false;

        /// <summary>
        /// Determines whether to scale the displayed values.
        /// </summary>
        /// <value>True if scaling is enabled, false otherwise.</value>
        public bool Scale { get => this.scale; }

        /// <summary>
        /// Initializes the text renderer, subscribes to data providers.
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
                if (typeof(TProvidedData).IsAssignableFrom(var_Provider.ProvidedDataType))
                {
                    this.unsubscriber.Add(var_Provider.Subscribe(this));
                }
            }
        }

        /// <summary>
        /// Refresh the text renderer on editor value changed.
        /// </summary>
        public virtual void RefreshText()
        {
        }

        /// <summary>
        /// Called when a new data value is received.
        /// </summary>
        /// <param name="_Next">The new data received.</param>
        public abstract void OnNext(TProvidedData _Next);

        /// <summary>
        /// Called when a new data value is received.
        /// </summary>
        /// <param name="_Next">The new data received.</param>
        void IObserver<IProvidedData>.OnNext(IProvidedData _Next)
        {
            if (_Next is TProvidedData)
            {
                this.OnNext((TProvidedData)_Next);
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
    }
}
