// Microsoft
using System;

// Unity
using Unity.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetchs the gpu draw calls. 
    /// </summary>
    public class GpuDrawCallsProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Gpu Draws";

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
        /// The Gpu Draw Calls component is supported on all platforms.
        /// </summary>
        public override bool IsSupported
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// Has no default unit.
        /// </summary>
        public override String Unit
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Used to read draw calls count.
        /// </summary>
        private ProfilerRecorder drawCallsCountRecorder;

        /// <summary>
        /// Initialize the performance provider and the draw calls count recorder.
        /// </summary>
        protected override void Awake()
        {
            // Call base method.
            base.Awake();

            // Init recorder.
            this.drawCallsCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        }

        /// <summary>
        /// Return the next gpu frame time value.
        /// </summary>
        protected override float GetNextValue()
        {
            // Get the current draw calls count.
            long var_CurrentDrawCallsCount = this.drawCallsCountRecorder.LastValue;

            // Return value.
            return var_CurrentDrawCallsCount;
        }
    }
}
