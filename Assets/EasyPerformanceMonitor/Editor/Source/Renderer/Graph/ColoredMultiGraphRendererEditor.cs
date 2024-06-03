// Microsoft
using System;

// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Renderer;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for managing and configuring colored multi graph renderers in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(ColoredMultiGraphRenderer), editorForChildClasses: true)]
    public class ColoredMultiGraphRendererEditor : GraphRendererEditor
    {
        // The serialized properties of the RatedSingleGraphRenderer component.
        private SerializedProperty isStackedProp;
        private SerializedProperty colorsProp;
        private SerializedProperty legendImagesProp;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        protected override void OnEnable()
        {
            // Call base implementation.
            base.OnEnable();

            // Initialize serialized properties.
            this.isStackedProp = this.serializedObject.FindProperty("isStacked");
            this.colorsProp = this.serializedObject.FindProperty("colors");
            this.legendImagesProp = this.serializedObject.FindProperty("legendImages");
        }

        /// <summary>
        /// Override of the default inspector GUI to provide a custom interface for configuring graph renderers.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update serialized object.
            this.serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Graph Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(this.isStackedProp, new GUIContent("Is Stacked", "Toggle to render the graphs as a stacked graph or next to each other."));

            // Display and edit the text properties.
            var var_Providers = this.GetProvider();

            GUILayout.Label(new GUIContent("Provider - Settings:", "There are " + var_Providers.Length + " Providers attached. This is a multiple graph renderer, so you can apply custom settings for each provided data."));

            for (int i = 0; i < var_Providers.Length; i++)
            {
                // Show provider name.
                EditorGUILayout.LabelField(new GUIContent("-> Provider " + (i + 1) + " - " + var_Providers[i].Name + ":", "Settings for provider " + (i + 1) + "."));

                // Indent.
                EditorGUI.indentLevel++;

                // Add color property, if not present.
                if (this.colorsProp.arraySize <= i)
                {
                    this.colorsProp.InsertArrayElementAtIndex(i);
                }

                // Display color property.
                EditorGUILayout.PropertyField(this.colorsProp.GetArrayElementAtIndex(i), new GUIContent("Color", "The color used for rendering the graph."));

                // Add legend image property, if not present.
                if (this.legendImagesProp.arraySize <= i)
                {
                    this.legendImagesProp.InsertArrayElementAtIndex(i);
                }

                // Display legend image property.
                EditorGUILayout.PropertyField(this.legendImagesProp.GetArrayElementAtIndex(i), new GUIContent("Legend - Image", "The image used to display the legend color for the single graph in the renderer."));

                // Unindent.
                EditorGUI.indentLevel--;
            }

            // Apply modified properties to the serialized object.
            bool var_Modified = this.serializedObject.hasModifiedProperties;

            this.serializedObject.ApplyModifiedProperties();

            if (var_Modified)
            {
                if (Application.isPlaying)
                {
                    (this.serializedObject.targetObject as AGraphRenderer).RefreshGraph();
                }
            }

            // Call base implementation - Display Design Settings.
            base.OnInspectorGUI();
        }
    }
}