// Microsoft
using System;
using System.Collections.Generic;
using System.Reflection;

// Unity
using UnityEngine;

// GUPS
using GUPS.EasyPerformanceMonitor.Platform;
using GUPS.EasyPerformanceMonitor.Provider;
using GUPS.EasyPerformanceMonitor.Observer;

namespace GUPS.EasyPerformanceMonitor.Renderer
{
    /// <summary>
    /// Abstract implementation of a graph renderer displaying one or multiple graphs on a Unity UI Image component
    /// using data provided by a performance provider on the same GameObject.
    /// </summary>
    /// <remarks>
    /// The <see cref="AGraphRenderer"/> class provides a foundation for rendering performance graphs on a Unity UI Image
    /// component. It handles the subscription and management of performance providers, initialization of the rendering
    /// components, and the rendering process itself. This class is meant to be extended for specific graph rendering
    /// implementations.
    /// </remarks>
    [Obfuscation(Exclude = true)]
    public abstract class AGraphRenderer : MonoBehaviour, IGraphRenderer
    {
        /// <summary>
        /// Gets the list of subscribed performance providers.
        /// </summary>
        /// <remarks>This list is populated by finding all components implementing the IProvider interface on this GameObject.</remarks>
        public List<IProvider> Provider { get; private set; }

        /// <summary>
        /// Stores the unsubscriber for the performance provider.
        /// </summary>
        /// <remarks>This list is used to manage subscriptions and unsubscribe from performance providers during disposal.</remarks>
        private List<IDisposable> unsubscriber;

        /// <summary>
        /// The image component used for rendering the graph.
        /// </summary>
        [SerializeField]
        private UnityEngine.UI.Image target;

        /// <summary>
        /// The image component used for rendering the graph.
        /// </summary>
        public UnityEngine.UI.Image Target { get => this.target; }

        /// <summary>
        /// Gets the shader used for rendering the graph.
        /// </summary>
        [SerializeField]
        private Shader graphShader;

        [SerializeField]
        /// <summary>
        /// Gets the mobile version of the shader used for rendering the graph.
        /// </summary>
        private Shader graphShaderMobile;

        /// <summary>
        /// Gets the shader used for rendering the graph.
        /// </summary>
        public Shader GraphShader
        { 
            get
            {
                // Get the current platform.
                EPlatform var_Platform = PlatformHelper.GetCurrentPlatform();

                // Return the desktop / console or mobile shader.
                return var_Platform == EPlatform.Mobile ? this.graphShaderMobile : this.graphShader;
            }
        }

        [SerializeField]
        /// <summary>
        /// Gets a value indicating whether the graph is rendered as a Line or Bar graph.
        /// </summary>
        private bool isLine;

        /// <summary>
        /// Gets a value indicating whether the graph is rendered as a Line or Bar graph.
        /// </summary>
        public bool IsLine { get => this.isLine; }

        [SerializeField]
        /// <summary>
        /// Gets a value indicating whether interpolation is applied between values.
        /// </summary>
        private bool isSmooth;

        /// <summary>
        /// Gets a value indicating whether interpolation is applied between values.
        /// </summary>
        public bool IsSmooth { get => this.isSmooth; }

        [SerializeField]
        /// <summary>
        /// Gets a value indicating whether the graph rendering has anti-aliasing enabled.
        /// </summary>
        private bool hasAntiAliasing;

        /// <summary>
        /// Gets a value indicating whether the graph rendering has anti-aliasing enabled.
        /// </summary>
        public bool HasAntiAliasing { get => this.hasAntiAliasing; }

        /// <summary>
        /// The maxium number of values that ca be passed to the graph shader.
        /// </summary>
        public const int CMaxGraphValues = 1024;

        /// <summary>
        /// The maxium number of values that ca be passed to the mobile graph shader.
        /// </summary>
        public const int CMaxGraphValuesMobile = 512;

        [SerializeField]
        /// <summary>
        /// Gets the number of values to be rendered by the graph.
        /// </summary>
        private int graphValues = 128;

        /// <summary>
        /// Gets the number of values to be rendered by the graph.
        /// </summary>
        public int GraphValues { get => this.graphValues; }

        /// <summary>
        /// The property identifier for indicating whether the graph is rendered as a line in the shader.
        /// </summary>
        public static readonly int LinePropertyId = Shader.PropertyToID("_Line");

        /// <summary>
        /// The property identifier for indicating whether the graph is rendered with smooth interpolation in the shader.
        /// </summary>
        public static readonly int SmoothPropertyId = Shader.PropertyToID("_Smooth");

        /// <summary>
        /// The property identifier for indicating whether anti-aliasing is applied to the graph rendering in the shader.
        /// </summary>
        public static readonly int AntiAliasingPropertyId = Shader.PropertyToID("_AntiAliasing");

