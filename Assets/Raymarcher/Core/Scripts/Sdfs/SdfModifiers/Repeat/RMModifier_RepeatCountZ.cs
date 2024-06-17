using UnityEngine;

using Raymarcher.Constants;

namespace Raymarcher.Objects.Modifiers
{
    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Repeat Count Z")]
    public sealed class RMModifier_RepeatCountZ : RMModifier_RepeatBase
    {
        public int repeatCount = 4;

        private const string SECOND_PARAM_CONST = nameof(repeatCount);

        private const RepeatAxis Axis = RepeatAxis.z;
        private readonly string AxisStr = Axis.ToString();

        public override ISDFEntity.SDFUniformField[] SdfUniformFields
            => GetSdfUniformFields(AxisStr, SECOND_PARAM_CONST);

        public override string SdfMethodBody
            => GetRepeatCountMethod(Axis, SECOND_PARAM_CONST);

        public override string SdfMethodName => AxisStr + "RepeatCountModifier";

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
            => PushRepeatEntityToShader(raymarcherSceneMaterial, iterationIndex, AxisStr, SECOND_PARAM_CONST, Mathf.Abs(repeatCount));
    }
}