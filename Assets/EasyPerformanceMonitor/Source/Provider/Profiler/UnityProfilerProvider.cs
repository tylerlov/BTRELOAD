// Microsoft
using System;
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;
using Unity.Profiling;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    [Serializable]
    [Obfuscation(Exclude = true)]
    public class UnityProfilerProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "Unity Profiler";

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
        /// The Profiler component is supported on all platforms.
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
                return "";
            }
        }

        /// <summary>
        /// The category of the profiler recorder.
        /// </summary>
        [SerializeField]
        public String Category = ProfilerCategory.Audio.Name;

        /// <summary>
        /// The name of the status to read from the recorder.
        /// </summary>
        [SerializeField]
        public String StatusName = String.Empty;

        /// <summary>
        /// Is a custom user assigned profiler status.
        /// </summary>
        [SerializeField]
        public bool IsCustomStatus = false;

        /// <summary>
        /// Used to read data from the recorder
        /// </summary>
        private ProfilerRecorder recorder;

        /// <summary>
        /// Initialize the performance provider and profiler recorder.
        /// </summary>
        protected override void Awake()
        {
            // Call base method.
            base.Awake();

            // Get the profiler category.
            ProfilerCategory var_ProfilerCategory = new ProfilerCategory(this.Category);

            // Start new recorder.
            this.recorder = ProfilerRecorder.StartNew(var_ProfilerCategory, this.StatusName);
        }

        /// <summary>
        /// Find the current profiler value and adds it to the graph.
        /// </summary>
        /// <returns></returns>
        protected override float GetNextValue()
        {
            // Get the current value.
            long var_CurrentValue = this.recorder.LastValue;

            // Return value.
            return var_CurrentValue;
        }

        /// <summary>
        /// Refresh the recorder.
        /// </summary>
        public override void Refresh()
        {
            // Call base method.
            base.Refresh();

            // Refresh recorder.
            this.recorder.Stop();
            this.recorder.Dispose();

            // Get the profiler category.
            ProfilerCategory var_ProfilerCategory = new ProfilerCategory(this.Category);

            // Start new recorder.
            this.recorder = ProfilerRecorder.StartNew(var_ProfilerCategory, this.StatusName);
        }
    }
}
