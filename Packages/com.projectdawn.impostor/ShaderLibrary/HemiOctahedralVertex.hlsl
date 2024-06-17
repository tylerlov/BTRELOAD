#ifndef UNITY_IMPOSTOR_VERT_INCLUDED
#define UNITY_IMPOSTOR_VERT_INCLUDED

#include "Packages/com.projectdawn.impostor/ShaderLibrary/Core.hlsl"

void Billboard(float3 positionOS, float3 normalOS, float3 offset, float size, out float3 outPosition, out float3 outNormal, out float3 outTangent)
{
    // face the billboard to camera
    outPosition = ProjectOnPlane(-normalOS, positionOS) * size + offset;
    outNormal = normalOS; // already normalized
    outTangent = cross(outNormal, /*upAxis*/ float3(0, 1, 0));
}

void HemiOctahedronMapping(float3 cameraDirOS, float framesXY, out float2 tile0Indexes, out float2 tile1Indexes, out float2 tile2Indexes, out float3 weights)
{
    // project camera ray into 2D grid
    float2 grid = HemiOctahedronGrid(-cameraDirOS);

    //find the center of 4 tiles quad
    float quadCount = (framesXY - 1);
    float2 camPointUV = GridToUV(grid) * quadCount;
    float2 quadCenterUV = clamp(floor(camPointUV), 0, quadCount - 1); // need to clamp for case when grid = 1
    quadCenterUV += 0.5;

    const float2x2 rot = float2x2(float2(0, 1), float2(-1, 0));
    float2 quadrantDiagonal = mul(JustSign(grid), rot) * 0.5;
    float2 camOffsetDir = (camPointUV - quadCenterUV);

    float2 diagonalOffset = dot(camOffsetDir, quadrantDiagonal) > 0 ? quadrantDiagonal : -quadrantDiagonal;

    float2 t0 = JustSign(grid) * 0.5;
    float2 tile0center = round(quadCenterUV + t0);
    float2 tile1center = round(quadCenterUV - t0);
    float2 tile2center = round(quadCenterUV + diagonalOffset);

    tile0Indexes = tile0center;
    tile1Indexes = tile1center;
    tile2Indexes = tile2center;

    weights = Barycentric(camPointUV, tile0center, tile1center, tile2center);
}

void ImpostorVert2_float(
    float3 position, float framesXY, float3 offset, float size,
    out float3 outPosition, out float3 outNormal, out float3 outTangent,
    out float4 outFrame0, out float4 outFrame1, out float4 outFrame2, out float4 outWeights, out float4 outDepths, out float4 outTiles0, out float4 outTiles1)
{
    // Get camera direction and position with offset
    float3 cameraPosOS, cameraDirOS;
    GetCameraPosDirOS(offset, cameraPosOS, cameraDirOS);

    // Frce the mesh to camera
    Billboard(position, -cameraDirOS, offset, size, outPosition, outNormal, outTangent);

    // Find three best frames with barycentric coordinate
    float2 tile0Indexes;
    float2 tile1Indexes;
    float2 tile2Indexes;
    float3 weights;
    HemiOctahedronMapping(cameraDirOS, framesXY, tile0Indexes, tile1Indexes, tile2Indexes, weights);

    // Find the capture points of these frames
    float3 cap0 = HemiOctahedronCapturePoint(tile0Indexes, framesXY);
    float3 cap1 = HemiOctahedronCapturePoint(tile1Indexes, framesXY);
    float3 cap2 = HemiOctahedronCapturePoint(tile2Indexes, framesXY);

    // Find the ray to the vertex
    float3 rayOrigin;
    float3 rayDirection;
    Ray(outPosition, cameraPosOS, cameraDirOS, rayOrigin, rayDirection);

    // Transform to plane space
    float3 projectedDir0 = TransformToTangentSpace(cap0, normalize(rayDirection));
    float3 projectedDir1 = TransformToTangentSpace(cap1, normalize(rayDirection));
    float3 projectedDir2 = TransformToTangentSpace(cap2, normalize(rayDirection));

    // Calculate ray intersaction to virtual frames
    float3 hit0 = PlaneRaycast(offset, cap0, rayOrigin, rayDirection);
    float3 hit1 = PlaneRaycast(offset, cap1, rayOrigin, rayDirection);
    float3 hit2 = PlaneRaycast(offset, cap2, rayOrigin, rayDirection);

    // Transform to plane space
    float3 projectedPoint0 = PlanePointProjection(cap0, hit0);
    float3 projectedPoint1 = PlanePointProjection(cap1, hit1);
    float3 projectedPoint2 = PlanePointProjection(cap2, hit2);

    float2 projectedGrid0 = projectedPoint0.xy / size + 0.5; //shift into UV space [0:1]
    float2 projectedGrid1 = projectedPoint1.xy / size + 0.5; //shift into UV space [0:1]
    float2 projectedGrid2 = projectedPoint2.xy / size + 0.5; //shift into UV space [0:1]

    float2 projectedUV0 = (projectedGrid0 + floor(tile0Indexes)) / framesXY; // scale down to tile size and shift origin
    float2 projectedUV1 = (projectedGrid1 + floor(tile1Indexes)) / framesXY; // scale down to tile size and shift origin
    float2 projectedUV2 = (projectedGrid2 + floor(tile2Indexes)) / framesXY; // scale down to tile size and shift origin

    float distanceToCamera0 = TransformObjectToHClip(hit0).w;
    float distanceToCamera1 = TransformObjectToHClip(hit1).w;
    float distanceToCamera2 = TransformObjectToHClip(hit2).w;

    float4 depths = float4(distanceToCamera0, distanceToCamera1, distanceToCamera2, 0);

    outFrame0 = float4(projectedUV0, PackDirectionTS(projectedDir0));
    outFrame1 = float4(projectedUV1, PackDirectionTS(projectedDir1));
    outFrame2 = float4(projectedUV2, PackDirectionTS(projectedDir2));

    //outFrame0 = float4(projectedGrid0, PackDirectionTS(projectedDir0));
    //outFrame1 = float4(projectedGrid1, PackDirectionTS(projectedDir1));
    //outFrame2 = float4(projectedGrid2, PackDirectionTS(projectedDir2));

    float2 encodedTileIndexes = EncodeTileIndexes(tile0Indexes, tile1Indexes, tile2Indexes);
    outWeights = float4(weights.xy, encodedTileIndexes);

    outTiles0 = float4(tile0Indexes, tile1Indexes);
    outTiles1 = float4(tile2Indexes, tile2Indexes);

    // for perspective corrected interpolation
    // https://www.comp.nus.edu.sg/~lowkl/publications/lowk_persp_interp_techrep.pdf
    outFrame0 /= depths.x;
    outFrame1 /= depths.y;
    outFrame2 /= depths.z;

    // cancel out billboard perspective corrected interpolation
    depths.w = TransformObjectToHClip(outPosition).w;
    depths.xyz = depths.w / depths.xyz;
    outFrame0 *= depths.w;
    outFrame1 *= depths.w;
    outFrame2 *= depths.w;
    depths.w = 1.0 / depths.w;

    outDepths = depths;
}

#endif
