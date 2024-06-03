// Microsoft
using System;

// Unity
using UnityEngine;
using Unity.Profiling;

// GUPS
using GUPS.EasyPerformanceMonitor.Platform;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Fetches the time in milliseconds the GPU needs per frame. Recommend a development build or the activation of 'Frame Time Stats'.
    /// </summary>
    /// <remarks>
    /// The <see cref="GpuFrameTimeProvider"/> class is responsible for retrieving the GPU frame time, which represents the time the
    /// GPU requires to process each frame in milliseconds. It is designed to work with the Unity FrameTimingManager, and it
    /// requires a development build or the activation of 'Frame Time Stats' for accurate measurements.
    /// <br>-> Recommended Desktop frame time - Thresholds above: Warning 22ms (45 fps) Critical 33ms (30 fps)</br>
    /// <br>-> Recommended Mobile frame time - Thresholds above: Warning 33ms (30 fps) Critical 50ms (20 fps)</br>
    /// <br>-> Recommended Console frame time - Thresholds above: Warning 33ms (30 fps) Critical 50ms (20 fps)</br>
    /// </remarks>
    public class GpuFrameTimeProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "GPU";

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
        /// The Gpu Time component works best if the Unity FrameTimingManager is activated.
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

        /// <summary>
        /// The active platform.
        /// </summary>
        private EPlatform platform;

#if UNITY_2022_1_OR_NEWER

        /// <summary>
        /// Used to read frame timings.
        /// </summary>
        private FrameTiming[] frameTimings = new FrameTiming[1];

#endif

        /// <summary>
        /// Used to read data from the 'camera renderer' recorder, if the frame time stats are not enabled.
        /// </summary>
        private ProfilerRecorder recorderGpu;

        /// <summary>
        /// In Unity 2021 neither the 'frame time' nor the 'camera renderer' recorder are available on release builds, so we need to use a fallback value.
        /// </summary>
        private float fallBackValue = 0;

        /// <summary>
        /// Stores the last gpu frame time value.
        /// </summary>
        private float lastValue = 0;

        /// <summary>
        /// Initialize the performance provider, check if is supported and get the active platform.
        /// </summary>
        protected override void Awake()
        {
            // Call the base method.
            base.Awake();

            // Get the active platform.
            this.platform = PlatformHelper.GetCurrentPlatform();

#if UNITY_2022_1_OR_NEWER

            // If the frame time stats are not enabled, then use the profiler.
            if (!FrameTimingManager.IsFeatureEnabled())
            {
                // Start new recorder.
                this.recorderGpu = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Camera.Render", options: ProfilerRecorderOptions.GpuRecorder | ProfilerRecorderOptions.SumAllSamplesInFrame);
            }

#else

            // Start new recorder.
            this.recorderGpu = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Camera.Render", options: ProfilerRecorderOptions.GpuRecorder | ProfilerRecorderOptions.SumAllSamplesInFrame);

#endif

        }

        /// <summary>
        /// Calculate the gpu frame time fallback based on the fps.
        /// </summary>
        protected override void Update()
        {
            // Call the base method.
            base.Update();

            // Based on the fps estimate the gpu frame time. DeltaTime is in seconds so we need to convert it to milliseconds.
            this.fallBackValue = UnityEngine.Time.unscaledDeltaTime * 1000f;
        }

        /// <summary>
        /// Read frame timings.
        /// </summary>
        private void FixedUpdate()
        {

#if UNITY_2022_1_OR_NEWER

            // If the frame time stats are enabled, then use the FrameTimingManager...
            if (FrameTimingManager.IsFeatureEnabled())
            {
                FrameTimingManager.CaptureFrameTimings();

                uint var_Available_FrameTimings = FrameTimingManager.GetLatestTimings((uint)this.frameTimings.Length, this.frameTimings);

                if (var_Available_FrameTimings > 0)
                {
                    // On mobile devices there is no dedicated GPU, so the 'GPU frame time' is estimated on the CPU frame time.
                    if (this.platform == EPlatform.Mobile)
                    {
                        this.lastValue = (float)this.frameTimings[0].cpuRenderThreadFrameTime;
                    }
                    else
                    {
                        this.lastValue = (float)this.frameTimings[0].gpuFrameTime;
                    }
                }
            }
            // ...else use the profiler.  
            else
            {
                // Unity records the cpu frame time in nanoseconds, so we need to convert it to milliseconds.
                this.lastValue = this.recorderGpu.LastValue * 1e-6f;

                // If the gpu frame time is 0, we use the fallback value.
                if (this.lastValue == 0)
                {
                    this.lastValue = this.fallBackValue;
                }
            }

#else

            // Unity records the gpu frame time in nanoseconds, so we need to convert it to milliseconds.
            this.lastValue = this.recorderGpu.LastValue * 1e-6f;

            // If the gpu frame time is 0, we use the fallback value.
            if(this.lastValue == 0)
            {
                this.lastValue = this.fallBackValue;
            }

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
