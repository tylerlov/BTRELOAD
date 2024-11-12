// Copyright (c) 2024 Léo Chaumartin
// The pipeline and platform independant impostor geometry helper in HLSL


#ifndef MIRAGE_GEOMETRY_HELPER
#define MIRAGE_GEOMETRY_HELPER

#ifndef DEG2RAD
#define DEG2RAD 0.01745328
#endif

#ifndef UNITY_MATRIX_I_M
#define UNITY_MATRIX_I_M unity_WorldToObject
#endif

void Billboarding(inout float3 vertex, inout float3 normal, inout float3 tangent) {
    if (_BillboardingEnabled < 0.5)
        return;

    float3 viewVec;

    // Ugly fix until we get some macros consistency across pipelines
    if (length(UNITY_MATRIX_V._m03_m13_m23 - UNITY_MATRIX_I_V._m03_m13_m23) > 0.01)
        viewVec = -_WorldSpaceCameraPos + mul(UNITY_MATRIX_M, float4(0, 0, 0, 1)).xyz;
    else
        viewVec = -UNITY_MATRIX_V._m03_m13_m23 + mul(UNITY_MATRIX_M, float4(0, 0, 0, 1)).xyz;

    float3 viewDir = normalize(viewVec);

    if (_ClampLatitude) {
        float currentLatitude = -asin(viewDir.y);
        float currentLongitude = atan2(viewDir.z, viewDir.x);

        currentLatitude = -clamp(currentLatitude, (_LatitudeOffset - _LatitudeSamples) * _LatitudeStep * DEG2RAD, (_LatitudeOffset + _LatitudeSamples) * _LatitudeStep * DEG2RAD);

        float3 clampedViewDir;
        clampedViewDir.y = sin(currentLatitude);
        float cosLatitude = cos(currentLatitude);
        clampedViewDir.x = cosLatitude * cos(currentLongitude);
        clampedViewDir.z = cosLatitude * sin(currentLongitude);
        viewDir = clampedViewDir;
    }
    float3 M2 = mul((float3x3)UNITY_MATRIX_I_M, viewDir);
    float3 M0 = cross(mul((float3x3)UNITY_MATRIX_I_M, float3(0, 1, 0)), M2);
    float3 M1 = cross(M2, M0);
    float3x3 mat = float3x3(normalize(M0), normalize(M1), M2);

    if (_ZOffset > 0) {
        float trYawOffset = atan2(UNITY_MATRIX_M._31, UNITY_MATRIX_M._11);
        float3x3 rotationMatrix = float3x3(
            cos(trYawOffset), 0, sin(trYawOffset),
            0, 1, 0,
            -sin(trYawOffset), 0, cos(trYawOffset)
            );
        float t = UNITY_MATRIX_P._m11;
        float fov = atan(1.0f / t) * 2.0 / DEG2RAD;
        vertex.xyz *= (1.0 - _ZOffset / length(viewVec)); // Scaling to keep the same pixel size than the original object
        vertex.xyz = mul(vertex.xyz, mat) - mul(rotationMatrix, viewDir) * _ZOffset;
    }
    else
        vertex.xyz = mul(vertex.xyz, mat);

    normal.xyz = mul(normal.xyz, mat);
    tangent.xyz = mul(tangent.xyz, mat);
    return;
}

void Billboarding_float(in float3 vertex, in float3 normal, in float3 tangent, out float3 vertexOut, out float3 normalOut, out float3 tangentOut) {
    vertexOut = vertex;
    normalOut = normal;
    tangentOut = tangent;
    Billboarding(vertexOut, normalOut, tangentOut);
}

#endif