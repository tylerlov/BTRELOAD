using HohStudios.Editor;
using UnityEditor;
using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner
{
    /// <summary>
    /// Custom editor window for the ObjectParticle.cs component. Draws the inspector for user convenience.
    /// </summary>
    [CustomEditor(typeof(ObjectParticle), true)]
    [CanEditMultipleObjects]
    public class ObjectParticleEditor : UnityEditor.Editor
    {
        ObjectParticle _objectParticle;
        EditorDualTheme _labelThemes;
        EditorDualTheme _contentThemes;

        // Editor theme colors
        public static readonly Color OrangeDark = new Color(0.3f, 0.2f, 0f, 1f);
        public static readonly Color HoHGoldDark = new Color(0.92f, 0.55f, 0f, 1f);

        public static readonly Color OrangeCalm = new Color(0.9f, 0.63f, 0.19f, 0.9f);
        public static readonly Color HoHGold = new Color(0.94f, 0.84f, 0.12f, 0.9f);
        public static readonly Color WhiteCream = new Color(0.95f, 1f, 0.9f, 0.95f);
        private bool _foldOut = true;

        private void OnEnable()
        {
            _objectParticle = target as ObjectParticle;

            // Set editor themes
            _labelThemes.DarkTheme.SetLabels(default, default, default, 0.7f, default);
            _labelThemes.DarkTheme.SetColors(OrangeCalm, HoHGold, HoHGold, HoHGold);
            _contentThemes.DarkTheme.SetColors(WhiteCream, WhiteCream, WhiteCream, WhiteCream);

            _labelThemes.LightTheme.SetLabels(default, default, default, 0.7f, default);
            _labelThemes.LightTheme.SetColors(OrangeDark, HoHGoldDark, HoHGoldDark, HoHGoldDark);
        }


        public override void OnInspectorGUI()
        {
            // Draw the inspector
            EditorTemplates.DrawCustomInspector(serializedObject, () =>
            {
                _contentThemes.LightTheme.SetColors(OrangeDark, OrangeDark, OrangeDark, OrangeDark);



                // Set the initial theme for the override system settings bool
                EditorThemes.SetEditorTheme(_contentThemes.CurrentTheme);

                var overrideSettings = EditorTemplates.DrawField("OverrideSystemSettings", serializedObject);
                _contentThemes.LightTheme.SetColors(default, default, default, default);

                var labelTheme = _labelThemes.CurrentTheme;
                var contentTheme = _contentThemes.CurrentTheme;

                EditorThemes.SetEditorTheme(labelTheme, contentTheme);

                if (serializedObject.FindProperty("OverrideSystemSettings").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.indentLevel++;
                    _foldOut = EditorGUILayout.Foldout(_foldOut, "Settings", true);
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;

                    if (_foldOut)
                    {
                        EditorGUI.indentLevel++;

                        // Set the rest of the editor theme
                        // If we're overriding, expose the overriding settings
                        if (_objectParticle.OverrideSystemSettings)
                        {
                            EditorGUI.indentLevel++;
                            EditorTemplates.DrawField("ObjectSettings.InheritMovement", serializedObject);
                            EditorTemplates.DrawField("ObjectSettings.InheritRotation", serializedObject);
                            EditorTemplates.DrawField("ObjectSettings.InheritScale", serializedObject);

                            GUILayout.Space(2);
                            EditorTemplates.DrawField("ObjectSettings.CallFixedUpdate", serializedObject);
                            GUILayout.Space(2);

                            EditorTemplates.DrawField("ObjectSettings.ReleaseOnRigidbodyCollision", serializedObject);

                            if (_objectParticle.ObjectSettings.ReleaseOnRigidbodyCollision)
                            {
                                labelTheme.LabelFontStyle = FontStyle.Italic;
                                EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                                EditorGUI.indentLevel++;

                                EditorGUILayout.HelpBox(new GUIContent(
                                    "Must be attached to a RigidBody to subscribe to collisions. Does not work properly if collision module of particle system is enabled."));
                                EditorTemplates.DrawField("ObjectSettings.IgnoreLayers", serializedObject);
                                EditorTemplates.DrawField("ObjectSettings.IgnoreTag", serializedObject);

                                labelTheme.LabelFontStyle = FontStyle.Normal;
                                EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                                EditorGUI.indentLevel--;
                            }

                            EditorTemplates.DrawField("ObjectSettings.RecycleOnRelease", serializedObject);
                            labelTheme.LabelFontStyle = FontStyle.Italic;

                            if (!_objectParticle.ObjectSettings.RecycleOnRelease)
                            {
                                labelTheme.LabelFontStyle = FontStyle.Italic;
                                EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                                EditorGUI.indentLevel++;

                                EditorTemplates.DrawField("ObjectSettings.DestroyComponentOnRelease", serializedObject,
                                    "Destroy Component");
                                EditorTemplates.DrawField("ObjectSettings.ReleaseContainer", serializedObject);

                                labelTheme.LabelFontStyle = FontStyle.Normal;
                                EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                                EditorGUI.indentLevel--;
                            }

                            labelTheme.LabelFontStyle = FontStyle.Normal;
                            EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                            EditorGUI.indentLevel--;
                            EditorGUI.indentLevel--;

                            EditorTemplates.DrawSeparatorLine(10, 5);

                        }
                    }
                }
            });
        }
    }
}
