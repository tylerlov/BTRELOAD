Shader "Hidden/Impostor/ConvertDepth"
{
	Properties
	{
		[MainTexture] _MainTex("Input", 2D) = "white" {}
		_ColorWriteMask("ColorMask", Int) = 1
	}
    SubShader
    {
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ NORMALIZE_DEPTH_ON
            #pragma editor_sync_compilation
            //#pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SamplerState sampler_PointClamp;
            float _ImpostorRadius;

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
                float depth = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, input.texcoord.xy).r;
#if NORMALIZE_DEPTH_ON
                depth = 1 - (depth / _ImpostorRadius);
#endif
                return (depth < 0.001 ? 0.5 : depth).xxxx;
            }

            ENDHLSL
        }
    }

    Fallback Off
}
