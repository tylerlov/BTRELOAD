Shader "Selected Effect --- Wireframe/Basic" {
	Properties {
		[Header(Wireframe)]
		_LineColor ("Line Color", Color) = (0, 1, 0, 1)
		_LineWidth ("Line Width", Float) = 0.5
		_ScanlineScale ("Scanline Scale", Float) = 10
		[Header(RenderState)]
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend Src", Int) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend Dst", Int) = 0
	}
	SubShader {
		Tags { "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True" }
		Pass {
			Cull [_Cull]
			Blend [_BlendSrc] [_BlendDst]

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma shader_feature ENABLE_DOUBLE_SIDE_COLOR
			#include "Wireframe.cginc"
			ENDCG
		}
		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
		UsePass "Universal Render Pipeline/Lit/DepthOnly"
		UsePass "Universal Render Pipeline/Lit/Meta"
	}
	FallBack Off
}
