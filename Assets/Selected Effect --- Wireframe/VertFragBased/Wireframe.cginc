#ifndef WIREFRAME_INCLUDED
#define WIREFRAME_INCLUDED

#include "UnityCG.cginc"

float4 _LineColor, _LineFrontColor, _LineBackColor, _StylizedWireframeColor;
float _LineWidth, _StylizedWireframeThickness, _StylizedWireframeSqueezeMin, _StylizedWireframeSqueezeMax;
float _StylizedWireframeDashRepeats, _StylizedWireframeDashLength, _ScanlineScale;

struct v2f
{
	float4 pos : SV_POSITION;
	float3 barycentric : TEXCOORD0;
	float3 wldpos : TEXCOORD1;
};
v2f vert (appdata_full v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.barycentric = v.color;
	o.wldpos = mul(unity_ObjectToWorld, v.vertex).xyz;
	return o;
}
float4 frag (v2f input, fixed facing : VFACE) : SV_TARGET
{
	float3 d = fwidth(input.barycentric);
	float3 a3 = smoothstep(float3(0, 0, 0), d * 1.0, input.barycentric);
	float mindist = min(min(a3.x, a3.y), a3.z);
	float edge = 1.0 - mindist;
	clip(edge - _LineWidth);

	float sl = 1.0;
#ifdef WIREFRAME_SCROLL
	sl = frac((input.wldpos.y * _ScanlineScale) + _Time.y);
	sl = step(sl, 0.5);
#endif

#if ENABLE_DOUBLE_SIDE_COLOR
	return facing > 0 ? _LineFrontColor : _LineBackColor;
#else
	return float4(_LineColor.rgb, sl);
#endif
}
//////////////////////////////////////////////////////////////////////////////////////////////////
float aastep (float threshold, float dist)
{
	float afwidth = fwidth(dist) * 0.5;
	return smoothstep(threshold - afwidth, threshold + afwidth, dist);
}
float4 fragStylizedWireframe (v2f i) : SV_TARGET
{
	float d = min(min(i.barycentric.x, i.barycentric.y), i.barycentric.z);

	float along = max(i.barycentric.x, i.barycentric.y);
	if (i.barycentric.y < i.barycentric.x && i.barycentric.y < i.barycentric.z)
		along = 1.0 - along;

	float thickness = _StylizedWireframeThickness;
#ifdef WIREFRAME_SQUEEZE
	thickness *= lerp(_StylizedWireframeSqueezeMin, _StylizedWireframeSqueezeMax, (1.0 - sin(along * 3.1415926)));
#endif
#ifdef WIREFRAME_DASH
	float offset = 1.0 / _StylizedWireframeDashRepeats * _StylizedWireframeDashLength / 2.0;
	float pattern = frac((along + offset) * _StylizedWireframeDashRepeats);
	thickness *= 1.0 - aastep(_StylizedWireframeDashLength, pattern);
#endif

	float edge = 1.0 - aastep(thickness, d);

	float4 c;
	c.rgb = _StylizedWireframeColor.rgb;
	c.a = edge;
	return c;
}

#endif