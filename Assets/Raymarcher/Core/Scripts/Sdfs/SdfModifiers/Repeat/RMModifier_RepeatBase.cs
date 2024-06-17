using UnityEngine;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    public abstract class RMModifier_RepeatBase : RMObjectModifierBase
    {
        public float spacing = 5;

        protected const string SPACING = nameof(spacing);

        protected enum RepeatAxis { x, y, z };

        public override InlineMode ModifierInlineMode() => InlineMode.SdfInstancePosition;

        protected ISDFEntity.SDFUniformField[] GetSdfUniformFields(string repeatAxisConst, string secondParamConst) => new ISDFEntity.SDFUniformField[2]
        {
            new ISDFEntity.SDFUniformField(SPACING + repeatAxisConst, ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(secondParamConst + repeatAxisConst, ISDFEntity.SDFUniformType.Float)
        };

        protected string GetRepeatMethod(RepeatAxis repeatAxis, string scroll)
            => $"{VARCONST_POSITION}.{repeatAxis} += {scroll}{repeatAxis} * _Time.y;\n" +
                $"float sp = {SPACING}{repeatAxis} * 0.5;\n" +
                $"{VARCONST_POSITION}.{repeatAxis} = fmod({VARCONST_POSITION}.{repeatAxis} + sp, {SPACING}{repeatAxis}) - sp;\n" +
                $"{VARCONST_POSITION}.{repeatAxis} = fmod({VARCONST_POSITION}.{repeatAxis} - sp, {SPACING}{repeatAxis}) + sp;\n";

        protected string GetRepeatCountMethod(RepeatAxis repeatAxis, string repeatCountConst)
            => $"float sp = {SPACING}{repeatAxis} * 0.5;\n" +
                $"{VARCONST_POSITION}.{repeatAxis} = {VARCONST_POSITION}.{repeatAxis} - sp *" +
                $"max(0, min(round({VARCONST_POSITION}.{repeatAxis}.x / sp), {repeatCountConst}{repeatAxis}));\n";

        protected void PushRepeatEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex, in string repeatAxisConst, in string secondParamConst, in float secondParamVal)
        {
            raymarcherSceneMaterial.SetFloat(SPACING + repeatAxisConst + iterationIndex, spacing);
            raymarcherSceneMaterial.SetFloat(secondParamConst + repeatAxisConst + iterationIndex, secondParamVal);
        }
    }
}