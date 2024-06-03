// Microsof
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Window
{
    /// <summary>
    /// Represents a specialized monitor window for displaying performance-related information. 
    /// Extends the base functionality provided by <see cref="MonitorWindow"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="PerformanceMonitorWindow"/> class inherits from <see cref="MonitorWindow"/> and adds 
    /// functionality specific to performance monitoring. It automatically checks the activity status of 
    /// performance providers associated with child game objects and sets the window to be inactive if all providers are inactive.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class PerformanceMonitorWindow : MonitorWindow
    {
        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the monitor window by checking the activity status of performance providers 
        /// associated with child game objects and sets the window to be inactive if all providers are inactive. 
        /// Additionally, places the monitor elements based on the specified position.
        /// </summary>
        protected override void Start()
        {
            // Call the base method.
            base.Start();

            // Iterate all canvas child transforms.
            foreach (Transform var_Child in this.MonitorCanvas.transform)
            {
                // Get the child game object.
                GameObject var_ChildGameObject = var_Child.gameObject;

                // Get all performance providers.
                List<IProvider> var_Provider = new List<IProvider>(var_ChildGameObject.GetComponentsInChildren<IProvider>());

                // If all provider are not active, set the game object to inactive.
                if(var_Provider.TrueForAll(var_Provider => !var_Provider.IsActive))
                {
                    var_ChildGameObject.SetActive(false);
                }
            }

            // Replace the monitor elements.
            this.PlaceMonitorElements();
        }
    }
}
