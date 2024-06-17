#ifndef INCLUDE_RM_COMMON
#pragma exclude_renderers gles
#define INCLUDE_RM_COMMON

// Common constants

#define PI2x 6.28318
#define EPSILON 1.0e-5
#define EPSILONUP 1.0e-2
#define EPSILONUPUP 2.0e-2
#define EPSILONUPUPTWO 2.0e-1
#define EPSILONZEROFIVE 0.5

// Raymarcher built-in defines

#define RM_SAMPLE_TEXTURE2D(name, uv) tex2Dlod(name, half4(uv, 0, 0))

#define RM_SAMPLE_TEXTURE3D(name, uv) tex3Dlod(name, half4(uv, 0))

#define RM_SAMPLE_ARRAY_SPECIFIC(texName, texSamplerName, uv, index) texName.SampleLevel(texSamplerName, half3(uv, index), 0)

#define RM_SAMPLE_ARRAY(name, uv, index) RM_SAMPLE_ARRAY_SPECIFIC(name##Textures, sampler_##name##Textures, uv, index)

#ifdef RAYMARCHER_TYPE_QUALITY
	half3 RM_SAMPLE_TEX(in float3 p, in half4 textureData, in half4 textureScale, in half4 color)
	{
		half3 result = color.rgb;
		half3 normal = normalize(frac(abs(p) * textureScale.xyz));

		half3 texXY = RM_SAMPLE_ARRAY(RaymarcherSamplerPack, p.xy * textureData.y, textureData.x).rgb;
		half3 texXZ = RM_SAMPLE_ARRAY(RaymarcherSamplerPack, p.xz * textureData.y, textureData.x).rgb;
		half3 texYZ = RM_SAMPLE_ARRAY(RaymarcherSamplerPack, p.yz * textureData.y, textureData.x).rgb;

		half3 absNormal = abs(normal);
		absNormal *= pow(absNormal, max(EPSILONUP, 10.0));
		absNormal /= absNormal.x + absNormal.y + absNormal.z;
		half3 tex = texXY * absNormal.z + texXZ * absNormal.y + texYZ * absNormal.x;

		return lerp(result, tex, textureData.z * step(0, textureData.x));
	}
#endif

#ifndef RAYMARCHER_TYPE_PERFORMANT
	#define RM_TRANS(p,model) mul((float3x4)model, float4(p, 1)).xyz
#else
	#define RM_TRANS(p,model) p - model
#endif

// Hashes

#define HASH1(p) frac(sin(p * 1024.64) * 768.32)

// Pixel filters

half3 ApplyHue(in half3 aColor, in half aHue)
{
	static const half3 k = 0.577;
#ifndef RAYMARCHER_TYPE_QUALITY
	half angle = radians(aHue + RaymarcherGlobalHueSpectrumOffset);
#else
	half angle = radians(aHue);
#endif
	half cosAngle = cos(angle);
	return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
}

// Raymarcher built-in Scene & Fog calculations

half3 CalculateSceneColor(float2 screenUV)
{
#ifndef RAYMARCHER_PIPELINE_HDRP
	return tex2D(RaymarcherGrabSceneColor, screenUV).rgb;
#else
	return SampleCameraColor(screenUV.xy);
#endif
}

float CalculateSceneDepth(Varyings i)
{
#ifdef RAYMARCHER_SCENE_DEPTH
	#ifndef RAYMARCHER_PIPELINE_HDRP
		float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.ray.uv).r);
	#else
		float depth = LinearEyeDepth(LoadCameraDepth(i.vertexCS.xy), _ZBufferParams);
	#endif
	depth -= _ProjectionParams.y;
	depth *= length(i.ray.d);
	return depth;
#else
	return 0;
#endif
}

half3 CalculateDistanceFog(in half3 entryColor, in float3 start, in float3 end, in half dist, in half smh)
{
	return lerp(entryColor, RaymarcherDistanceFogColor.rgb, saturate(smoothstep(dist - smh, dist + smh, distance(start, end))) * RaymarcherDistanceFogColor.a);
}

// Raymarcher built-in smooth union calculations

float SmoothUnion1(float a, float b, half smoothness)
{
// Credits for the original smooth union OP formula: Inigo Quilez
// https://iquilezles.org/articles/distfunctions/
	float h = clamp(0.5 + 0.5 * (b - a) / max(EPSILON, smoothness), 0.0, 1.0);
	return lerp(b, a, h) - smoothness * h * (1.0 - h);
}

#ifdef RAYMARCHER_TYPE_QUALITY
	float2x4 GroupSmoothUnion(float2x4 a, float2x4 b, half smoothness)
	{
// Credits for the original smooth union OP formula: Inigo Quilez
// https://iquilezles.org/articles/distfunctions/
		float h = clamp(0.5 + 0.5 * (b[0].x - a[0].x) / max(EPSILON, smoothness), 0.0, 1.0);
		return float2x4(
			float4(lerp(b[0].x, a[0].x, h) - smoothness * h * (1.0 - h), lerp(b[0].yzw, a[0].yzw, h)), // sdf, color rgb
			float4(lerp(b[1].x, a[1].x, h), lerp(b[1].y, a[1].y, step(a[0].x, b[0].x)), 0, 0)); // material type, material instance, - , -
	}
#elif defined(RAYMARCHER_TYPE_STANDARD)
	float4 GroupSmoothUnion(float4 a, float4 b, half smoothness)
	{
// Credits for the original smooth union OP formula: Inigo Quilez
// https://iquilezles.org/articles/distfunctions/
		float h = clamp(0.5 + 0.5 * (b.x - a.x) / max(EPSILON, smoothness), 0.0, 1.0);
		return float4(lerp(b.x, a.x, h) - smoothness * h * (1.0 - h), lerp(b.yz, a.yz, h), lerp(b.a, a.a, step(a.x, b.x))); // sdf, hue, material type, material instance
	}
#else // PERFORMANT
	half2 GroupSmoothUnion(half2 a, half2 b, half smoothness)
	{
// Credits for the original smooth union OP formula: Inigo Quilez
// https://iquilezles.org/articles/distfunctions/
		half h = clamp(0.5 + 0.5 * (b.x - a.x) / max(EPSILON, smoothness), 0.0, 1.0);
		return half2(lerp(b.x, a.x, h) - smoothness * h * (1.0 - h), lerp(b.y, a.y, h)); // sdf, hue
	}
#endif

#endif