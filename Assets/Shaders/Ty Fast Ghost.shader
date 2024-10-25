Shader "Custom/TY_Fast Ghost"
{
    Properties
    {
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
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
            "DisableBatching"="False"
        }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        #pragma target 3.5
        #pragma multi_compile_instancing
        #pragma instancing_options forcemaxcount:50
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags {"LightMode" = "UniversalForward"}

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float instanceTime : TEXCOORD3;
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
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                float timeOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeOffset);
                output.instanceTime = _Time.y + timeOffset;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half NdotV = dot(normalize(input.normalWS), normalize(input.viewDirWS));
                half fresnelFactor = _Invert ? NdotV : (1.0 - NdotV);
                half fresnel = _FresnelBias + _FresnelIntensity * pow(abs(fresnelFactor), _FresnelPower);
                half3 fresnelColor = _FresnelColor.rgb * fresnel * _SelfIllumination;

                half amplitude = lerp(_MinValueAmplitude, _MaxValueAmplitude, 
                    (sin(input.instanceTime * _AmplitudeSpeed) + 1) * 0.5);
                
                half4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                half instanceOpacity = UNITY_ACCESS_INSTANCED_PROP(Props, _Opacity);
                
                half3 finalColor = fresnelColor * amplitude * instanceColor.rgb;
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, instanceOpacity * instanceColor.a);
            }
            ENDHLSL
        }
    }
}
