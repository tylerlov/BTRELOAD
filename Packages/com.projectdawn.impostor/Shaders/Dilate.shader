Shader "Hidden/Impostor/Dilate"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma editor_sync_compilation

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            int _Frames;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 Tap(float2 uv)
            {
                float4 tap = tex2Dlod(_MainTex, float4(uv, 0, 0));
                tap.rgb *= tap.a;
                return tap;
            }

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(i.positionOS);
                o.uv = i.texcoord;
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;

                float4 pixel = tex2D(_MainTex, i.uv);

                // Skip if not background
                if (pixel.a > 0.1)
                    return pixel;

                float2 tileIndex = floor(i.uv * _Frames);
                float2 tileSize = 1.0 / _Frames;
                float2 mins = tileSize * tileIndex;
                float2 maxs = mins + tileSize;

                // Skip frame borders to add some padding
                if (any(i.uv - mins <= texelSize*2) || any(maxs - i.uv <= texelSize*2))
                    return pixel;

                float4 dilation = 0;
#if 1
                dilation += Tap(clamp(i.uv + texelSize * float2(1, 0), mins, maxs));
                dilation += Tap(clamp(i.uv + texelSize * float2(-1, 0), mins, maxs));
                dilation += Tap(clamp(i.uv + texelSize * float2(0, 1), mins, maxs));
                dilation += Tap(clamp(i.uv + texelSize * float2(0, -1), mins, maxs));

                dilation += Tap(clamp(i.uv + texelSize * float2(1, 1), mins, maxs));
                dilation += Tap(clamp(i.uv + texelSize * float2(1, -1), mins, maxs));
                dilation += Tap(clamp(i.uv + texelSize * float2(-1, 1), mins, maxs));
                dilation += Tap(clamp(i.uv + texelSize * float2(-1, -1), mins, maxs));
#else
                dilation = max(Tap(clamp(i.uv + texelSize * float2(1, 0), mins, maxs)), dilation);
                dilation = max(Tap(clamp(i.uv + texelSize * float2(-1, 0), mins, maxs)), dilation);
                dilation = max(Tap(clamp(i.uv + texelSize * float2(0, 1), mins, maxs)), dilation);
                dilation = max(Tap(clamp(i.uv + texelSize * float2(0, -1), mins, maxs)), dilation);

                dilation = max(Tap(clamp(i.uv + texelSize * float2(1, 1), mins, maxs)), dilation);
                dilation = max(Tap(clamp(i.uv + texelSize * float2(1, -1), mins, maxs)), dilation);
                dilation = max(Tap(clamp(i.uv + texelSize * float2(-1, 1), mins, maxs)), dilation);
                dilation = max(Tap(clamp(i.uv + texelSize * float2(-1, -1), mins, maxs)), dilation);
#endif

                if (dilation.a > 0)
                {
                    dilation.rgb /= dilation.a;
                    return dilation;
                }
                else
                {
                    return pixel;
                }
            }
            ENDCG
        }
    }
}
