// Microsoft
using System;
using System.Collections.Generic;

// GUPS
using GUPS.EasyPerformanceMonitor.Observer;
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Defines an interface for a renderer, incorporating methods for data observation and disposal.
    /// </summary>
    /// <remarks>
    /// The <see cref="IRenderer"/> interface extends <see cref="IObserver<IProvidedData>"/> to enable the observation of
    /// data updates from associated providers. It also extends <see cref="IDisposable"/> to allow proper cleanup and resource disposal 
    /// when the renderer is no longer needed.
    /// </remarks>
    public interface IRenderer : IObserver<IProvidedData>, IDisposable
    {
        /// <summary>
        /// Gets the list of performance provider associated with the renderer.
        /// </summary>
        List<IProvider> Provider { get; }
    }
}
