//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using SonicBloom.Koreo.Players.FMODStudio;
using UnityEditor;
using UnityEngine;


namespace SonicBloom.Koreo.EditorUI.FMODStudioTools
{
    /// <summary>
    /// THe property drawer responsible for rendering <c>FMODKoreoEntry</c> instances in the
    /// Unity Editor inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(FMODKoreoEntry))]
    public class FMODKoreoEntryDrawer : PropertyDrawer
    {
        #region Static Fields


        /// <summary>
        /// Content to describe the Koreography field.
        /// </summary>
        static GUIContent koreoContent = new GUIContent("Koreography", "A reference to specific Koreography data.");

        /// <summary>
        /// Content to describe the audio name represented as a UTF8 byte array.
        /// </summary>
        static GUIContent utf8NameContent = new GUIContent("UTF8 Name", "The name of the audio file referenced by the Koreography " +
                                                           "represented as a UTF8 Byte Array. The current values are equivallent to:\n");


        #endregion
        #region Overrides


        /// <summary>
        /// Override this method to make your own IMGUI based GUI for the property.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            {
                // Create field rects.
                Rect koreoRect = new Rect(position);
                koreoRect.height = EditorGUIUtility.singleLineHeight;

                Rect nameRect = new Rect(koreoRect);
                nameRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                nameRect.xMin = EditorGUIUtility.labelWidth;

                SerializedProperty koreoProp = property.FindPropertyRelative("koreo");
                SerializedProperty utf8NameProp = property.FindPropertyRelative("utf8Name");

                // Koreography field.
                EditorGUI.PropertyField(koreoRect, koreoProp, koreoContent);

                // This is set internally. Always disallow editing.
                EditorGUI.BeginDisabledGroup(true);
                {
                    byte[] utf8Bytes = new byte[utf8NameProp.arraySize];
                    for (int i = 0; i < utf8Bytes.Length; ++i)
                    {
                        utf8Bytes[i] = (byte)utf8NameProp.GetArrayElementAtIndex(i).intValue;
                    }

                    GUIContent nameContent = new GUIContent(utf8NameContent);
                    nameContent.tooltip += System.Text.Encoding.UTF8.GetString(utf8Bytes);
                    System.Text.Encoding.UTF8.GetString(utf8Bytes);

                    EditorGUI.TextField(nameRect, nameContent, System.BitConverter.ToString(utf8Bytes));
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Override this method to specify how tall the GUI for this field is in pixels.
        /// 
        /// The default is one line high.
        /// </summary>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        /// <returns>The height in pixels.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // We're showing two fields per entry: two lines plus some spacing.
            return (EditorGUIUtility.singleLineHeight * 2f) + EditorGUIUtility.standardVerticalSpacing;
        }


        #endregion
    }
}

