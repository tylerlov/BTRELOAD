// Microsoft
using System;
using System.Reflection;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Base interface for a data provider that supplies <see cref="PerformanceData"/> to subscribed observers.
    /// </summary>
    /// <remarks>
    /// The <see cref="IPerformanceProvider"/> interface extends <see cref="IProvider"/> to provide additional properties for performance data.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public interface IPerformanceProvider : IProvider
    {
        /// <summary>
        /// Gets or sets whether the provided data is scaleable.
        /// </summary>
        bool IsScaleAble { get; }

        /// <summary>
        /// Gets or sets the scale factor for the provided data.
        /// </summary>
        int ScaleFactor { get; }

        /// <summary>
        /// Gets or sets the scale suffixes for the provided data.
        /// </summary>
        String[] ScaleSuffixes { get; }

        /// <summary>
        /// Gets or sets the default unit of the provided data.
        /// </summary>
        String Unit { get; }
    }
}
