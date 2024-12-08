Shader "OccaSoftware/LSPP/Merge"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "MergePass"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"

            #include "CrossUpsampling.hlsl"
            
            TEXTURE2D_X(_Source);
            TEXTURE2D_X(_Scattering_LSPP);

            
            float3 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 screenColor = SAMPLE_TEXTURE2D_X_LOD(_Source, linear_clamp_sampler, input.texcoord, 0).rgb;
                float3 upscaleResults = CrossSample(_Scattering_LSPP, input.texcoord, _ScreenParams.xy * 0.5, 2.0);
                return screenColor + upscaleResults;
            }
            ENDHLSL
        }
    }
}