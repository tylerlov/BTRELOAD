// Unity
using UnityEngine;
using UnityEditor;

// GUPS
using GUPS.EasyPerformanceMonitor.Renderer;

namespace GUPS.EasyPerformanceMonitor.Editor
{
    /// <summary>
    /// Custom editor for inspecting and modifying the RatedTextRenderer component in the Unity editor.
    /// </summary>
    [CustomEditor(typeof(NormalizedTextRenderer), editorForChildClasses: true)]
    public class NormalizedTextRendererEditor : TextRendererEditor
    {
        // The serialized properties of the NormalizedTextRendererEditor component.
        private SerializedProperty scaleProp;
        private SerializedProperty patternProp;
        private SerializedProperty uiMeanTextsProp;
        private SerializedProperty uiPercentTextsProp;

        /// <summary>
        /// Initializes serialized properties when the editor is enabled.
        /// </summary>
        private void OnEnable()
        {
            // Initialize serialized properties.
            this.scaleProp = this.serializedObject.FindProperty("scale");
            this.patternProp = this.serializedObject.FindProperty("pattern");
            this.uiMeanTextsProp = this.serializedObject.FindProperty("uiMeanTexts");
            this.uiPercentTextsProp = this.serializedObject.FindProperty("uiPercentTexts");
        }

        /// <summary>
        /// Overrides the default inspector GUI to display custom options for the RatedTextRenderer component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update serialized object.
            serializedObject.Update();

            // General settings section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Text Renderer Settings", EditorStyles.boldLabel);

            // Display and edit the 'Scale' property.
            EditorGUILayout.PropertyField(scaleProp, new GUIContent("Scale Values", "Toggle to scale the displayed values."));

            // Display and edit the 'Pattern' property.
            EditorGUILayout.PropertyField(patternProp, new GUIContent("Rendering Pattern", "The text pattern used to render the value and unit in the ui texts. The 0 represents the value and the # represents the unit. Possible patterns are: 0, 0.0, 0.00, 0.000, ... or 0#, 0.0#, 0.00#, 0.000#, ..."));

            // Display and edit the text properties.
            var var_Providers = this.GetProvider();

            for (int i = 0; i < var_Providers.Length; i++)
            {
                // Show provider name.
                EditorGUILayout.LabelField(new GUIContent("-> Provider " + (i + 1) + " - " + var_Providers[i].Name + ":", "Settings for provider " + (i + 1) + "."));

                // Indent.
                EditorGUI.indentLevel++;

                // Add mean text property, if not present.
                if (this.uiMeanTextsProp.arraySize <= i)
                {
                    this.uiMeanTextsProp.InsertArrayElementAtIndex(i);
                }

                // Display and edit the 'Mean Texts' property with the option to expand array elements.
                EditorGUILayout.PropertyField(uiMeanTextsProp.GetArrayElementAtIndex(i), new GUIContent("Mean Texts", "UI Text components associated with mean values."), true);
                
                // Add percent text property, if not present.
                if (this.uiPercentTextsProp.arraySize <= i)
                {
                    this.uiPercentTextsProp.InsertArrayElementAtIndex(i);
                }

                // Display and edit the 'Percent Texts' property with the option to expand array elements.
                EditorGUILayout.PropertyField(uiPercentTextsProp.GetArrayElementAtIndex(i), new GUIContent("Percent Texts", "UI Text components associated with normalized percentage values."), true);

                // Unindent.
                EditorGUI.indentLevel--;
            }

            if(var_Providers.Length == 0)
            {
                EditorGUILayout.LabelField("-> No provider found.");
            }

            // Apply modified properties to the serialized object.
            bool var_Modified = this.serializedObject.hasModifiedProperties;

            this.serializedObject.ApplyModifiedProperties();

            if (var_Modified)
            {
                if (Application.isPlaying)
                {
                    (this.serializedObject.targetObject as ITextRenderer).RefreshText();
                }
            }
        }
    }
}