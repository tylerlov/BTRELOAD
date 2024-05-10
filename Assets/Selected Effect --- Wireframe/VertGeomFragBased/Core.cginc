#ifndef SEW_CORE_INCLUDED
#define SEW_CORE_INCLUDED

#include "UnityCG.cginc"

// calculate barycentric distances
#define DISTTOLINE_LEN(p0, p1, p2, l) length(cross(p0 - p1, p0 - p2)) / l
void wireframe_geom (float3 p0, float3 p1, float3 p2, out float3 d0, out float3 d1, out float3 d2)
{
	float l0 = length(p0 - p1);
	float l1 = length(p0 - p2);
	float l2 = length(p2 - p1);

	float4 d = float4(0.0,
		DISTTOLINE_LEN(p2, p0, p1, l0),
		DISTTOLINE_LEN(p0, p1, p2, l2),
		DISTTOLINE_LEN(p1, p0, p2, l1));
	d /= min(d.y, min(d.z, d.w));

	d0 = d.xzx;
	d1 = d.xxw;
	d2 = d.yxx;
#if ENABLE_QUAD
	d0.x = ((l0 > l1) && (l0 > l2));
	d0.z = ((l1 >= l0) && (l1 > l2));
	d1.y = ((l2 >= l0) && (l2 >= l1));
#endif
}

float _WireThickness, _AASmooth;
void wireframe (float3 dist, float2 uv, out half mask, out float3 thickness, out float wire)
{
	thickness = _WireThickness.xxx;
	float3 fwd = fwidth(dist);

	float3 df = dist - thickness;
	df /= _AASmooth * fwd + 1e-6;
	wire = min(df.x, min(df.y, df.z));
	wire = smoothstep(0.0, 1.0, wire + 0.5);
	mask = 1.0;   // keep it for now
}

half4 _GlowColor;
half _GlowDist, _GlowPower;
#define LERP3(c0, c1, w) lerp(c0, lerp(c0, lerp(c0, c1, w.x), w.y), w.z)
void glow (float3 dist, float3 thickness, half mask, inout half4 col)
{
	float3 df = max(0, dist - thickness * 0.95);
	df /= _GlowDist + 1e-6;
	df = smoothstep(0.0, 1.0, sqrt(df));

	half4 glowCol = _GlowColor * mask;
	half blend = glowCol.a * _GlowPower;
	glowCol.rgb = lerp(col.rgb, glowCol.rgb, blend);
	glowCol.a = lerp(col.a, 1.0, blend);

	col = LERP3(glowCol, col, df);
}

#endif