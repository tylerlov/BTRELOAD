Shader "Selected Effect --- Wireframe/Stylized" {
	Properties {
		_StylizedWireframeColor ("Wireframe", Color) = (0, 1, 0, 1)
		_StylizedWireframeThickness ("Thickness", Float) = 0.5
		_StylizedWireframeSqueezeMin ("Squeeze Min", Float) = 0.2
		_StylizedWireframeSqueezeMax ("Squeeze Max", Float) = 0.8
		_StylizedWireframeDashRepeats ("Dash Repeats", Float) = 6
		_StylizedWireframeDashLength ("Dash Length", Float) = 0.6
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalRenderPipeline" "IgnoreProjector"="True" "RenderType"="Transparent" "Queue"="Transparent" }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragStylizedWireframe
			#pragma multi_compile _ WIREFRAME_SQUEEZE
			#pragma multi_compile _ WIREFRAME_DASH
			#include "Wireframe.cginc"
			ENDCG
		}
		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
		UsePass "Universal Render Pipeline/Lit/DepthOnly"
		UsePass "Universal Render Pipeline/Lit/Meta"
	}
	FallBack Off
}
