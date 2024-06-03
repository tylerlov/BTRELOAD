Shader "GUPS/EP/Colored Multi Graph"
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
			
			fixed _Stacked;
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

				// Get coordinates [0,1]
                float x = IN.texcoord.x;
                float y = IN.texcoord.y;
				
				// Get the value count per graph.
				float valueCount = _ValueCount / _GraphCount;
								
				// Alpha need to be shared.
				float maxAlpha = 0;
				
				// The target color.
				fixed3 targetColor = fixed3(0,0,0);
				
				// Find the graphs applying color to the target.
				float count = 0;
				
				[loop]
				for (int i = 0; i < _GraphCount; i++)
				{
					if(_Stacked < 1)
					{
						if(x < i / _GraphCount || x > (i + 1) / _GraphCount)
						{
							continue;
						}
					}					
					
					// The color for the current iterated graph.
					fixed4 graphColor = color;
					
					// Calculate the start index. 
					float startIndex = i * valueCount;					
					
					// Calculate the graph value index.
					float g = 0;
					if(_Stacked < 1)
					{
						g = floor(x * _ValueCount);
					}
					else
					{
						g = startIndex + floor(x * valueCount);
					}
					
					// Get the graph value.
					float value = _Values[g];
					
					// If smoothing is enabled, lerp with privious value.
					if(_Smooth > 0)
					{
						float a = _Values[g];
						float b = _Values[g];
						if (g > startIndex)
						{
							a = _Values[g - 1.0f];
						}
					
						// Interpolate between a and b based on the fractional part of x
						float fracValue = x * valueCount - floor(x * valueCount);
						value = lerp(a, b, fracValue);
					}
					
					// Lower graph value
					value *= 0.95f;

					// Assign the corresponding color.
					graphColor *= _GraphColors[i];

					// Apply anti aliasing or not.
					if(_AntiAliasing > 0)
					{
						// If line, remove lower values.
						if(_Line > 0)
						{
							// Remove lower graph values.
							if( y < value - 0.05f)
							{
								graphColor.a = 0;
							}
							else if(y < value)
							{
								graphColor.a *= smoothstep(value - 0.05f, value, y);
							}
						}
						
						// Remove higher graph values.
						if( y > value + 0.05f)
						{
							graphColor.a = 0;
						}
						else if(y > value)
						{
							graphColor.a *= 1.0f - smoothstep(value, value + 0.05f, y);
						}
					}
					else
					{
						// If line, remove lower values.
						if(_Line > 0)
						{
							// Remove lower graph values.
							if( y < value - 0.02f)
							{
								graphColor.a = 0;
							}
						}
						
						// Remove higher graph values.
						if( y > value + 0.02f)
						{
							graphColor.a = 0;
						}
					}
					
					// Reduce alpha for transparency.
					if( y < value - 0.05f)
					{
						graphColor.a *= 0.4f;
					}
					
					// Apply alpha to graph color. 
					graphColor.rgb *= graphColor.a;
					
					// Find the max alpha.
					if(graphColor.a > maxAlpha)
					{
						maxAlpha = graphColor.a;
					}
					
					// If the graph color is rendered, apply to target color.
					if(graphColor.a > 0)
					{
						targetColor += graphColor.rgb;
						
						count += 1;
					}
				}
				
				// Find the color.
				if(count > 1)
				{
					color.rgb = targetColor / count;
				}
				else
				{
					color.rgb = targetColor;
				}
				
				// Apply alpha to color. 
				if(maxAlpha > 0)
				{
					color.a = maxAlpha;
				}
				else
				{
					color.rgba = 0.0f;
				}

				// Return the result.
                return color;
            }
            ENDCG
        }
    }
}