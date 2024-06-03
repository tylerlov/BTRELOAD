// Microsoft
using System.Reflection;

namespace GUPS.EasyPerformanceMonitor.Window
{
    /// <summary>
    /// Defines the position options for a monitor window.
    /// </summary>
    /// <remarks>
    /// The <see cref="EMonitorWindowPosition"/> enum represents various positions where a monitor window can be
    /// located, providing options such as top, bottom, top-left, top-right, bottom-left, and bottom-right.
    /// Each member corresponds to a specific layout arrangement, guiding the placement of the monitor window.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public enum EMonitorWindowPosition : byte
    {
        /// <summary>
        /// Positions the monitor window on the top (left to right).
        /// </summary>
        Top = 0,

        /// <summary>
        /// Positions the monitor window on the top-left side (top to down).
        /// </summary>
        Top_Left = 1,

        /// <summary>
        /// Positions the monitor window on the top-right side (top to down).
        /// </summary>
        Top_Right = 2,

        /// <summary>
        /// Positions the monitor window on the bottom (left to right).
        /// </summary>
        Bottom = 3,

        /// <summary>
        /// Positions the monitor window on the bottom-left side (down to top).
        /// </summary>
        Bottom_Left = 4,

        /// <summary>
        /// Positions the monitor window on the bottom-right side (down to top).
        /// </summary>
        Bottom_Right = 5,

        /// <summary>
        /// Positions the monitor window freely, without adjusting it to any specific / fixed position.
        /// </summary>
        Free = 10,
    }
}
