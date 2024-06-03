namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Represents an interface for text renderers, defining the contract for rendering textual information.
    /// </summary>
    /// <remarks>
    /// The <see cref="ITextRenderer"/> interface extends the functionality of <see cref="IRenderer"/>
    /// and provides methods specifically for rendering text. Implementing classes are expected to adhere to
    /// this contract, allowing for consistent handling of text rendering in diverse contexts.
    /// </remarks>
    public interface ITextRenderer : IRenderer
    {
        /// <summary>
        /// Gets a value indicating whether the displayed values should be scaled.
        /// </summary>
        bool Scale { get;}

        /// <summary>
        /// Refresh the text renderer on editor value changed by the user.
        /// </summary>
        void RefreshText();
    }
}