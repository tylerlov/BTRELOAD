Shader "GuardingPearSoftware/ColorGridShader" {

	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_OverlayTex("AO (RGB)", 2D) = "white" {}
		_Scale("Texture Scale", Float) = 1.0
	}
			
	SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert

		fixed4 _Color;
		sampler2D _OverlayTex;
		float _Scale;

		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float4 color : COLOR;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			// Object Position.
			float3 objectPos = unity_ObjectToWorld._m03_m13_m23;
			
			// Color of the Overlay.
			fixed4 c;
			
			if (abs(IN.worldNormal.x)>0.5)
			{
				float2 UV = IN.worldPos.yz - objectPos.yz; // side
				c = tex2D(_OverlayTex, UV * _Scale); // use WALLSIDE texture
			}
			else if (abs(IN.worldNormal.z)>0.5)
			{
				float2 UV = IN.worldPos.xy - objectPos.xy; // front
				c = tex2D(_OverlayTex, UV * _Scale); // use WALL texture
			}
			else
			{
				float2 UV = IN.worldPos.xz - objectPos.xz; // top
				c = tex2D(_OverlayTex, UV * _Scale); // use FLR texture
			}

			fixed4 v = IN.color;
			o.Albedo = v.rgb * c.rgb * _Color;
		}
		ENDCG
	}

	Fallback "VertexLit"
}