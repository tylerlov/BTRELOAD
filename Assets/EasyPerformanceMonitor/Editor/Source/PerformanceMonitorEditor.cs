// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Window;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for the performance monitor.
    /// </summary>
    [CustomEditor(typeof(PerformanceMonitor), editorForChildClasses: true)]
    public class PerformanceMonitorEditor : UnityEditor.Editor
    {
        private SerializedProperty onlyInDevelopmentBuildProp;
        private SerializedProperty showOnStartProp;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        private void OnEnable()
        {
            // Initialize serialized properties.
            this.onlyInDevelopmentBuildProp = serializedObject.FindProperty("onlyInDevelopmentBuild");
            this.showOnStartProp = serializedObject.FindProperty("showOnStart");
        }

        /// <summary>
        /// Override of the default inspector GUI to provide a custom interface for configuring the performance monitor.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update serialized object.
            this.serializedObject.Update();

            // General settings section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Monitor - Settings", EditorStyles.boldLabel);

            // Display and edit the 'Is Scalable' property.
            EditorGUILayout.PropertyField(this.onlyInDevelopmentBuildProp, new GUIContent("Development build only", "Toggle to activate the monitor for development builds only."));

            // Display and edit the 'Is Scalable' property.
            EditorGUILayout.PropertyField(this.showOnStartProp, new GUIContent("Show on start", "Toggle to show the monitor at start."));

            // Monitor windows section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Monitor - Windows", EditorStyles.boldLabel);

            // Get all monitor windows.
            MonitorWindow[] var_MonitorWindows = this.GetWindows();

            // Show attached monitor windows.
            EditorGUILayout.LabelField("Attached " + var_MonitorWindows.Length + " windows:");

            // Display all monitor windows.
            foreach (MonitorWindow var_MonitorWindow in var_MonitorWindows)
            {
                // Display the monitor window.
                EditorGUILayout.LabelField("-> " + var_MonitorWindow.name); 
            }

            // Apply modified properties to the serialized object.
            this.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Gets the monitor windows in the children.
        /// </summary>
        /// <returns>An array of provider.</returns>
        private MonitorWindow[] GetWindows()
        {
            return (this.serializedObject.targetObject as PerformanceMonitor).GetComponentsInChildren<MonitorWindow>();
        }
    }
}