#ifndef INCLUDE_RM_INPUT
#define INCLUDE_RM_INPUT

// Raymarcher Camera

uniform float4x4 RaymarcherCamWorldMatrix;

// Raymarcher Shared Properties

#ifndef RAYMARCHER_TYPE_QUALITY
	uniform half RaymarcherGlobalHueSpectrumOffset;
	uniform half RaymarcherGlobalHueSaturation;
#endif
#ifndef RAYMARCHER_PIPELINE_HDRP
	uniform sampler2D RaymarcherGrabSceneColor;
	uniform sampler2D _CameraDepthTexture;
#endif

// Raymarcher Essential Rendering Data

uniform half RaymarcherMaxRenderDistance;
uniform float RaymarcherRenderQuality;

uniform half RaymarcherSceneDepthSmoothness;

uniform half RaymarcherGlobalSdfObjectSmoothness;
uniform half RaymarcherSceneGeometrySmoothness;

uniform half4 RaymarcherRendererColorTint;
uniform half RaymarcherRendererExposure;

uniform half RaymarcherDistanceFogDistance;
uniform half RaymarcherDistanceFogSmoothness;
uniform half4 RaymarcherDistanceFogColor;

uniform half RaymarcherPixelationSize;

#ifdef RAYMARCHER_TYPE_QUALITY
	uniform Texture2DArray RaymarcherSamplerPackTextures;
	uniform SamplerState sampler_RaymarcherSamplerPackTextures;
#endif

// Raymarcher Global Lighting Data

uniform half3 RaymarcherDirectionalLightDir;
uniform half4 RaymarcherDirectionalLightColor; // rgb = color, a = intensity
#if defined(RAYMARCHER_ADDITIONAL_LIGHTS) && (RAYMARCHER_LIGHT_COUNT)
	uniform half4 RaymarcherAddLightsData[RAYMARCHER_LIGHT_COUNT];
	// half4 l0 = RaymarcherAddLightsData[i];		// xyz = light position, w = light intensity
	// half4 l1 = RaymarcherAddLightsData[i + 1];	// rgb = light color, w = light range
	// half4 l2 = RaymarcherAddLightsData[i + 2];	// x = light shadow intensity, y = light shadow attenuation offset
#endif

// Raymarcher Data Containers

struct Attributes
{
	float4 vertexOS : POSITION;	// Vertex local position (Object Space)
	half2 uv : TEXCOORD0;		// Uv 0-1
};

struct Ray
{
	float3 o;			// Ray origin
	float3 d;			// Ray direction
	half3 nd;			// Ray normalized direction
	float3 p;			// Ray hit position / ray relative position
	float l;			// Ray length
	half sceneDepth;	// Scene depth (Geometry)
	half2 uv;			// Computed screen uv
};

struct Varyings
{
	float4 vertexCS : SV_POSITION;	// Vertex clip position (Clip Space)
	Ray ray : TEXCOORD0;			// Ray data container
};

#endif