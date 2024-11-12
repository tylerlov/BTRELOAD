Shader "Mirage/Demo/FadingLabel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FadingDistance("Fading Distance", float) = 4.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent"}

        Blend SrcAlpha OneMinusSrcAlpha
//          ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float camDistance : TEXCOORD1;

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FadingDistance;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float3 worldPos = mul(unity_ObjectToWorld, o.vertex).xyz;
                o.camDistance = length(worldPos - _WorldSpaceCameraPos.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dst = length(mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz - _WorldSpaceCameraPos);
                float opacity = 1.0 / pow(max(1, dst - _FadingDistance), 4.0);
                fixed4 col = tex2D(_MainTex, i.uv) * opacity;
                return col;
            }
            ENDCG
        }
    }
}
