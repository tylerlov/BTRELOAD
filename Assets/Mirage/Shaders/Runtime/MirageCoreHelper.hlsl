// Copyright (c) 2024 Léo Chaumartin
// The pipeline and platform independant impostor core helper in HLSL

#ifndef MIRAGE_CORE_HELPER
#define MIRAGE_CORE_HELPER

#ifndef PI
#define PI 3.14159
#endif
#ifndef HALF_PI
#define HALF_PI 1.570795
#endif
#ifndef DEG2RAD
#define DEG2RAD 0.01745328
#endif
#ifndef TWO_PI
#define TWO_PI 6.28318
#endif

float3 SaturateColor(float3 color, float saturation)
{
    float gray = dot(color, float3(0.299, 0.587, 0.114));
    return lerp(float3(gray, gray, gray), color, saturation);
}

float3 RGBToHSV(float3 rgb)
{
    float r = rgb.r, g = rgb.g, b = rgb.b;
    float maxVal = max(max(r, g), b);
    float minVal = min(min(r, g), b);
    float delta = maxVal - minVal;

    float h = 0.0;
    float s = 0.0;
    float v = maxVal;

    if (delta != 0.0)
    {
        s = delta / maxVal;

        if (r == maxVal)
            h = (g - b) / delta;
        else if (g == maxVal)
            h = 2 + (b - r) / delta;
        else
            h = 4 + (r - g) / delta;

        h /= 6.0;
        if (h < 0.0)
            h += 1.0;
    }

    return float3(h, s, v);
}

float3 HSVToRGB(float3 hsv)
{
    float h = hsv.x, s = hsv.y, v = hsv.z;
    float r = 0.0, g = 0.0, b = 0.0;

    int i = int(h * 6);
    float f = h * 6 - i;
    float p = v * (1 - s);
    float q = v * (1 - f * s);
    float t = v * (1 - (1 - f) * s);

    if (i == 0) { r = v; g = t; b = p; }
    else if (i == 1) { r = q; g = v; b = p; }
    else if (i == 2) { r = p; g = v; b = t; }
    else if (i == 3) { r = p; g = q; b = v; }
    else if (i == 4) { r = t; g = p; b = v; }
    else { r = v; g = p; b = q; }

    return float3(r, g, b);
}

float Dither(float2 pos, float alpha) {
    float2 uv = pos.xy * _ScreenParams.xy;
    const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    return alpha - DITHER_THRESHOLDS[index];
}


