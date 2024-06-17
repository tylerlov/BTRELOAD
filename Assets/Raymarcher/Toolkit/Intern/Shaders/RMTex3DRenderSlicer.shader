Shader "Matej Vanco/Raymarcher/Extras/RMTex3DRenderSlicer"
{
    Properties
    {
        _ColorOutsideFov("Out Of The Fov Color", Color) = (1,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertexLP : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertexHC : SV_POSITION;
                float2 uv : TEXCOORD0;
                float dist : TEXCOORD1;
                float3 vertexWS : TEXCOORD2;
            };

            half4 _ColorOutsideFov;

            uniform half _Slice, _SliceWidth;
            uniform float3 _Min, _Max;
            uniform float _PreviewSlice;
            uniform float _AdjustToView;
            uniform float _AdjustDerivateX;
            uniform float _AdjustDerivateY;

            #define ROT(a) float2x2(sin(a), -cos(a), cos(a), sin(a))
            static float3 ADJUSTVERT(float3 vert)
            {
                vert.xy = mul(vert.xy, ROT(radians(_AdjustDerivateY)));
                vert.xz = mul(vert.xz, ROT(radians(_AdjustDerivateX)));
                return vert;
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                v.vertexLP.xyz = lerp(v.vertexLP.xyz, ADJUSTVERT(v.vertexLP.xyz), _AdjustToView);
                o.vertexHC = UnityObjectToClipPos(v.vertexLP);
                o.vertexWS = mul(unity_ObjectToWorld, v.vertexLP).xyz;
                float3 d = max(_Min.xyz - o.vertexWS, o.vertexWS - _Max.xyz);
                o.dist = step(max(max(d.x, d.y), d.z), 0.);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float dz = _Max.z - i.vertexWS.z;
                float depth = lerp(_Min.z, _Max.z, ((_Max.z - i.vertexWS.z) / (_Max.z - _Min.z)));
                float sliceT = lerp(_Max.z, _Min.z, _Slice);
                clip(step(depth - _SliceWidth - sliceT, _SliceWidth) * step(_SliceWidth, depth + _SliceWidth - sliceT) - _PreviewSlice);
                return lerp(half4(1,1,1,1), _ColorOutsideFov, step(i.dist, 0.));
            }
            ENDCG
        }
    }
}
