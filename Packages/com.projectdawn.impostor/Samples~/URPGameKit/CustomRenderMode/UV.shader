Shader "Unlit/UV"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

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

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(i.positionOS);
                o.uv = i.texcoord;
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                return float4(i.uv, 0, 0);
            }
            ENDCG
        }
    }
}
