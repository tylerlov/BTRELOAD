#ifndef UNITY_IMPOSTOR_FRAG_INCLUDED
#define UNITY_IMPOSTOR_FRAG_INCLUDED

#include "Packages/com.projectdawn.impostor/ShaderLibrary/Core.hlsl"

float2 ParallaxOffsetStep1(float3 eyeTS, float depth, float framesXY)
{
    depth = 0.5 - depth;
    return (eyeTS.xy / framesXY) * depth;
}

float3 UVToObjectSpace(float2 uv, float depth, float3 capVec, float framesXY, float3 offset, float size)
{
    float3 tangent = normalize(cross(capVec, float3(0, 1, 0)));
    float3 binormal = normalize(cross(tangent, capVec));
    float3x3 M = float3x3(tangent, binormal, normalize(capVec));

    float3 originOffset = mul(M, offset);

    float height = depth; //billboard is at the object center
    uv *= framesXY;
    uv = frac(uv) - 0.5;

    float3 offsetVS = float3(uv, depth) * size;
    offsetVS.z -= size / 2;
    offsetVS.xy += originOffset.xy;
    offsetVS.z += originOffset.z;
    float3 posVS = offsetVS;

    M = transpose(M); // inverse transform
    return mul(M, posVS);
}

bool IsUvInside(float2 uvs, int2 tileIndex, float framesXY)
{
    int2 targetTile = tileIndex;
    int2 sourceTile = floor(uvs * framesXY);
    return targetTile.x == sourceTile.x && targetTile.y == sourceTile.y; // Subtract?
}

float3 NormalizeBarycentricsAndClip(float3 barycentrics)
{
    float sum = (barycentrics.x + barycentrics.y + barycentrics.z);
    clip(sum - 0.001); // No point to render point that is in background
    return barycentrics / sum;
}

void ImpostorFrag_float(
    float3 positionOS, float framesXY, float3 offset, float size, TEXTURE2D_PARAM(depthTexture, depthSampler),
    float4 frame0, float4 frame1, float4 frame2, float4 w, float4 depths, float4 tiles0, float4 tiles1,
    out float2 outUv0, out float2 outUv1, out float2 outUv2, out float3 outBarycentrics, out float outDepthOffset)
{
    // cancel out billboard perspective divide
    float d = 1.0 / depths.w;
    depths.xyz /= d;
    frame0 /= d;
    frame1 /= d;
    frame2 /= d;

    // Interpolation perspective correction
    frame0 /= depths.x;
    frame1 /= depths.y;
    frame2 /= depths.z;

    float3 weights = float3(w.xy, 1 - w.x - w.y);

    float2 uv0 = frame0.xy;
    float2 uv1 = frame1.xy;
    float2 uv2 = frame2.xy;

    int2 tile0Indexes, tile1Indexes, tile2Indexes;
    DecodeTileIndexes(w.zw, tile0Indexes, tile1Indexes, tile2Indexes);

    float2 offset0 = tile0Indexes / framesXY;
    float2 offset1 = tile1Indexes / framesXY;
    float2 offset2 = tile2Indexes / framesXY;
    float2 tileSize = 1.0 / framesXY;

    //uv0 = (saturate(uv0) + tile0Indexes) / framesXY;
    //uv1 = (saturate(uv1) + tile1Indexes) / framesXY;
    //uv2 = (saturate(uv2) + tile2Indexes) / framesXY;

    uv0 = clamp(uv0, offset0, offset0 + tileSize);
    uv1 = clamp(uv1, offset1, offset1 + tileSize);
    uv2 = clamp(uv2, offset2, offset2 + tileSize);

    float3 eyeTS0 = normalize(UnpackDirectionTS(frame0.zw));
    float3 eyeTS1 = normalize(UnpackDirectionTS(frame1.zw));
    float3 eyeTS2 = normalize(UnpackDirectionTS(frame2.zw));

    float depth0 = SAMPLE_TEXTURE2D(depthTexture, depthSampler, uv0).a;
    float depth1 = SAMPLE_TEXTURE2D(depthTexture, depthSampler, uv1).a;
    float depth2 = SAMPLE_TEXTURE2D(depthTexture, depthSampler, uv2).a;

    uv0 += ParallaxOffsetStep1(eyeTS0, depth0, framesXY);
    uv1 += ParallaxOffsetStep1(eyeTS1, depth1, framesXY);
    uv2 += ParallaxOffsetStep1(eyeTS2, depth2, framesXY);

    //int2 tile0Indexes, tile1Indexes, tile2Indexes;
    //DecodeTileIndexes(w.zw, tile0Indexes, tile1Indexes, tile2Indexes);

    weights *= float3(
        IsUvInside(uv0, tile0Indexes, framesXY),
        IsUvInside(uv1, tile1Indexes, framesXY),
        IsUvInside(uv2, tile2Indexes, framesXY));

    //weights *= float3(0, 1, 0);

    weights = NormalizeBarycentricsAndClip(weights);

    outUv0 = uv0;
    outUv1 = uv1;
    outUv2 = uv2;
    outBarycentrics = weights;

    float3 cameraPosOS, cameraDirOS;
    GetCameraPosDirOS(offset, cameraPosOS, cameraDirOS);

    float3 billboardPositionOS = positionOS;

    float3 cap0 = HemiOctahedronCapturePoint(tile0Indexes, framesXY);
    float3 cap1 = HemiOctahedronCapturePoint(tile1Indexes, framesXY);
    float3 cap2 = HemiOctahedronCapturePoint(tile2Indexes, framesXY);

    float3 pointOS0 = UVToObjectSpace(uv0, depth0, cap0, framesXY, offset, size);
    float3 pointOS1 = UVToObjectSpace(uv1, depth1, cap1, framesXY, offset, size);
    float3 pointOS2 = UVToObjectSpace(uv2, depth2, cap2, framesXY, offset, size);

    float3 offsetPositionOS = pointOS0 * weights.x + pointOS1 * weights.y + pointOS2 * weights.z;
    outDepthOffset =  dot(offsetPositionOS - positionOS, cameraDirOS);
}

#endif
