// Microsoft
using System;
using System.Reflection;

// GUPS
using GUPS.EasyPerformanceMonitor.Observer;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Base interface for a data provider that supplies <see cref="IProvidedData"/> to subscribed observers.
    /// </summary>
    /// <remarks>
    /// The <see cref="IProvider"/> interface is a contract for providers, allowing them to supply <see cref="IProvidedData"/> to subscribed observers. 
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public interface IProvider : IObservable<IProvidedData>, IDisposable
    {
        /// <summary>
        /// Gets the provider name.
        /// </summary>
        String Name { get; }

        /// <summary>
        /// Gets whether the provider is supported on the current platform.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Gets whether the provider is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Get the type of <see cref="IProvidedData"/> provided by the provider.
        /// </summary>
        Type ProvidedDataType { get; }
    }
}
