using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Active State")]
    public sealed class RMModifier_ActiveState : RMObjectModifierBase
    {
        private string iterIndex;
        private Material rmMaterial;

        private const string PARAM = "activeState";

        public override string SdfMethodBody
            => "sdf = lerp(sdf, activeState, saturate(activeState));";

        public override string SdfMethodName => "ActiveStateModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[1]
        {
            new ISDFEntity.SDFUniformField(PARAM, ISDFEntity.SDFUniformType.Float),
        };

        public override InlineMode ModifierInlineMode() => InlineMode.PostSdfInstance;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            iterIndex = iterationIndex;
            rmMaterial = raymarcherSceneMaterial;
        }

        private void OnEnable()
        {
            if(rmMaterial)
                rmMaterial.SetFloat(PARAM + iterIndex, 0);
        }

        private void OnDisable()
        {
            if (!rmMaterial)
                return;
            if (!int.TryParse(iterIndex, out int myIndex))
                return;
            rmMaterial.SetFloat(PARAM + iterIndex, myIndex + 1);
        }
    }
}