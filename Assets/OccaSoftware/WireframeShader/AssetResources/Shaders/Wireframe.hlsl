#ifndef OS_WIREFRAME_INCLUDE
#define OS_WIREFRAME_INCLUDE



///////////////////////////////////////////////////////////////////////////////
//                      Includes                                             //
///////////////////////////////////////////////////////////////////////////////

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
// See ShaderVariablesFunctions.hlsl in com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl



///////////////////////////////////////////////////////////////////////////////
//                      Properties                                           //
///////////////////////////////////////////////////////////////////////////////

CBUFFER_START(UnityPerMaterial)
    // COLOR
    float3 _Color;
    float3 _Emission;
    float _Opacity;
    
    float _WireframeSize;
    // RENDER SETTINGS
    float _FadeStartDistance;
    float _MaximumDistance;
    int _PreferQuadsEnabled;
    
    // LIGHTING SETTINGS
    int _ReceiveShadowsEnabled;
    int _ReceiveAmbientLightingEnabled;
    int _ReceiveFogEnabled;
    int _ReceiveDirectLightingEnabled;
    int _LightingMode;
CBUFFER_END



///////////////////////////////////////////////////////////////////////////////
//                      Helper Functions                                     //
///////////////////////////////////////////////////////////////////////////////

// Math
float InverseLerp(float a, float b, float v)
{
	return (v - a) / (b - a);
}

float RemapUnclamped(float iMin, float iMax, float oMin, float oMax, float v)
{
	float t = InverseLerp(iMin, iMax, v);
	return lerp(oMin, oMax, t);
}

float Remap(float iMin, float iMax, float oMin, float oMax, float v)
{
	v = clamp(v, iMin, iMax);
	return RemapUnclamped(iMin, iMax, oMin, oMax, v);
}

float CheapSqrt(float a)
{
    return 1.0 - ((1.0 - a) * (1.0 - a));
}


// Structs
struct OSLightData
{
    float3 direction;
    float3 color;
    float attenuation;
};


struct MaterialData{
    float3 positionWS;
    float3 normalWS;
    float4 positionHCS;
};


// Transforms
float3 _LightDirection;
float4 GetClipSpacePosition(float3 positionWS, float3 normalWS)
{
    #if CAST_SHADOWS_PASS
        float4 positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
        
        #if UNITY_REVERSED_Z
            positionHCS.z = min(positionHCS.z, positionHCS.w * UNITY_NEAR_CLIP_VALUE);
        #else
            positionHCS.z = max(positionHCS.z, positionHCS.w * UNITY_NEAR_CLIP_VALUE);
        #endif
        
        return positionHCS;
    #endif
    
    return TransformWorldToHClip(positionWS);
}


float4 GetShadowCoord(float3 positionWS, float4 positionHCS)
{
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        return ComputeScreenPos(positionHCS);
    #else
        return TransformWorldToShadowCoord(positionWS);
    #endif
}

float4 GetMainLightShadowCoord(float3 PositionWS)
{
    #ifdef SHADOWS_SCREEN
        float4 clipPos = TransformWorldToHClip(PositionWS);
        return ComputeScreenPos(clipPos);
    #else
		return TransformWorldToShadowCoord(PositionWS);
    #endif
}


void GetMainLightData(float3 PositionWS, out OSLightData light)
{
    float4 shadowCoord = GetMainLightShadowCoord(PositionWS);
    
    Light l = GetMainLight(shadowCoord);
    light.color = float3(l.color);
    light.attenuation = float(l.shadowAttenuation);
    light.direction = float3(l.direction);
}

void GetAdditionalLightData(float3 PositionWS, float3 NormalWS, out float3 Color)
{
    int count = GetAdditionalLightsCount();
	for (int i = 0; i < count; i++)
    {
		Light light = GetAdditionalLight(i, PositionWS);
        float3 attenuatedLightColor = light.color * light.distanceAttenuation;
		Color += attenuatedLightColor * saturate(dot(light.direction, NormalWS));
	}
}

float CookTorranceGSF(float NoH, float NoV, float NoL, float VoH)
{
    float gsf = min(1.0, min(2.0 * NoH * NoV / VoH, 2.0 * NoH * NoL / VoH));
    return gsf;
}

