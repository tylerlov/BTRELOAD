// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Provider;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for configuring and inspecting GameObjectTagCountProvider instances in the Unity Editor.
    /// </summary>
    /// <remarks>
    /// The <see cref="GameObjectTagCountProviderEditor"/> class provides a custom interface within the Unity Editor
    /// for configuring and inspecting <see cref="GameObjectTagCountProvider"/> instances. It extends the functionality of
    /// the base <see cref="PerformanceProviderEditor"/> and introduces specific settings related to tracking the count of
    /// GameObjects with a specified tag in the scene.
    /// </remarks>
    [CustomEditor(typeof(GameObjectTagCountProvider), editorForChildClasses: true)]
    public class GameObjectTagCountProviderEditor : PerformanceProviderEditor
    {
        /// <summary>
        /// The serialized property for the GameObject Tag to track.
        /// </summary>
        private SerializedProperty tagProp;

        /// <summary>
        /// Called when the editor is enabled, initializing serialized properties.
        /// </summary>
        protected override void OnEnable()
        {
            // Call the base implementation.
            base.OnEnable();

            // Initialize serialized properties.
            this.tagProp = this.serializedObject.FindProperty("Tag");
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
            EditorGUILayout.LabelField("GameObject Settings", EditorStyles.boldLabel);

            // Render a text field to enter a tag to track.
            this.tagProp.stringValue = EditorGUILayout.TagField(new GUIContent("Tag", "Monitor the count of GameObjects in the scene that have a specified tag."), this.tagProp.stringValue);

            // Check if there is a modified property to refresh the performance provider.
            bool var_Modified = this.serializedObject.hasModifiedProperties;

            // Apply modified properties to the serialized object.
            this.serializedObject.ApplyModifiedProperties();

            // Refresh the performance provider if a property was modified.
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
