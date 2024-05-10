using HohStudios.Editor;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace HohStudios.Tools.ObjectParticleSpawner
{

    /// <summary>
    /// The custom editor for the ObjectParticleSpawner to draw the inspector for user convenience.
    /// </summary>
    [CustomEditor(typeof(ObjectParticleSpawner), true)]
    [CanEditMultipleObjects]
    public class ObjectParticleSpawnerEditor : UnityEditor.Editor
    {
        ObjectParticleSpawner _spawner;
        EditorDualTheme _labelThemes;
        EditorDualTheme _contentThemes;

        // Editor theme colors
        public static readonly Color OrangeDark = new Color(0.3f, 0.2f, 0f, 1f);
        public static readonly Color HoHGoldDark = new Color(0.92f, 0.55f, 0f, 1f);

        public static readonly Color OrangeCalm = new Color(0.9f, 0.63f, 0.19f, 0.9f);
        public static readonly Color HoHGold = new Color(0.94f, 0.84f, 0.12f, 0.9f);
        public static readonly Color WhiteCream = new Color(0.95f, 1f, 0.9f, 0.95f);

        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            _spawner = target as ObjectParticleSpawner;

            // Set editor themes
            _labelThemes.DarkTheme.SetLabels(default, default, default, 0.7f, default);
            _labelThemes.DarkTheme.SetColors(OrangeCalm, HoHGold, HoHGold, HoHGold);
            _contentThemes.DarkTheme.SetColors(WhiteCream, WhiteCream, WhiteCream, WhiteCream);

            _labelThemes.LightTheme.SetLabels(default, default, default, 0.7f, default);
            _labelThemes.LightTheme.SetColors(OrangeDark, HoHGoldDark, HoHGoldDark, HoHGoldDark);

        }

        public override void OnInspectorGUI()
        {
            // Draw the editor
            EditorTemplates.DrawCustomInspector(serializedObject, () =>
            {
                _contentThemes.LightTheme.SetColors(WhiteCream, WhiteCream, WhiteCream, WhiteCream);
                EditorTemplates.DrawTitleBar("HoH Studios", _contentThemes.CurrentTheme, _spawner);

                // Set content theme back to black after drawing title bar
                _contentThemes.LightTheme.SetColors(default, default, default, default);

                var labelTheme = _labelThemes.CurrentTheme;
                var contentTheme = _contentThemes.CurrentTheme;

                // Draw the disabled group into the titlebar template with modified theme
                GUILayout.Space(-28);
                EditorGUI.BeginDisabledGroup(true);

                var disabledThemes = _labelThemes;
                disabledThemes.DarkTheme.NormalColor = Color.white * 0.85f;
                disabledThemes.LightTheme.NormalColor = Color.black * 0.95f;

                var disabledTheme = disabledThemes.CurrentTheme;
                disabledTheme.Alignment = TextAnchor.MiddleLeft;
                disabledTheme.LabelFontSize = 9;
                EditorThemes.SetEditorTheme(disabledTheme, contentTheme);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Particles:  " + _spawner.NumberOfAliveParticles);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(-21);

                disabledTheme.Alignment = TextAnchor.MiddleRight;
                EditorThemes.SetEditorTheme(disabledTheme, contentTheme);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Objects:  " + _spawner.NumberOfAliveObjects);
                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                // Start the general editor theme
                EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                GUILayout.Space(6);

                // Draw the actual editor contents
                EditorTemplates.DrawField("SystemSettings.InheritMovement", serializedObject);
                EditorTemplates.DrawField("SystemSettings.InheritRotation", serializedObject);
                EditorTemplates.DrawField("SystemSettings.InheritScale", serializedObject);

                GUILayout.Space(2);
                EditorTemplates.DrawField("SystemSettings.CallFixedUpdate", serializedObject);
                GUILayout.Space(2);

                EditorTemplates.DrawField("SystemSettings.ReleaseOnRigidbodyCollision", serializedObject);

                if (_spawner.SystemSettings.ReleaseOnRigidbodyCollision)
                {
                    labelTheme.LabelFontStyle = FontStyle.Italic;
                    EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(new GUIContent(
                        "An ObjectParticle must be attached to a RigidBody to subscribe to collisions. Does not work properly if collision module of particle system is enabled."));
                    EditorTemplates.DrawField("SystemSettings.IgnoreLayers", serializedObject);
                    EditorTemplates.DrawField("SystemSettings.IgnoreTag", serializedObject);
                    EditorGUI.indentLevel--;
                    labelTheme.LabelFontStyle = FontStyle.Normal;
                    EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                }

                // Only show DestroyComponent if DestroyOnRelease is false
                EditorTemplates.DrawField("SystemSettings.RecycleOnRelease", serializedObject);
                if (!_spawner.SystemSettings.RecycleOnRelease)
                {
                    labelTheme.LabelFontStyle = FontStyle.Italic;
                    EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                    EditorGUI.indentLevel++;
                    EditorTemplates.DrawField("SystemSettings.DestroyComponentOnRelease", serializedObject,
                        "Destroy Component");
                    EditorTemplates.DrawField("SystemSettings.ReleaseContainer", serializedObject);
                    EditorGUI.indentLevel--;
                    labelTheme.LabelFontStyle = FontStyle.Normal;
                    EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                }

                GUILayout.Space(4);
                EditorTemplates.DrawSeparatorLine();
            });

            // Default the pool container to the spawners transform
            if (_spawner.ObjectPool.PoolContainer == null)
                _spawner.ObjectPool.PoolContainer = _spawner.gameObject.transform;
        }
    }
}