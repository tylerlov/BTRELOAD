// Microsoft
using System;

// Unity
using UnityEngine.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetches the total reserved memory of the application. The total memory Unity has reserved for current and future allocations. If the 
    /// reserved memory is fully used, Unity will allocate more memory from the system as required.
    /// </summary>
    /// <remarks>
    /// The <see cref="ReservedMemoryProvider"/> class is responsible for fetching and providing information about the reserved memory used 
    /// by the application. This component is supported on all platforms and uses the Unity Profiler.GetTotalReservedMemoryLong method to 
    /// calculate the total reserved memory used by the application in bytes.
    /// <br>-> Recommended Desktop memory - Thresholds above: Warning 8GB Critical 12GB</br>
    /// <br>-> Recommended Mobile memory - Thresholds above: Warning 200MB Critical 500MB</br>
    /// <br>-> Recommended Console memory - Thresholds above: Warning 4GB Critical 6GB</br>
    /// </remarks>
    public class ReservedMemoryProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Reserved Memory";

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
        /// The Reserved Memory component is supported on all platforms.
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
        /// Calculates the current reserved memory and adds it to the graph.
        /// </summary>
        /// <returns></returns>
        protected override float GetNextValue()
        { 
            // Get the total memory reserved by the application in bytes.
            long var_CurrentMemory = Profiler.GetTotalReservedMemoryLong();

            // Add value.
            return var_CurrentMemory;
        }
    }
}
