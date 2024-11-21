#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AudioManager manager = (AudioManager)target;
        
        EditorGUILayout.LabelField("Audio Pool Statistics", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Pool Size Per Sound: {AudioManager.POOL_SIZE_PER_SOUND}");
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "Audio instances are managed at runtime. Check the profiler for detailed statistics.", 
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Enter play mode to view audio pool statistics.", 
                MessageType.Info
            );
        }
        
        EditorGUI.indentLevel--;
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif 