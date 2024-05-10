#ifndef LOCAL_GI_INCLUDE
#define LOCAL_GI_INCLUDE


// These variables are all set dynamically in the renderer feature based on data from the LocalGIHandler.
float3 _LocalGIProbePosition;
TextureCube<float4> _DiffuseIrradianceData;
SamplerState my_linear_repeat_sampler;
float _LocalGIMaxDistance;


void GetLocalGI(float3 normalWS, float3 positionWS, out float3 irradiance)
{
	float d = distance(_LocalGIProbePosition, positionWS);
    float falloff = 1.0 - saturate(d / _LocalGIMaxDistance);
	irradiance = _DiffuseIrradianceData.SampleLevel(my_linear_repeat_sampler, normalWS, 0).rgb;
	irradiance *= falloff;
}


// You should multiply the results of the Local GI by the material's albedo (the base color) to retain physically-based lighting results.
void GetLocalGI_float(float3 normalWS, float3 positionWS, out float3 Irradiance)
{
	Irradiance = 0;
	#ifndef SHADERGRAPH_PREVIEW
	GetLocalGI(normalWS, positionWS, Irradiance);
	#endif
}

#endif