Shader "GUPS/EP/Mobile/Rated Single Graph"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
        [HideInInspector] _Color("Base", Color) = (1,1,1,0.9)
        [MaterialToggle] _PixelSnap("Pixel Snap", Float) = 0

        [MaterialToggle] _HighIsGood("High is Good", Float) = 1
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
						
            fixed _HighIsGood;
            fixed _Thresholds[9];
            fixed4 _Colors[10];
			float _ColorCount;

            uniform float _Values[256];
            uniform float _ValueCount;

            fixed4 frag(v2f IN) : SV_Target
            {
				// Get init color.
                fixed4 color = IN.color;

				// Get coordinates [0,1]
                float x = IN.texcoord.x;
                float y = IN.texcoord.y;
				
				// Calculate the global graph index. 
				float globalIndex = floor(x * _ValueCount);

				// Get the graph value.
				float value = _Values[globalIndex];
				
				[loop]
				for (int i = 0; i < _ColorCount; i++)
				{
					// Find the upper and lower thresholds.
					float upperThreshold = step(1.0f, _HighIsGood);
					
					float lowerThreshold = 1.0f - step(1.0f, _HighIsGood);
					
					if(i > 0)
					{
						upperThreshold = _Thresholds[i - 1];
					}
					
					if(i < _ColorCount - 1)
					{
						lowerThreshold = _Thresholds[i];
					}
					
					// Skip based on the threshold.
					if(_HighIsGood > 0)
					{					
						// Skip everything above.
						if(value > upperThreshold)
						{
							continue;
						}
						
						// Skip everything below.
						if(value < lowerThreshold)
						{
							continue;
						}
					}
					else
					{
						// Skip everything below.
						if(value < upperThreshold)
						{
							continue;
						}
						
						// Skip everything above.
						if(value > lowerThreshold)
						{
							continue;
						}
					}
					
					// Lower graph value
					value *= 0.95f;

					// Assign the corresponding color.
					color *= _Colors[i];
					
					// Remove higher graph values.
					if( y > value + 0.02f)
					{
						color.a = 0;
					}
					
					// Reduce alpha for transparency.
					if( y < value - 0.05f)
					{
						color.a *= 0.4f;
					}
				}

				// Apply alpha to color. 
                color.rgb *= color.a;

                return color;
            }
            ENDCG
        }
    }
}