// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Platform
{
    /// <summary>
    /// Provides utility methods for retrieving information about the current platform in a cross-platform manner.
    /// </summary>
    /// <remarks>
    /// The <see cref="PlatformHelper"/> class includes a method, <see cref="GetCurrentPlatform"/>, which returns
    /// an instance of the <see cref="EPlatform"/> enum based on the runtime platform of the application.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to use the <see cref="PlatformHelper"/> class to obtain the current platform:
    /// <code>
    /// // Retrieve the current platform
    /// EPlatform currentPlatform = PlatformHelper.GetCurrentPlatform();
    /// 
    /// // Perform platform-specific operations
    /// switch (currentPlatform)
    /// {
    ///     case EPlatform.Desktop:
    ///         Console.WriteLine("Running on a desktop platform.");
    ///         break;
    ///     case EPlatform.Mobile:
    ///         Console.WriteLine("Running on a mobile platform.");
    ///         break;
    ///     case EPlatform.Console:
    ///         Console.WriteLine("Running on a console platform.");
    ///         break;
    ///     case EPlatform.Unknown:
    ///         Console.WriteLine("Platform information is unknown.");
    ///         break;
    /// }
    /// </code>
    /// </example>
    public static class PlatformHelper
    {
        /// <summary>
        /// Retrieves the current platform based on the runtime platform of the application.
        /// </summary>
        /// <returns>
        /// An instance of the <see cref="EPlatform"/> enum representing the current platform.
        /// </returns>
        /// <remarks>
        /// The method uses the Unity <see cref="Application.platform"/> property to determine the runtime platform,
        /// and maps it to the corresponding <see cref="EPlatform"/> enum value.
        /// </remarks>
        public static EPlatform GetCurrentPlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                    return EPlatform.Desktop;

                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    return EPlatform.Mobile;

                case RuntimePlatform.PS4:
                case RuntimePlatform.XboxOne:
                case RuntimePlatform.Switch:
                    return EPlatform.Console;

                default:
                    return EPlatform.Unknown;
            }
        }
    }
}
