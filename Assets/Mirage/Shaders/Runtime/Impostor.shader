// Copyright (c) 2024 Léo Chaumartin
// The BiRP Lit Impostor shader


Shader "Mirage/Impostor"
{

    Properties
    {
        [HideInInspector]_MainTex("Albedo", 2D) = "white" {}
        [HideInInspector]_NormalMap("Normals", 2D) = "bump" {}
        [HideInInspector]_MaskMap("Normals", 2D) = "white" {}
        [HideInInspector]_GridSize("Grid Size", float) = 64
        [HideInInspector]_LongitudeSamples("Longitude Samples", range(1, 64)) = 16
        [HideInInspector]_LatitudeSamples("Latitude Samples", range(1, 15)) = 3
        [HideInInspector]_LatitudeOffset("Latitude Offset", range(-15, 15)) = 0
        [HideInInspector]_LatitudeStep("Latitude Step", range(0.0, 90.0)) = 15.0
        [HideInInspector][MaterialToggle]_SmartSphere("Smart Sphere", Float) = 0

        [MaterialToggle]_BillboardingEnabled("Enabled", Float) = 1
        [MaterialToggle]_ClampLatitude("Clamp Latitude", Float) = 1
        _ZOffset("Z Offset", range(0, 4)) = 0.5
        
        _Brightness("Brightness", range(0, 2)) = 1.0
        _Saturation("Saturation", range(0, 2)) = 1.0
        _Smoothness("Smoothness", range(0, 1)) = 0.5
        _Metallic("Metallic", range(0, 1)) = 1.0
        _Occlusion("Occlusion", range(0, 1)) = 1.0
        _NormalStrength("NormalStrength", range(0, 10)) = 1.0
        _CurvedOcclusion("Curvature Occlusion", range(0.0, 0.99)) = 0.0

        _Cutout("Cutout", range(0, 1)) = 0.355
        [MaterialToggle]_Smooth("Interpolate", Float) = 1
        [MaterialToggle]_DitheringFade("DitheringFade", Float) = 0

        _YawOffset("Yaw Offset", range(0, 6.283185)) = 0.0
        _ElevationOffset("Elevation Offset", range(-1.570796, 1.570796)) = 0.0

    }
        SubShader
        {



            CGINCLUDE
        #pragma require integers
        #pragma target 3.0  
        #pragma multi_compile_instancing

            ENDCG
            Tags{ "RenderType" = "Opaque" }
            ZWrite On

            Cull Off // Renders both sides for two-sided shadows

            CGPROGRAM
            #pragma surface surf Standard vertex:vert alphatest:_Cutout addshadow fullforwardshadows dithercrossfade 
            #define PI 3.14159
            #define TWO_PI 6.28318
            #define DEG2RAD 0.01745328

            struct Input {
              float2 uv_MainTex;
              float3 screenPos;
              float3 originViewDir;
            };

            float3 normalize(float3 v)
            {
                return rsqrt(dot(v, v)) * v;
            }

            sampler2D _MainTex;
            sampler2D _NormalMap;
            sampler2D _MaskMap;
            float _GridSize;
            uint _LongitudeSamples;
            float _LatitudeSamples;
            float _LatitudeOffset;
            float _LatitudeStep;
            float _Brightness;
            float _Saturation;
            float _Smoothness;
            float _Metallic;
            float _NormalStrength;
            float _Occlusion;
            float _SmartSphere;
            float _Smooth;
            float _DitheringFade;
            float _YawOffset;
            float _ElevationOffset;
            float _CurvedOcclusion;
            float _BillboardingEnabled;
            float _ClampLatitude;
            float _ZOffset;
            float4 _TreeInstanceColor;

            #include "MirageGeometryHelper.hlsl"
            #include "MirageCoreHelper.hlsl"
            
            void vert(inout appdata_full v, out Input o) {
                Billboarding(v.vertex.xyz, v.normal.xyz, v.tangent.xyz);
                o.uv_MainTex = v.texcoord.xy;
                o.screenPos = ComputeScreenPos(v.vertex).xyz;
                o.originViewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz);
            }

            void surf(Input i, inout SurfaceOutputStandard  o)
            {
                float4 color, normal;
                float metallic;
                float alpha, beta;
                float2 gridUv, gridUvLB, gridUvLT, gridUvRB, gridUvRT;
                
                GetImpostorUV_float(i.uv_MainTex, i.originViewDir, gridUv, gridUvLB, gridUvLT, gridUvRB, gridUvRT, alpha, beta);
                if (_Smooth > 0.5) {

                    float4 colorLB = tex2D(_MainTex, gridUvLB);
                    float4 colorRB = tex2D(_MainTex, gridUvRB);
                    float4 colorLT = tex2D(_MainTex, gridUvLT);
                    float4 colorRT = tex2D(_MainTex, gridUvRT);
                    float4 colorB = lerp(colorRB, colorLB, beta);
                    float4 colorT = lerp(colorRT, colorLT, beta);
                    color = lerp(colorT, colorB, alpha);

                    colorLB = tex2D(_NormalMap, gridUvLB);
                    colorRB = tex2D(_NormalMap, gridUvRB);
                    colorLT = tex2D(_NormalMap, gridUvLT);
                    colorRT = tex2D(_NormalMap, gridUvRT);
                    colorB = lerp(colorRB, colorLB, beta);
                    colorT = lerp(colorRT, colorLT, beta);
                    normal = lerp(colorT, colorB, alpha);

                    colorLB = tex2D(_MaskMap, gridUvLB);
                    colorRB = tex2D(_MaskMap, gridUvRB);
                    colorLT = tex2D(_MaskMap, gridUvLT);
                    colorRT = tex2D(_MaskMap, gridUvRT);
                    colorB = lerp(colorRB, colorLB, beta);
                    colorT = lerp(colorRT, colorLT, beta);
                    metallic = lerp(colorT, colorB, alpha).r;
                }
                else {
                    
                    color = tex2D(_MainTex, gridUv);
                    normal = tex2D(_NormalMap, gridUv);
                    metallic = tex2D(_MaskMap, gridUv).r;
                }
                if (_DitheringFade > .5 && Dither(i.screenPos, color.a * 2.0) < .0)
                    discard;
                if (_TreeInstanceColor.r > 0.0) {
                    float3 hsv = RGBToHSV(color.rgb);
                    hsv.x += _TreeInstanceColor.r;
                    o.Albedo = clamp(RGBToHSV(hsv) * _Brightness, 0.0, 1.0);
                 }
                else
                    o.Albedo = clamp(SaturateColor(color, _Saturation) * _Brightness, 0.0, 1.0);
                normal = (normal * 2.0 - 1.0);
                normal.y = -normal.y;
                normal.xy *= _NormalStrength;
                normalize(normal);
                
                o.Normal = normal;
                o.Smoothness = _Smoothness;
                o.Metallic = _Metallic * metallic;
                float curvatureOcclusion = (1.0 - _CurvedOcclusion*((abs(ddx(normal)) + abs(ddy(normal)))));
                o.Occlusion =  clamp(curvatureOcclusion, 0.0, _Occlusion);
                o.Alpha = color.a;
            }
            ENDCG
        }
    Fallback "Diffuse"
    CustomEditor "Mirage.Impostors.Elements.MirageLitShaderGUI"
}