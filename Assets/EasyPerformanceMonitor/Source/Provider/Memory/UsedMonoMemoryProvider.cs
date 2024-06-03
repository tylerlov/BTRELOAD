// Microsoft
using System;

// Unity
using UnityEngine.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetches the total managed-memory of the application. The amount of allocated managed-memory (The memory allocated through the code) for 
    /// all objects, both live and non-collected (garbage collected).
    /// </summary>
    /// <remarks>
    /// The <see cref="UsedMonoMemoryProvider"/> class is responsible for fetching and providing information about the total allocated 
    /// managed memory used by the application. This component is supported on all platforms and uses the Unity Profiler.GetMonoUsedSizeLong 
    /// method to calculate the total allocated managed memory used by the application in bytes.
    /// <br>-> Recommended Desktop memory - Thresholds above: Warning 8GB Critical 12GB</br>
    /// <br>-> Recommended Mobile memory - Thresholds above: Warning 200MB Critical 500MB</br>
    /// <br>-> Recommended Console memory - Thresholds above: Warning 4GB Critical 6GB</br>
    /// </remarks>
    public class UsedMonoMemoryProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Used Mono Memory";

        /// <summary>
        /// Returns the performance component name.
        /// </summary>
        public override string Name
        {
            get
            {
                return CName;
            }
        }

        /// <summary>
        /// Returns true if the performance component is supported on the current platform.
        /// The Used Mono Memory component is supported on all platforms.
        /// </summary>
        public override bool IsSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The default unit is bytes.
        /// </summary>
        public override String Unit
        {
            get
            {
                return "B";
            }
        }

        /// <summary>
        /// Calculates the current total allocated managed memory and adds it to the graph.
        /// </summary>
        /// <returns></returns>
        protected override float GetNextValue()
        {
            // Get the total allocated managed memory by the application in bytes.
            long var_CurrentMemory = Profiler.GetMonoUsedSizeLong();

            // Add value.
            return var_CurrentMemory;
        }
    }
}
