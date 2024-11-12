Shader "GPUInstancerPro/Custom/TY_Fast Ghost"
{
    Properties
    {
        [Toggle(_TRANSPARENT_ON)] _TransparentMode("Transparent Mode", Float) = 1
        _Color("Main Color", Color) = (1,1,1,1)
        [HDR]_FresnelColor("Fresnel Color", Color) = (0.6933962,1,0.9814353,1)
        _SelfIllumination("Self Illumination", Range( 1 , 10)) = 1
        _FresnelIntensity("Fresnel Intensity", Float) = 4
        _FresnelPower("Fresnel Power", Float) = 4
        _FresnelBias("Bias", Range( 0 , 1)) = 0
        [Toggle]_Invert("Invert", Float) = 0
        _MinValueAmplitude("Min Value", Float) = 1
        _MaxValueAmplitude("Max Value", Float) = 2
        _AmplitudeSpeed("Speed", Float) = 1
        _Opacity("Opacity", Range( 0 , 1)) = 1
    }
    SubShader
    {
        Tags 
        {
            "RenderPipeline"="UniversalPipeline"
            "DisableBatching"="False"
        }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        #pragma target 3.5
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags 
            {
                "LightMode" = "UniversalForward"
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _TRANSPARENT_ON
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fresnel : TEXCOORD0;
                half amplitude : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _FresnelColor;
                half _SelfIllumination;
                half _FresnelIntensity;
                half _FresnelPower;
                half _FresnelBias;
                half _Invert;
                half _MinValueAmplitude;
                half _MaxValueAmplitude;
                half _AmplitudeSpeed;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(half, _Opacity)
                UNITY_DEFINE_INSTANCED_PROP(float, _TimeOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                half NdotV = dot(normalize(normalWS), normalize(viewDirWS));
                half fresnelFactor = _Invert ? NdotV : (1.0 - NdotV);
                output.fresnel = _FresnelBias + _FresnelIntensity * pow(abs(fresnelFactor), _FresnelPower);
                
                float timeOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset);
                half instanceTime = _Time.y + timeOffset;
                half sinVal = sin(instanceTime * _AmplitudeSpeed);
                output.amplitude = lerp(_MinValueAmplitude, _MaxValueAmplitude, sinVal * 0.5 + 0.5);
                
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 fresnelColor = _FresnelColor.rgb * input.fresnel * _SelfIllumination;
                
                half4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                half instanceOpacity = UNITY_ACCESS_INSTANCED_PROP(Props, _Opacity);
                
                half3 finalColor = fresnelColor * input.amplitude * instanceColor.rgb;
                finalColor = MixFog(finalColor, input.fogFactor);
                
                half finalAlpha;
                #ifdef _TRANSPARENT_ON
                    finalAlpha = instanceOpacity * instanceColor.a;
                #else
                    finalAlpha = 1.0;
                #endif
                
                return half4(finalColor, finalAlpha);
            }
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing

ENDHLSL
        }
    }
}