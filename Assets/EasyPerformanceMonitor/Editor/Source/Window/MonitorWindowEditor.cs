// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Window;

namespace GUPS.EasyPerformanceMonitor.Editor.Window
{
    /// <summary>
    /// Custom editor for the monitor windows.
    /// </summary>
    [CustomEditor(typeof(MonitorWindow), editorForChildClasses: true)]
    public class MonitorWindowEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Override of the default inspector GUI to validate for changes and refresh the window if necessary.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Check for changes.
            EditorGUI.BeginChangeCheck();

            // Call the base method and display the default inspector.
            base.OnInspectorGUI();

            // Check if the inspector has changed.
            if(EditorGUI.EndChangeCheck())
            {
                // Check if the application is playing.
                if (Application.isPlaying)
                {
                    // If the editor has changed, and in play mode, refresh the window.
                    (this.serializedObject.targetObject as MonitorWindow).RefreshWindow();
                }
            }
        }
    }
}