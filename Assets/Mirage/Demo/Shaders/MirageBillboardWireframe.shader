


Shader "Mirage/Demo/BillboardWireframe"
{
    Properties
    {
        [Header(RGB)]
        _Color("Color", Color) = (1,1,1,1)

        [Header(Alpha)]
        _Opacity("Opacity", Range(0.0,1.0)) = 0.1

        [Header(Fading)]
        _FadeDistance("Max distance", float) = 0.5

        [Header(Impostor Specific Settings)]
        _LatitudeSamples("Latitude Samples", range(1, 15)) = 3
        _LatitudeOffset("Latitude Offset", range(-15, 15)) = 0
        _LatitudeStep("Latitude Step", range(0.0, 90.0)) = 15.0
        [MaterialToggle]_BillboardingEnabled("Enabled", Float) = 1
        [MaterialToggle]_ClampLatitude("Clamp Latitude", Float) = 1
        _ZOffset("Z Offset", range(0, 1)) = 0.1
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" "Queue" = "Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off
            Cull Off

            Pass
            {
                Name "LINES"
                CGPROGRAM
                #pragma vertex vert
                #pragma geometry geom
                #pragma fragment frag
                #include "UnityCG.cginc"

            // Structs and global variables here
            struct v2g
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float2 uv : TEXCOORD0;
};

struct g2f
{
    float4 vertex : SV_POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    float3 wpos : TEXCOORD1;
    float visibility : TEXCOORD2;
};



half4 _Color;
float _Opacity;
float _FadeDistance;
float _BillboardingEnabled;
float _ZOffset;
float _ClampLatitude;
float _LatitudeSamples;
float _LatitudeOffset;
float _LatitudeStep;


g2f getg2f(v2g v)
{
    g2f o;
    v.vertex.xyz += v.normal.xyz * 0.0001;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.uv = v.uv;
    o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.visibility = _Opacity;
    return o;
}

            #ifdef UNITY_WEBGL
            void vert(inout v2g v) {
            // Keep vertex shader minimal for WebGL
        }

        [maxvertexcount(32)]
        void geom(triangle v2g v[3], uint pid : SV_PRIMITIVEID, inout LineStream<g2f> stream) {
            // Geometry shader is omitted in WebGL since it's not supported
        }

        half4 frag(g2f i) : SV_Target {
            discard; // Make fragment shader discard all pixels on WebGL
            return half4(0,0,0,0); // This line is technically not necessary because of discard
        }
        #else


#include "../../Shaders/Runtime/MirageGeometryHelper.hlsl"



        // Non-WebGL vertex, geometry, and fragment shader code here
        void vert(inout v2g v) {
            Billboarding(v.vertex.xyz, v.normal.xyz, v.tangent.xyz);
        }

        [maxvertexcount(32)]
        void geom(triangle v2g v[3], uint pid : SV_PRIMITIVEID, inout LineStream<g2f> stream) {
            // Original geometry shader code
            g2f g0 = getg2f(v[0]);
            g2f g1 = getg2f(v[1]);
            g2f g2 = getg2f(v[2]);

            stream.Append(g0);
            stream.Append(g1);
            stream.Append(g2);
            stream.Append(g0);
            stream.RestartStrip();

            stream.Append(g0);
            stream.RestartStrip();
            stream.Append(g1);
            stream.RestartStrip();
            stream.Append(g2);
            stream.RestartStrip();
        }

        half4 frag(g2f i) : SV_Target {
            // Original fragment shader code
            float3 vd = normalize(i.wpos - _WorldSpaceCameraPos);
            float facing = -dot(i.normal.xyz , vd);
            float visibility = smoothstep(-0.1,0.5,facing) + smoothstep(0.1,-0.5,facing);
            visibility = saturate(visibility) * i.visibility;
            float dst = length(mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz - _WorldSpaceCameraPos);
            visibility *= 1.0 / pow(max(1, dst - _FadeDistance), 8.0);
            if (visibility < 0.001) discard;
            half4 col = half4(_Color.rgb , _Color.a * visibility);
            return col;
        }
        #endif

        ENDCG
    }
        }
}
