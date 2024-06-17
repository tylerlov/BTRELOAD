#ifndef INCLUDE_RM_RENDER_BASE
#pragma exclude_renderers gles
#define INCLUDE_RM_RENDER_BASE

half4 RM_RenderBase(Varyings i)
{
#ifndef RAYMARCHER_SMOOTH_BLEND
	RaymarcherGlobalSdfObjectSmoothness = 0;
#endif

	half4 pixelColor = half4(0, 0, 0, 1);

#ifdef RAYMARCHER_TYPE_QUALITY
	float2x4 sdfData = 0.0;		// [0]: x = sdf, rgb = color. [1]: x = materialType, y = materialInstance, zw = unused
#elif defined(RAYMARCHER_TYPE_STANDARD)
	float4 sdfData = 0.0;		// x = sdf, y = hue, z = materialType, w = materialInstance
#else // PERFORMANT
	half2 sdfData = 0.0;		// x = sdf, y = hue
#endif

	if (RaymarchIntersection(i.ray, sdfData))
	{
		const half3 DEFCOL = half3(.5, .5, .5);
		const half3 DEFRED = half3(1, 0, 0);

#ifdef RAYMARCHER_TYPE_QUALITY
		half4 sdfRGBA = half4(sdfData[0].gba, 1);
		int sdfMaterialType = ceil(sdfData[1].x);
		int sdfMaterialInstance = ceil(sdfData[1].y);
#elif defined(RAYMARCHER_TYPE_STANDARD)
		half4 sdfRGBA = half4(lerp(DEFCOL, ApplyHue(DEFRED, sdfData.y), RaymarcherGlobalHueSaturation), 1);
		int sdfMaterialType = ceil(sdfData.z);
		int sdfMaterialInstance = ceil(sdfData.w);
#else
		pixelColor = half4(lerp(DEFCOL, ApplyHue(DEFRED, sdfData.y), RaymarcherGlobalHueSaturation), 1);
#endif

#ifndef RAYMARCHER_TYPE_PERFORMANT
		pixelColor = RM_PerObjRenderMaterials(i.ray, sdfRGBA, sdfMaterialType, sdfMaterialInstance);
		pixelColor = RM_PerObjPostRenderMaterials(i.ray, pixelColor, sdfMaterialType, sdfMaterialInstance);
#endif
		pixelColor = RM_GlobalRenderMaterials(i.ray, pixelColor, 0, 0);
		pixelColor = RM_GlobalPostRenderMaterials(i.ray, pixelColor, 0, 0);

		pixelColor.rgb = clamp(pixelColor.rgb, 0, 256);
		pixelColor.a = saturate(pixelColor.a);
	}
	else
	{
		pixelColor = half4(0, 0, 0, 0);
	}

#ifdef RAYMARCHER_DISTANCE_FOG
	pixelColor.rgb = CalculateDistanceFog(pixelColor.rgb, i.ray.o, i.ray.p, RaymarcherDistanceFogDistance, RaymarcherDistanceFogSmoothness);
#endif

#ifdef RAYMARCHER_SCENE_DEPTH
	pixelColor.a *= smoothstep(0.0, RaymarcherSceneDepthSmoothness, i.ray.sceneDepth - i.ray.l);
#endif

	return half4(pixelColor.rgb * RaymarcherRendererColorTint.rgb * RaymarcherRendererExposure, pixelColor.a);
}

#endif