Shader "GUPS/EP/Normalized Multi Graph"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
        [HideInInspector] _Color("Base", Color) = (1,1,1,0.9)
        [MaterialToggle] _Line("Line", Float) = 0
        [MaterialToggle] _Smooth("Smooth", Float) = 0
        [MaterialToggle] _AntiAliasing("Anti Aliasing", Float) = 0
        [MaterialToggle] _PixelSnap("Pixel Snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;

            v2f vert(appdata_t IN)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_OUTPUT(v2f, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
					OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }
			
			fixed _Line;
			fixed _Smooth;
			fixed _AntiAliasing;
			
            fixed4 _GraphColors[4];		
			uniform float _GraphCount;
			
            uniform float _Values[1024];
            uniform float _ValueCount;

            fixed4 frag(v2f IN) : SV_Target
            {
				// Get init color.
                fixed4 color = IN.color;

				// Get coordinates [0,1].
                float x = IN.texcoord.x;
                float y = IN.texcoord.y;
				
				// Get the value count per graph.
				float valueCount = _ValueCount / _GraphCount;
								
				[loop]
				for (int i = 0; i < _GraphCount; i++)
				{
					// Calculate the local graph index.
					float localIndex = floor(x * valueCount);
					
					// Calculate the global graph index. 
					float globalIndex = valueCount * i + localIndex;
					
					// Get the value at the global index.
					float value = _Values[globalIndex];					
					
					// If smoothing is enabled, lerp with privious value.
					if(_Smooth > 0)
					{
						float a = _Values[globalIndex];
						float b = _Values[globalIndex];
						if (globalIndex > valueCount * i)
						{
							a = _Values[globalIndex - 1.0f];
						}
					
						// Interpolate between a and b based on the fractional part of x
						float fracValue = x * _ValueCount - floor(x * _ValueCount);
						value = lerp(a, b, fracValue);
					}
					
					// Calculate the min height to start render from.
					float minHeight = 0.0f;
							
					[loop]
					for (int j = 0; j < i; j++)
					{
						minHeight += _Values[valueCount * j + localIndex];
					}
					
					// Calculate the max height to render to.
					float maxHeight = minHeight + value;
					
					// Skip all below.
					if(y < minHeight)
					{
						continue;
					}
					
					// Skip all above.
					if(y > maxHeight)
					{
						color.a = 0.0f;
						continue;
					}
					
					// Reset alpha.
					color.a = 0.9f;
										
					// Assign the corresponding color.
					color *= _GraphColors[i];					
					
					// Reduce alpha for transparency.
					if( y < maxHeight - 0.05f)
					{
						color.a *= 0.4f;
					}
				}				

				// Apply alpha to color. 
                color.rgb *= color.a;

				// Return the result.
                return color;
            }
            ENDCG
        }
    }
}