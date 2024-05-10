using HohStudios.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HohStudios.Common
{
    /// <summary>
    /// The custom drawer script to display the object pooling field in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(ObjectPool), true)]
    [CanEditMultipleObjects]
    public class ObjectPoolDrawer : PropertyDrawer
    {
        //Cached variables
        private bool _spawnObjFoldout = true;
        SerializedProperty _currentProperty;

        EditorDualTheme _labelThemes;
        EditorDualTheme _contentThemes;

        // Editor theme colors
        public static readonly Color OrangeDark = new Color(0.3f, 0.2f, 0f, 1f);
        public static readonly Color HoHGoldDark = new Color(0.92f, 0.55f, 0f, 1f);

        public static readonly Color OrangeCalm = new Color(0.9f, 0.63f, 0.19f, 0.9f);
        public static readonly Color HoHGold = new Color(0.94f, 0.84f, 0.12f, 0.9f);
        public static readonly Color WhiteCream = new Color(0.95f, 1f, 0.9f, 0.95f);

        /// <summary>
        /// The Objects To Spawn reorderable list, with an auto-instantiating property to initialize as needed
        /// </summary>
        private ReorderableList _reorderableList;
        private ReorderableList ReorderableList
        {
            get
            {
                if (_reorderableList == null)
                {
                    if (_currentProperty == null) return _reorderableList;

                    //Get the private serialized objects to spawn list
                    _reorderableList = new ReorderableList(_currentProperty.serializedObject, _currentProperty.FindPropertyRelative("_objectsToSpawn"));

                    //Custom drawing callbacks to draw the reorderable list
                    _reorderableList.drawHeaderCallback -= DrawListHeader;
                    _reorderableList.drawElementCallback -= DrawListElements;
                    _reorderableList.onAddCallback -= AddListElements;

                    _reorderableList.drawHeaderCallback += DrawListHeader;
                    _reorderableList.drawElementCallback += DrawListElements;
                    _reorderableList.onAddCallback += AddListElements;
                }

                return _reorderableList;
            }
        }


        /// <summary>
        /// Adds an empty spawn object to the list
        /// </summary>
        private void AddListElements(ReorderableList list)
        {
            //Adding handles multi - selected objects too
            var multiSelect = _currentProperty.serializedObject.targetObjects;
            var propName = _currentProperty.name;
            foreach (var obj in multiSelect)
            {
                var sobj = new SerializedObject(obj);
                var prop = sobj.FindProperty(propName);
                var pool = EditorTemplates.GetSerializedPropertyBaseObject<ObjectPool>(prop);
                pool?.SpawnObjectsAdd(null, 0);
            }
        }

        /// <summary>
        /// Draws the reorderable list header
        /// </summary>
        private void DrawListHeader(Rect rect)
        {
            //Uses a custom aligned style based on the content theme colors to draw
            var style = EditorThemes.DefaultTheme.Style(EditorStyles.label);//_contentTheme.Style(EditorStyles.label);
            style.alignment = TextAnchor.MiddleLeft;
            EditorGUI.LabelField(new Rect(rect.x + 20, rect.y, rect.width, rect.height), "Spawn Objects", style);
            EditorGUI.LabelField(new Rect(rect.width / 1.5f + 40, rect.y, rect.width / 3 - 20f, rect.height),
                "Weights", style);
        }

        /// <summary>
        /// Draws the reoderable list element rows
        /// </summary>
        private void DrawListElements(Rect rect, int index, bool isactive, bool isfocused)
        {
            //Set height of each line
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += EditorGUIUtility.standardVerticalSpacing / 2;

            //Get the current element
            var spawnObj = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);

            //Needed so that we refresh multi-editing views on adding new obj because we don't use the standard serialized property setters 
            spawnObj.serializedObject.SetIsDifferentCacheDirty();

            if (spawnObj.FindPropertyRelative("Object") == null)
                return;

            //Draw the entry's contents with proper width spacing
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width / 1.5f, rect.height),
                spawnObj.FindPropertyRelative("Object"), GUIContent.none);
            EditorGUI.PropertyField(new Rect(rect.width / 1.5f + 50, rect.y, rect.width / 3 - 25f, rect.height),
                spawnObj.FindPropertyRelative("Weight"), GUIContent.none);
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _currentProperty = property;


            EditorTemplates.DrawCustomInspector(property, () =>
            {
                GUILayout.Space(-20);

                //Set the color of the theme for the IsAwake property and draw it

                // Set editor themes

                _labelThemes.DarkTheme.SetColors(Color.white * 0.85f, default, default, default);
                _labelThemes.LightTheme.SetColors(Color.black * 0.95f, default, default, default);

                var labelTheme = _labelThemes.CurrentTheme;
                var contentTheme = _contentThemes.CurrentTheme;

                labelTheme.SetLabels(default, 11, default, 0.7f, default);

                EditorThemes.SetEditorTheme(labelTheme, contentTheme);

                EditorGUI.BeginDisabledGroup(true);
                EditorTemplates.DrawField("_isAwake", property);
                EditorGUI.EndDisabledGroup();

                //Set the rest of the editor theme to the desired general theme here

                _labelThemes.DarkTheme.SetLabels(default, default, default, 0.7f, default);
                _labelThemes.DarkTheme.SetColors(OrangeCalm, HoHGold, HoHGold, HoHGold);
                _contentThemes.DarkTheme.SetColors(WhiteCream, WhiteCream, WhiteCream, WhiteCream);
                _labelThemes.LightTheme.SetLabels(default, default, default, 0.7f, default);
                _labelThemes.LightTheme.SetColors(OrangeDark, HoHGoldDark, HoHGoldDark, HoHGoldDark);
                _contentThemes.LightTheme.SetColors(default, default, default, default);

                labelTheme = _labelThemes.CurrentTheme;
                contentTheme = _contentThemes.CurrentTheme;
                //Set theme and draw the rest of the prop tree

                EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                GUILayout.Space(-4);

                //Draw properties

                EditorTemplates.DrawField("SpawnPoolOnAwake", property);

                var objectPoolSize = EditorTemplates.DrawField("PoolSize", property);
                if (objectPoolSize.intValue < 0)
                    objectPoolSize.intValue = 0;

                var currentContainer = EditorTemplates.DrawField("PoolContainer", property);
                if (currentContainer.objectReferenceValue == null)
                    currentContainer.objectReferenceValue = (_currentProperty.serializedObject?.targetObject as Component)?.transform;

                var adaptivePool = EditorTemplates.DrawField("AllowAdaptivePool", property);
                if (adaptivePool.boolValue)
                {
                    labelTheme.LabelFontStyle = FontStyle.Italic;
                    EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                    EditorGUI.indentLevel++;

                    var adaptivePadding = EditorTemplates.DrawField("AdaptivePoolPadding", property);
                    if (adaptivePadding.intValue < 0)
                        adaptivePadding.intValue = 0;

                    var adaptiveSpeed = EditorTemplates.DrawField("AdaptivePoolSpeed", property);
                    if (adaptiveSpeed.intValue < 0)
                        adaptiveSpeed.intValue = 0;


                    var adaptiveShrink = EditorTemplates.DrawField("AdaptiveShrinkDelay", property);
                    if (adaptiveShrink.floatValue < 0)
                        adaptiveShrink.floatValue = 0;

                    EditorGUI.indentLevel--;
                    labelTheme.LabelFontStyle = FontStyle.Normal;
                    EditorThemes.SetEditorTheme(labelTheme, contentTheme);
                }

                GUILayout.Space(4);

                EditorTemplates.DrawSeparatorLine();

                //Create a new content theme based GUIStyle for the foldout to stylize it and draw the foldout
                // Draw the foldout and the reorderable lsit

                var style = _labelThemes.CurrentTheme.Style(EditorStyles.foldout);
                style.onNormal = new GUIStyleState() { textColor = (_labelThemes.CurrentTheme.NormalColor / 1.33f) };

                //Draw the foldout and the reorderable lsit
                _spawnObjFoldout = EditorGUILayout.Foldout(_spawnObjFoldout, "Objects To Spawn", true, style);
                if (_spawnObjFoldout)
                {
                    GUILayout.Space(2);
                    ReorderableList?.DoLayoutList();
                }
            });
        }
    }
}