void GetImpostorUV_float(float2 uv_MainTex, float3 viewDir, out float2 gridUv, out float2 gridUvLB, out float2 gridUvRB, out float2 gridUvLT, out float2 gridUvRT, out float alpha, out float beta)
{
    float gridSide = round(sqrt(_GridSize));
    float gridStep = 1.0 / gridSide;
    float3x3 rotationScaleMatrix = float3x3(UNITY_MATRIX_M[0].xyz, UNITY_MATRIX_M[1].xyz, UNITY_MATRIX_M[2].xyz);

    float3x3 rotationOnlyMatrix;
    rotationOnlyMatrix[0] = normalize(rotationScaleMatrix[0]);
    rotationOnlyMatrix[1] = normalize(rotationScaleMatrix[1]);
    rotationOnlyMatrix[2] = normalize(rotationScaleMatrix[2]);
    float trYawOffset = -atan2(rotationOnlyMatrix._31, rotationOnlyMatrix._11);
    float trLatOffset = 0; //TO DO

    float yaw = atan2(viewDir.z, viewDir.x) + TWO_PI + _YawOffset + trYawOffset;
    float elevation = asin(viewDir.y) + _ElevationOffset + trLatOffset;
    float elevationId = (max(min(round(elevation / (_LatitudeStep * DEG2RAD)) - _LatitudeOffset, _LatitudeSamples), -_LatitudeSamples));
    float elevationFrac = frac(elevation / (_LatitudeStep * DEG2RAD));
    float offset = 0;

    float currentLongitudeSamples;
    float lastLongitudeSamples;
    if (_SmartSphere > 0.5) {
        for (int l = -_LatitudeSamples; l < elevationId; ++l) {
            if (l == elevationId - 1) {
                lastLongitudeSamples = round(cos(l * HALF_PI / (_LatitudeSamples + 1.0)) * _LongitudeSamples);
                offset += lastLongitudeSamples;
            }
            else
                offset += round(cos(l * HALF_PI / ((_LatitudeSamples + 1.0))) * _LongitudeSamples);
        }
        currentLongitudeSamples = round(cos(elevationId * _LatitudeStep * DEG2RAD) * _LongitudeSamples);
    }
    else
        currentLongitudeSamples = _LongitudeSamples;
    float yawId = (round((yaw / TWO_PI) * currentLongitudeSamples) % currentLongitudeSamples);
    if (_Smooth > 0.5) {
        gridUv = float2(0.0, 0.0); // unused
        float yawFrac = frac((yaw / TWO_PI) * currentLongitudeSamples);
        float subdLB;
        float subdRB;
        float subdLT;
        float subdRT;
        alpha = 1.0 - yawFrac;
        beta = 1.0 - elevationFrac;
        if (_SmartSphere < 0.5) {
            if (elevationFrac < 0.5) {
                if (yawFrac < 0.5) {
                    subdLB = yawId + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
                    subdRB = (yawId + 1) % currentLongitudeSamples + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
                    subdLT = (yawId + (elevationId + (elevationId == _LatitudeSamples ? 0 : 1) + _LatitudeSamples) * currentLongitudeSamples); ///
                    subdRT = ((yawId + 1) % currentLongitudeSamples + (elevationId + (elevationId == _LatitudeSamples ? 0 : 1) + _LatitudeSamples) * currentLongitudeSamples);
                }
                else {
                    subdLB = (yawId - 1) % currentLongitudeSamples + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
                    subdRB = yawId + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
                    subdLT = ((yawId - 1) % currentLongitudeSamples + (elevationId + (elevationId == _LatitudeSamples ? 0 : 1) + _LatitudeSamples) * currentLongitudeSamples);
                    subdRT = (yawId + (elevationId + (elevationId == _LatitudeSamples ? 0 : 1) + _LatitudeSamples) * currentLongitudeSamples); ///
                }
            }
            else {
                if (yawFrac < 0.5) {
                    subdLB = yawId + (elevationId - (elevationId == -_LatitudeSamples ? 0 : 1) + _LatitudeSamples) * currentLongitudeSamples; ///
                    subdRB = (yawId + 1) % currentLongitudeSamples + (elevationId + _LatitudeSamples - (elevationId == -_LatitudeSamples ? 0 : 1)) * currentLongitudeSamples;
                    subdLT = yawId + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
                    subdRT = ((yawId + 1) % currentLongitudeSamples + (elevationId + _LatitudeSamples) * currentLongitudeSamples);
                }
                else {
                    subdLB = (yawId - 1) % currentLongitudeSamples + (elevationId + _LatitudeSamples - (elevationId == -_LatitudeSamples ? 0 : 1)) * currentLongitudeSamples;
                    subdRB = yawId + (elevationId + _LatitudeSamples - (elevationId == -_LatitudeSamples ? 0 : 1)) * currentLongitudeSamples; ///
                    subdLT = ((yawId - 1) % currentLongitudeSamples) + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
                    subdRT = yawId + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
                }
            }
        }
        else {
            if (elevationFrac < 0.5) {
                if (yawFrac < 0.5) {
                    subdLB = yawId + offset;
                    subdRB = (yawId + 1) % currentLongitudeSamples + offset;
                    subdLT = subdLB;
                    subdRT = subdRB;
                }
                else {
                    subdLB = (yawId - 1) % currentLongitudeSamples + offset;
                    subdRB = yawId + offset;
                    subdLT = subdLB;
                    subdRT = subdRB;
                }
            }
            else {
                if (yawFrac < 0.5) {
                    subdLT = yawId + offset;
                    subdRT = ((yawId + 1) % currentLongitudeSamples + offset);
                    subdLB = subdLT;
                    subdRB = subdRT;
                }
                else {
                    subdLT = ((yawId - 1) % currentLongitudeSamples) + offset;
                    subdRT = yawId + offset;
                    subdLB = subdLT;
                    subdRB = subdRT;
                }
            }
        }
        gridUvLB = uv_MainTex / gridSide + float2((subdLB % gridSide), floor(subdLB / gridSide)) * gridStep;
        gridUvRB = uv_MainTex / gridSide + float2((subdRB % gridSide), floor(subdRB / gridSide)) * gridStep;
        gridUvRT = uv_MainTex / gridSide + float2((subdRT % gridSide), floor(subdRT / gridSide)) * gridStep;
        gridUvLT = uv_MainTex / gridSide + float2((subdLT % gridSide), floor(subdLT / gridSide)) * gridStep;
    }
    else {
        gridUvLB = gridUvLT = gridUvRB = gridUvRT = float2(0.0, 0.0); // unused
        alpha = beta = 0.0; // unused
        float subd = (_SmartSphere < 0.5 ? yawId + (elevationId + _LatitudeSamples) * currentLongitudeSamples : yawId + offset);
        gridUv = uv_MainTex / gridSide + float2((subd % gridSide), floor(subd / gridSide)) * gridStep;
    }
}
#endif