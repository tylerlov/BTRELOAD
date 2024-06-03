// Microsoft
using System;

// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Renderer;
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for inspecting and modifying the IGraphRenderer component in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(IGraphRenderer), editorForChildClasses: true)]
    public class GraphRendererEditor : UnityEditor.Editor
    {
        // The serialized properties of the IGraphRenderer component.
        private SerializedProperty targetProp;
        private SerializedProperty graphShaderProp;
        private SerializedProperty graphShaderMobileProp;
        private SerializedProperty isLineProp;
        private SerializedProperty isSmoothProp;
        private SerializedProperty hasAntiAliasingProp;
        private SerializedProperty graphValuesProp;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        protected virtual void OnEnable()
        {
            this.targetProp = this.serializedObject.FindProperty("target");
            this.graphShaderProp = this.serializedObject.FindProperty("graphShader");
            this.graphShaderMobileProp = this.serializedObject.FindProperty("graphShaderMobile");
            this.isLineProp = this.serializedObject.FindProperty("isLine");
            this.isSmoothProp = this.serializedObject.FindProperty("isSmooth");
            this.hasAntiAliasingProp = this.serializedObject.FindProperty("hasAntiAliasing");
            this.graphValuesProp = this.serializedObject.FindProperty("graphValues");
        }

        /// <summary>
        /// Override of the default inspector GUI to provide a custom interface for configuring graph renderers.
        /// </summary>
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Design Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(this.targetProp, new GUIContent("Target Image", "The image component used for rendering the graph."));
            EditorGUILayout.PropertyField(this.graphShaderProp, new GUIContent("Graph Shader", "The shader used for rendering the graph."));
            EditorGUILayout.PropertyField(this.graphShaderMobileProp, new GUIContent("Mobile Graph Shader", "The mobile version of the shader used for rendering the graph."));

            this.isLineProp.boolValue = 0 == EditorGUILayout.Popup(new GUIContent("Render as:", "Either render the graph as line or bar."), isLineProp.boolValue ? 0 : 1, new String[] { "Line", "Bar" });

            EditorGUILayout.PropertyField(this.isSmoothProp, new GUIContent("Is Smooth", "Toggle to apply interpolation between values."));
            EditorGUILayout.PropertyField(this.hasAntiAliasingProp, new GUIContent("Has Anti-Aliasing", "Toggle to enable anti-aliasing for graph rendering."));
            this.graphValuesProp.intValue = EditorGUILayout.IntSlider(new GUIContent("Graph Values", "The number of last read values to be rendered by the graph."), this.graphValuesProp.intValue, 1, 512);

            // Apply modified properties to the serialized object.
            bool var_Modified = this.serializedObject.hasModifiedProperties;

            this.serializedObject.ApplyModifiedProperties();

            if(var_Modified)
            {
                if (Application.isPlaying)
                {
                    (this.serializedObject.targetObject as IGraphRenderer).RefreshGraph();
                }
            }
        }

        /// <summary>
        /// Get all performance provider of the target object.
        /// </summary>
        /// <returns>An array of performance provider.</returns>
        protected IPerformanceProvider[] GetProvider()
        {
            // Get the target object.
            MonoBehaviour var_Target = this.serializedObject.targetObject as MonoBehaviour;

            // Get the providers.
            var var_Providers = var_Target.GetComponents<IPerformanceProvider>();

            // Return the providers.
            return var_Providers;
        }
    }
}