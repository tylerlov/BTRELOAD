Shader "Hidden/Impostor/Blit"
{
    Properties
    {
        [MainTexture] _MainTex("Input", 2D) = "white" {}
        /*_ColorWriteMask("ColorMask", Int) = 15
        _ColorMixR("ColorMixR", Vector) = (1,0,0,0)
        _ColorMixG("ColorMixG", Vector) = (0,1,0,0)
        _ColorMixB("ColorMixB", Vector) = (0,0,1,0)
        _ColorMixA("ColorMixA", Vector) = (0,0,0,1)*/
    }
    SubShader
    {
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma editor_sync_compilation

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SamplerState sampler_PointClamp;
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetQuadVertexPosition(input.vertexID);
                output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
                output.texcoord = GetQuadTexCoord(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_TARGET
            {
                float4 inputColor = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, input.texcoord.xy);
                float4 outputColor = inputColor;
                return outputColor;
            }

            ENDHLSL
        }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off ColorMask[_ColorWriteMask]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma editor_sync_compilation

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SamplerState sampler_PointClamp;

            float4 _ColorMixR;
            float4 _ColorMixG;
            float4 _ColorMixB;
            float4 _ColorMixA;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetQuadVertexPosition(input.vertexID);
                output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
                output.texcoord = GetQuadTexCoord(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_TARGET
            {
                float4 inputColor = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, input.texcoord.xy);
                float4 outputColor = inputColor;
                outputColor.r = dot(inputColor, _ColorMixR);
                outputColor.g = dot(inputColor, _ColorMixG);
                outputColor.b = dot(inputColor, _ColorMixB);
                outputColor.a = dot(inputColor, _ColorMixA);
                return outputColor;
            }

            ENDHLSL
        }
    }

    Fallback Off
}
