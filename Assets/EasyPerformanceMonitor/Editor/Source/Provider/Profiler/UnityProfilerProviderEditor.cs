// Microsoft
using System;
using System.Collections.Generic;
using System.Linq;

// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    [CustomEditor(typeof(UnityProfilerProvider), editorForChildClasses: true)]
    public class UnityProfilerProviderEditor : PerformanceProviderEditor
    {
        /// <summary>
        /// The unity profiler category.
        /// </summary>
        private SerializedProperty profilerCategoryProp;

        /// <summary>
        /// The unity profiler status name.
        /// </summary>
        private SerializedProperty statusNameProp;

        /// <summary>
        /// Is a user custom profiler status name.
        /// </summary>
        private SerializedProperty isCustomStatusProp;

        /// <summary>
        /// The index of the current profiler category.
        /// </summary>
        private int profilerCategoryIndex = 0;

        /// <summary>
        /// A list of all available profiling categories.
        /// </summary>
        private String[] profilerCategories = new String[0];

        /// <summary>
        /// Map the profiling category to their status.
        /// </summary>
        private Dictionary<String, String[]> profilerCategoryToStatus = new Dictionary<String, String[]>();

        /// <summary>
        /// Custom profiler status.
        /// </summary>
        private String customStatus = String.Empty;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        protected override void OnEnable()
        {
            // Call the base implementation.
            base.OnEnable();

            // Initialize serialized properties.
            this.profilerCategoryProp = this.serializedObject.FindProperty("Category");
            this.statusNameProp = this.serializedObject.FindProperty("StatusName");
            this.isCustomStatusProp = this.serializedObject.FindProperty("IsCustomStatus");

            // Get the profiler categories.
            this.profilerCategories = ProfilingCategory.AvailableCategories.Select(c => c.Category).ToArray();

            // Get the profiler category to status map.
            this.profilerCategoryToStatus = ProfilingCategory.AvailableCategories.ToDictionary(c => c.Category, c => c.Status.ToArray());

            // Find the index of the current profiler category.
            this.profilerCategoryIndex = Array.FindIndex(this.profilerCategories, n => n == this.profilerCategoryProp.stringValue);

            // Find the index.
            if (this.isCustomStatusProp.boolValue)
            {
                // Set the custom status.
                this.customStatus = this.statusNameProp.stringValue;
            }
            else
            {
                // If the index is not valid, set it to 0.
                if (this.profilerCategoryIndex < 0)
                {
                    this.profilerCategoryIndex = 0;
                }
            }
        }

        /// <summary>
        /// Override of the default inspector GUI to provide a custom interface for configuring performance providers.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Call the base implementation.
            base.OnInspectorGUI();

            // Update serialized object.
            this.serializedObject.Update();

            // General settings section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profiler Settings", EditorStyles.boldLabel);

            // Store the old profiler category index.
            int var_OldIndex = this.profilerCategoryIndex;

            // Display the profiler category as DropDown.
            this.profilerCategoryIndex = EditorGUILayout.Popup(new GUIContent("Profiler Category:", "The profiler category to track."), this.profilerCategoryIndex, this.profilerCategories);

            // Get the profiling category as text.
            String var_ProfilingCategory = this.profilerCategories[this.profilerCategoryIndex];

            // Set the profiler category property.
            this.profilerCategoryProp.stringValue = var_ProfilingCategory;

            // Get the profiler category status array.
            String[] var_ProfilingStatus = this.profilerCategoryToStatus[var_ProfilingCategory];

            // Find the selected profiler category status index.
            int var_StatusIndex = 0;

            // If the status is not custom, find the index of the current status.
            if (!this.isCustomStatusProp.boolValue)
            {
                var_StatusIndex = Array.FindIndex(var_ProfilingStatus, n => n == this.statusNameProp.stringValue);

                // If the index changed, reset the selected status or it is not valid, set it to 0.
                if (var_OldIndex != this.profilerCategoryIndex || var_StatusIndex < 0)
                {
                    // Set the index to 0.
                    var_StatusIndex = 0;

                    // Set the custom status property to true.
                    this.isCustomStatusProp.boolValue = true;
                }
            }

            // Display the status name property.
            var_StatusIndex = EditorGUILayout.Popup(new GUIContent("Profiler Status:", "The name of the status to read of the selected profiler category."), var_StatusIndex, var_ProfilingStatus);

            // If the status index is 0 'Custom', display a text field to enter a custom status name.
            if (var_StatusIndex == 0)
            {
                // Render a text field to enter a custom status name.
                this.customStatus = EditorGUILayout.TextField(new GUIContent(" ", "The name of the custom status to read from the profiler."), this.customStatus);

                // Set the status name properties.
                this.statusNameProp.stringValue = this.customStatus;

                // Set the custom status property to true.
                this.isCustomStatusProp.boolValue = true;
            }
            else
            {
                // Set the status name properties.
                this.statusNameProp.stringValue = var_ProfilingStatus[var_StatusIndex];

                // Set the custom status property to false.
                this.isCustomStatusProp.boolValue = false;
            }

            // Check if there is a modified property, to refresh the performance provider.
            bool var_Modified = this.serializedObject.hasModifiedProperties;

            // Apply modified properties to the serialized object.
            this.serializedObject.ApplyModifiedProperties();

            // Refresh the performance provider, if a property was modified.
            if (var_Modified)
            {
                if (Application.isPlaying)
                {
                    (this.serializedObject.targetObject as APerformanceProvider).Refresh();
                }
            }
        }
    }
}
