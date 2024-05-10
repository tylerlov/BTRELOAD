#ifndef PSE_INCLUDED
#define PSE_INCLUDED

float map(float value, float fromIn, float toIn, float fromOut, float toOut)
{
	return (value - fromIn) / (toIn - fromIn) * (toOut - fromOut) + fromOut;
}

float InvPow(float x, float y)
{
	return pow(1.0 - x, y);
}

float Soften(float a, float b)
{
	return 1.0 - (1.0 - a) * b;
}

float3 nlerp(float3 a, float3 b, float t)
{
	return normalize(lerp(a, b, t));
}

float GetFixedLighting(float angle, float distanceOffset, float3 normalDirectionWS, float size)
{
	float2 directionVector = normalize(float2(cos(angle), sin(angle)));
	directionVector *= distanceOffset;
	
	float3 dVn = float3(-directionVector.xy, 1.0);
	float m = 1.0 - saturate(dot(normalDirectionWS, normalize(TransformObjectToWorld(dVn))));
	directionVector *= m;
	
	float3 dVp = float3(directionVector.xy, 1.0);
	float o = saturate(dot(normalDirectionWS, normalize(TransformObjectToWorld(dVp))));
	
	return step(1.0 - size, o);
}

float GetMainLighting(float3 viewDirection, float3 mainLightDirectionWS, float3 normalDirectionWS, float mainLightingSize)
{
	float3 halfDirectionWS = normalize(viewDirection + mainLightDirectionWS);
	float c = saturate(dot(normalDirectionWS, halfDirectionWS));
	
	return step(1.0 - mainLightingSize, c);
}

float3 GetReflectionLuma(float3 viewDirection, float3 normalDirectionWS, float intensity)
{
	float3 reflectionProbeData = SHADERGRAPH_REFLECTION_PROBE(viewDirection, normalDirectionWS, 2);
	return reflectionProbeData * max(intensity, 0);
}

float3 GetColor(float t, float pow, float edge, float3 innerColor, float3 outerColor)
{
	float g = saturate(map(t, edge, 1.0, 0.0, 1.0));
	g = InvPow(g, max(pow, 0.1));
	return lerp(innerColor, outerColor, g);
}

void GetEye_float(float2 uv, float3 normalDirectionWS, 
float3 viewDirection, float3 pupilColorInner, float3 pupilColorOuter, float pupilBlend,
float3 irisColorInner, float3 irisColorOuter, float irisBlend, float3 scleraColor, 
UnityTexture2D noiseTex, float pupilSize, float irisSize, float3 positionOS,
float noiseScalePupil, float noiseIntensityPupil, float noiseScaleIris, float noiseIntensityIris,
float fixedLightingDirection, float fixedLightingOffset, float fixedLightingSize,
float reflectionIntensity, float mainLightingIntensity, float fixedLightingIntensity,
out float3 result)
{
	result = 0;
	#ifndef SHADERGRAPH_PREVIEW
	normalDirectionWS = normalize(normalDirectionWS);
	viewDirection = normalize(viewDirection);
	float3 mainLightDirectionWS = normalize(GetMainLight().direction);
	float nDotV = saturate(dot(normalDirectionWS, viewDirection));
	
	
	float noisePupil = 1.0 + (SAMPLE_TEXTURE2D(noiseTex.tex, noiseTex.samplerstate, float2(positionOS.x, positionOS.y) * noiseScalePupil) - 0.5) * noiseIntensityPupil * 2.0;
	float noiseIris = 1.0 + (SAMPLE_TEXTURE2D(noiseTex.tex, noiseTex.samplerstate, float2(positionOS.x, positionOS.y) * noiseScaleIris) - 0.5) * noiseIntensityIris * 2.0;
	
	
	irisSize = max(irisSize, pupilSize);
	float pupilEdge = 1.0 - pupilSize;
	float irisEdge = 1.0 - irisSize;
	float pupil = step(pupilEdge, uv.y);
	float iris = step(irisEdge, uv.y);
	float scleraEdge = 1.0 - iris;
	
	
	float scleraGradient = Soften(nDotV, 0.6);
	
	float3 pupilColor = GetColor(uv.y, pupilBlend, pupilEdge, pupilColorInner, pupilColorOuter) * noisePupil;
	float3 irisColor = GetColor(uv.y, irisBlend, irisEdge, irisColorInner, irisColorOuter) * noiseIris;
	
	
	float3 eyeColor = lerp(scleraColor * scleraGradient, irisColor, iris);
	eyeColor = lerp(eyeColor, pupilColor, pupil);
	
	
	const float mainLightingSize = 0.02;
	float mainEyeLighting = GetMainLighting(viewDirection, mainLightDirectionWS, normalDirectionWS, mainLightingSize);
	
	
	float fixedLighting = GetFixedLighting(fixedLightingDirection, fixedLightingOffset, normalDirectionWS, fixedLightingSize);
	float lighting = saturate(mainEyeLighting + fixedLighting);
	float lightingMin1 = max(mainEyeLighting * max(mainLightingIntensity, 1.0), fixedLighting * max(fixedLightingIntensity, 1.0));
	float lightingMin0 = max(mainEyeLighting * max(mainLightingIntensity, 0.0), fixedLighting * max(fixedLightingIntensity, 0.0));
	result = lerp(eyeColor, lightingMin1, lighting * min(1.0, lightingMin0));
	
	float3 luma = GetReflectionLuma(viewDirection, normalDirectionWS, reflectionIntensity);
	result += luma * (1.0 - nDotV);
	#endif
}

#endif