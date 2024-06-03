// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Represents an interface for a graph renderer used to visualize one or multiple graphs on a Unity UI Image component.
    /// </summary>
    /// <remarks>
    /// The <see cref="IGraphRenderer"/> interface defines the properties necessary for configuring and interacting with
    /// a graph renderer component. Implementing classes are expected to provide functionality for rendering graphs on a
    /// specified target image, utilizing a specific shader or its mobile variant. Additionally, various rendering
    /// characteristics such as graph type, interpolation, and anti-aliasing settings are exposed through this interface.
    /// </remarks>
    public interface IGraphRenderer : IRenderer
    {
        /// <summary>
        /// The image component used for rendering the graph.
        /// </summary>
        public UnityEngine.UI.Image Target { get; }

        /// <summary>
        /// Gets the shader used for rendering the graph.
        /// </summary>
        Shader GraphShader { get; }

        /// <summary>
        /// Gets a value indicating whether the graph is rendered as a Line or Bar graph.
        /// </summary>
        bool IsLine { get; }

        /// <summary>
        /// Gets a value indicating whether interpolation is applied between values.
        /// </summary>
        bool IsSmooth { get; }

        /// <summary>
        /// Gets a value indicating whether the graph rendering has anti-aliasing enabled.
        /// </summary>
        bool HasAntiAliasing { get; }

        /// <summary>
        /// Gets the number of graph values associated with the renderer.
        /// </summary>
        int GraphValues { get; }

        /// <summary>
        /// Pass parameter to the graph shader on editor value changed by the user.
        /// </summary>
        void RefreshGraph();
    }
}