#ifndef UNITY_IMPOSTOR_HEMI_OCTAHEDRON_INCLUDED
#define UNITY_IMPOSTOR_HEMI_OCTAHEDRON_INCLUDED

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

#endif //UNITY_IMPOSTOR_UTILITIES_INCLUDED