float ImplicitGSF(float NoL, float NoV)
{
    float gsf = NoL*NoV;
	return gsf;
}

float SchlickFresnel(float NoV)
{
    float v = 1.0 - NoV;
    float v5 = v * v * v * v * v;
    return v5;
}


float3 EvaluateColorUnlit(MaterialData materialData)
{
    return _Color;
}

float3 EvaluateColorLit(MaterialData materialData)
{
    OSLightData mainLight;
    
    // Lighting
    float3 additionalLights = float3(0,0,0);
    GetAdditionalLightData(materialData.positionWS, materialData.normalWS, additionalLights);
    
    GetMainLightData(materialData.positionWS, mainLight);
    if(_ReceiveShadowsEnabled == 0)
    {
        
    }
    
    
    float3 color = float3(0.0,0.0,0.0);
    
    float3 viewDir = normalize(GetWorldSpaceViewDir(materialData.positionWS));
    float3 H = normalize(mainLight.direction + viewDir);
    
    float NoV = dot(materialData.normalWS, viewDir);
    float NoL = dot(materialData.normalWS, mainLight.direction);
    float NoH = dot(materialData.normalWS, H);
    float VoH = dot(viewDir, H);
    float VoL = dot(viewDir, mainLight.direction);
    
    // #define PI 3.14159
    // #define Roughness 1.0
    // Assume Roughness is 1.0. When Roughness is 1.0, the GGX NDF is 0.3183 for every value of NoH.
    // Therefore, we can skip the calculation and just use 0.3183 as a constant.
    // ndf = (Roughness * Roughness) / (PI * pow(((NoH * NoH) * ((Roughness * Roughness) - 1.0) + 1.0), 2);
    float ndf = 0.3183;
    float gsf = ImplicitGSF(NoL, NoV);
    float fresnel = SchlickFresnel(NoV);
    
    float specularity = (gsf * fresnel * ndf) / (4.0 * (NoL * NoV));
    float diffuse = ImplicitGSF(NoL, NoV);
    
    float lightingModel = saturate(diffuse + specularity);
    
    lightingModel *= lerp(1.0, mainLight.attenuation, _ReceiveShadowsEnabled);
    
    ///////////////////////////////
    //   CALCULATE COLOR         //
    ///////////////////////////////
    
    // Setup lighting
    
    if(_ReceiveAmbientLightingEnabled == 1)
    {
        float3 ambient = SampleSH(materialData.normalWS);
        color += ambient;
    }
    
    //color += ambient;
    
    if(_ReceiveDirectLightingEnabled == 1)
    {
        color += mainLight.color * lightingModel;
        color += additionalLights;
    }
    float3 albedo = _Color;
    
    // Apply Decals
    #ifdef _DBUFFER
        ApplyDecalToBaseColor(materialData.positionHCS, albedo);
    #endif
    
    
    color *= albedo;
    
    color += _Emission;
    
    // Mix Fog
    if(_ReceiveFogEnabled == 1)
    {
        float fogFactor = InitializeInputDataFog(float4(materialData.positionWS, 1), 0);
        color = MixFog(color, fogFactor);
    }
    
    return color;
}

float3 EvaluateColor(MaterialData materialData)
{
    if(_LightingMode == 0)
    {
        return EvaluateColorUnlit(materialData);
    }
    else
    {
        return EvaluateColorLit(materialData);
    }
}


/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
///                                                                           ///
///                      SHADER BODY                                          ///
///                                                                           ///
/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////
//                      Structs                                              //
///////////////////////////////////////////////////////////////////////////////

