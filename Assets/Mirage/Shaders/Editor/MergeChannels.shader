// Copyright (c) 2021-2024 Léo Chaumartin

Shader "Mirage/Internal/MergeChannels"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _DepthTex("Depth (RGB)", 2D) = "white" {}
        _AlphaClippingDiscriminatorTex("Alpha Clipping Discriminator (RGB)", 2D) = "white" {}
    }
        SubShader
        {
            Pass
            {
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                uniform sampler2D _MainTex;
                uniform sampler2D _DepthTex;
                uniform sampler2D _AlphaClippingDiscriminatorTex;
                uniform half4 _MainTex_TexelSize;

                struct input
                {
                    float4 pos : POSITION;
                    half2 uv : TEXCOORD0;
                };

                struct output
                {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
                };


                output vert(input i)
                {
                    output o;
                    o.pos = UnityObjectToClipPos(i.pos);
                    o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, i.uv);
                    #if UNITY_UV_STARTS_AT_TOP
                    if (_MainTex_TexelSize.y < 0)
                            o.uv.y = 1 - o.uv.y;
                    #endif

                    return o;
                }

                fixed4 frag(output o) : COLOR
                {
                    float4 color = tex2D(_MainTex, o.uv);
                    float depth = tex2D(_DepthTex, o.uv).r;
                    depth *= 1.0 - 2.0 * length(color - tex2D(_AlphaClippingDiscriminatorTex, o.uv));
                    return float4(color.xyz, depth);
                }

                ENDCG
            }
        }
}