namespace GUPS.EasyPerformanceMonitor.Platform
{
    /// <summary>
    /// Represents the possible platforms that an application or system can target.
    /// </summary>
    /// <remarks>
    /// The <see cref="EPlatform"/> enum is used to categorize different types of platforms,
    /// facilitating conditional logic and platform-specific behavior in software development.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to use the <see cref="EPlatform"/> enum:
    /// <code>
    /// // Check if the current platform is a desktop environment
    /// if (currentPlatform == EPlatform.Desktop)
    /// {
    ///     // Perform desktop-specific operations
    ///     Console.WriteLine("Running on a desktop platform.");
    /// }
    /// </code>
    /// </example>
    public enum EPlatform : byte
    {
        /// <summary>
        /// Represents an unknown or undefined platform.
        /// </summary>
        /// <remarks>
        /// This value is typically used as a default or fallback when the platform information is not available or cannot be determined.
        /// </remarks>
        Unknown = 0,

        /// <summary>
        /// Represents a desktop platform.
        /// </summary>
        /// <remarks>
        /// Desktop platforms include traditional personal computers and workstations.
        /// </remarks>
        Desktop = 1,

        /// <summary>
        /// Represents a mobile platform.
        /// </summary>
        /// <remarks>
        /// Mobile platforms include smartphones, tablets, and other handheld devices.
        /// </remarks>
        Mobile = 2,

        /// <summary>
        /// Represents a console platform.
        /// </summary>
        /// <remarks>
        /// Console platforms include dedicated gaming consoles and similar devices.
        /// </remarks>
        Console = 3
    }
}
