// Microsoft
using System;
using System.Reflection;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Provides performance metrics related to the count of GameObjects with a specific tag in the scene.
    /// </summary>
    /// <remarks>
    /// The <see cref="GameObjectTagCountProvider"/> class inherits from <see cref="APerformanceProvider"/> and focuses on
    /// monitoring the count of GameObjects in the scene that have a specified tag. It periodically updates the count and provides
    /// this information as a performance metric.
    /// </remarks>
    [Serializable]
    [Obfuscation(Exclude = true)]
    public class GameObjectTagCountProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name for GameObject Tag Count.
        /// </summary>
        public const String CName = "GameObject Tag Count";

        /// <summary>
        /// Gets the performance component name.
        /// </summary>
        public override string Name => CName;

        /// <summary>
        /// Gets a value indicating whether the performance component is supported on the current platform.
        /// GameObject Tag Count is supported on all platforms.
        /// </summary>
        public override bool IsSupported => true;

        /// <summary>
        /// Gets the default unit for GameObject Tag Count, which is an empty string.
        /// </summary>
        public override String Unit => "";

        /// <summary>
        /// The tag of GameObjects to monitor.
        /// </summary>
        [SerializeField]
        public String Tag = String.Empty;

        /// <summary>
        /// The last recorded count of GameObjects with the assigned tag.
        /// </summary>
        private int lastGameObjectCount = 0;

        /// <summary>
        /// Initializes the GameObjectTagCountProvider and starts periodic updates of GameObject counts.
        /// </summary>
        protected override void Awake()
        {
            // Call the base method.
            base.Awake();

            // Get the initial count of GameObjects with the assigned tag.
            this.lastGameObjectCount = this.TotalGameObjectCount();

            // Start periodic updates of the count.
            this.InvokeRepeating("UpdateTotalGameObjectCount", 0, 0.5f);
        }

        /// <summary>
        /// Retrieves the total count of GameObjects with the assigned tag in the scene.
        /// </summary>
        /// <returns>The count of GameObjects with the specified tag.</returns>
        private int TotalGameObjectCount()
        {
            // Get the total count of GameObjects with the specified tag.
            int var_Count = GameObject.FindGameObjectsWithTag(this.Tag).Length;

            // Return the count.
            return var_Count;
        }

        /// <summary>
        /// Updates the total count of GameObjects on a fixed interval, because it is an expensive operation.
        /// </summary>
        private void UpdateTotalGameObjectCount()
        {
            this.lastGameObjectCount = this.TotalGameObjectCount();
        }

        /// <summary>
        /// Calculates the current count of GameObjects with the assigned tag and provides it as the next performance metric value.
        /// </summary>
        /// <returns>The current count of GameObjects with the assigned tag.</returns>
        protected override float GetNextValue()
        {
            // Return the current count of GameObjects with the assigned tag.
            return this.lastGameObjectCount;
        }
    }
}
