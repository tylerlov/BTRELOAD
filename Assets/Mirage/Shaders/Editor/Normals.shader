// Copyright (c) 2021-2024 Léo Chaumartin

Shader "Mirage/Internal/Normals"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 normal : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldNormal = normalize(i.normal);
                float3 cameraNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
                return float4(cameraNormal * 0.5 + 0.5, 1.0);
            }

            ENDCG
        }
    }
}