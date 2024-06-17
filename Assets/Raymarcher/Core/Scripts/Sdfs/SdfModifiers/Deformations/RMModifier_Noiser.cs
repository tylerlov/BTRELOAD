using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Noiser")]
    public sealed class RMModifier_Noiser : RMObjectModifierBase
    {
        public float noiseAmountX = 0;
        public float noiseAmountY = 0;
        public float noiseAmountZ = 0;
        [Range(0f, 1f)] public float noiseBlend = 0.0f;

        public override string SdfMethodBody =>
@"p.x *= lerp(1, HASH1(p.x) * NoiseData.x, NoiseData.w);
p.y *= lerp(1, HASH1(p.y) * NoiseData.y, NoiseData.w);
p.z *= lerp(1, HASH1(p.z) * NoiseData.z, NoiseData.w);";

        public override string SdfMethodName => "NoiserModifier";

        private const string NOISE_DATA = "NoiseData";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields =>
            new ISDFEntity.SDFUniformField[1]
            {
                new ISDFEntity.SDFUniformField(NOISE_DATA, ISDFEntity.SDFUniformType.Float4)
            };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(NOISE_DATA + iterationIndex, new Vector4(noiseAmountX, noiseAmountY, noiseAmountZ, noiseBlend));
        }

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;
    }
}