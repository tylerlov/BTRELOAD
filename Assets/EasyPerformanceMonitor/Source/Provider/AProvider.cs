// Microsoft
using System;
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Observer;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Abstract base implementation for a data provider that supplies <see cref="TProvidedData"/> to subscribed observers.
    /// </summary>
    /// <remarks>
    /// The <see cref="AProvider<TProvidedData>"/> inherites the <see cref="IProvider"/> interface and provides a base implementation for providers, 
    /// allowing them to supply <see cref="TProvidedData"/> to subscribed observers.
    /// </remarks>
    [Serializable]
    [Obfuscation(Exclude = true)]
    public abstract class AProvider<TProvidedData> : MonoBehaviour, IProvider
        where TProvidedData : IProvidedData
    {
        /// <summary>
        /// List of observers subscribed to this provider.
        /// </summary>
        protected List<IObserver<IProvidedData>> ObserverList { get; } = new List<IObserver<IProvidedData>>();

        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public abstract String Name { get; }

        /// <summary>
        /// Gets a value indicating whether the provider is supported.
        /// </summary>
        public abstract bool IsSupported { get; }

        /// <summary>
        /// Activate or deactivate the provider, start or stop providing values.
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// Get the type of <see cref="TProvidedData"/> provided by the provider.
        /// </summary>
        public Type ProvidedDataType => typeof(TProvidedData);

        /// <summary>
        /// Initialize the provider and check if is supported.
        /// </summary>
        protected virtual void Awake()
        {
            // If the provider is not supported by the current platform, deactivate it.
            if (!this.IsSupported)
            {
                this.IsActive = false;
            }
        }

        /// <summary>
        /// Subscribes an observer to receive updates from this provider.
        /// </summary>
        /// <param name="_Observer">The observer to subscribe.</param>
        /// <returns>An IDisposable for unsubscribing the observer.</returns>
        public IDisposable Subscribe(IObserver<IProvidedData> _Observer)
        {
            // Add the observer to the observer list if it does not exist.
            if (!this.ObserverList.Contains(_Observer))
            {
                this.ObserverList.Add(_Observer);
            }

            // Return an unsubscriber for the observer.
            return new Unsubscriber(this.ObserverList, _Observer);
        }

        /// <summary>
        /// Class representing an unsubscriber for a observer, allowing for unsubscribing.
        /// </summary>
        private class Unsubscriber : IDisposable
        {
            /// <summary>
            /// List of observers from the observed subject.
            /// </summary>
            private List<IObserver<IProvidedData>> observers;

            /// <summary>
            /// The specific observer to be unsubscribed.
            /// </summary>
            private IObserver<IProvidedData> observer;

            /// <summary>
            /// Initializes a new instance of the Unsubscriber class with the specified observer list and observer.
            /// </summary>
            /// <param name="_Observers">The list of observers.</param>
            /// <param name="_Observer">The observer to unsubscribe.</param>
            public Unsubscriber(List<IObserver<IProvidedData>> _Observers, IObserver<IProvidedData> _Observer)
            {
                this.observers = _Observers;
                this.observer = _Observer;
            }

            /// <summary>
            /// Disposes the unsubscriber and removes the observer from the observer list.
            /// </summary>
            public void Dispose()
            {
                // Removes the observer from the observer list if it exists.
                if (this.observer != null && this.observers.Contains(this.observer))
                {
                    this.observers.Remove(this.observer);
                }
            }
        }

        /// <summary>
        /// Disposes the provider and notifies observers that no more values will be provided.
        /// </summary>
        public void Dispose()
        {
            // Notify all observers that the provider is disposed and so no more values will be provided.
            foreach (var var_Observer in this.ObserverList)
            {
                var_Observer.OnCompleted();
            }
        }
    }
}
