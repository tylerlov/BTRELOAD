using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HohStudios.Common.Attributes
{
    /// <summary>
    /// Property drawer to draw the TagField attribute in the inspector
    /// 
    /// Can optionally default to unity's tag field drawer if desired
    /// </summary>
    [CustomPropertyDrawer(typeof(TagFieldAttribute))]
    public class TagFieldPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Override the OnGUI to draw the property
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // If it isn't a string serialized type, just draw whatever it is and return immediately
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUILayout.PropertyField(property, label);
                return;
            }

            // Get the tag field attribute, return if null
            if (!(attribute is TagFieldAttribute tagField))
                return;

            // If we're drawing unity default tag field, just draw it and return
            if (tagField.DrawUnityDefault)
            {
                property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
                return;
            }

            // Begin drawing the custom tag field
            EditorGUI.BeginProperty(position, label, property);

            // Get the current tags and create a new array with the "No Tag" option
            var currentTags = InternalEditorUtility.tags;
            var tagArray = new string[currentTags.Length + 1];
            tagArray[0] = "<None>";

            for (var i = 0; i < currentTags.Length; i++)
                tagArray[i + 1] = currentTags[i];


            // Get the initial tag popup index based on current tag value
            var currentTag = property.stringValue;
            var tagIndex = 0;

            // Find which tag the index should start at, defaulting to "No Tag"
            if (currentTag != string.Empty)
            {
                for (var i = 1; i < tagArray.Length; i++)
                {
                    if (currentTag != tagArray[i])
                        continue;

                    tagIndex = i;
                    break;
                }
            }

            tagIndex = EditorGUI.Popup(position, label.text, tagIndex, tagArray);

            // Set the string value to the appropriate tag value, where "No Tag" is an empty string
            if (tagIndex == 0)
                property.stringValue = string.Empty;
            else if (tagIndex >= 1)
                property.stringValue = tagArray[tagIndex];

            EditorGUI.EndProperty();
        }
    }
}