struct Attributes
{
    float4 positionOS    : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Geoms
{
    float4 positionHCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 barycentricCoordinates : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};





///////////////////////////////////////////////////////////////////////////////
//                      Vertex                                               //
///////////////////////////////////////////////////////////////////////////////

Varyings Vertex (Attributes IN)
{
	Varyings OUT = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.positionWS = mul(unity_ObjectToWorld, IN.positionOS).xyz;
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
	
    return OUT;
}



///////////////////////////////////////////////////////////////////////////////
//                      Geometry                                             //
///////////////////////////////////////////////////////////////////////////////


[maxvertexcount(3)]
void Geometry(triangle Varyings IN[3], inout TriangleStream<Geoms> triangleStream)
{
    Geoms GEO = (Geoms)0;
    UNITY_SETUP_INSTANCE_ID(IN[0]);
	UNITY_TRANSFER_INSTANCE_ID(IN[0], GEO);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(GEO);
    
    //////////////////////////////////////////////////
    //   Setup Barycentric Coordinates              //
    //////////////////////////////////////////////////
    float3 coords[3] = 
    {
        float3(1,0,0),
        float3(0,1,0),
        float3(0,0,1)
    };
    
    float3 longestChord = float3(0,0,0);
    
    if(_PreferQuadsEnabled == 1)
    {
        float chords[3];
        chords[0] = distance(IN[1].positionWS, IN[2].positionWS);
        chords[1] = distance(IN[2].positionWS, IN[0].positionWS);
        chords[2] = distance(IN[0].positionWS, IN[1].positionWS);
    
        // Check if one of the chords is longest. If it is, oversize it's barycentric coordinate.
        if(chords[0] > chords[1] && chords[0] > chords[2])
        {
            longestChord = float3(1,0,0);
        }
        else if(chords[1] > chords[0] && chords[1] > chords[2])
        {
            longestChord = float3(0,1,0);
        }
        else if(chords[2] > chords[0] && chords[2] > chords[1])
        {
            longestChord = float3(0,0,1);
        }
    }
    
    
    ///////////////////////////////
    //   Setup TriangleStream    //
    ///////////////////////////////
    
    
    GEO.positionWS = IN[0].positionWS;
    GEO.normalWS = IN[0].normalWS;
    GEO.positionHCS = GetClipSpacePosition(GEO.positionWS, GEO.normalWS);
    GEO.barycentricCoordinates = coords[0] + longestChord;
    triangleStream.Append(GEO);
    
    GEO.positionWS = IN[1].positionWS;
    GEO.normalWS = IN[1].normalWS;
    GEO.positionHCS = GetClipSpacePosition(GEO.positionWS, GEO.normalWS);
    GEO.barycentricCoordinates = coords[1] + longestChord;
    triangleStream.Append(GEO);
    
    GEO.positionWS = IN[2].positionWS;
    GEO.normalWS = IN[2].normalWS;
    GEO.positionHCS = GetClipSpacePosition(GEO.positionWS, GEO.normalWS);
    GEO.barycentricCoordinates = coords[2] + longestChord;
    triangleStream.Append(GEO);
}




///////////////////////////////////////////////////////////////////////////////
//                      Fragment                                             //
///////////////////////////////////////////////////////////////////////////////

float GetLine(float3 barycentricCoordinates)
{
    float3 triangleScale = fwidth(barycentricCoordinates);
    
    // The diff between start & stop is equal to the triangleScale (aka fwidth).
    float3 start = (triangleScale * _WireframeSize) - triangleScale; // If _WireframeSize < 1, start = 0.
    float3 stop = triangleScale * _WireframeSize;
    
    float3 edge = smoothstep(start, stop, barycentricCoordinates);
    
    
    float closestDistance = min(edge.x, min(edge.y, edge.z));
    float alpha = 1.0 - closestDistance;
    alpha *= _Opacity;
    clip(alpha - 0.0001);
    return alpha;
}


float4 Fragment (Geoms IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    
    ///////////////////////////////
    //   CLIP LINES              //
    ///////////////////////////////
    
    float alpha = GetLine(IN.barycentricCoordinates);
    
    ///////////////////////////////
    //   CALCULATE COLOR         //
    ///////////////////////////////
    
    MaterialData materialData;
    materialData.positionHCS = IN.positionHCS;
    materialData.normalWS = IN.normalWS;
    materialData.positionWS = IN.positionWS;
    float3 color = EvaluateColor(materialData);
    
    return float4(color, alpha);
}


float3 FragmentDepthNormalsOnly(Geoms IN) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    
    float alpha = GetLine(IN.barycentricCoordinates);
    return normalize(IN.normalWS);
}


float FragmentDepthOnly(Geoms IN) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(IN);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    
    float alpha = GetLine(IN.barycentricCoordinates);
    return 0;
}
#endif