#ifndef INCLUDE_RM_RAY_INTERSECTION
#pragma exclude_renderers gles
#define INCLUDE_RM_RAY_INTERSECTION

#ifdef RAYMARCHER_TYPE_QUALITY
bool RaymarchIntersection(inout Ray ray, inout float2x4 sdfResult)
#elif defined(RAYMARCHER_TYPE_STANDARD)
bool RaymarchIntersection(inout Ray ray, inout float4 sdfResult)
#else // PERFORMANT
bool RaymarchIntersection(inout Ray ray, inout float2 sdfResult)
#endif
{
	float t = 0;
	for (uint i = 0; i < RAYMARCHER_ITERATIONS; i++)
	{
		if (t > RaymarcherMaxRenderDistance)
		{
			return false;
		}

#ifdef RAYMARCHER_SCENE_DEPTH
		if (t >= ray.sceneDepth)
		{
			return false;
		}
#endif

		ray.p = ray.o + (ray.nd * t);
#ifdef RAYMARCHER_PIXELATION
		ray.p = RaymarcherPixelationSize * floor(ray.p / RaymarcherPixelationSize);
#endif

#ifdef RAYMARCHER_TYPE_QUALITY
		float2x4 sdfObj = SdfObjectBuffer(ray.p);
		float sdfDistance = sdfObj[0].x;
#elif defined(RAYMARCHER_TYPE_STANDARD)
		float4 sdfObj = SdfObjectBuffer(ray.p);
		float sdfDistance = sdfObj.x;
#else // PERFORMANT
		half2 sdfObj = SdfObjectBuffer(ray.p);
		half sdfDistance = sdfObj.x;
#endif

#if defined(RAYMARCHER_REACT_GEOMETRY) && defined(RAYMARCHER_SCENE_DEPTH)
		float genericDistance = ray.sceneDepth - length(ray.p - ray.o);
		if (genericDistance < EPSILONUP)
		{
			return false;
		}
		sdfDistance = SmoothUnion1(genericDistance, sdfDistance, RaymarcherSceneGeometrySmoothness);
#endif
		t += sdfDistance;
		ray.l = t;

		if (sdfDistance < RaymarcherRenderQuality)
		{
			sdfResult = sdfObj;
			return true;
		}
	}

	return false;
}

void RaymarchIntersectionAccumulative(inout Ray ray, out float sdfAccumulation)
{
	float t = 0;
	for (uint i = 0; i < 128; i++)
	{
		if (t > RaymarcherMaxRenderDistance)
		{
			return;
		}

#ifdef RAYMARCHER_SCENE_DEPTH
		if (t >= ray.sceneDepth)
		{
			return;
		}
#endif

		ray.p = ray.o + (ray.nd * t);

#ifdef RAYMARCHER_TYPE_QUALITY
		float sdfDistance = SdfObjectBuffer(ray.p)[0].x;
#elif defined(RAYMARCHER_TYPE_STANDARD)
		float sdfDistance = SdfObjectBuffer(ray.p).x;
#else // PERFORMANT
		half sdfDistance = SdfObjectBuffer(ray.p).x;
#endif

#if defined(RAYMARCHER_REACT_GEOMETRY) && defined(RAYMARCHER_SCENE_DEPTH)
		float genericDistance = ray.sceneDepth - length(ray.p - ray.o);
		if (genericDistance < EPSILONUP)
		{
			return;
		}
		sdfDistance = SmoothUnion1(genericDistance, sdfDistance, RaymarcherSceneGeometrySmoothness);
#endif

		t += sdfDistance;
		sdfAccumulation += t;
		ray.l = t;
	}

	return;
}

#endif