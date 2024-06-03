// Microsoft
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Singleton;
using GUPS.EasyPerformanceMonitor.Window;

namespace GUPS.EasyPerformanceMonitor
{
    /// <summary>
    /// The singleton class responsible for managing and controlling the display of performance monitor windows.
    /// </summary>
    /// <remarks>
    /// The <see cref="PerformanceMonitor"/> class ensures that only one instance of the performance monitor exists,
    /// and it provides functionality to show or hide monitor windows. It also supports configuration options to control
    /// its behavior, such as restricting activation to editor or development builds, and specifying whether to show the
    /// monitor windows on startup.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class PerformanceMonitor : PersistentSingleton<PerformanceMonitor>
    {
#pragma warning disable

        /// <summary>
        /// If enabled, the monitor is only active in the editor or development builds (recommended to set it to true).
        /// </summary>
        [SerializeField]
        private bool onlyInDevelopmentBuild = true;

#pragma warning restore

        /// <summary>
        /// If enabled, the performance monitor will be shown at start.
        /// </summary>
        [SerializeField]
        private bool showOnStart = true;

        /// <summary>
        /// Setup the singleton and add the needed performance components (if they are supported for the current platform).
        /// </summary>
        protected override void Awake()
        {
            // Init singleton.
            base.Awake();

            // Check if the performance monitor should be deactivated.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
#else
            if(this.onlyInDevelopmentBuild)
            {
                this.gameObject.SetActive(false);
            }
#endif

            // Hide the performance monitor at start, if deactivated.
            if (!this.showOnStart)
            {
                // Get all monitor windows.
                List<MonitorWindow> var_MonitorWindows = this.GetMonitorWindows();

                // Hide all monitor windows.
                var_MonitorWindows.ForEach(var_MonitorWindow => var_MonitorWindow.Toggle(false));
            }
        }

        /// <summary>
        /// Get all monitor windows in the children.
        /// </summary>
        /// <returns>A list of monitor windows.</returns>
        public List<MonitorWindow> GetMonitorWindows()
        {
            // Get all monitor windows.
            List<MonitorWindow> var_MonitorWindows = new List<MonitorWindow>(this.GetComponentsInChildren<MonitorWindow>());

            // Return the monitor windows.
            return var_MonitorWindows;
        }
    }
}