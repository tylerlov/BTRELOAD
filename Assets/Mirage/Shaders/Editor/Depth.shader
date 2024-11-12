// Copyright (c) 2021-2024 Léo Chaumartin

Shader "Mirage/Internal/Depth"
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
            };

            struct v2f
            {
                float depth : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Transform vertex position to camera space
                float4 cameraSpacePos = mul(UNITY_MATRIX_MV, v.vertex);
                // Use the negative z value because camera space looks down the negative z-axis
                o.depth = -cameraSpacePos.z;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Normalize depth based on camera's far plane
                float normalizedDepth = 0.75 + (1.0 - i.depth / _ProjectionParams.z)/4.0;
                // Apply depth level
            return fixed4(normalizedDepth, normalizedDepth, normalizedDepth, 1.0);
            }

        ENDCG
    }
    }
}