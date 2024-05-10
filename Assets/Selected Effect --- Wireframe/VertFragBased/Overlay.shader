Shader "Selected Effect --- Wireframe/Overlay" {
	Properties {
		[HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0

		[MainColor] _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1)
		[MainTexture] _BaseMap ("Albedo", 2D) = "white" {}

		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Smoothness    ("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale ("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		_SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0

		[Gamma] _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap ("Metallic", 2D) = "white" {}

		_SpecColor    ("Specular", Color) = (0.2, 0.2, 0.2)
		_SpecGlossMap ("Specular", 2D) = "white" {}

		[ToggleOff] _SpecularHighlights     ("Specular Highlights", Float) = 1.0
		[ToggleOff] _EnvironmentReflections ("Environment Reflections", Float) = 1.0

		_BumpScale ("Scale", Float) = 1.0
		_BumpMap   ("Normal Map", 2D) = "bump" {}

		_OcclusionStrength ("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap      ("Occlusion", 2D) = "white" {}

		_EmissionColor ("Color", Color) = (0, 0, 0)
		_EmissionMap   ("Emission", 2D) = "white" {}

		// Blending state
		[HideInInspector] _Surface   ("__surface", Float) = 0.0
		[HideInInspector] _Blend     ("__blend", Float) = 0.0
		[HideInInspector] _AlphaClip ("__clip", Float) = 0.0
		[HideInInspector] _SrcBlend  ("__src", Float) = 1.0
		[HideInInspector] _DstBlend  ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite    ("__zw", Float) = 1.0
		[HideInInspector] _Cull      ("__cull", Float) = 2.0

		_ReceiveShadows ("Receive Shadows", Float) = 1.0

		[HideInInspector] _QueueOffset ("Queue offset", Float) = 0.0

		[Header(Wireframe)][Space(5)]
		_LineColor  ("Line Color", Color) = (0, 1, 0, 1)
		_LineWidth  ("Line Width", Float) = 0.5
	}
	SubShader {
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True" }
		LOD 300
		Pass {
			Name "StandardLit"
			Tags { "LightMode" = "UniversalForward" }

			Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite] Cull [_Cull]

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _EMISSION
			#pragma shader_feature_local _METALLICSPECGLOSSMAP
			#pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _OCCLUSIONMAP

			#pragma shader_feature _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature _GLOSSYREFLECTIONS_OFF
			#pragma shader_feature_local _SPECULAR_SETUP
			#pragma shader_feature _RECEIVE_SHADOWS_OFF

			// -------------------------------------
			// Universal Render Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				float4 tangentOS  : TANGENT;
				float2 uv         : TEXCOORD0;
				float2 uvLM       : TEXCOORD1;
				float3 color      : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings
			{
				float2 uv               : TEXCOORD0;
				float2 uvLM             : TEXCOORD1;
				float4 positionWSAndFog : TEXCOORD2; // xyz: positionWS, w: vertex fog factor
				half3  normalWS         : TEXCOORD3;
#if _NORMALMAP
				half3 tangentWS         : TEXCOORD4;
				half3 bitangentWS       : TEXCOORD5;
#endif
#ifdef _MAIN_LIGHT_SHADOWS
				float4 shadowCoord      : TEXCOORD6; // compute shadow coord per-vertex for the main light
#endif
				float3 barycentric      : TEXCOORD7;
				float4 positionCS       : SV_POSITION;
			};
			Varyings LitPassVertex (Attributes input)
			{
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

				Varyings output;
				output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
				output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				output.positionWSAndFog = float4(vertexInput.positionWS, fogFactor);
				output.normalWS = vertexNormalInput.normalWS;
#ifdef _NORMALMAP
				output.tangentWS = vertexNormalInput.tangentWS;
				output.bitangentWS = vertexNormalInput.bitangentWS;
#endif
#ifdef _MAIN_LIGHT_SHADOWS
				output.shadowCoord = GetShadowCoord(vertexInput);
#endif
				output.positionCS = vertexInput.positionCS;
				output.barycentric = input.color;
				return output;
			}

			half _LineWidth;
			half3 _LineColor;

			half4 LitPassFragment (Varyings input) : SV_Target
			{
				SurfaceData surfaceData;
				InitializeStandardLitSurfaceData(input.uv, surfaceData);

#if _NORMALMAP
				half3 normalWS = TransformTangentToWorld(surfaceData.normalTS, half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
#else
				half3 normalWS = input.normalWS;
#endif
				normalWS = normalize(normalWS);

#ifdef LIGHTMAP_ON
				half3 bakedGI = SampleLightmap(input.uvLM, normalWS);
#else
				half3 bakedGI = SampleSH(normalWS);
#endif

				float3 positionWS = input.positionWSAndFog.xyz;
				half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);

				BRDFData brdfData;
				InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

#ifdef _MAIN_LIGHT_SHADOWS
				Light mainLight = GetMainLight(input.shadowCoord);
#else
				Light mainLight = GetMainLight();
#endif

				half3 color = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS);
				color += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);

#ifdef _ADDITIONAL_LIGHTS
				int additionalLightsCount = GetAdditionalLightsCount();
				for (int i = 0; i < additionalLightsCount; ++i)
				{
					Light light = GetAdditionalLight(i, positionWS);
					color += LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS);
				}
#endif

				color += surfaceData.emission;
				float fogFactor = input.positionWSAndFog.w;
				color = MixFog(color, fogFactor);

				// wireframe
				float3 d = fwidth(input.barycentric);
				float3 a3 = smoothstep(0.0, d, input.barycentric);
				float mindist = min(min(a3.x, a3.y), a3.z);
				float edge = 1.0 - mindist;
				float f = saturate(edge - _LineWidth);
				color = lerp(color, _LineColor, f);

				return half4(color, surfaceData.alpha);
			}
			ENDHLSL
		}
		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
		UsePass "Universal Render Pipeline/Lit/DepthOnly"
		UsePass "Universal Render Pipeline/Lit/Meta"
	}
	FallBack Off
}