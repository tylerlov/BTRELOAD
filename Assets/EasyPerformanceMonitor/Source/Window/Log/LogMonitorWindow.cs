// Microsof
using System;
using System.IO;
using System.Reflection;
using System.Text;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Persistent;

namespace GUPS.EasyPerformanceMonitor.Window
{
    /// <summary>
    /// A specialized monitor window for displaying unity log messages with customizable settings such as log levels,
    /// maximum log lines, and styling for different log types (Info, Warning, Error).
    /// </summary>
    /// <remarks>
    /// The <see cref="LogMonitorWindow"/> class extends the <see cref="MonitorWindow"/> base class to provide a
    /// window for monitoring and displaying log messages. It features a UI Text element for log display, a scrollbar,
    /// configurable log level threshold, and the ability to customize the appearance of different log types.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class LogMonitorWindow : MonitorWindow
    {
        [Header("Log Window - Settings")]

        /// <summary>
        /// The UI Text component responsible for displaying log messages.
        /// </summary>
        [Tooltip("The log text UI element.")]
        public UnityEngine.UI.Text LogText;

        /// <summary>
        /// The UI Scrollbar element associated with the log text.
        /// </summary>
        [Tooltip("The scrollbar UI element.")]
        public UnityEngine.UI.Scrollbar LogScrollbar;

        /// <summary>
        /// The log level from which log messages will be displayed.
        /// Info = 2, Warning = 1, Error/Exception = 0. Default is Info (2).
        /// </summary>
        [Tooltip("The log level from which to display log messages. Info = 2, Warning = 1, Error/Exception = 0. Default is Info (2).")]
        [Range(0, 2)]
        public int LogLevel = 2;

        /// <summary>
        /// The maximum number of log lines to display in the log window.
        /// </summary>
        [Tooltip("The maximum number of log lines to display.")]
        [Range(1, 100)]
        public int LogMaxLines = 15;

        /// <summary>
        /// The maximum length of a log line until it will be truncated.
        /// </summary>
        public const int CMaxLineLength = 500;

        /// <summary>
        /// Array to store log lines.
        /// </summary>
        private String[] logLines = new String[15];

        /// <summary>
        /// Concatenated log text for display.
        /// </summary>
        private String logText;

        /// <summary>
        /// Flag to indicate when log text needs to be refreshed.
        /// </summary>
        private bool needRefresh = false;

        /// <summary>
        /// Save the log to a file in 'Application.persistentDataPath'.
        /// </summary>
        [Tooltip("Save the log to a file in 'Application.persistentDataPath'.")]
        public bool SaveToFile = false;

        /// <summary>
        /// The log file writer.
        /// </summary>
        private StringFileWriter logFileWriter;

        /// <summary>
        /// Flag to indicate if the log window has been fully set up.
        /// </summary>
        private bool isSetup = false;

        /// <summary>
        /// Enables the log message callback when the component is enabled.
        /// </summary>
        protected override void OnEnable()
        {
            // Call the base method.
            base.OnEnable();

            // Enable the log message callback.
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        /// <summary>
        /// Sets up the log window, and initializes log lines.
        /// </summary>
        protected override void Start()
        {
            // Call the base method.
            base.Start();

            // Setup the window.
            this.LogText.text = "";

            // Setup the log lines.
            this.logLines = new String[this.LogMaxLines];

            // Finish the setup.
            this.isSetup = true;
        }

        /// <summary>
        /// Callback method for processing log messages.
        /// </summary>
        /// <param name="_Log">The log message.</param>
        /// <param name="_Stacktrace">The stack trace associated with the log.</param>
        /// <param name="_Type">The type of log (Info, Warning, Error).</param>
        private void OnLogMessageReceived(String _Log, String _Stacktrace, LogType _Type)
        {
            // If the window is not set up, do nothing.
            if (!this.isSetup)
            {
                return;
            }

            // Compose a new log line.
            StringBuilder var_LineBuilder = new StringBuilder();

            // Append the time.
            var_LineBuilder.Append("[");
            var_LineBuilder.Append(DateTime.Now.ToString("HH:mm:ss"));
            var_LineBuilder.Append("]");

            var_LineBuilder.Append(" ");

            // Append the log type.
            var_LineBuilder.Append("[");
            var_LineBuilder.Append(_Type.ToString());
            var_LineBuilder.Append("]");

            var_LineBuilder.Append(" ");

            // Append the log message based on log type.
            switch (_Type)
            {
                case LogType.Log:
                    if (this.LogLevel < 2)
                    {
                        return;
                    }
                    var_LineBuilder.Append(_Log);
                    break;
                case LogType.Warning:
                    if (this.LogLevel < 1)
                    {
                        return;
                    }
                    var_LineBuilder.Append(String.Format("<color=#F1E31A>{0}</color>", _Log));
                    break;
                case LogType.Assert:
                case LogType.Error:
                case LogType.Exception:
                    var_LineBuilder.Append(String.Format("<color=#E72D2D>{0}: {1}</color>", _Log, _Stacktrace.Trim()));
                    break;
                default:
                    var_LineBuilder.Append(_Log);
                    break;
            }

            // Build the new log line.
            String var_LogLine = var_LineBuilder.ToString();

            // Truncate the string if it is too long.
            if (var_LogLine.Length > CMaxLineLength)
            {
                var_LogLine = var_LogLine.Substring(0, CMaxLineLength);
            }

            // Push new line to the start of the array and shift the array to the right.
            for (int i = this.logLines.Length - 1; i >= 0; i--)
            {
                // Push to the right.
                if (i > 0)
                {
                    this.logLines[i] = this.logLines[i - 1];
                }

                // Store the current line.
                if (i == 0)
                {
                    this.logLines[i] = var_LogLine;
                }
            }

            // Build the log text.
            StringBuilder var_LogTextBuilder = new StringBuilder();

            // Append the log lines.
            for (int i = 0; i < this.logLines.Length; i++)
            {
                var_LogTextBuilder.AppendLine(this.logLines[i]);
            }

            // Update the log text.
            this.logText = var_LogTextBuilder.ToString();

            // Set the refresh flag.
            this.needRefresh = true;

            // If the user wants to save the log to a file, append the new log line to the file.
            if (this.SaveToFile)
            {
                // If the log file writer is null, initialize it.
                if (this.logFileWriter == null)
                {
                    // Get the current time.
                    DateTime var_CurrentDateTime = DateTime.Now;

                    // Subtract the game start time.
                    var_CurrentDateTime.AddSeconds(-Time.realtimeSinceStartup);

                    // Format the date time.
                    String var_FormattedDateTime = var_CurrentDateTime.ToString("yyyy.MM.dd_HH.mm.ss");

                    // Create the file path.
                    String var_FilePath = Path.Combine(Application.persistentDataPath, var_FormattedDateTime + "_Log.txt");

                    // Initialize the log file writer.
                    this.logFileWriter = new StringFileWriter(var_FilePath);
                }

                // Write the log to the file.
                this.logFileWriter.Write(var_LogLine);
            }
        }

        /// <summary>
        /// Updates the log text in the UI Text component.
        /// </summary>
        protected override void Update()
        {
            // Call the base update method.
            base.Update();

            // Update the log text if needed.
            if (this.needRefresh)
            {
                this.needRefresh = false;

                this.LogText.text = this.logText;
            }
        }

        /// <summary>
        /// Disables the log message callback when the component is disabled.
        /// </summary>
        protected override void OnDisable()
        {
            // Call the base method.
            base.OnDisable();

            // Disable the log message callback.
            Application.logMessageReceived -= OnLogMessageReceived;
        }
    }
}
