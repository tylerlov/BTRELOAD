using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Mirror")]
    public sealed class RMModifier_Mirror : RMObjectModifierBase
    {
        [Space]
        public bool mirrorX = true;
        public bool mirrorY = true;
        public bool mirrorZ = true;
        public Vector3 mirrorSpacing = Vector3.one;

        public override string SdfMethodBody => 
@"
p.x = abs(p.x); p.x -= mirrorSpacing.x;
p.y = abs(p.y); p.y -= mirrorSpacing.y;
p.z = abs(p.z); p.z -= mirrorSpacing.z;
";
        
        public override string SdfMethodName => "MirrorModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(nameof(mirrorSpacing), ISDFEntity.SDFUniformType.Float3)
        };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(nameof(mirrorSpacing) + iterationIndex, 
                new Vector3(mirrorSpacing.x * (mirrorX ? 1 : 0), mirrorSpacing.y * (mirrorY ? 1 : 0), mirrorSpacing.z * (mirrorZ ? 1 : 0)));
        }
    }
}