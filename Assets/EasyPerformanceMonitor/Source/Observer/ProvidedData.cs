// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Observer
{
    /// <summary>
    /// Simple struct implementation of data provided by an <see cref="IProvider"/> interface.
    /// </summary>
    public struct ProvidedData<TValue> : IProvidedData<TValue>
    {
        /// <summary>
        /// The sender of the data.
        /// </summary>
        public IProvider Sender { get; set; }

        /// <summary>
        /// The value of the data.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// The value of the data.
        /// </summary>
        object IProvidedData.Value => this.Value;

        /// <summary>
        /// Initializes a new instance of the ProvidedData struct.
        /// </summary>
        /// <param name="_Sender">The provider sender.</param>
        /// <param name="_Value">The value of the data.</param>
        public ProvidedData(IProvider _Sender, TValue _Value)
        {
            this.Sender = _Sender;
            this.Value = _Value;
        }
    }
}