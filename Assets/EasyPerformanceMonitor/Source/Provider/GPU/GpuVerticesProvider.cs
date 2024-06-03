// Microsoft
using System;

// Unity
using Unity.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetches the rendered and visible vertice count.
    /// </summary>
    /// <remarks>
    /// The <see cref="GpuVerticesProvider"/> class is responsible for fetching and providing information about the rendered and visible
    /// vertice count on the GPU. This component is supported on all platforms and uses the Unity ProfilerRecorder to capture
    /// the triangle count data.
    /// </remarks>
    public class GpuVerticesProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Verts";

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
        /// The Gpu Vertices Count component is supported on all platforms.
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
        /// Used to read the vertices count.
        /// </summary>
        private ProfilerRecorder verticesCountRecorder;

        /// <summary>
        /// Initialize the performance provider and the vertices count recorder.
        /// </summary>
        protected override void Awake()
        {
            // Call base method.
            base.Awake();

            // Start the recorder.
            this.verticesCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        }

        /// <summary>
        /// Fetchs the current rendered vertices and adds it to the graph.
        /// </summary>
        protected override float GetNextValue()
        {
            // Get the current rendered vertices.
            long var_CurrentVertices = this.verticesCountRecorder.LastValue;

            // Return value.
            return var_CurrentVertices;
        }
    }
}
