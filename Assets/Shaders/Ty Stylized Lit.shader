Shader "Custom/Ty Stylized Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [HDR][MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        
        _SelfShadingSize ("Self Shading Size", Range(0, 1.0)) = 0.5
        [HDR] _ShadowColor("Shadow Color", Color) = (0.5,0.5,0.5,1)
        
        [Toggle(DR_SPECULAR_ON)] _SpecularEnabled("Enable Specular", Int) = 0
        [HDR] _SpecularColor("[DR_SPECULAR_ON]Specular Color", Color) = (0.9,0.9,0.9,1)
        _SpecularSize("[DR_SPECULAR_ON]Specular Size", Range(0.0, 1.0)) = 0.1
        _SpecularSmoothness("[DR_SPECULAR_ON]Specular Smoothness", Range(0.0, 1.0)) = 0.5
        
        [Toggle(DR_RIM_ON)] _RimEnabled("Enable Rim", Int) = 0
        [HDR] _RimColor("[DR_RIM_ON]Rim Color", Color) = (1,1,1,1)
        _RimSize("[DR_RIM_ON]Rim Size", Range(0, 1)) = 0.5
        _RimSmoothness("[DR_RIM_ON]Rim Smoothness", Range(0, 1)) = 0.5
        
        [Toggle(DR_GRADIENT_ON)] _GradientEnabled("Enable Height Gradient", Int) = 0
        [HDR] _ColorGradient("[DR_GRADIENT_ON]Gradient Color", Color) = (0.85023, 0.85034, 0.85045, 0.85056)
        _GradientCenterX("[DR_GRADIENT_ON]Center X", Float) = 0
        _GradientCenterY("[DR_GRADIENT_ON]Center Y", Float) = 0
        _GradientSize("[DR_GRADIENT_ON]Size", Float) = 10.0
        _GradientAngle("[DR_GRADIENT_ON]Gradient Angle", Range(0, 360)) = 0

        [Toggle(DR_VERTEX_COLORS_ON)] _VertexColorsEnabled("Enable Vertex Colors", Int) = 0

        _DetailMap("Detail Map", 2D) = "white" {}
        _DetailMapColor("Detail Color", Color) = (1,1,1,1)
        [KeywordEnum(Multiply, Add, Interpolate)] _DetailMapBlendingMode("Detail Blending Mode", Float) = 0
        _DetailMapImpact("Detail Impact", Range(0, 1)) = 0.0

        [Toggle(DR_OUTLINE_ON)] _OutlineEnabled("Enable Outline", Int) = 0
        _OutlineColor("[DR_OUTLINE_ON]Color", Color) = (0,0,0,1)
        _OutlineWidth("[DR_OUTLINE_ON]Width", Range(0, 5)) = 1.0
        _OutlineScale("[DR_OUTLINE_ON]Scale", Float) = 1.0
        _OutlineDepthOffset("[DR_OUTLINE_ON]Depth Offset", Range(0, 1)) = 0.0
        _CameraDistanceImpact("[DR_OUTLINE_ON]Camera Distance Impact", Range(0, 1)) = 0.5

        [Space(10)]
        _LightContribution("Light Color Impact", Range(0, 1)) = 1
        [Toggle(DR_LIGHT_ATTENUATION)] _OverrideLightAttenuation("Override Light Attenuation", Int) = 0
        [Vector]_LightAttenuation("[DR_LIGHT_ATTENUATION]Attenuation Remap", Vector) = (0, 1, 0, 0)
        _ShadowColor("[DR_LIGHT_ATTENUATION]Shadow Color", Color) = (0, 0, 0, 1)

        [Toggle(DR_BAKED_GI)] _OverrideBakedGi("Override Baked GI", Int) = 0
        [Gradient]_BakedGIRamp("[DR_BAKED_GI]Baked Light Lookup", 2D) = "white" {}

        [Toggle(DR_ENABLE_LIGHTMAP_DIR)] _OverrideLightmapDir("Override Light Direction", Int) = 0
        _LightmapDirectionPitch("[DR_ENABLE_LIGHTMAP_DIR]Pitch", Range(0, 360)) = 0
        _LightmapDirectionYaw("[DR_ENABLE_LIGHTMAP_DIR]Yaw", Range(0, 360)) = 0

        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionPower("Emission Power", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local DR_SPECULAR_ON
            #pragma shader_feature_local DR_RIM_ON
            #pragma shader_feature_local DR_GRADIENT_ON
            #pragma shader_feature_local DR_VERTEX_COLORS_ON
            #pragma shader_feature_local DR_LIGHT_ATTENUATION
            #pragma shader_feature_local DR_BAKED_GI
            #pragma shader_feature_local DR_ENABLE_LIGHTMAP_DIR
            #pragma shader_feature_local _DETAILMAPBLENDINGMODE_MULTIPLY _DETAILMAPBLENDINGMODE_ADD _DETAILMAPBLENDINGMODE_INTERPOLATE
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float2 lightmapUV : TEXCOORD4;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailMap);
            SAMPLER(sampler_DetailMap);
            TEXTURE2D(_BakedGIRamp);
            SAMPLER(sampler_BakedGIRamp);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DetailMap_ST;
                half4 _BaseColor;
                half _SelfShadingSize;
                half4 _SpecularColor;
                half _SpecularSize;
                half _SpecularSmoothness;
                half4 _RimColor;
                half _RimSize;
                half _RimSmoothness;
                half _SpecHighlights;
                half4 _SpecColor;
                half _Surface;
                half _Blend;
                half _Cull;
                half4 _ColorGradient;
                half _GradientSize;
                half _GradientAngle;
                float _GradientCenterX;
                float _GradientCenterY;
                half4 _DetailMapColor;
                half _DetailMapImpact;
                half _LightContribution;
                half2 _LightAttenuation;
                half4 _ShadowColor;
                half _LightmapDirectionPitch;
                half _LightmapDirectionYaw;
                half4 _EmissionColor;
                half _EmissionPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.lightmapUV = input.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Early out if fully transparent
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = baseMap * _BaseColor;
                
                #if _ALPHATEST_ON
                    clip(color.a - _Cutoff);
                #endif
                
                if (color.a < 0.001)
                    discard;

                // Simplified lighting calculation
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                
                // Simplified NdotL calculation
                float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                
                // Simplified self-shadowing
                half selfShadowing = smoothstep(0, _SelfShadingSize, NdotL);
                
                // Simplified shadow handling
                #if defined(DR_LIGHT_ATTENUATION)
                    half shadowAttenuation = lerp(_LightAttenuation.x, _LightAttenuation.y, mainLight.shadowAttenuation);
                    color.rgb = lerp(_ShadowColor.rgb, color.rgb, shadowAttenuation);
                #endif

                // Simplified light contribution
                color.rgb *= lerp(1, mainLight.color, _LightContribution);
                color.rgb *= lerp(_ShadowColor.rgb, 1, selfShadowing);
                
                #if defined(DR_SPECULAR_ON)
                    float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                    float3 halfVector = normalize(mainLight.direction + viewDirWS);
                    float NdotH = dot(normalWS, halfVector);
                    half specular = pow(saturate(NdotH), _SpecularSmoothness * 100);
                    specular = step(_SpecularSize, specular);
                    color.rgb += _SpecularColor.rgb * specular;
                #endif
                
                #if defined(DR_RIM_ON)
                    float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                    float rimDot = 1 - dot(viewDir, normalWS);
                    half rim = pow(rimDot, _RimSmoothness * 3);
                    rim = smoothstep(_RimSize, _RimSize + 0.1, rim);
                    color.rgb += _RimColor.rgb * rim;
                #endif

                #if defined(DR_GRADIENT_ON)
                    float2 gradientUV = input.positionWS.xy - float2(_GradientCenterX, _GradientCenterY);
                    gradientUV = float2(
                        gradientUV.x * cos(_GradientAngle) - gradientUV.y * sin(_GradientAngle),
                        gradientUV.x * sin(_GradientAngle) + gradientUV.y * cos(_GradientAngle)
                    );
                    float gradientFactor = (gradientUV.y + _GradientSize) / (_GradientSize * 2);
                    color.rgb = lerp(color.rgb, _ColorGradient.rgb, saturate(gradientFactor));
                #endif

                #if defined(DR_BAKED_GI)
                    #if defined(LIGHTMAP_ON)
                        half3 bakedGI = SampleLightmap(input.lightmapUV, normalWS);
                        half2 bakedGICoords = half2(Luminance(bakedGI), 0);
                        bakedGI = SAMPLE_TEXTURE2D(_BakedGIRamp, sampler_BakedGIRamp, bakedGICoords).rgb;
                        color.rgb *= bakedGI;
                    #endif
                #endif

                color.rgb += _EmissionColor.rgb * _EmissionPower;
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // Outline pass
        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Front

            HLSLPROGRAM
            #pragma vertex VertexProgram
            #pragma fragment FragmentProgram
            #pragma multi_compile _ DR_OUTLINE_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct VertexInput
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float3 normal : NORMAL;
                float fogCoord : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineScale;
                float _OutlineDepthOffset;
                float _CameraDistanceImpact;
                half4 _EmissionColor;
                half _EmissionPower;
            CBUFFER_END

            float4 ObjectToClipPos(float4 pos)
            {
                return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(pos.xyz, 1)));
            }

            VertexOutput VertexProgram(VertexInput v)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if defined(DR_OUTLINE_ON)
                    float4 clipPosition = ObjectToClipPos(v.position * _OutlineScale);
                    float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)UNITY_MATRIX_M, v.normal));
                    
                    half outlineWidth = _OutlineWidth;
                    half cameraDistanceImpact = lerp(clipPosition.w, 4.0, _CameraDistanceImpact);
                    float2 aspectRatio = float2(_ScreenParams.x / _ScreenParams.y, 1);
                    float2 offset = normalize(clipNormal.xy) / aspectRatio * outlineWidth * cameraDistanceImpact * 0.005;
                    
                    clipPosition.xy += offset;
                    
                    #if UNITY_REVERSED_Z
                        clipPosition.z -= _OutlineDepthOffset * 0.1;
                    #else
                        clipPosition.z += _OutlineDepthOffset * 0.1 * (1.0 - UNITY_NEAR_CLIP_VALUE);
                    #endif

                    o.position = clipPosition;
                    o.normal = clipNormal;
                    o.fogCoord = ComputeFogFactor(clipPosition.z);
                #else
                    o.position = 0;
                    o.normal = 0;
                    o.fogCoord = 0;
                #endif

                return o;
            }

            half4 FragmentProgram(VertexOutput i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 color = _OutlineColor;
                color.rgb = MixFog(color.rgb, i.fogCoord);
                color.rgb += _EmissionColor.rgb * _EmissionPower;
                return color;
            }
            ENDHLSL
        }

        // ShadowCaster pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    CustomEditor "TyStylizedLitGUI"
} 