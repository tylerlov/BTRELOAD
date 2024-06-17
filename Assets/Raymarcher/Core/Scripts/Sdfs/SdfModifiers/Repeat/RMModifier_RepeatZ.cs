using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Repeat Z")]
    public sealed class RMModifier_RepeatZ : RMModifier_RepeatBase
    {
        public float scroll;

        private const string SECOND_PARAM_CONST = nameof(scroll);

        private const RepeatAxis Axis = RepeatAxis.z;
        private readonly string AxisStr = Axis.ToString();

        public override ISDFEntity.SDFUniformField[] SdfUniformFields
            => GetSdfUniformFields(AxisStr, SECOND_PARAM_CONST);

        public override string SdfMethodBody
            => GetRepeatMethod(Axis, SECOND_PARAM_CONST);

        public override string SdfMethodName => AxisStr + "RepeatModifier";

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
            => PushRepeatEntityToShader(raymarcherSceneMaterial, iterationIndex, AxisStr, SECOND_PARAM_CONST, scroll);
    }
}