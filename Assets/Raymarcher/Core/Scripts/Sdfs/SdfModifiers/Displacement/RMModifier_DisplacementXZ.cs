using System;

namespace Raymarcher.Objects.Modifiers
{
    [Serializable]
    [UnityEngine.AddComponentMenu(Constants.RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Displace XZ")]
    public sealed class RMModifier_DisplacementXZ : RMModifier_DisplacementBase
    {
        public override string SdfMethodBody => $"{VARCONST_POSITION}.xy -= " +
            $"RM_SAMPLE_TEXTURE2D({nameof(DisplaceModifier.displaceTexture)}, {VARCONST_POSITION}.xz * {nameof(DisplaceModifier.textureTiling)}).r * {nameof(DisplaceModifier.displacementAmount)};";

        public override string SdfMethodName => "DisplaceModifierXZ";
    }
}