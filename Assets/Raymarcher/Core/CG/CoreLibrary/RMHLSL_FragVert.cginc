#ifndef INCLUDE_RM_FRAGMENT_VERTEX
#define INCLUDE_RM_FRAGMENT_VERTEX

#ifdef RAYMARCHER_PIPELINE_HDRP
	float4 ComputeScreenPos4(float4 csPosition)
	{
		float4 o = csPosition * 0.5f;
		o.xy = float2(o.x, o.y) + o.w;
		o.zw = csPosition.zw;
		return o;
	}
#endif

Varyings RMVertex(Attributes v)
{
	Varyings o = (Varyings)0;

	Ray r = (Ray)0;
	r.o = mul(RaymarcherCamWorldMatrix, v.vertexOS).xyz;
	r.d = v.vertexOS.xyz;
	r.d /= abs(r.d.z);
	r.d = mul(RaymarcherCamWorldMatrix, float4(r.d, 0)).xyz;

#ifndef RAYMARCHER_PIPELINE_HDRP
	o.vertexCS = UnityObjectToClipPos(v.vertexOS.xyz);
	half4 suv = ComputeScreenPos(o.vertexCS);
#else
	o.vertexCS = TransformObjectToHClip(v.vertexOS.xyz);
	half4 suv = ComputeScreenPos4(o.vertexCS);
#endif
	r.uv = suv.xy / suv.w;

	o.ray = r;

	return o;
}

half4 RMFragment(Varyings i) : SV_Target
{
	i.ray.nd = normalize(i.ray.d);
	i.ray.sceneDepth = CalculateSceneDepth(i);

#ifdef RAYMARCHER_PIPELINE_HDRP
	PositionInputs posInput = GetPositionInput(i.vertexCS.xy, _ScreenSize.zw, i.ray.sceneDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
	i.ray.uv = posInput.positionNDC.xy;
#endif

	return RM_RenderBase(i);
}


#endif