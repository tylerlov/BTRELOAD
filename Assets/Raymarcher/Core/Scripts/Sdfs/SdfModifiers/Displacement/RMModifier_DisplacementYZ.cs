using System;

namespace Raymarcher.Objects.Modifiers
{
    [Serializable]
    [UnityEngine.AddComponentMenu(Constants.RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Displace YZ")]
    public sealed class RMModifier_DisplacementYZ : RMModifier_DisplacementBase
    {
        public override string SdfMethodBody => $"{VARCONST_POSITION}.xy -= " +
            $"RM_SAMPLE_TEXTURE2D({nameof(DisplaceModifier.displaceTexture)}, {VARCONST_POSITION}.yz * {nameof(DisplaceModifier.textureTiling)}).r * {nameof(DisplaceModifier.displacementAmount)};";

        public override string SdfMethodName => "DisplaceModifierYZ";
    }
}