//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using FMODUnity;
using SonicBloom.Koreo.Players.FMODStudio;
using UnityEditor;
using UnityEngine;


namespace SonicBloom.Koreo.EditorUI.FMODStudioTools
{
    /// <summary>
    /// The custom Editor override for the Koreographed Event Emitter component.
    /// </summary>
    [CustomEditor(typeof(KoreographedEventEmitter))]
    [CanEditMultipleObjects]
    public class KoreographedEventEmitterEditor : StudioEventEmitterEditor
    {
        /// <summary>
        /// Draws the component-appropriate GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawLine();

            var koreoSet = serializedObject.FindProperty("koreographySet");
            var koreoCom = serializedObject.FindProperty("targetKoreographer");
            var isPlayer = serializedObject.FindProperty("isMusicPlayer");

            EditorGUILayout.PropertyField(koreoSet, new GUIContent("Koreography Set"));
            EditorGUILayout.PropertyField(koreoCom, new GUIContent("Target Koreographer"));
            EditorGUILayout.PropertyField(isPlayer, new GUIContent("Is Music Player"));

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws a horizontal line in IMGUI.
        /// </summary>
        static void DrawLine()
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));

            r.y += r.height * 0.5f;
            r.height = 1f;

            EditorGUI.DrawRect(r, Color.gray);
        }
    }
}
