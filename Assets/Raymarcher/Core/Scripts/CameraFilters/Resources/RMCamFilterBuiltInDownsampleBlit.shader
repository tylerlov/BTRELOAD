Shader "Matej Vanco/Raymarcher/BuiltInDownsampleBlit"
{
    Properties
    { }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
        Cull Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            uniform sampler2D _RaymarcherFrame;
            uniform half _Sharpness;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 rmColor = tex2D(_RaymarcherFrame, input.uv);
                return lerp(half4(0,0,0,0), rmColor, step(_Sharpness, rmColor.a));
            }

            ENDCG
        }
    }
}
