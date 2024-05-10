Shader "Selected Effect --- Wireframe/GeometryWireframe" {
	Properties {
		_Color   ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Main", 2D) = "white" {}

		[Header(Wireframe)][Space(5)]
		_WireColor     ("Wire Color", Color) = (1, 1, 1, 1)
		_WireThickness ("Wire Thickness", Float) = 0.05
		_AASmooth      ("Anti Smoothing", Float) = 1.5

		[Header(Glow)][Space(5)]
		[Toggle(ENABLE_GLOW)] _1 ("Glow Enable", Float) = 0.0
		_GlowColor ("Glow Color", Color) = (0, 0, 0, 1)
		_GlowDist  ("Glow Distance", Float) = 0.35
		_GlowPower ("Glow Power", Float) = 0.5

		[Header(Quad)][Space(5)]
		[Toggle(ENABLE_QUAD)] _2 ("Use Quad", Float) = 0.0
		[Toggle(ENABLE_HOLLOW)] _3 ("Use Hollow", Float) = 0.0
		_Cutoff    ("Cutoff", Float) = 0.8

		[Header(Scanline)][Space(5)]
		[Toggle(ENABLE_SCANLINE)] _4 ("Use Scanline", Float) = 0.0
		_ScanlineDensity ("Density", Float) = 1
		_ScanlineSpeed   ("Speed", Float) = 3
		_ScanlineWidth   ("Width", Range(0, 1)) = 0.2
		_ScanlineFade    ("Fade", Float) = 0.8

		[Header(RenderState)][Space(5)]
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 0
	}
	SubShader {
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		Cull [_Cull]
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma shader_feature ENABLE_GLOW
			#pragma shader_feature ENABLE_QUAD
			#pragma shader_feature ENABLE_HOLLOW
			#pragma shader_feature ENABLE_SCANLINE
			#include "Core.cginc"

			sampler2D _MainTex;
			half4 _MainTex_ST, _WireColor, _Color;
			half _Cutoff, _ScanlineWidth, _ScanlineSpeed, _ScanlineDensity, _ScanlineFade;

			struct v2f
			{
				float4 pos     : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				float3 dist    : TEXCOORD1;
				float3 wldpos  : TEXCOORD2;
				UNITY_FOG_COORDS(3)
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert (appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.wldpos = mul(unity_ObjectToWorld, v.vertex).xyz;

				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			[maxvertexcount(3)]
			void geom (triangle v2f i[3], inout TriangleStream<v2f> stream)
			{
				v2f i0, i1, i2; i0 = i[0]; i1 = i[1]; i2 = i[2];
				wireframe_geom(i0.wldpos, i1.wldpos, i2.wldpos, i0.dist, i1.dist, i2.dist);
				stream.Append(i0); stream.Append(i1); stream.Append(i2);
			}
			half4 frag (v2f input) : SV_Target
			{
				half4 col = tex2D(_MainTex, input.texcoord);
				half4 orig = col;

				half4 surfCol = col;
				half mask = 0.0, wire = 0.0;
				float3 thickness = 0.0;
				wireframe(input.dist, input.texcoord, mask, thickness, wire);
#if ENABLE_GLOW
				glow(input.dist, thickness, mask, col);
#endif
				half3 wireCol = lerp(surfCol.rgb, _WireColor.rgb, _WireColor.a); // alpha blend wireframe to surface
				col.rgb = lerp(wireCol.rgb, col.rgb, wire);
#if ENABLE_HOLLOW
				clip(_Cutoff - wire);
				col = _WireColor;
#endif
#if ENABLE_SCANLINE
				float l = frac((input.wldpos.y * _ScanlineDensity) + _Time.x * _ScanlineSpeed);
				l = smoothstep(_ScanlineWidth, _ScanlineWidth + _ScanlineFade, l);
				col = lerp(orig, col, l);
#endif
				UNITY_APPLY_FOG(input.fogCoord, col);
				return col;
			}
			ENDCG
		}
		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
		UsePass "Universal Render Pipeline/Lit/DepthOnly"
		UsePass "Universal Render Pipeline/Lit/Meta"
	}
	FallBack Off
}