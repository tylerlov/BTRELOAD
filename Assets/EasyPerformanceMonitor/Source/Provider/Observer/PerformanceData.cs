// GUPS
using GUPS.EasyPerformanceMonitor.Observer;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Performance data provided by a performance provider.
    /// </summary>
    /// <remarks>
    /// The <see cref="PerformanceData"/> struct encapsulates information about the performance of a system/hardware/software or component.
    /// </remarks>
    public struct PerformanceData : IProvidedData<float>
    {
        /// <summary>
        /// Gets or sets the observed performance provider sender.
        /// </summary>
        public IProvider Sender { get; private set;}

        /// <summary>
        /// Gets or sets the latest performance value.
        /// </summary>
        public float Value { get; private set; }

        /// <summary>
        /// Gets the latest performance value.
        /// </summary>
        object IProvidedData.Value => this.Value;

        /// <summary>
        /// Gets or sets the minimum performance value for a fixed range of values.
        /// </summary>
        public float MinValue { get; private set; }

        /// <summary>
        /// Gets or sets the mean performance value for a fixed range of values.
        /// </summary>
        public float MeanValue { get; private set; }

        /// <summary>
        /// Gets or sets the maximum performance value for a fixed range of values.
        /// </summary>
        public float MaxValue { get; private set; }

        /// <summary>
        /// Gets or sets the number of last values used to calculate the min/mean/max values.
        /// </summary>
        public int ValueCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PerformanceData struct.
        /// </summary>
        /// <param name="_Sender">The performance provider sender.</param>
        /// <param name="_Value">The latest performance value.</param>
        /// <param name="_MinValue">The minimum performance value.</param>
        /// <param name="_MeanValue">The mean performance value.</param>
        /// <param name="_MaxValue">The maximum performance value.</param>
        /// <param name="_ValueCount">Number of values used to calculate the min/mean/max values.</param>
        public PerformanceData(IPerformanceProvider _Sender, float _Value, float _MinValue, float _MeanValue, float _MaxValue, int _ValueCount)
        {
            this.Sender = _Sender;
            this.Value = _Value;
            this.MinValue = _MinValue;
            this.MeanValue = _MeanValue;
            this.MaxValue = _MaxValue;
            this.ValueCount = _ValueCount;
        }
    }
}