using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Editor;

namespace Micosmo.SensorToolkit.BehaviorDesigner.Editor {

    [CustomObjectDrawer(typeof(ObjectTypeAttribute))]
    public class ObjectTypeDrawer : ObjectDrawer {

        public override void OnGUI(GUIContent label) {
            EditorGUILayout.BeginVertical();

            var val = (Value is SharedVariable)
                ? (Value as SharedVariable).GetValue()
                : Value;

            var typeConstraint = ((ObjectTypeAttribute)attribute).ObjectType;
            var isError = val != null && !typeConstraint.IsInstanceOfType(val);

            if (isError) {
                EditorGUILayout.HelpBox($"{fieldInfo.Name} must be an instance of {typeConstraint.Name}", MessageType.Error);
            }

            var prevGuiColor = GUI.color;
            if (isError) {
                GUI.color = Color.red;
            }
            value = FieldInspector.DrawFields(task, value, label);
            GUI.color = prevGuiColor;

            EditorGUILayout.EndVertical();
        }

    }

}
