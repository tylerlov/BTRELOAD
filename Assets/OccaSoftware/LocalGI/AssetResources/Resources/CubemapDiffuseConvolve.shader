Shader "Hidden/CubemapDiffuseConvolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "DiffuseConvolve"
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TextureCube _EnvironmentMap;
            SamplerState my_linear_clamp_sampler;
            float _Exposure;
            
            float4      CustomRenderTextureParameters;
            #define     CustomRenderTexture3DTexcoordW  CustomRenderTextureParameters.y
            float4      _CustomRenderTextureInfo;
            #define _CustomRenderTextureCubeFace    _CustomRenderTextureInfo.w

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                float3 direction : TEXCOORD1;
            };
            
            
            float3 CustomRenderTextureComputeCubeDirection(float2 globalTexcoord)
            {
                float2 xy = globalTexcoord * 2.0 - 1.0;
                
                float3 direction;
                
                if(_CustomRenderTextureCubeFace == 0.0)
                {
                    direction = normalize(float3(1.0, -xy.y, -xy.x));
                }
                else if(_CustomRenderTextureCubeFace == 1.0)
                {
                    direction = normalize(float3(-1.0, -xy.y, xy.x));
                }
                else if(_CustomRenderTextureCubeFace == 2.0)
                {
                    direction = normalize(float3(xy.x, 1.0, xy.y));
                }
                else if(_CustomRenderTextureCubeFace == 3.0)
                {
                    direction = normalize(float3(xy.x, -1.0, -xy.y));
                }
                else if(_CustomRenderTextureCubeFace == 4.0)
                {
                    direction = normalize(float3(xy.x, -xy.y, 1.0));
                }
                else if(_CustomRenderTextureCubeFace == 5.0)
                {
                    direction = normalize(float3(-xy.x, -xy.y, -1.0));
                }

                return direction;
            }

            Varyings Vertex (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.texcoord = float3(IN.texcoord.xy, CustomRenderTexture3DTexcoordW);
                OUT.direction = CustomRenderTextureComputeCubeDirection(IN.texcoord.xy);
                return OUT;
            }
            #ifndef PI
            #define PI 3.14159265359
            #endif
            float3 DiffuseConvolution(float3 direction)
            {
                float3 irradiance = 0;
                
                float3 up    = float3(0.0, 1.0, 0.0);
                float3 right = normalize(cross(up, direction));
                up           = normalize(cross(direction, right));

                
                float sampleDelta = 0.1;
                float nrSamples   = 0.0;
                
                
                for(float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
                {
                    const float sinPhi = sin(phi);
                    const float cosPhi = cos(phi);
                    
                    for(float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
                    {
                        // spherical to cartesian (in tangent space)
                        const float sinTheta = sin(theta);
                        const float cosTheta = cos(theta);
                        
                        float3 tangentSample = float3(sinTheta * cosPhi,  sinTheta * sinPhi, cosTheta);
                        
                        // tangent space to world
                        float3 sampleVec = tangentSample.x * right + tangentSample.y * up + tangentSample.z * direction; 

                        irradiance += _EnvironmentMap.SampleLevel(my_linear_clamp_sampler, sampleVec, 0).rgb * cosTheta * sinTheta;
                        nrSamples++;
                    }
                }
                irradiance = PI * irradiance * rcp(nrSamples);
                return irradiance;
            }

            float3 Fragment (Varyings IN) : SV_Target
            {
                return DiffuseConvolution(IN.direction) * _Exposure;            
            }
            ENDHLSL
        }
    }
}
