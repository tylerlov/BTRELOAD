using System;

namespace Raymarcher.Objects.Modifiers
{
    [Serializable]
    [UnityEngine.AddComponentMenu(Constants.RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Displace Radial")]
    public sealed class RMModifier_DisplacementRadial : RMModifier_DisplacementBase
    {
        public override string SdfMethodExtension
            =>
            @"
#ifndef SPHERE_TO_UV
#define SPHERE_TO_UV
    float2 SphereToUV(float3 sphereCoord)
    {
        float r = length(sphereCoord);
        float theta = acos(sphereCoord.y / r);
        float phi = atan2(sphereCoord.z, sphereCoord.x);
        return float2(phi / PI2x, 1.0 - (theta / 3.14159));
    }
#endif";

        public override string SdfMethodBody => $"{VARCONST_POSITION}.xy -= " +
            $"RM_SAMPLE_TEXTURE2D({nameof(DisplaceModifier.displaceTexture)}, SphereToUV({VARCONST_POSITION}) * {nameof(DisplaceModifier.textureTiling)}).r * {nameof(DisplaceModifier.displacementAmount)};";

        public override string SdfMethodName => "DisplaceModifierRadial";
    }
}