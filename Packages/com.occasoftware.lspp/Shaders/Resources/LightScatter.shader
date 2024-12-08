Shader "OccaSoftware/LSPP/LightScatter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "LightScatterPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "LightScattering.hlsl"
            
            TEXTURE2D_X(_Occluders_LSPP);
            float _Density;
            bool _DoSoften;
            bool _DoAnimate;
            float _MaxRayDistance;
            int lspp_NumSamples;
            float3 _Tint;
            bool _LightOnScreenRequired;
            float _FalloffIntensity;
            
            float3 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return EstimateLightScattering(_Occluders_LSPP, input.texcoord, _Density, _DoSoften, _DoAnimate, _MaxRayDistance, lspp_NumSamples, _Tint, _LightOnScreenRequired, _FalloffIntensity);
            }
            ENDHLSL
        }
    }
}