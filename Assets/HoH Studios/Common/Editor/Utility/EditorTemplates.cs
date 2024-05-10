using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HohStudios.Editor
{
    /// <summary>
    /// Class to hold pre-customized templates for gui controls for the editor.
    /// </summary>
    public sealed class EditorTemplates : UnityEditor.Editor
    {
        // These dictionaries hold temporary static values on "which fields to exclude" when drawing custom inspectors.
        // The editorexclusion is for drawing serialized objects in a custom monobehaviour inspector
        private static readonly Dictionary<SerializedObject, List<string>> EditorFieldsToExclude =
            new Dictionary<SerializedObject, List<string>>();
        // The drawerexclusion is for drawing serialized properties in a custom property drawer
        private static readonly Dictionary<SerializedProperty, List<string>> DrawerFieldsToExclude =
            new Dictionary<SerializedProperty, List<string>>();

        /// <summary>
        /// Call this function inside OnInspectorGUI() for a custom drawing function to control the drawing execution and base template
        /// </summary>
        public static void DrawCustomInspector(SerializedObject serializedObject, Action drawInspector, params string[] propertiesToHideInChildClasses)
        {
            if (serializedObject == null)
            {
                Debug.Log("[Error] Failed to draw custom inspector because given serialized object is null");
                return;
            }

            // Set default label width of all custom inspectors to be a little larger than normal if theme isnt set explicitly
            if (EditorThemes.InitialLabelWidth < 0.01f)
                EditorThemes.InitialLabelWidth = EditorGUIUtility.labelWidth;
            if (Mathf.Abs(EditorGUIUtility.labelWidth - EditorThemes.InitialLabelWidth) < 0.01f)
                EditorGUIUtility.labelWidth = EditorThemes.InitialLabelWidth * 1.33f;

            // Update any dynamic GUI
            serializedObject.Update();

            // Clear exclusion list before repopulating
            if (EditorFieldsToExclude.ContainsKey(serializedObject))
                EditorFieldsToExclude[serializedObject]?.Clear();

            EditorGUI.BeginChangeCheck();

            // Draw the inspector
            drawInspector?.Invoke();

            // Draw children properties but exclude any undesirable properties
            var hideList = propertiesToHideInChildClasses.ToList();
            hideList.Add("m_Script");    // Hides the monoscript by default

            if (EditorFieldsToExclude.ContainsKey(serializedObject) && EditorFieldsToExclude[serializedObject] != null)
                hideList.AddRange(EditorFieldsToExclude[serializedObject]);

            // Draws the child properties excluding any undesirable props
            DrawPropertiesExcluding(serializedObject, hideList.ToArray());

            // Update dynamic GUI
            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(serializedObject.targetObject);

            EditorThemes.RestoreEditorTheme();
        }

        /// <summary>
        /// Call this function inside OnGUI() for a custom drawing function to control the drawing execution and base template
        /// </summary>
        public static void DrawCustomInspector(SerializedProperty serializedProperty, Action drawInspector, params string[] propertiesToHideInChildClasses)
        {
            if (serializedProperty == null)
            {
                Debug.Log("[Error] Failed to draw custom inspector because given serialized property is null");
                return;
            }

            // Set default label width of all custom inspectors to be a little larger than normal if theme isnt set explicitly
            if (EditorThemes.InitialLabelWidth < 0.01f)
                EditorThemes.InitialLabelWidth = EditorGUIUtility.labelWidth;
            if (Mathf.Abs(EditorGUIUtility.labelWidth - EditorThemes.InitialLabelWidth) < 0.01f)
                EditorGUIUtility.labelWidth = EditorThemes.InitialLabelWidth * 1.33f;

            // Update any dynamic GUI
            serializedProperty.serializedObject.Update();

            // Clear exclusion list before repopulating
            if (DrawerFieldsToExclude.ContainsKey(serializedProperty))
                DrawerFieldsToExclude[serializedProperty]?.Clear();

            EditorGUI.BeginChangeCheck();

            //// Draw the inspector
            drawInspector?.Invoke();

            // Draw children properties but exclude any undesirable properties
            var hideList = propertiesToHideInChildClasses.ToList();

            if (DrawerFieldsToExclude.ContainsKey(serializedProperty) && DrawerFieldsToExclude[serializedProperty] != null)
                hideList.AddRange(DrawerFieldsToExclude[serializedProperty]);

            DrawPropertiesExcluding(serializedProperty, hideList.ToArray());

            // Update dynamic GUI
            if (GUI.changed)
                serializedProperty.serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(serializedProperty.serializedObject.targetObject);

            EditorThemes.RestoreEditorTheme();
        }

        /// <summary>
        /// Template to draw a custom label with included generic properties
        /// </summary>
        public static void DrawLabel(string text, int fontSize = 14, FontStyle fontStyle = FontStyle.Bold, TextAnchor fontAlignment = default, Color color = default, params GUILayoutOption[] layouts)
        {
            // Default color = white dimmed if not set
            color = color == default ? (Color.white * 0.9f) : color;

            // Change size and color
            var newStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                normal = { textColor = color },
                alignment = fontAlignment
            };

            if (layouts == null || layouts.Length == 0)
                layouts = new[] { GUILayout.Height(fontSize * 1.75f) };

            // Draw label
            EditorGUILayout.LabelField(text, newStyle, layouts);
        }

        /// <summary>
        /// Template to draw button and modify generic properties of the control
        /// </summary>
        public static void DrawButton(string text, Action func = null, int fontSize = 11, FontStyle fontStyle = FontStyle.Normal, TextAnchor fontAlignment = TextAnchor.MiddleCenter, Color backgroundColor = default, params GUILayoutOption[] layouts)
        {
            var startColor = GUI.backgroundColor;

            // Change background color if wanted
            if (backgroundColor != default)
                GUI.backgroundColor = backgroundColor;

            // Set fonts
            var style = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = fontAlignment
            };

            // Display button and return color to default
            if (GUILayout.Button(text, style, layouts))
                func?.Invoke();

            GUI.backgroundColor = startColor;
        }



        /// <summary>
        /// Template to create a single foldout menu with a given ref bool and generic properties
        /// 
        /// Foldoutcontent is what is drawn inside of the foldout.
        /// </summary>
        public static void DrawFoldout(ref bool isVisible, Action foldoutContent, string title, int titleSize = 16, int titlePadding = 20, float contentPadding = 20)
        {
            var style = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = titleSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = (Color.white * 0.9f) },
                onNormal = { textColor = (Color.white * 0.9f) },
                padding = { left = titlePadding }
            };

            isVisible = EditorGUILayout.Foldout(isVisible, title, true, style);
            if (isVisible)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(contentPadding);
                GUILayout.BeginVertical();
                foldoutContent.DynamicInvoke(); // Calls the delegate given to draw everything inside of the template
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

        }

        /// <summary>
        /// Template to create progrommatic foldouts by giving a list of bools as a reference for foldout
        /// visibility as well as the index of the list to point to the current bool being drawn
        /// 
        /// Foldoutcontent is what is drawn inside of the foldout.
        /// </summary>
        public static void DrawFoldout(Dictionary<string, bool> isVisible, string isVisibleIndex, Action foldoutContent, string title, int titleSize = 16, int titlePadding = 20, Color? titleColor = null, int contentPadding = 20)
        {
            var color = titleColor ?? (Color.white * 0.9f);


            // Custom title style
            var style = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = titleSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = color },
                onNormal = { textColor = color },
                focused = { textColor = color },
                onFocused = { textColor = color },
                padding = { left = titlePadding }
            };

            // Gets the correct bool reference for foldout visibility and draws the foldout
            isVisible[isVisibleIndex] = EditorGUILayout.Foldout(isVisible[isVisibleIndex], title, true, style);
            if (isVisible[isVisibleIndex])
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(contentPadding);
                GUILayout.BeginVertical();
                foldoutContent.Invoke(); // Calls the delegate given to draw everything inside of the template
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

        }

        /// <summary>
        /// Allow draw the monoscript bar, typically used at start of DrawCustomInspector() function
        /// </summary>
        public static void DrawMonoScript(SerializedObject serializedObject)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawField("m_Script", serializedObject);
            EditorGUI.EndDisabledGroup();
        }


        /// <summary>
        /// Draws a serialized field and its children based on given serialized property. Returns drawn property.
        /// Hides the drawn property from being duplicate drawn when child inherits.
        /// </summary>
        public static SerializedProperty DrawField(SerializedProperty serializedProperty, string label = null)
        {
            if (serializedProperty == null)
            {
                Debug.Log($"[ERROR] Failed to draw field because given serialized property is null");
                return null;
            }

            HideProperties(serializedProperty, serializedProperty.name);
            EditorGUILayout.PropertyField(serializedProperty, label == "" ? GUIContent.none : new GUIContent(label ?? serializedProperty.displayName), true);

            if (GUI.changed)
                serializedProperty.serializedObject.ApplyModifiedProperties();

            return serializedProperty;
        }


        /// <summary>
        /// Draws a serialized field and its children based on given serialized property. Returns drawn property.
        /// Hides the drawn property from being duplicate drawn when child inherits.
        /// </summary>
        public static SerializedProperty DrawField(string propName, SerializedProperty serializedProperty, string label = null)
        {
            if (serializedProperty == null)
            {
                Debug.Log($"[ERROR] Failed to draw field because given serialized property is null when looking for property '{propName}'");
                return null;
            }

            var prop = serializedProperty.FindPropertyRelative(propName);
            if (prop == null)
                throw new Exception("[Error] Could not draw field for custom inspector. Likely the field name " + propName + " is not being serialized and therefore cannot be drawn.");

            HideProperties(serializedProperty, propName);
            EditorGUILayout.PropertyField(prop, label == "" ? GUIContent.none : new GUIContent(label ?? prop.displayName), true);

            if (GUI.changed)
                serializedProperty.serializedObject.ApplyModifiedProperties();

            return prop;
        }

        /// <summary>
        /// Draws a serialized field and its children based on given serialized object and prop name. Returns drawn property.
        /// Hides the drawn property from being duplicate drawn when child inherits.
        /// </summary>
        public static SerializedProperty DrawField(string propName, SerializedObject serializedObject, string label = null)
        {
            if (serializedObject == null)
            {
                Debug.Log($"[ERROR] Failed to draw field because given serialized object is null when looking for property '{propName}'");
                return null;
            }

            var prop = serializedObject.FindProperty(propName);
            if (prop == null)
                throw new Exception("[Error] Could not draw field for custom inspector. Likely the field name " + propName + " is not being serialized and therefore cannot be drawn.");

            HideProperties(serializedObject, propName);
            EditorGUILayout.PropertyField(prop, label == "" ? GUIContent.none : new GUIContent(label ?? prop.displayName), true);

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();

            return prop;
        }

        /// <summary>
        /// Draws all of the properties of the given serialized property, skipping over any excluded properties
        /// </summary>
        public static void DrawPropertiesExcluding(SerializedProperty serializedProperty, params string[] propertiesToExclude)
        {
            var lastPropPath = "";
            var excludedList = propertiesToExclude.ToList();

            // Immediately return if we're excluding this property
            if (excludedList.Contains(serializedProperty.name))
                return;

            // If we're not excluding the parent property, loop through all the sub-properties to draw
            foreach (SerializedProperty prop in serializedProperty)
            {
                // Skip drawing the prop if we are excluding it
                if (excludedList.Contains(prop.name))
                    continue;

                // Avoid drawing duplicate array contents by short circuiting
                if (!string.IsNullOrEmpty(lastPropPath) && prop.propertyPath.Contains(lastPropPath))
                    continue;

                lastPropPath = prop.propertyPath;

                DrawField(prop);
            }
        }

        /// <summary>
        /// Draw a custom stylized separator line with easily modifiable properties and parameters
        /// </summary>
        public static void DrawSeparatorLine(float horizontalPadding = 10, float beforePadding = 2.5f, float afterPadding = 2.5f, Color lineColor = default)
        {
            GUILayout.Space(beforePadding);
            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = lineColor == default ? Color.gray : lineColor;
            Handles.DrawLine(new Vector2(rect.x + (horizontalPadding - 17f), rect.y), new Vector2(rect.width - (horizontalPadding - 22f), rect.y));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(afterPadding);
        }

        /// <summary>
        /// Draws a default label title bar for the editor. Title can be clicked to ping a given monobehaviour monoscript
        /// </summary>
        public static void DrawTitleBar(string title, EditorTheme theme, MonoBehaviour monoToPing)
        {
            var copy = theme;
            copy.Alignment = TextAnchor.LowerCenter;
            copy.LabelFontSize = 15;
            EditorThemes.SetEditorTheme(copy);
            if (GUILayout.Button(title, EditorStyles.label))
            {
                if (monoToPing != null)
                    EditorGUIUtility.PingObject(MonoScript.FromMonoBehaviour(monoToPing));
            }

            EditorThemes.SetEditorTheme(theme);
            DrawSeparatorLine(10, 3, 3);
        }


        /// <summary>
        /// Allow an easy function to hide a bunch of properties from being drawn in child classes. Call this inside of DrawCustomInspector().
        /// </summary>
        public static void HideProperties(SerializedObject objToHide, params string[] propertiesToHide)
        {
            // Add all the properties to hide to the exclusion dictionary
            foreach (var prop in propertiesToHide)
            {
                if (EditorFieldsToExclude.ContainsKey(objToHide))
                    EditorFieldsToExclude[objToHide].Add(prop);
                else
                    EditorFieldsToExclude.Add(objToHide, new List<string>() { prop });
            }
        }
        /// <summary>
        /// Allow an easy function to hide a bunch of properties from being drawn in child classes. Call this inside of DrawCustomInspector().
        /// </summary>
        public static void HideProperties(SerializedProperty propToHide, params string[] propertiesToHide)
        {
            // Add all the properties to hide to the exclusion dictionary
            foreach (var prop in propertiesToHide)
            {
                if (DrawerFieldsToExclude.ContainsKey(propToHide))
                    DrawerFieldsToExclude[propToHide].Add(prop);
                else
                    DrawerFieldsToExclude.Add(propToHide, new List<string>() { prop });
            }
        }

        /// <summary>
        /// Returns the serialized property's base object
        /// </summary>
        public static T GetSerializedPropertyBaseObject<T>(SerializedProperty property)
        {
            if (property == null)
            {
                Debug.Log(
                    "[ERROR] Could not find the serialized property base object because property given was null.");
                return default(T);
            }

            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();
            var field = targetObjectClassType.GetField(property.propertyPath);
            if (field != null)
            {
                return (T)field.GetValue(targetObject);
            }

            Debug.Log(
                $"[ERROR] Could not find the serialized property base object for ' {property.name} '. The property probably isn't being serialized.");
            return default(T);
        }


    }
}
