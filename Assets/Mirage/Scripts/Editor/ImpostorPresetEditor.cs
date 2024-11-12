/*
 * Copyright (c) Léo CHAUMARTIN 2021-2024
 * All Rights Reserved
 * 
 * File: ImpostorPresetEditor.cs
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mirage.Impostors.Elements
{
    using Core;
    /// <summary>
    /// The ImpostorPreset custom editor
    /// </summary>
    [CustomEditor(typeof(ImpostorPreset))]
    public class ImpostorPresetEditor : Editor
    {

        ImpostorPreset presetObject;
        int resIndex = 4;
        int latitudeSamples = 4;
        int latitudeOffset = 0;
        float latitudeAngularStep = 15f;
        int longitudeSamples = 16;
        public SphereType type = SphereType.UV;

        string[] typeNames = { "UV Sphere", "Pseudo-Fibonacci (Experimental)" };
        string[] resOptions = new string[6] { "128", "256", "512", "1024", "2048", "4096" };


        private GUIStyle centeredStyle;

        public override void OnInspectorGUI()
        {
            if (presetObject == null)
            {
                presetObject = serializedObject.targetObject as ImpostorPreset;
                resIndex = presetObject.resIndex;
                latitudeSamples = presetObject.latitudeSamples;
                latitudeOffset = presetObject.latitudeOffset;
                latitudeAngularStep = presetObject.latitudeAngularStep;
                longitudeSamples = presetObject.longitudeSamples;
                type = presetObject.type;

            }

            if (centeredStyle == null)
            {
                centeredStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold

                };
            }

            GUILayout.Label(Resources.Load<Texture>("MirageLogo"), centeredStyle, GUILayout.Height(96f), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            resIndex = EditorGUILayout.Popup("Texture Size", resIndex, resOptions);
            type = (SphereType)EditorGUILayout.Popup("Sphere Type", (int)type, typeNames);
            switch (type)
            {
                case SphereType.UV:
                    longitudeSamples = EditorGUILayout.IntSlider("Longitude Samples", longitudeSamples, 1, 64);
                    latitudeSamples = EditorGUILayout.IntSlider("Latitude Samples", latitudeSamples, 0, 15);
                    latitudeOffset = EditorGUILayout.IntSlider("Latitude Offset", latitudeOffset, -latitudeSamples, latitudeSamples);
                    latitudeAngularStep = EditorGUILayout.Slider("Latitude Angle Step", latitudeAngularStep, 0, 90f / (1f + latitudeSamples + Mathf.Abs(latitudeOffset)));
                    break;
                case SphereType.PseudoFibonacci:
                    longitudeSamples = EditorGUILayout.IntSlider("Density", longitudeSamples, 1, 64);
                    latitudeSamples = longitudeSamples / 4;
                    latitudeAngularStep = 90f / (1f + latitudeSamples);
                    latitudeOffset = 0;
                    break;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel", GUILayout.Height(24)))
            {
                resIndex = presetObject.resIndex;
                latitudeSamples = presetObject.latitudeSamples;
                latitudeOffset = presetObject.latitudeOffset;
                latitudeAngularStep = presetObject.latitudeAngularStep;
                longitudeSamples = presetObject.longitudeSamples;
                type = presetObject.type;
            }
            if (GUILayout.Button("Apply", GUILayout.Height(24)))
            {
                presetObject.resIndex = resIndex;
                presetObject.latitudeSamples = latitudeSamples;
                presetObject.latitudeOffset = latitudeOffset;
                presetObject.latitudeAngularStep = latitudeAngularStep;
                presetObject.longitudeSamples = longitudeSamples;
                presetObject.type = type;
                EditorUtility.SetDirty(presetObject);
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
