using UnityEditor;
using UnityEngine;

namespace SelectedEffectWireframe
{
	[CustomEditor(typeof(Wireframe))]
	public class WireframeInspector : Editor
	{
		SerializedProperty m_SP_TriggerMethod;
		SerializedProperty m_SP_Persistent;
		SerializedProperty m_SP_Style;
		SerializedProperty m_SP_Width;
		SerializedProperty m_SP_Color;
		SerializedProperty m_SP_FrontColor;
		SerializedProperty m_SP_BackColor;
		SerializedProperty m_SP_EnableSqueeze;
		SerializedProperty m_SP_EnableDash;
		SerializedProperty m_SP_StylizedWireframeSqueezeMin;
		SerializedProperty m_SP_StylizedWireframeSqueezeMax;
		SerializedProperty m_SP_StylizedWireframeDashRepeats;
		SerializedProperty m_SP_StylizedWireframeDashLength;
		SerializedProperty m_SP_MatBasic;
		SerializedProperty m_SP_MatOverlay;
		SerializedProperty m_SP_MatStylized;

		void OnEnable()
		{
			m_SP_TriggerMethod = serializedObject.FindProperty("m_TriggerMethod");
			m_SP_Persistent = serializedObject.FindProperty("m_Persistent");
			m_SP_Style = serializedObject.FindProperty("m_Style");
			m_SP_Width = serializedObject.FindProperty("m_Width");
			m_SP_Color = serializedObject.FindProperty("m_Color");
			m_SP_FrontColor = serializedObject.FindProperty("m_FrontColor");
			m_SP_BackColor = serializedObject.FindProperty("m_BackColor");
			m_SP_EnableSqueeze = serializedObject.FindProperty("m_EnableSqueeze");
			m_SP_EnableDash = serializedObject.FindProperty("m_EnableDash");
			m_SP_StylizedWireframeSqueezeMin = serializedObject.FindProperty("m_StylizedWireframeSqueezeMin");
			m_SP_StylizedWireframeSqueezeMax = serializedObject.FindProperty("m_StylizedWireframeSqueezeMax");
			m_SP_StylizedWireframeDashRepeats = serializedObject.FindProperty("m_StylizedWireframeDashRepeats");
			m_SP_StylizedWireframeDashLength = serializedObject.FindProperty("m_StylizedWireframeDashLength");
			m_SP_MatBasic = serializedObject.FindProperty("m_MatBasic");
			m_SP_MatOverlay = serializedObject.FindProperty("m_MatOverlay");
			m_SP_MatStylized = serializedObject.FindProperty("m_MatStylized");
		}
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_SP_TriggerMethod, true);
			EditorGUILayout.PropertyField(m_SP_Persistent, true);
			EditorGUILayout.PropertyField(m_SP_Style, true);
			EditorGUILayout.PropertyField(m_SP_MatBasic, true);
			EditorGUILayout.PropertyField(m_SP_MatOverlay, true);
			EditorGUILayout.PropertyField(m_SP_MatStylized, true);
			EditorGUILayout.PropertyField(m_SP_Width, true);
			int enumIdx = m_SP_Style.enumValueIndex;
			if (enumIdx == 0 || enumIdx == 1 || enumIdx == 3)
			{
				EditorGUILayout.PropertyField(m_SP_Color, true);
			}
			else if (enumIdx == 2)
			{
				EditorGUILayout.PropertyField(m_SP_FrontColor, true);
				EditorGUILayout.PropertyField(m_SP_BackColor, true);
			}
			else if (enumIdx == 4)
			{
				EditorGUILayout.PropertyField(m_SP_Color, true);
				EditorGUILayout.PropertyField(m_SP_EnableSqueeze, true);
				EditorGUILayout.PropertyField(m_SP_EnableDash, true);
				EditorGUILayout.PropertyField(m_SP_StylizedWireframeSqueezeMin, true);
				EditorGUILayout.PropertyField(m_SP_StylizedWireframeSqueezeMax, true);
				EditorGUILayout.PropertyField(m_SP_StylizedWireframeDashRepeats, true);
				EditorGUILayout.PropertyField(m_SP_StylizedWireframeDashLength, true);
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}