#if HLSL
    #define SAMPLE(texture, sampler, uv) SAMPLE_TEXTURE2D_X(texture, sampler, uv)
#else
    #define SAMPLE(texture, sampler, uv) UNITY_SAMPLE_SCREENSPACE_TEXTURE(texture, uv)
#endif

#ifdef BLOCK_SIZE_4
    #define BLOCK_SIZE 4
#endif
#ifdef BLOCK_SIZE_8
    #define BLOCK_SIZE 8
#endif
#ifdef BLOCK_SIZE_16
    #define BLOCK_SIZE 16
#endif
#ifndef BLOCK_SIZE
    #define BLOCK_SIZE 4
#endif


//
float basis1D(float k, float i)
{
    float4 _G = float4(2, 1, 2, 2);
    float _Contrast = 0.0;
    return k == 0 ? sqrt(1. / float(BLOCK_SIZE)) : sqrt((_G.w + _Contrast) / float(BLOCK_SIZE)) * cos(float((_G.x * float(i) + _G.y) * k) * 3.14159265358 / (_G.z * float(BLOCK_SIZE)));
}
float basis2D(float2 jk, float2 xy)
{
    return basis1D(jk.x, xy.x) * basis1D(jk.y, xy.y);
}
float4 jpg(float2 uv, int m)
{
    float _Quality = 4.0;
    float quality = length(float2(_Quality, _Quality));
    float4 outColor = float4(0, 0, 0, 1);
    
    float2 textureSize = _Downscaled_TexelSize.zw;
    textureSize = floor(textureSize / 2.0) * 2.0;

    float2 coords = int2(textureSize * uv);
    float2 inBlock = coords % BLOCK_SIZE - m * 0.5;
    float2 block = coords - inBlock;

    // Somehow a bit faster than [unroll]
    [loop]
    for (int2 xy = 0; xy.x < BLOCK_SIZE; xy.x++)
    {
        [loop]
        for (xy.y = 0; xy.y < BLOCK_SIZE; xy.y++)
        {
            outColor += SAMPLE(_Input, sampler_LinearClamp, float2(block + xy) / textureSize)
                            * basis2D(lerp(inBlock, xy, m), lerp(inBlock, xy, 1.0 - m));
        }
    }
    outColor *= lerp(step(length(float2(inBlock)), quality), 1.0, m);
    return outColor;
}
//


float4 Downscale_Frag(Varyings input) : SV_Target
{
    return SAMPLE(_Input, sampler_LinearClamp, input.uv);
}

float4 Encode_Frag(Varyings input) : SV_Target
{
    float4 col = jpg(input.uv, 0);
    
    if(_ColorCrunch == 0.0) return col;
    #ifndef COLOR_CRUNCH_SKYBOX
        float depth = SAMPLE(_CameraDepthTexture, sampler_LinearClamp, input.uv).x; 
        if (depth == 0.0 || depth == 1.0) return col;
    #endif
    
    float truncation = log10(lerp(1.0, 0.0001, _ColorCrunch));
    return round(col / truncation) * truncation;
}

float4 Decode_Frag(Varyings input) : SV_Target
{
    float4 col = jpg(input.uv, 1);
    col.a = 1.0;
    return col;
}

float hash1(uint n) 
{
    n++;
    n = (n << 13U) ^ n;
    n = n * (n * n * 15731U + 789221U) + 1376312589U;
    return float(n & uint(0x7fffffffU))/float(0x7fffffff);
}
float4 Upscale_Pull_Frag(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    
    float3 center = SAMPLE(_Input, sampler_LinearClamp, uv + _Downscaled_TexelSize.xy * float2(0, 0)).rgb;
    float3 col;
    if (_Sharpening > 0.0)
    {
        float3 up = SAMPLE(_Input, sampler_LinearClamp, uv + _Downscaled_TexelSize.xy * float2(0, 1)).rgb;
        float3 left = SAMPLE(_Input, sampler_LinearClamp, uv + _Downscaled_TexelSize.xy * float2(-1, 0)).rgb;
        float3 right = SAMPLE(_Input, sampler_LinearClamp, uv + _Downscaled_TexelSize.xy * float2(1, 0)).rgb;
        float3 down = SAMPLE(_Input, sampler_LinearClamp, uv + _Downscaled_TexelSize.xy * float2(0, -1)).rgb;
        _Sharpening *= 2.0;
        col = (1.0 + 4.0 * _Sharpening) * center - _Sharpening * (up + left + right + down);
    }
    else
    {
        col = center;
    }

    #ifdef VIZ_MOTION_VECTORS
        return 0.5 + float4(SAMPLE(_MotionVectorTexture, sampler_LinearClamp, uv).xy * float2(1920, 1080), 0.0, 1.0);
    #endif

    #ifdef REPROJECTION
        float2 snappedUV = (floor(uv / (_Downscaled_TexelSize.xy * BLOCK_SIZE)) + 0.5) * (_Downscaled_TexelSize.xy * BLOCK_SIZE);
        int2 blockID = floor(uv / (_Downscaled_TexelSize.xy * BLOCK_SIZE));
        
        float2 motionVector = SAMPLE(_MotionVectorTexture, sampler_LinearClamp, snappedUV).xy;
        float3 pull = SAMPLE(_PrevScreen, sampler_LinearClamp, uv - motionVector).rgb;
        if (all(pull == 0.0)) return float4(col, 1.0);
        
        //if (blockID.x % 2 == 0)
        /*working in full_hd uv pixel scale units so stays consistent across resolutions and scale for user more sensible*/
        if (hash1((123 + blockID.x) * (456 + blockID.y) + (_Time.y * _ReprojectSpeed)) < _ReprojectPercent + min(length(motionVector * float2(1920, 1080)) * _ReprojectLengthInfluence, 0.7))
            return float4(pull, 1.0);
    #endif
    
    return float4(col, 1.0);
}

float4 CopyToPrev_Frag(Varyings input) : SV_Target
{
    return SAMPLE(_Input, sampler_LinearClamp, input.uv);
}