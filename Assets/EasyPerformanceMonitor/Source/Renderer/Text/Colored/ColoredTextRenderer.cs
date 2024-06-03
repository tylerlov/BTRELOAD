// Microsoft
using System.Reflection;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Text renderer implementation for a colored graph displaying the min, max, and mean values as text of the subscribed 
    /// performance providers on the same GameObject.
    /// </summary>
    /// <remarks>
    /// The <see cref="ColoredTextRenderer"/> class extends the functionality of <see cref="RatedTextRenderer"/> to provide
    /// text rendering for visualizing performance metrics from subscribed providers on a colored graph. It displays the 
    /// minimum, maximum, and mean values, providing options to show suffixes for scaled values based on the associated 
    /// performance provider's settings.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public class ColoredTextRenderer : RatedTextRenderer
    {
    }
}
