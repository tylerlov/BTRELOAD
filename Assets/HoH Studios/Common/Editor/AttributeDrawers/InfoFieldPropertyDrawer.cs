using UnityEditor;
using UnityEngine;

namespace HohStudios.Common.Attributes
{
    /// <summary>
    /// Property drawer to draw the InfoField attribute in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(InfoFieldAttribute))]
    public class InfoFieldPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Override the OnGUI to draw the property
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!(attribute is InfoFieldAttribute infoField))
                return;

            var infotype = MessageType.None;

            // Convert runtime enum to editor enum
            switch (infoField.InfoType)
            {
                case InfoFieldAttribute.MessageType.Info:
                    infotype = MessageType.Info;
                    break;
                case InfoFieldAttribute.MessageType.Warning:
                    infotype = MessageType.Warning;
                    break;
                case InfoFieldAttribute.MessageType.Error:
                    infotype = MessageType.Error;
                    break;
            }

            GUILayout.Space(infoField.PaddingAbove);
            EditorGUILayout.HelpBox(infoField.Message, infotype, infoField.Expand);
            EditorGUILayout.PropertyField(property, label);
            GUILayout.Space(infoField.PaddingBelow);
        }
    }
}