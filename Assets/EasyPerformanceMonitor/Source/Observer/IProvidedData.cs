// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Observer
{
    /// <summary>
    /// Base interface for data provided by an <see cref="IProvider"/> interface.
    /// </summary>
    public interface IProvidedData
    {
        /// <summary>
        /// The sender of the data.
        /// </summary>
        IProvider Sender { get; }

        /// <summary>
        /// The value of the data.
        /// </summary>
        object Value { get; }
    }

    /// <summary>
    /// Base interface for data provided by an <see cref="IProvider"/> interface, extending the <see cref="IProvidedData"/> interface
    /// with a generic value property.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface IProvidedData<out TValue> : IProvidedData
    {
        /// <summary>
        /// The value of the data.
        /// </summary>
        new TValue Value { get; }
    }
}