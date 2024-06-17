using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Fragment")]
    public sealed class RMModifier_Fragment : RMObjectModifierBase
    {
        public Vector3 fragmentTiling = Vector3.one * 5;
        public float fragmentEvolution = 0.1f;
        [Space]
        public Vector3 fragmentScroll = Vector3.zero;

        private const string FRAGMENT_TILING = nameof(fragmentTiling);
        private const string FRAGMENT_SCROLL = nameof(fragmentScroll);

        public override string SdfMethodBody =>
$@"float frg = (sin({FRAGMENT_TILING}.x * ({VARCONST_POSITION}.x + _Time.y * {FRAGMENT_SCROLL}.x))
* sin({FRAGMENT_TILING}.y * ({VARCONST_POSITION}.y + _Time.y * {FRAGMENT_SCROLL}.y))
* sin({FRAGMENT_TILING}.z * ({VARCONST_POSITION}.z + _Time.y * {FRAGMENT_SCROLL}.z))) * {FRAGMENT_TILING}.w;
{VARCONST_SDF} += frg;";

        public override string SdfMethodName => "FragmentModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields =>
            new ISDFEntity.SDFUniformField[2]
            {
                new ISDFEntity.SDFUniformField(FRAGMENT_TILING, ISDFEntity.SDFUniformType.Float4),
                new ISDFEntity.SDFUniformField(FRAGMENT_SCROLL, ISDFEntity.SDFUniformType.Float3)
            };

        public override void PushSdfEntityToShader(in Material raymarcherSessionMaterial, in string iterationIndex)
        {
            raymarcherSessionMaterial.SetVector(FRAGMENT_TILING + iterationIndex,
                new Vector4(fragmentTiling.x, fragmentTiling.y, fragmentTiling.z, fragmentEvolution));
            raymarcherSessionMaterial.SetVector(FRAGMENT_SCROLL + iterationIndex, fragmentScroll);
        }
    }
}