// Microsof
using GUPS.EasyPerformanceMonitor.Persistent;
using System;
using System.IO;
using System.Reflection;

// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Window
{
    /// <summary>
    /// A monitor window designed to display system-related information, such as operating system details,
    /// device specifications, processor information, memory statistics, graphic device details, and graphic memory size.
    /// </summary>
    /// <remarks>
    /// The <see cref="SystemMonitorWindow"/> class extends the <see cref="MonitorWindow"/> base class and is designed
    /// to showcase key system metrics. It provides UI Text components to display information related to the
    /// operating system, device, processor, memory, graphic device, and graphic memory size. The details are retrieved
    /// using <see cref="SystemInfo"/> and presented in a formatted manner for better readability.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class SystemMonitorWindow : MonitorWindow
    {
        [Header("System Window - Settings")]
        /// <summary>
        /// The UI Text component displaying the operating system information.
        /// </summary>
        [Tooltip("The UI Text component displaying the operating system information.")]
        public UnityEngine.UI.Text OperatingSystemText;

        /// <summary>
        /// The UI Text component displaying device-related information.
        /// </summary>
        [Tooltip("The UI Text component displaying device-related information.")]
        public UnityEngine.UI.Text DeviceText;

        /// <summary>
        /// The UI Text component displaying processor details.
        /// </summary>
        [Tooltip("The UI Text component displaying processor details.")]
        public UnityEngine.UI.Text ProcessorText;

        /// <summary>
        /// The UI Text component displaying memory-related information.
        /// </summary>
        [Tooltip("The UI Text component displaying memory-related information.")]
        public UnityEngine.UI.Text MemorySizeText;

        /// <summary>
        /// The UI Text component displaying graphic device details.
        /// </summary>
        [Tooltip("The UI Text component displaying graphic device details.")]
        public UnityEngine.UI.Text GraphicDeviceText;

        /// <summary>
        /// The UI Text component displaying graphic memory size information.
        /// </summary>
        [Tooltip("The UI Text component displaying graphic memory size information.")]
        public UnityEngine.UI.Text GraphicMemorySizeText;

        /// <summary>
        /// Save the system information on start to a file in 'Application.persistentDataPath'.
        /// </summary>
        [Tooltip("Save the system information on start to a file in 'Application.persistentDataPath'.")]
        public bool SaveToFile = false;

        /// <summary>
        /// Initializes the System Monitor window and populates UI Text components with system information.
        /// </summary>
        protected override void Start()
        {
            // Call the base method.
            base.Start();

            // Build and display the operating system information.
            String var_OperatingSystem = SystemInfo.operatingSystem;
            String var_OperatingSystemFamily = SystemInfo.operatingSystemFamily.ToString();
            this.OperatingSystemText.text = var_OperatingSystemFamily + " - " + var_OperatingSystem;

            // Build and display the device information.
            String var_DeviceModel = SystemInfo.deviceModel;
            String var_DeviceType = SystemInfo.deviceType.ToString();
            this.DeviceText.text = var_DeviceType + " - " + var_DeviceModel;

            // Build and display the processor information.
            String var_ProcessorType = SystemInfo.processorType;
            String var_ProcessorCount = SystemInfo.processorCount.ToString();
            String var_ProcessorFrequency = String.Format("{0:0.0}{1}", SystemInfo.processorFrequency * .001f, "GHz");
            this.ProcessorText.text = var_ProcessorType + " (" + var_ProcessorCount + "x " + var_ProcessorFrequency + ")";

            // Build and display the memory information.
            String var_MemorySize = String.Format("{0:0.0}{1}", SystemInfo.systemMemorySize / 1024f, "GB");
            this.MemorySizeText.text = var_MemorySize;

            // Build and display the graphic device information.
            String var_GraphicsDeviceName = SystemInfo.graphicsDeviceName;
            String var_GraphicsDeviceType = SystemInfo.graphicsDeviceType.ToString();
            this.GraphicDeviceText.text = var_GraphicsDeviceName + " (" + var_GraphicsDeviceType + ")";

            // Build and display the graphic memory information.
            String var_GraphicsMemorySize = String.Format("{0:0.0}{1}", SystemInfo.graphicsMemorySize / 1024f, "GB");
            this.GraphicMemorySizeText.text = var_GraphicsMemorySize;

            // If the user wants to save the system info to a file, store it here.
            if (this.SaveToFile)
            {
                // Get the current time.
                DateTime var_CurrentDateTime = DateTime.Now;

                // Subtract the game start time.
                var_CurrentDateTime.AddSeconds(-Time.realtimeSinceStartup);

                // Format the date time.
                String var_FormattedDateTime = var_CurrentDateTime.ToString("yyyy.MM.dd_HH.mm.ss");

                // Create the file path.
                String var_FilePath = Path.Combine(Application.persistentDataPath, var_FormattedDateTime + "_SystemInfo.txt");

                // Initialize the file writer.
                StringFileWriter var_Writer = new StringFileWriter(var_FilePath);

                // Write the system info to the file.
                var_Writer.Write("Operating System: " + var_OperatingSystemFamily + " - " + var_OperatingSystem);
                var_Writer.Write("Device: " + var_DeviceType + " - " + var_DeviceModel);
                var_Writer.Write("Processor: " + var_ProcessorType + " (" + var_ProcessorCount + "x " + var_ProcessorFrequency + ")");
                var_Writer.Write("Memory: " + var_MemorySize);
                var_Writer.Write("Graphic Device: " + var_GraphicsDeviceName + " (" + var_GraphicsDeviceType + ")");
                var_Writer.Write("Graphic Memory: " + var_GraphicsMemorySize);

                // Flush the file writer.
                var_Writer.Flush();

                // Dispose the file writer.
                var_Writer.Dispose();
            }
        }
    }
}
