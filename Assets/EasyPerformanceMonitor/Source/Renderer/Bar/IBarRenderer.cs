namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Represents an interface for bar renderers, defining the contract for rendering bar-based information.
    /// </summary>
    public interface IBarRenderer : IRenderer
    {
        /// <summary>
        /// Refresh the bar renderer on editor value changed by the user.
        /// </summary>
        void RefreshBar();
    }
}
