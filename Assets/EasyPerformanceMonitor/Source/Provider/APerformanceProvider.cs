// Microsoft
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Persistent;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Abstract base implementation for a data provider that supplies <see cref="PerformanceData"/> to subscribed observers.
    /// </summary>
    /// <remarks>
    /// The <see cref="APerformanceProvider"/> inherites the <see cref="AProvider<PerformanceData>"/> interface and provides a base 
    /// implementation for providers, allowing them to supply <see cref="PerformanceData"/> to subscribed observers.
    /// </remarks>
    [Serializable]
    [Obfuscation(Exclude = true)]
    public abstract class APerformanceProvider : AProvider<PerformanceData>, IPerformanceProvider
    {
        /// <summary>
        /// Gets or sets whether the provided data is scaleable.
        /// </summary>
        [SerializeField]
        private bool isScaleAble = false;

        /// <summary>
        /// Gets or sets whether the provided data is scaleable.
        /// </summary>
        public bool IsScaleAble { get => isScaleAble; }

        /// <summary>
        /// Gets or sets the scale factor for the provided data.
        /// </summary>
        [SerializeField]
        private int scaleFactor = 1;

        /// <summary>
        /// Gets or sets the scale factor for the provided data.
        /// </summary>
        public int ScaleFactor { get => scaleFactor; }

        /// <summary>
        /// Gets or sets the scale suffixes for the provided data.
        /// </summary>
        [SerializeField]
        private String[] scaleSuffixes = new String[0];

        /// <summary>
        /// Gets or sets the scale suffixes for the provided data.
        /// </summary>
        public String[] ScaleSuffixes { get => scaleSuffixes; }

        /// <summary>
        /// Gets or sets the default unit of the provided data.
        /// </summary>
        public abstract String Unit { get; }

        /// <summary>
        /// The count of last read values, used to calculate min/mean/max values.
        /// </summary>
        [SerializeField]
        private int historySize = 25;

        /// <summary>
        /// List of the last 'historySize' performance values.
        /// </summary>
        private float[] values = new float[0];

        /// <summary>
        /// Current minimum performance value.
        /// </summary>
        private float valueMin = 0;

        /// <summary>
        /// Current mean performance value.
        /// </summary>
        private float valueMean = 0;

        /// <summary>
        /// Current maximum performance value.
        /// </summary>
        private float valueMax = 0;

        /// <summary>
        /// Interval in seconds to fetch the performance value.
        /// </summary>
        [SerializeField]
        private float fetchInterval = 0.1f;

        /// <summary>
        /// The last time the performance value was fetched.
        /// </summary>
        private float lastFetchTime = 0;

        /// <summary>
        /// Store the graph values inside the 'Application.persistentDataPath' in a csv file
        /// </summary>
        [SerializeField]
        private bool storeValuesInCsvFile = false;

        /// <summary>
        /// The csv file writer.
        /// </summary>
        private CsvFileWriter csvFileWriter;

        /// <summary>
        /// Initialize the performance provider and check if is supported.
        /// </summary>
        protected override void Awake()
        {
            // Call the base method.
            base.Awake();

            // Initialize the value list.
            this.values = new float[this.historySize];
        }

        /// <summary>
        /// Fetch the performance value if the fetch interval is reached.
        /// </summary>
        protected virtual void Update()
        {
            // Do nothing if the performance provider is not active.
            if (!this.IsActive)
            {
                return;
            }

            // Fetch the performance value if the fetch interval is reached.
            if(Time.unscaledTime - this.lastFetchTime > this.fetchInterval)
            {
                // Reset the last fetch time.
                this.lastFetchTime = Time.unscaledTime;

                // Fetch the performance value.
                this.Fetch();
            }
        }

        /// <summary>
        /// Adds a new performance value to the value list and updates min, mean, and max values.
        /// </summary>
        /// <param name="_Value">The value to add.</param>
        private void AddValue(float _Value)
        {
            // Store the min and max values.
            float var_MinValue = float.MaxValue;
            float var_MaxValue = 0;

            // Store the mean value.
            float var_MeanValue = 0;
            int var_MeanCounter = 0;

            // Push new values to the end of the array and shift the array to the left.
            for (int i = 0; i < this.historySize; i++)
            {
                // Push to the left.
                if (i < this.historySize - 1)
                {
                    this.values[i] = this.values[i + 1];
                }
                // Store the current value.
                else
                {
                    this.values[i] = _Value;
                }

                // Find the min and max value.
                if (this.values[i] < var_MinValue)
                {
                    var_MinValue = this.values[i];
                }
                if (this.values[i] > var_MaxValue)
                {
                    var_MaxValue = this.values[i];
                }

                // Increase the mean value, if the value is greater than zero.
                if (this.values[i] > 0)
                {
                    var_MeanValue += this.values[i];
                    var_MeanCounter += 1;
                }
            }

            // Find the min, mean, and max value.
            this.valueMin = var_MinValue;
            this.valueMean = var_MeanCounter > 0 ? var_MeanValue / var_MeanCounter : 0;
            this.valueMax = var_MaxValue;

            // Store the value in a csv file.
            if (this.storeValuesInCsvFile)
            {
                if (this.csvFileWriter == null)
                {
                    // Get the current time.
                    DateTime var_CurrentDateTime = DateTime.Now;

                    // Find the game start time.
                    var_CurrentDateTime.AddSeconds(-Time.realtimeSinceStartup);

                    // Format the date time.
                    String var_FormattedDateTime = var_CurrentDateTime.ToString("yyyy.MM.dd_HH.mm.ss");

                    // Create the file path.
                    String var_FilePath = Path.Combine(Application.persistentDataPath, var_FormattedDateTime + "_" + this.Name + ".csv");

                    // Create the csv file appender.
                    this.csvFileWriter = new CsvFileWriter(var_FilePath);
                }

                // Append the value async over a task.
                Task.Run(async () => { await this.csvFileWriter.AppendAsync(_Value); });
            }
        }

        /// <summary>
        /// Gets the next value of the performance provider.
        /// </summary>
        /// <returns>The next value of the performance provider.</returns>
        protected abstract float GetNextValue();

        /// <summary>
        /// Fetch the next performance value and notify all observers.
        /// </summary>
        private void Fetch()
        {
            // Get the next value.
            float var_CurrentValue = this.GetNextValue();

            // Add the next value.
            this.AddValue(var_CurrentValue);

            // Notify all observers with the new performance data.
            PerformanceData var_PerformanceDate = new PerformanceData(
                this, var_CurrentValue, this.valueMin, this.valueMean, this.valueMax, this.historySize);

            foreach (var var_Observer in this.ObserverList)
            {
                var_Observer.OnNext(var_PerformanceDate);
            }
        }

        /// <summary>
        /// Refresh the performance provider through the Inspector.
        /// </summary>
        public virtual void Refresh()
        {

        }
    }
}
