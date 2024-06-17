Shader "Hidden/Universal Render Pipeline/JPG"
{
    HLSLINCLUDE

    #define HLSL 1
    #pragma target 3.0
    #pragma editor_sync_compilation

    #pragma multi_compile_local BLOCK_SIZE_4 BLOCK_SIZE_8 BLOCK_SIZE_16
    #pragma multi_compile_local _ COLOR_CRUNCH_SKYBOX
    #pragma multi_compile_local _ REPROJECTION
    #pragma multi_compile_local _ VIZ_MOTION_VECTORS
    
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
    TEXTURE2D_X(_Input);
    TEXTURE2D_X(_PrevScreen);
    TEXTURE2D_X(_CameraDepthTexture);
    TEXTURE2D_X(_MotionVectorTexture);
    SAMPLER(sampler_LinearClamp);
    SAMPLER(sampler_PointClamp);

    CBUFFER_START(FrequentlyUpdatedUniforms)
    float4 _Screen_TexelSize;
    float4 _Downscaled_TexelSize;

    float _ColorCrunch;
    float _Sharpening;
    
    float _ReprojectPercent;
    float _ReprojectSpeed;
    float _ReprojectLengthInfluence;
    CBUFFER_END

    struct Attributes
    {
        float3 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };
    
    Varyings Vert_Default(Attributes input)
    {
        Varyings o;

        UNITY_SETUP_INSTANCE_ID(input);
        o = (Varyings)0;
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = float4(input.vertex.xy, 0.0, 1.0);
        o.uv = (input.vertex.xy + 1.0) * 0.5;

        #if UNITY_UV_STARTS_AT_TOP
        o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif
        
        return o;
    }
    
    //URP specific functions:
    
    //

    ENDHLSL

    SubShader 
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "12.0.0"
        }
        
        ZWrite Off ZTest Always Cull Off
        
        Pass // 0
        {
            Name "Downscale"
            HLSLPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment Downscale_Frag
            #include "../../../Shaders/Shared.cginc"
            ENDHLSL
        }

        Pass // 1
        {
            Name "Encode"
            HLSLPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment Encode_Frag
            #include "../../../Shaders/Shared.cginc"
            ENDHLSL
        }
        
        Pass // 2
        {
            Name "Decode"
            HLSLPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment Decode_Frag
            #include "../../../Shaders/Shared.cginc"
            ENDHLSL
        }
        
        Pass // 3
        {
            Name "Upscale Pull"
            HLSLPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment Upscale_Pull_Frag
            #include "../../../Shaders/Shared.cginc"
            ENDHLSL
        }
        
        Pass // 4
        {
            //Stencil: only render if stencil buffer on this pixel is Equal to 32
            Stencil
            {
                Comp Equal
                Ref 32
            }
            
            Name "Upscale Pull Stenciled"
            HLSLPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment Upscale_Pull_Frag
            #include "../../../Shaders/Shared.cginc"
            ENDHLSL
        }
        
        Pass // 5
        {
            Name "Copy To Prev"
            HLSLPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment CopyToPrev_Frag
            #include "../../../Shaders/Shared.cginc"
            ENDHLSL
        }

    }

    Fallback Off
}