        /// <summary>
        /// The property identifier for passing the array of graph values to the shader.
        /// </summary>
        public static readonly int ValuesPropertyId = Shader.PropertyToID("_Values");

        /// <summary>
        /// The property identifier for passing the count of graph values to the shader.
        /// </summary>
        public static readonly int ValueCountPropertyId = Shader.PropertyToID("_ValueCount");

        /// <summary>
        /// Initializes the graph renderer, subscribes to performance providers.
        /// </summary>
        protected virtual void Awake()
        {
            // Find all providers on this game object.
            this.Provider = new List<IProvider>(this.GetComponents<IProvider>());

            // Initialize the unsubscriber list.
            this.unsubscriber = new List<IDisposable>();

            // Subscribe to all providers, that provide data as TProvidedData.
            foreach (IProvider var_Provider in this.Provider)
            {
                // Check if the provider provides the correct data type.
                if (typeof(PerformanceData).IsAssignableFrom(var_Provider.ProvidedDataType))
                {
                    this.unsubscriber.Add(var_Provider.Subscribe(this));
                }
            }
        }

        /// <summary>
        /// Initialize the graph with the desktop or mobile shader. 
        /// </summary>
        protected virtual void Start()
        {
            // Initialize the graph wit the desktop / console or mobile shader.
            this.InitializeGraph(this.GraphShader);
        }

        /// <summary>
        /// Initialize the graph with the desktop or mobile shader.
        /// </summary>
        /// <param name="_Shader">The shader used for rendering the graph.</param>
        public void InitializeGraph(Shader _Shader)
        {
            // Assign new material to the graph.
            this.Target.material = new Material(_Shader);

            // Pass parameter to the graph shader.
            this.Target.material.SetFloat(AGraphRenderer.LinePropertyId, this.IsLine ? 1.0f : 0.0f);
            this.Target.material.SetFloat(AGraphRenderer.SmoothPropertyId, this.IsSmooth ? 1.0f : 0.0f);
            this.Target.material.SetFloat(AGraphRenderer.AntiAliasingPropertyId, this.HasAntiAliasing ? 1.0f : 0.0f);
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, new float[CMaxGraphValuesMobile]);
            this.Target.material.SetFloat(AGraphRenderer.ValueCountPropertyId, this.GraphValues);

            // Call the graph initialization method.
            this.OnInitializeGraph(_Shader);
        }

        /// <summary>
        /// Called when the graph is initialized.
        /// </summary>
        /// <param name="_Shader">The shader used for rendering the graph.</param>
        protected virtual void OnInitializeGraph(Shader _Shader)
        {
            // Does nothing.
        }

        /// <summary>
        /// Pass parameter to the graph shader on editor value changed.
        /// </summary>
        public virtual void RefreshGraph()
        {
            // Pass parameter to the graph shader.
            this.Target.material.SetFloat(AGraphRenderer.LinePropertyId, this.IsLine ? 1.0f : 0.0f);
            this.Target.material.SetFloat(AGraphRenderer.SmoothPropertyId, this.IsSmooth ? 1.0f : 0.0f);
            this.Target.material.SetFloat(AGraphRenderer.AntiAliasingPropertyId, this.HasAntiAliasing ? 1.0f : 0.0f);
            this.Target.material.SetFloatArray(AGraphRenderer.ValuesPropertyId, new float[CMaxGraphValuesMobile]);
            this.Target.material.SetFloat(AGraphRenderer.ValueCountPropertyId, this.GraphValues);
        }

        /// <summary>
        /// Called when a new performance data value is received.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        public abstract void OnNext(PerformanceData _Next);

        /// <summary>
        /// Called when a new performance data value is received.
        /// </summary>
        /// <param name="_Next">The new performance data received.</param>
        void IObserver<IProvidedData>.OnNext(IProvidedData _Next)
        {
            if (_Next is PerformanceData)
            {
                this.OnNext((PerformanceData) _Next);
            }
        }

        /// <summary>
        /// Called when an error is encountered.
        /// </summary>
        /// <param name="_Error">The encountered error.</param>
        public virtual void OnError(Exception _Error)
        {
            // Does nothing.
        }

        /// <summary>
        /// Called when performance data observation is completed.
        /// </summary>
        public virtual void OnCompleted()
        {
            // Does nothing.
        }

        /// <summary>
        /// Disposes of the renderer by unsubscribing from all performance providers.
        /// </summary>
        public virtual void Dispose()
        {
            // Unsubscribe from all performance providers.
            foreach (IDisposable var_Unsubscriber in this.unsubscriber)
            {
                var_Unsubscriber.Dispose();
            }
        }

        /// <summary>
        /// Called when the GameObject is destroyed, unsubscribes from all performance providers.
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Unsubscribe from all performance providers.
            foreach (IDisposable var_Unsubscriber in this.unsubscriber)
            {
                var_Unsubscriber.Dispose();
            }
        }
    }
}
