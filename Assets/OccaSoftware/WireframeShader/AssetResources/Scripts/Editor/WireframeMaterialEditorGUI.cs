using UnityEngine;
using UnityEditor;
using System;

namespace OccaSoftware.Wireframe.Editor
{
	public class WireframeMaterialEditorGUI : ShaderGUI
	{
		Material t;
		public override void OnGUI(MaterialEditor e, MaterialProperty[] properties)
		{
			t = e.target as Material;

			// Color
			MaterialProperty _Color = FindProperty("_Color", properties);
			MaterialProperty _Emission = FindProperty("_Emission", properties);
			MaterialProperty _Opacity = FindProperty("_Opacity", properties);
			MaterialProperty _LightingMode = FindProperty("_LightingMode", properties);
			MaterialProperty _WireframeSize = FindProperty("_WireframeSize", properties);

			MaterialProperty _FadingEnabled = FindProperty("_FadingEnabled", properties);
			MaterialProperty _FadeMinMaxDistance = FindProperty("_FadeMinMaxDistance", properties);


			MaterialProperty _PreferQuadsEnabled = FindProperty("_PreferQuadsEnabled", properties);


			MaterialProperty _CastShadowsEnabled = FindProperty("_CastShadowsEnabled", properties);
			MaterialProperty _ReceiveShadowsEnabled = FindProperty("_ReceiveShadowsEnabled", properties);
			MaterialProperty _ReceiveDirectLightingEnabled = FindProperty("_ReceiveDirectLightingEnabled", properties);
			MaterialProperty _ReceiveAmbientLightingEnabled = FindProperty("_ReceiveAmbientLightingEnabled", properties);
			MaterialProperty _ReceiveFogEnabled = FindProperty("_ReceiveFogEnabled", properties);
			MaterialProperty _BlendSrc = FindProperty("_BlendSrc", properties);
			MaterialProperty _BlendDst = FindProperty("_BlendDst", properties);
			MaterialProperty _ZWrite = FindProperty("_ZWrite", properties);
			MaterialProperty _ZTest = FindProperty("_ZTest", properties);
			MaterialProperty _Culling = FindProperty("_Culling", properties);

			DrawSurfaceOptions();
			DrawSurfaceInputs();
			DrawAdvancedOptions();


			void DrawSurfaceInputs()
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Surface Inputs", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				DrawColorNoAlphaProperty(new GUIContent("Color"), _Color);
				if ((Runtime.LightingMode)_LightingMode.floatValue == Runtime.LightingMode.Lit)
				{
					DrawColorNoAlphaProperty(new GUIContent("Emission"), _Emission);
				}

				e.ShaderProperty(_Opacity, new GUIContent("Opacity"));
				EditorGUI.indentLevel--;


				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Wireframe Rendering Inputs", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				DrawFloatWithMinValue(new GUIContent("Wireframe Size"), _WireframeSize, 0f);
				e.ShaderProperty(_PreferQuadsEnabled, new GUIContent("Prefer Quads"));
				EditorGUI.indentLevel--;
			}

			void DrawSurfaceOptions()
			{
				EditorGUILayout.LabelField("Surface Options", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				DrawEnumProperty((Runtime.LightingMode)_LightingMode.floatValue, _LightingMode, new GUIContent("Lighting Mode"), SetShadowCasterPass);
				if ((Runtime.LightingMode)_LightingMode.floatValue == Runtime.LightingMode.Lit)
				{
					DrawShaderPassToggle(new GUIContent("Cast Shadows"), _CastShadowsEnabled, SetShadowCasterPass);
					e.ShaderProperty(_ReceiveShadowsEnabled, new GUIContent("Receive Shadows"));
					e.ShaderProperty(_ReceiveDirectLightingEnabled, new GUIContent("Receive Direct Lighting"));
					e.ShaderProperty(_ReceiveAmbientLightingEnabled, new GUIContent("Receive Ambient Lighting"));
					e.ShaderProperty(_ReceiveFogEnabled, new GUIContent("Receive Fog"));
				}
				EditorGUI.indentLevel--;
			}


			void DrawAdvancedOptions()
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;

				DrawEnumProperty((UnityEngine.Rendering.CullMode)_Culling.floatValue, _Culling, new GUIContent("Culling"));
				DrawEnumProperty((UnityEngine.Rendering.BlendMode)_BlendSrc.floatValue, _BlendSrc, new GUIContent("Blend Mode (Source)"));
				DrawEnumProperty((UnityEngine.Rendering.BlendMode)_BlendDst.floatValue, _BlendDst, new GUIContent("Blend Mode (Destination)"));
				DrawEnumProperty((Runtime.State)_ZWrite.floatValue, _ZWrite, new GUIContent("Depth Write"));
				DrawEnumProperty((UnityEngine.Rendering.CompareFunction)_ZTest.floatValue, _ZTest, new GUIContent("Depth Test"));
				EditorGUI.indentLevel--;
			}


			


			



			void SetShadowCasterPass()
			{
				bool shadowCasterEnabled = false;
				if ((Runtime.LightingMode)_LightingMode.floatValue == Runtime.LightingMode.Lit && Convert.ToBoolean(_CastShadowsEnabled.floatValue))
					shadowCasterEnabled = true;

				t.SetShaderPassEnabled("ShadowCaster", shadowCasterEnabled);
			}

			void DrawShaderPassToggle(GUIContent content, MaterialProperty a, Action SetPassMethod)
			{
				EditorGUI.BeginChangeCheck();

				e.ShaderProperty(a, content);
				if (EditorGUI.EndChangeCheck())
				{
					SetPassMethod();
				}
				EditorGUI.showMixedValue = false;
			}

			void DrawColorNoAlphaProperty(GUIContent content, MaterialProperty a)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = a.hasMixedValue;
				Color c = EditorGUILayout.ColorField(content, a.colorValue, true, false, true);
				if (EditorGUI.EndChangeCheck())
				{
					a.colorValue = c;
				}
				EditorGUI.showMixedValue = false;
			}


			void DrawFloatWithMinValue(GUIContent content, MaterialProperty a, float min)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = a.hasMixedValue;
				e.ShaderProperty(a, content);

				if (EditorGUI.EndChangeCheck())
					a.floatValue = Mathf.Max(min, a.floatValue);

				EditorGUI.showMixedValue = false;
			}


			void DrawEnumProperty(Enum e, MaterialProperty p, GUIContent c, Action onValueChanged = null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = p.hasMixedValue;
				var v = EditorGUILayout.EnumPopup(c, e);
				if (EditorGUI.EndChangeCheck())
				{
					p.floatValue = Convert.ToInt32(v);
					if(onValueChanged != null)
						onValueChanged();
				}
				EditorGUI.showMixedValue = false;
			}

		}

		
	}
}