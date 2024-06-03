// Microsoft
using System;

// Unity
using UnityEngine;
using Unity.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetches the time in milliseconds the CPU needs per frame. Recommend a development build or the activation of 'Frame Time Stats'.
    /// </summary>
    /// <remarks>
    /// The <see cref="CpuFrameTimeProvider"/> class is responsible for retrieving the CPU frame time, which represents the time the
    /// CPU requires to process each frame in milliseconds. It is designed to work with the Unity FrameTimingManager, and it
    /// requires a development build or the activation of 'Frame Time Stats' for accurate measurements.
    /// <br>-> Recommended Desktop frame time - Thresholds above: Warning 22ms (45 fps) Critical 33ms (30 fps)</br>
    /// <br>-> Recommended Mobile frame time - Thresholds above: Warning 33ms (30 fps) Critical 50ms (20 fps)</br>
    /// <br>-> Recommended Console frame time - Thresholds above: Warning 33ms (30 fps) Critical 50ms (20 fps)</br>
    /// </remarks>
    public class CpuFrameTimeProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "CPU";

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
        /// The Cpu Time component works best if the Unity FrameTimingManager is activated.
        /// </summary>
        public override bool IsSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The default unit is milliseconds.
        /// </summary>
        public override String Unit
        {
            get
            {
                return "ms";
            }
        }

#if UNITY_2022_1_OR_NEWER

        /// <summary>
        /// Used to read frame timings.
        /// </summary>
        private FrameTiming[] frameTimings = new FrameTiming[1];

#endif

        /// <summary>
        /// Used to read data from the recorder, if the frame time stats are not enabled.
        /// </summary>
        private ProfilerRecorder recorderCpu;

        /// <summary>
        /// Stores the last cpu frame time value.
        /// </summary>
        private float lastValue = 0;

        /// <summary>
        /// Initialize the performance provider and profiler recorder.
        /// </summary>
        protected override void Awake()
        {
            // Call base method.
            base.Awake();

#if UNITY_2022_1_OR_NEWER
            
            // If the frame time stats are not enabled, then use the profiler.
            if(!FrameTimingManager.IsFeatureEnabled())
            {
                // Start new recorder.
                this.recorderCpu = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            }

#else

            // Start new recorder.
            this.recorderCpu = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");

#endif
        }


        /// <summary>
        /// Read frame timings.
        /// </summary>
        private void FixedUpdate()
        {
#if UNITY_2022_1_OR_NEWER

            // If the frame time stats are enabled, then use the FrameTimingManager...
            if(FrameTimingManager.IsFeatureEnabled())
            {
                FrameTimingManager.CaptureFrameTimings();

                uint var_Available_FrameTimings = FrameTimingManager.GetLatestTimings((uint)this.frameTimings.Length, this.frameTimings);

                if (var_Available_FrameTimings > 0)
                {
                    this.lastValue = (float)this.frameTimings[0].cpuFrameTime;
                }
            }
            // ...else use the profiler.  
            else
            {
                // Unity records the cpu frame time in nanoseconds, so we need to convert it to milliseconds.
                this.lastValue = this.recorderCpu.LastValue * 1e-6f;
            }

#else

            // Unity records the cpu frame time in nanoseconds, so we need to convert it to milliseconds.
            this.lastValue = this.recorderCpu.LastValue * 1e-6f;

#endif
        }

        /// <summary>
        /// Return the next gpu frame time value.
        /// </summary>
        protected override float GetNextValue()
        {
            return this.lastValue;
        }
    }
}
