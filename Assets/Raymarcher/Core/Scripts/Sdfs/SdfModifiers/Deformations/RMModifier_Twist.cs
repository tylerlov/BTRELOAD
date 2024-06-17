using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Twist")]
    public sealed class RMModifier_Twist : RMObjectModifierBase
    {
        [Space]
        public float twistTileX = 2.0f;
        public float twistTileY = 2.0f;
        public float twistTileZ = 2.0f;
        [Space]
        [Range(0.0f,1.0f)] public float twistMultiplierX = 0.2f;
        [Range(0.0f, 1.0f)] public float twistMultiplierY = 0.2f;
        [Range(0.0f, 1.0f)] public float twistMultiplierZ = 0.2f;
        [Space]
        public float twistScrollX = 0.0f;
        public float twistScrollY = 0.0f;
        public float twistScrollZ = 0.0f;

        private const string TWIST_TILE = "twistTile";
        private const string TWIST_MULTI = "twistMultiplier";
        private const string TWIST_SCROLL = "twistScroll";

        public override string SdfMethodBody =>
@"
float3 m0 = lerp(0.0,0.05,twistMultiplier.y);
float3 m1 = lerp(0.0,0.05,twistMultiplier.x);
float3 m2 = lerp(0.0,0.05,twistMultiplier.z);

p.xz += mul(p.xz+5.0, Rot(p.y*twistTile.y+_Time.y*twistScroll.y)) * m0;
p.yz += mul(p.yz+5.0, Rot(p.x*twistTile.x+_Time.y*twistScroll.x)) * m1;
p.xy += mul(p.xy+5.0, Rot(p.z*twistTile.z+_Time.y*twistScroll.z)) * m2;
";

        public override string SdfMethodExtension => "#ifndef Rot\n#define Rot(a) float2x2(cos(a),-sin(a),sin(a),cos(a))\n#endif";

        public override string SdfMethodName => "TwistModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[3]
        {
            new ISDFEntity.SDFUniformField(TWIST_TILE, ISDFEntity.SDFUniformType.Float3),
            new ISDFEntity.SDFUniformField(TWIST_MULTI, ISDFEntity.SDFUniformType.Float3),
            new ISDFEntity.SDFUniformField(TWIST_SCROLL, ISDFEntity.SDFUniformType.Float3)
        };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(TWIST_TILE + iterationIndex, new Vector4(twistTileX, twistTileY, twistTileZ));
            raymarcherSceneMaterial.SetVector(TWIST_MULTI + iterationIndex, new Vector4(twistMultiplierX, twistMultiplierY, twistMultiplierZ));
            raymarcherSceneMaterial.SetVector(TWIST_SCROLL + iterationIndex, new Vector4(twistScrollX, twistScrollY, twistScrollZ));
        }
    }
}