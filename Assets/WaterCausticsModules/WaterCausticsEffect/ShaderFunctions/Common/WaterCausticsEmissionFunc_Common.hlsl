// WaterCausticsModules
// Copyright (c) 2021 Masataka Hakozaki

#ifndef WCE_CUSTOM_FUNCTION_COMMON_INCLUDED
#define WCE_CUSTOM_FUNCTION_COMMON_INCLUDED

#include "../../Effect/Shader/WaterCausticsEffectCommon.hlsl"


CBUFFER_START(_WCECF__)
    float _WCECF__Scale;
    float _WCECF__WaterSurfaceY;
    float _WCECF__WaterSurfaceAttenWide;
    float _WCECF__WaterSurfaceAttenOffset;
    half _WCECF__IntensityMainLit;
    half _WCECF__IntensityAddLit;
    float2 _WCECF__ColorShift;
    half _WCECF__LitSaturation;
    half _WCECF__MulOpaqueIntensity;
    half _WCECF__NormalAttenIntensity;
    half _WCECF__NormalAttenPower;
    half _WCECF__TransparentBackside;
    float4x4 _WCECF__WldObjMatrixOfVolume;
    int _WCECF__ClipOutsideVolume;
    int _WCECF__UseImageMask;
CBUFFER_END

#if defined(WCE_USE_SAMPLER2D_INSTEAD_TEXTURE2D)
    sampler2D _WCECF__CausticsTex;
    sampler2D _WCECF__ImageMaskTex;
    #define WCE_TEX_PARAMS_RAW(texName) texName
    #define WCE_TEX_SAMPLE_RAW(texName, uv) tex2D(texName, uv)
#else
TEXTURE2D(_WCECF__CausticsTex);
TEXTURE2D(_WCECF__ImageMaskTex);
SAMPLER(sampler_WCECF__CausticsTex);
SAMPLER(sampler_WCECF__ImageMaskTex);
#define WCE_TEX_PARAMS_RAW(texName) texName, sampler##texName
#define WCE_TEX_SAMPLE_RAW(texName, uv) texName.Sample(sampler##texName, (uv))
#endif


half3 WCE_emissionSyncCore(float3 WorldPos, half3 NormalWS, half3 BaseColor, half intensity = 1)
{
    half3 c = WCE_waterCausticsEmission(WorldPos, NormalWS, WCE_TEX_PARAMS_RAW(_WCECF__CausticsTex),
                                        _WCECF__Scale, _WCECF__WaterSurfaceY,
                                        _WCECF__WaterSurfaceY + _WCECF__WaterSurfaceAttenOffset,
                                        _WCECF__WaterSurfaceAttenWide, _WCECF__IntensityMainLit * intensity,
                                        _WCECF__IntensityAddLit * intensity,
                                        _WCECF__ColorShift, _WCECF__LitSaturation, _WCECF__NormalAttenIntensity,
                                        _WCECF__NormalAttenPower, _WCECF__TransparentBackside);

    c *= 1 - (1 - BaseColor) * _WCECF__MulOpaqueIntensity;
    return c;
}

half3 WCE_waterCausticsEmissionSync(float3 WorldPos, half3 NormalWS, half3 BaseColor)
{
    [branch] if (_WCECF__ClipOutsideVolume != 0 || _WCECF__UseImageMask != 0)
    {
        float3 posO = mul(_WCECF__WldObjMatrixOfVolume, float4(WorldPos, 1)).xyz;
        [branch] if (_WCECF__ClipOutsideVolume != 0 && (abs(posO.x) > 0.5 || abs(posO.y) > 0.5 || abs(posO.z) > 0.5))
        {
            return half3(0, 0, 0);
        }
        else
        {
            [branch] if (_WCECF__UseImageMask != 0)
            {
                half imageMask = WCE_TEX_SAMPLE_RAW(_WCECF__ImageMaskTex, posO.xz + 0.5).r;
                [branch] if (imageMask < 0.0001)
                {
                    return half3(0, 0, 0);
                }
                else
                {
                    return WCE_emissionSyncCore(WorldPos, NormalWS, BaseColor, imageMask);
                }
            }
            else
            {
                return WCE_emissionSyncCore(WorldPos, NormalWS, BaseColor);
            }
        }
    }
    else
    {
        return WCE_emissionSyncCore(WorldPos, NormalWS, BaseColor);
    }
}

#endif
