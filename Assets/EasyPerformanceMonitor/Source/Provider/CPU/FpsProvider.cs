// Microsoft
using System;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Calculates the frames per second, independent from the time scale.
    /// </summary>
    /// <remarks>
    /// The <see cref="FpsProvider"/> class is responsible for calculating and providing the frames per second (FPS) value, independent
    /// from the time scale. This component is supported on all platforms, and its calculations are based on the inverse of the
    /// unscaled delta time.
    /// <br>-> Recommended Desktop FPS - Thresholds above: Good 60 fps Warning 30 fps</br>
    /// <br>-> Recommended Mobile FPS - Thresholds above: Good 45 fps Warning 30 fps</br>
    /// <br>-> Recommended Console FPS - Thresholds above: Good 45 fps Warning 30 fps</br>
    /// </remarks>
    public class FpsProvider : APerformanceProvider
    {
        /// <summary>
        /// The performance component name.
        /// </summary>
        public const String CName = "FPS";

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
        /// The Fps component is supported on all platforms.
        /// </summary>
        public override bool IsSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The default unit is frames per second.
        /// </summary>
        public override String Unit
        {
            get
            {
                return "fps";
            }
        }

        /// <summary>
        /// Stores the last frames per second value.
        /// </summary>
        private float lastValue = 0;

        /// <summary>
        /// Calculates the frames per second.
        /// </summary>
        protected override void Update()
        {
            // Call the base method.
            base.Update();

            // Calculate the frames per second.
            this.lastValue = 1f / Time.unscaledDeltaTime;
        }

        /// <summary>
        /// Return the next fps value.
        /// </summary>
        /// <returns></returns>
        protected override float GetNextValue()
        {
            return this.lastValue;
        }
    }
}
