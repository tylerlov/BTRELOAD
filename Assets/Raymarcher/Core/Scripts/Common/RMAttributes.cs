using System;
using System.Collections;
using System.Threading.Tasks;
using System.Reflection;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Raymarcher.Attributes
{
    public static class RMAttributes
    {
        /// <summary>
        /// Create a conditional field (enums, bools, object references and ints only).
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
        public sealed class ShowIf : PropertyAttribute
        {
            public string[] fieldNames;
            public int evaluationValue;
            public bool addIndentLevel;
            public bool negativeEvaluation;
            public bool setFieldToOppositeValueIfConditionNotMet;
            public bool orOperator;

            public float backColorPanelOpacity;
            public bool backColorPanelAdjustToTopField;

            /// <summary>
            /// Show conditional field. Use '|' to make a dependency on multiple fields
            /// </summary>
            public ShowIf(string fieldNames, int fieldValue, bool addIndentLevel = true, bool notEquals = false, bool setFieldToOppositeValueIfConditionNotMet = false, bool orOperator = true, float backColorPanelOpacity = 0, bool backColorPanelAdjustToTopField = false)
            {
                if (!fieldNames.Contains("|"))
                    this.fieldNames = new string[1] { fieldNames };
                else
                    this.fieldNames = fieldNames.Split('|');
                evaluationValue = fieldValue;
                this.addIndentLevel = addIndentLevel;
                negativeEvaluation = notEquals;
                this.setFieldToOppositeValueIfConditionNotMet = setFieldToOppositeValueIfConditionNotMet;
                this.backColorPanelOpacity = backColorPanelOpacity;
                this.orOperator = orOperator;
                this.backColorPanelAdjustToTopField = backColorPanelAdjustToTopField;
            }
        }

        /// <summary>
        /// Draw a read-only field in the editor with an additional extension for 'condition-based-boolean-field' that controls if the certain field can be readonly or not.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public sealed class ReadOnly : PropertyAttribute
        {
            public string basedOnConditionField;

            public ReadOnly() => basedOnConditionField = "";

            public ReadOnly(string basedOnConditionField) => this.basedOnConditionField = basedOnConditionField;
        }

        /// <summary>
        /// Draw a button field in the editor replacing the target field.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public sealed class Button : PropertyAttribute
        {
            public string displayText;
            public string methodNameWithNoParameter;

            public Button(string displayText, string methodNameWithNoParameter)
            {
                this.displayText = displayText;
                this.methodNameWithNoParameter = methodNameWithNoParameter;
            }
        }

        /// <summary>
        /// Draw a grayscale background under specific fields.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public sealed class DrawBackgroundPanel : PropertyAttribute
        {
            public float backColorPanelOpacity;
            public float backColorContentPanelOpacity;

            public DrawBackgroundPanel(float backColorPanelOpacity, float backColorContentPanelOpacity = 0)
            {
                this.backColorPanelOpacity = backColorPanelOpacity;
                this.backColorContentPanelOpacity = backColorContentPanelOpacity;
            }
        }

        /// <summary>
        /// Highlight certain field contents in cyan.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public sealed class Required : PropertyAttribute
        {
            public Required() { }
        }

        /// <summary>
        /// Create an info-box dependency in the editor. An existing object must be specified and will be highlighted in the editor once clicked on the button.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public sealed class Dependency : PropertyAttribute
        {
            public string dependentOn;
            public Type typeToSelect;
            public string fieldToHighlight;
            public string dependencySubject;
            public bool plural;

            public Dependency(string dependentOn, string fieldToHighlight, Type typeToSelect, bool plural = false, string dependencySubject = "global feature")
            {
                this.dependentOn = dependentOn;
                this.fieldToHighlight = fieldToHighlight;
                this.typeToSelect = typeToSelect;
                this.dependencySubject = dependencySubject;
                this.plural = plural;
            }
        }

#if UNITY_EDITOR

        private static class RM_AttributeExtensions
        {
            /// <summary>
            /// Get property parent - outputs the received parent serialized property & parent path.
            /// </summary>
            /// <param name="serializedObject">Target serialized object</param>
            /// <param name="currentPath">Current property path</param>
            /// <param name="targetPropertyToFind">Target property name (fieldname)</param>
            /// <param name="receivedParent">Outputs received parent (could be null!)</param>
            /// <param name="parentPath">Outputs received parent path (could be the same as current property if the parent is null)</param>
            /// <returns>Returns true if successful, returns false if viceversa</returns>
            public static bool ReceivePropertyParent(SerializedObject serializedObject, string currentPath,
                string targetPropertyToFind, out SerializedProperty receivedParent, out string parentPath)
            {
                string bPath = ReturnPropertyParent(currentPath);
                bool nonDerivatedPath = bPath == currentPath;
                receivedParent = null;
                parentPath = bPath;

                SerializedProperty parentProperty = serializedObject.FindProperty(bPath);
                if (parentProperty == null)
                {
                    Debug.Log("Parent property is null. Serialized object: " + serializedObject.targetObject.name);
                    return false;
                }
                receivedParent = nonDerivatedPath ? serializedObject.FindProperty(targetPropertyToFind) : parentProperty.FindPropertyRelative(targetPropertyToFind);
                return true;
            }

            /// <summary>
            /// Returns a base path of the specific property path
            /// </summary>
            public static string ReturnPropertyParent(string originalPath)
            {
                if (!originalPath.Contains(".")) return originalPath;
                string p = "";
                var pathes = originalPath.Split('.');
                for (int i = 0; i < pathes.Length - 1; i++) p += pathes[i] + (i + 1 == pathes.Length - 1 ? "" : ".");
                return p;
            }

            public static bool HasRangeAttributeDrawer(SerializedProperty currentProperty, Rect position, GUIContent label, FieldInfo fieldInfo)
            {
                bool isFloatProp = currentProperty.propertyType == SerializedPropertyType.Float;
                bool isIntProp = currentProperty.propertyType == SerializedPropertyType.Integer;
                if (!isFloatProp && !isIntProp)
                    return false;

                RangeAttribute[] rangeAtts = fieldInfo.GetCustomAttributes(typeof(RangeAttribute), true) as RangeAttribute[];
                if (rangeAtts.Length != 1)
                    return false;

                RangeAttribute range = rangeAtts[0];
                if (isFloatProp)
                    currentProperty.floatValue = EditorGUI.Slider(position, label, currentProperty.floatValue, range.min, range.max);
                else
                    currentProperty.intValue = EditorGUI.IntSlider(position, label, currentProperty.intValue, (int)range.min, (int)range.max);
                return true;
            }
        }

        [CustomPropertyDrawer(typeof(ShowIf))]
        public sealed class ShowIfEditor : PropertyDrawer
        {
            private ShowIf showIfAttribute;

            public override float GetPropertyHeight(SerializedProperty currentProperty, GUIContent label)
            {
                showIfAttribute = attribute as ShowIf;

                if (!EvaluadeCondition(currentProperty))
                    return -EditorGUIUtility.standardVerticalSpacing;
                else
                {
                    if (currentProperty.propertyType == SerializedPropertyType.Generic && currentProperty.isExpanded)
                    {
                        float heighSum = 0.0f;

                        IEnumerator children = currentProperty.GetEnumerator();
                        while (children.MoveNext())
                        {
                            SerializedProperty child = children.Current as SerializedProperty;
                            heighSum += EditorGUI.GetPropertyHeight(child) + EditorGUIUtility.standardVerticalSpacing;
                        }

                        return heighSum + EditorGUIUtility.standardVerticalSpacing * 15;
                    }

                    return EditorGUI.GetPropertyHeight(currentProperty, label);
                }
            }

            public override void OnGUI(Rect position, SerializedProperty currentProperty, GUIContent label)
            {
                showIfAttribute = attribute as ShowIf;

                if (EvaluadeCondition(currentProperty))
                {
                    if (showIfAttribute.addIndentLevel)
                        EditorGUI.indentLevel++;
                    EditorGUI.DrawRect(new Rect(position.x - 16, position.y - (showIfAttribute.backColorPanelAdjustToTopField ? 2 : 0), 
                        position.width + 20, position.height + (showIfAttribute.backColorPanelAdjustToTopField ? 3 : 0)), 
                        Color.gray * showIfAttribute.backColorPanelOpacity);
                    if (!RM_AttributeExtensions.HasRangeAttributeDrawer(currentProperty, position, label, fieldInfo))
                    {
                        if (currentProperty.propertyType == SerializedPropertyType.Generic)
                            EditorGUI.PropertyField(position, currentProperty, label, true);
                        else
                            EditorGUI.PropertyField(position, currentProperty, label);
                    }
                    if (showIfAttribute.addIndentLevel)
                        EditorGUI.indentLevel--;
                }
            }

            private bool EvaluadeCondition(SerializedProperty currentProperty)
            {
                bool[] evaluations = new bool[showIfAttribute.fieldNames.Length];

                for (int i = 0; i < evaluations.Length; i++)
                {
                    if (!RM_AttributeExtensions.ReceivePropertyParent(currentProperty.serializedObject, currentProperty.propertyPath,
                    showIfAttribute.fieldNames[i], out SerializedProperty propToCompare, out _))
                    {
                        Debug.LogError($"ShowIf attribute error! Couldn't find a property's target field '{showIfAttribute.fieldNames}'!");
                        return false;
                    }

                    SerializedProperty toCompareProperty = propToCompare;
                    bool showProperty = GetTargetPropertyValue(toCompareProperty);
                    if (showIfAttribute.negativeEvaluation)
                        showProperty = !showProperty;
                    evaluations[i] = showProperty;
                    if (showIfAttribute.setFieldToOppositeValueIfConditionNotMet && !showProperty)
                    {
                        switch (currentProperty.propertyType)
                        {
                            case SerializedPropertyType.Boolean:
                                currentProperty.boolValue = toCompareProperty.boolValue;
                                break;
                        }
                    }
                }

                if(showIfAttribute.orOperator)
                {
                    foreach(bool eval in evaluations)
                    {
                        if (eval)
                            return true;
                    }
                    return false;
                }
                else
                {
                    foreach(bool eval in evaluations)
                    {
                        if (!eval)
                            return false;
                    }
                    return true;
                }
            }

            private bool GetTargetPropertyValue(SerializedProperty p)
            {
                switch (p.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        return (p.boolValue ? 1 : 0) == showIfAttribute.evaluationValue;
                    case SerializedPropertyType.Enum:
                        return p.enumValueIndex == showIfAttribute.evaluationValue;
                    case SerializedPropertyType.Integer:
                        return p.intValue == showIfAttribute.evaluationValue;
                    case SerializedPropertyType.ObjectReference:
                        return showIfAttribute.evaluationValue == 1 && p.objectReferenceValue != null;
                    default:
                        Debug.LogError("None of the property datatypes are listed in the method");
                        return false;
                }
            }
        }

        [CustomPropertyDrawer(typeof(DrawBackgroundPanel))]
        public sealed class DrawBackgroundPanelEditor : PropertyDrawer
        {
            private DrawBackgroundPanel panelAttribute;

            public override float GetPropertyHeight(SerializedProperty currentProperty, GUIContent label)
            {
                panelAttribute = attribute as DrawBackgroundPanel;
                if (currentProperty.propertyType == SerializedPropertyType.Generic && currentProperty.isExpanded)
                {
                    float heighSum = 0.0f;

                    IEnumerator children = currentProperty.GetEnumerator();
                    while (children.MoveNext())
                    {
                        SerializedProperty child = children.Current as SerializedProperty;
                        heighSum += EditorGUI.GetPropertyHeight(child) + EditorGUIUtility.standardVerticalSpacing;
                    }

                    return heighSum + EditorGUIUtility.standardVerticalSpacing * 15;
                }

                return EditorGUI.GetPropertyHeight(currentProperty, label);
            }

            public override void OnGUI(Rect position, SerializedProperty currentProperty, GUIContent label)
            {
                panelAttribute = attribute as DrawBackgroundPanel;
                if(panelAttribute.backColorContentPanelOpacity == 0)
                    EditorGUI.DrawRect(new Rect(position.x - 16, position.y, position.width + 20, position.height), Color.gray * panelAttribute.backColorPanelOpacity);
                else
                {
                    EditorGUI.DrawRect(new Rect(position.x - 16, position.y, position.width + 20, position.height), Color.gray * panelAttribute.backColorContentPanelOpacity);
                    EditorGUI.DrawRect(new Rect(position.x - 16, position.y, position.width + 20, 19), Color.gray * panelAttribute.backColorPanelOpacity);
                }
                if (currentProperty.propertyType == SerializedPropertyType.Generic)
                    EditorGUI.PropertyField(position, currentProperty, label, true);
                else
                    EditorGUI.PropertyField(position, currentProperty, label);
            }
        }

        [CustomPropertyDrawer(typeof(Button))]
        public sealed class ButtonEditor : PropertyDrawer
        {
            private Button buttonAttribute;

            public override float GetPropertyHeight(SerializedProperty currentProperty, GUIContent label)
            {
                buttonAttribute = attribute as Button;
                if (currentProperty.propertyType == SerializedPropertyType.Generic && currentProperty.isExpanded)
                {
                    float heighSum = 0.0f;

                    IEnumerator children = currentProperty.GetEnumerator();
                    while (children.MoveNext())
                    {
                        SerializedProperty child = children.Current as SerializedProperty;
                        heighSum += EditorGUI.GetPropertyHeight(child) + EditorGUIUtility.standardVerticalSpacing;
                    }

                    return heighSum + EditorGUIUtility.standardVerticalSpacing * 15;
                }

                return EditorGUI.GetPropertyHeight(currentProperty, label);
            }

            public override void OnGUI(Rect position, SerializedProperty currentProperty, GUIContent label)
            {
                buttonAttribute = attribute as Button;

                if(GUI.Button(position, buttonAttribute.displayText))
                {
                    var target = currentProperty.serializedObject.targetObject;
                    Type type = target.GetType();
                    MethodInfo method = type.GetMethod(buttonAttribute.methodNameWithNoParameter);
                    if (method == null)
                    {
                        Debug.LogError("Method couldn't be found on the custom Button attribute");
                        return;
                    }
                    if (method.GetParameters().Length > 0)
                    {
                        Debug.LogError("Method can't have parameters on the custom Button attribute");
                        return;
                    }
                    method.Invoke(target, null);
                }
            }
        }

        [CustomPropertyDrawer(typeof(ReadOnly))]
        public sealed class ReadOnlyDrawer : PropertyDrawer
        {
            private ReadOnly ro;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                ro = (ReadOnly)attribute;
                bool state = false;

                if (!string.IsNullOrEmpty(ro.basedOnConditionField))
                {
                    if (RM_AttributeExtensions.ReceivePropertyParent(property.serializedObject, property.propertyPath,
                    ro.basedOnConditionField, out SerializedProperty propToCompare, out _))
                    {
                        if (propToCompare.propertyType == SerializedPropertyType.Boolean)
                            state = !propToCompare.boolValue;
                    }
                }

                GUI.enabled = state;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }
        }

        [CustomPropertyDrawer(typeof(Required))]
        public sealed class RequiredDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                Color gc = GUI.backgroundColor;
                GUI.backgroundColor = Color.cyan;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.backgroundColor = gc;
            }
        }

        [CustomPropertyDrawer(typeof(Dependency))]
        public sealed class DependencyDrawer : PropertyDrawer
        {
            private const string displayField = "displayGlobalFeatureDependencies";
            private readonly Color dependencyColorInBox = Color.gray * 1.5f;
            private const int paddingHeight = 10;
            private const int marginHeight = 8;
            private float baseHeight = 0;
            private float addedHeight = 0;

            Dependency DepenAtt => (Dependency)attribute;

            MultilineAttribute MultilineAttribute
            {
                get
                {
                    var attributes = fieldInfo.GetCustomAttributes(typeof(MultilineAttribute), true);
                    return attributes != null && attributes.Length > 0 ? (MultilineAttribute)attributes[0] : null;
                }
            }

            public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
            {
                baseHeight = base.GetPropertyHeight(prop, label);
                if (!CanDisplay(prop))
                    return baseHeight;

                float minHeight = paddingHeight * 5;
                GUIContent content = new GUIContent(DepenAtt.dependentOn);
                GUIStyle style = GUI.skin.GetStyle("helpbox");
                style.richText = true;
                float height = style.CalcHeight(content, EditorGUIUtility.currentViewWidth);

                height += marginHeight * 2;

                if (MultilineAttribute != null && prop.propertyType == SerializedPropertyType.String)
                    addedHeight = 48f;

                return height > minHeight ? height + baseHeight + addedHeight : minHeight + baseHeight + addedHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
            {
                MultilineAttribute multiline = MultilineAttribute;

                EditorGUI.BeginProperty(position, label, prop);

                if(!CanDisplay(prop))
                {
                    EditorGUI.PropertyField(position, prop, label);
                    EditorGUI.EndProperty();
                    return;
                }

                Rect helpPos = position;
                helpPos.height -= baseHeight + marginHeight;

                if (multiline != null)
                    helpPos.height -= addedHeight;

                GUIStyle ss = new GUIStyle(GUI.skin.box);
                ss.richText = true;
                ss.alignment = TextAnchor.UpperLeft;
                ss.active.textColor = ss.normal.textColor = ss.hover.textColor = dependencyColorInBox;
                ss.wordWrap = false;
                GUI.Box(helpPos, $"{(DepenAtt.plural ? "These fields are" : "This field is")} dependent on <b>{DepenAtt.dependentOn}</b> {DepenAtt.dependencySubject}.", ss);
                helpPos.y += 20;
                helpPos.height -= 20;
                ss = new GUIStyle(GUI.skin.button);
                ss.alignment = TextAnchor.MiddleLeft;
                ss.fixedWidth = 145;
                if (GUI.Button(helpPos, "Highlight global feature", ss))
                {
                    var obj = GameObject.FindObjectOfType(DepenAtt.typeToSelect);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        var _ = CreateHighlight(DepenAtt.fieldToHighlight);
                    }
                    GUIUtility.ExitGUI();
                }

                helpPos.y += 22;
                helpPos.x += 10;
                helpPos.height += 6;
                Color col = GUI.backgroundColor;
                GUI.backgroundColor = Color.gray;
                if(!DepenAtt.plural)
                    GUI.Box(helpPos, "");
                GUI.backgroundColor = col;
                helpPos.height -= 6;

                helpPos.height += 20;

                position.y += helpPos.height + marginHeight;
                position.height = baseHeight;

                if (multiline != null && prop.propertyType == SerializedPropertyType.String)
                {
                    GUIStyle style = GUI.skin.label;
                    style.richText = true;
                    float size = style.CalcHeight(label, EditorGUIUtility.currentViewWidth);
                    EditorGUI.LabelField(position, label);
                    position.y += size;
                    position.height += addedHeight - size;
                    prop.stringValue = EditorGUI.TextArea(position, prop.stringValue);
                }
                else
                {
                    if (!RM_AttributeExtensions.HasRangeAttributeDrawer(prop, position, label, fieldInfo))
                        EditorGUI.PropertyField(position, prop, label);
                }
                EditorGUI.EndProperty();
            }

            private bool CanDisplay(SerializedProperty prop)
            {
                var displayProp = prop.serializedObject.FindProperty(displayField);
                if (displayProp != null)
                {
                    if (displayProp.propertyType == SerializedPropertyType.Boolean)
                        return displayProp.boolValue;
                    else
                        return true;
                }
                return true;
            }

            private async Task CreateHighlight(string field)
            {
                await Task.Delay(50);
                if (!Highlighter.Highlight("Inspector", field))
                    return;
                await Task.Delay(2000);
                Highlighter.Stop();
            }
        }
#endif
    }
}