// Microsoft
using System;

// Unity
using Unity.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetches the rendered and visible triangle count.
    /// </summary>
    /// <remarks>
    /// The <see cref="GpuTrianglesProvider"/> class is responsible for fetching and providing information about the rendered and visible
    /// triangle count on the GPU. This component is supported on all platforms and uses the Unity ProfilerRecorder to capture
    /// the triangle count data.
    /// </remarks>
    public class GpuTrianglesProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Tris";

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
        /// The Gpu Triangles Count component is supported on all platforms.
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
        /// Used to read the triangle count.
        /// </summary>
        private ProfilerRecorder triangleCountRecorder;

        /// <summary>
        /// Initialize the performance provider and the triangle count recorder.
        /// </summary>
        protected override void Awake()
        {
            // Call base method.
            base.Awake();

            // Start the recorder.
            this.triangleCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        /// <summary>
        /// Fetchs the current rendered triangles and adds it to the graph.
        /// </summary>
        protected override float GetNextValue()
        {
            // Get the current rendered triangles.
            long var_CurrentTriangles = this.triangleCountRecorder.LastValue;

            // Return value.
            return var_CurrentTriangles;
        }
    }
}
