// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomPropertyDrawer(typeof(GPUIPrototype))]
    public class GPUIPrototypePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();
            container.Add(DrawPrototypeFields(property));
            return container;
        }

        private VisualElement DrawPrototypeFields(SerializedProperty property)
        {
            int prototypeType = property.FindPropertyRelative("prototypeType").intValue;
            bool isAllowEdit = false;
            if (property.serializedObject.targetObject is GPUIManager manager)
                isAllowEdit = manager.Editor_IsAllowEditPrototype(prototypeType);

            VisualElement prototypeFieldsVE = new VisualElement();
            prototypeFieldsVE.SetEnabled(!Application.isPlaying && isAllowEdit);

            switch (prototypeType)
            {
                case 0:
                    prototypeFieldsVE.Add(GPUIEditorUtility.DrawSerializedProperty(property.FindPropertyRelative("prefabObject")));
                    break;
                case 1:
                    prototypeFieldsVE.Add(GPUIEditorUtility.DrawSerializedProperty(property.FindPropertyRelative("gpuiLODGroupData")));
                    break;
                case 2:
                    prototypeFieldsVE.Add(GPUIEditorUtility.DrawSerializedProperty(property.FindPropertyRelative("prototypeMesh")));
                    prototypeFieldsVE.Add(GPUIEditorUtility.DrawSerializedProperty(property.FindPropertyRelative("prototypeMaterials")));
                    break;
            }
            if (!Application.isPlaying && isAllowEdit)
                prototypeFieldsVE.RegisterCallbackDelayed<SerializedPropertyChangeEvent>(OnChange);

            return prototypeFieldsVE;
        }

        private void OnChange(SerializedPropertyChangeEvent evt)
        {
            if (Application.isPlaying)
                return;
            evt.changedProperty.serializedObject.ApplyModifiedProperties();
            evt.changedProperty.serializedObject.Update();
            if (evt.changedProperty.serializedObject.targetObject is GPUIManager gpuiManager && gpuiManager.IsInitialized)
                gpuiManager.Initialize();
            if (GPUIRenderingSystem.IsActive)
                GPUIRenderingSystem.Instance.LODGroupDataProvider.RegenerateLODGroups();
        }
    }
}
