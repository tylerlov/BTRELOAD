// Microsoft
using System;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Observer;

namespace GUPS.EasyPerformanceMonitor.Provider
{
    /// <summary>
    /// Provides the operating system.
    /// </summary>
    /// <remarks>
    /// The <see cref="OperatingSystemProvider"/> class inherits from <see cref="AProvider<String>"/> and provides 
    /// the current operating system.
    /// </remarks>
    [Serializable]
    [Obfuscation(Exclude = true)]
    public class OperatingSystemProvider : AProvider<IProvidedData<String>>
    {
        /// <summary>
        /// The performance component name for the provider.
        /// </summary>
        public const String CName = "Operating System";

        /// <summary>
        /// Gets the component name.
        /// </summary>
        public override string Name => CName;

        /// <summary>
        /// Gets a value indicating whether the component is supported on the current platform.
        /// The OperatingSystemProvider is supported on all platforms.
        /// </summary>
        public override bool IsSupported => true;
        
        /// <summary>
        /// If the provider is active, send the system information to the observers.
        /// </summary>
        protected virtual void Start()
        {
            // If the provider is active, send the system information to the observers.
            if(this.IsActive)
            {
                // Get the current operating system.
                String var_System = SystemInfo.operatingSystem;

                // Notify the observers with the current operating system.
                foreach (var var_Observer in this.ObserverList)
                {
                    var_Observer.OnNext(new ProvidedData<String>(this, var_System));
                }
            }
        }
    }
}
