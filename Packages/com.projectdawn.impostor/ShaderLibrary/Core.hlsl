#ifndef UNITY_IMPOSTOR_CORE_INCLUDED
#define UNITY_IMPOSTOR_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

float3 ProjectOnPlane(float3 planeNormal, float3 position)
{
    float3 xaxis = normalize(cross(float3(0, 1, 0), planeNormal));
    float3 yaxis = normalize(cross(planeNormal, xaxis));
    return position.x * xaxis + position.y * yaxis + position.z * 0;
}

float3 IntersectPlane(float3 planeP, float3 planeN, float3 rayP, float3 rayD)
{
    float t = (dot(planeP, planeN) - dot(rayP, planeN)) / dot(rayD, planeN);
    return rayP + t * rayD;
}

float3 PlaneRaycast(float3 projectionPlaneOffset, float3 planeNormalOS, float3 viewPosOS, float3 eyeDirOS)
{
    float3 projectedPoint = IntersectPlane(projectionPlaneOffset, planeNormalOS, viewPosOS, eyeDirOS);
    projectedPoint -= projectionPlaneOffset;
    return projectedPoint;
}

float2 GridToUV(float2 gridXY)
{
    return (gridXY + 1.0) * 0.5;
}

float2 PackDirectionTS(float3 direction)
{
    return direction.xy * -1 / direction.z;
}

float3 UnpackDirectionTS(float2 direction)
{
    return float3(direction, -1);
}

float3 TransformToTangentSpace(float3 planeNormal, float3 position)
{
    float3 tangent = cross(planeNormal, float3(0, 1, 0));
    tangent = normalize(tangent);
    float3 binormal = normalize(cross(tangent, planeNormal));
    float3x3 M = float3x3(tangent, binormal, normalize(planeNormal));
    return mul(M, position).xyz;
}

float2 EncodeTileIndexes(int2 tile0Indexes, int2 tile1Indexes, int2 tile2Indexes)
{
    int range = 64;
    float xs = tile0Indexes.x + tile1Indexes.x * range + tile2Indexes.x * range * range;
    float ys = tile0Indexes.y + tile1Indexes.y * range + tile2Indexes.y * range * range;

    return float2(xs, ys);
}

void DecodeTileIndexes(float2 input, out int2 tile0Indexes, out int2 tile1Indexes, out int2 tile2Indexes)
{
    int range = 64;
    int rangeSquared = range * range;
    input = round(input);
    tile2Indexes.x = floor(input.x / rangeSquared);
    tile1Indexes.x = floor((input.x - tile2Indexes.x * rangeSquared) / range);
    tile0Indexes.x = floor(input.x - tile2Indexes.x * rangeSquared - tile1Indexes.x * range);

    tile2Indexes.y = floor(input.y / range / range);
    tile1Indexes.y = floor((input.y - tile2Indexes.y * rangeSquared) / range);
    tile0Indexes.y = floor(input.y - tile2Indexes.y * rangeSquared - tile1Indexes.y * range);
}

half JustSign(half v)
{
    return v > 0 ? 1 : -1;
}

half2 JustSign(half2 v)
{
    return half2(v.x > 0 ? 1 : -1, v.y > 0 ? 1 : -1);
}

half2 HemiOctahedronGrid(half3 vec)
{
    vec.y = max(0.001, vec.y);
    vec = normalize(vec);
    vec.xz /= dot(1.0, abs(vec));
    return half2(vec.x + vec.z, vec.x - vec.z);
}

half3 HemiOctahedronEncode(half2 coord)
{
    coord = half2(coord.x + coord.y, coord.x - coord.y) * 0.5;
    half3 vec = half3(coord.x, 1.0 - dot(half2(1.0, 1.0), abs(coord.xy)), coord.y);
    return vec;
}

float3 HemiOctahedronCapturePoint(int2 tile, float framesXY)
{
    float2 pos = (tile) / (framesXY - 1);
    return HemiOctahedronEncode(pos * 2 - 1);
}

float3 Barycentric(float2 p, float2 a, float2 b, float2 c)
{
    float2 v0 = b - a;
    float2 v1 = c - a;
    float2 v2 = p - a;
    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);
    float denom = d00 * d11 - d01 * d01;
    float v = (d11 * d20 - d01 * d21) / denom;
    float w = (d00 * d21 - d01 * d20) / denom;
    float u = 1.0f - v - w;
    return float3(u, v, w);
}

void GetCameraPosDirOS(float3 offset, out float3 cameraPosOS, out float3 cameraDirOS)
{
    cameraPosOS = TransformWorldToObject(GetCameraRelativePositionWS(_WorldSpaceCameraPos.xyz));

    //cameraPosOS = _WorldSpaceCameraPos;
    //cameraPosOS = TransformWorldToObject(GetCurrentViewPosition());
    if (IsPerspectiveProjection())
    {
        cameraDirOS = offset - cameraPosOS; // point to object geometric center instead of origin
    }
    else
    {
        cameraDirOS = TransformWorldToObjectDir(GetViewForwardDir(), false);
    }
    cameraDirOS = normalize(cameraDirOS);
}

void Ray(float3 vertPosOS, float3 viewPosOS, float3 viewDirOS, out float3 eyePosOS, out float3 eyeDirOS)
{
    if (IsPerspectiveProjection())
    {
        eyeDirOS = vertPosOS - viewPosOS;
        eyePosOS = viewPosOS;
    }
    else
    {
        eyeDirOS = viewDirOS;
        eyePosOS = vertPosOS - eyeDirOS;
    }
}

float3 PlanePointProjection(float3 planeNormalOS, float3 projectedPoint)
{
    float3 projectedUV = TransformToTangentSpace(planeNormalOS, projectedPoint);
    return projectedUV;
}

#endif //UNITY_IMPOSTOR_UTILITIES_INCLUDED
