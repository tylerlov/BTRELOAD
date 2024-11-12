// Copyright (c) 2024 Léo Chaumartin
// This shader estimate the metallic value of each pixels given a ColorMap with white environment reflection and one whithout.
// /!\ ONLY WORKS FOR BRIGHT PIXELS - SUITABLE FOR IMPOSTORS ONLY
//
// More estimations may be added in the future such as smoothness and occlusion.

Shader "Mirage/Internal/MaskEstimator"
{
    Properties
    {
        _MainTex("ColorMap", 2D) = "white" {}
        _TexER0("ColorMap (No Environment Reflections) ", 2D) = "white" {}
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
                uniform sampler2D _TexER0;
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
                    float4 colorER0 = tex2D(_TexER0, o.uv);

                    float metallic = 1.0 - length(colorER0.xyz/color.xyz);

                    return float4(clamp(metallic, 0.0, 1.0), 0.0, 0.0, 0.0);
                }

                ENDCG
            }
        }
}