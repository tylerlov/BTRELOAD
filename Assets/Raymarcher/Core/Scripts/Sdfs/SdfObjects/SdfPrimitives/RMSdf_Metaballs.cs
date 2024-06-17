using UnityEngine;

namespace Raymarcher.Objects.Primitives
{
    public sealed class RMSdf_Metaballs : RMSdfObjectBase
    {
        public float metaballRadius = 0.1f;
        public float metaballSizeVariation = 0.1f;
        [Space]
        public float metaballSpacing = 2.0f;
        public float metaballMaxHeight = 0.2f;
        [Space]
        public float metaballFloatingSpeed = 0.5f;
        [Space]
        public float metaballSmoothness = 0.3f;

        private const string METALBALL_DATA0 = "metaBallTransform";
        private const string METALBALL_DATA1 = "metaBallFloating";

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Metaballs")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_Metaballs>();
#endif

        public override string SdfMethodBody =>
@"
float t = _Time.y * metaBallFloating.x;
float ball0 = length(p + half3(-0.2 - sin(t / 1.5) * 0.2, metaBallFloating.y, 0.2 + cos(t / 0.5) * 0.2) * sin(t / 0.89) * metaBallTransform.z) - metaBallTransform.x - ((1.8 + cos(t)) * metaBallTransform.w);
float ball1 = length(p + half3(0.2 + cos(t / 0.49) * 0.2, metaBallFloating.y, -0.2 + sin(t / 1.24) * 0.2) * cos(t / 1.32) * metaBallTransform.z) - metaBallTransform.x - ((1.5 + sin(t / 2.8)) * metaBallTransform.w);
float ball2 = length(p + half3(0.1 + cos(t / 2.19) * 0.1, metaBallFloating.y, 0.1 + sin(t / 2.0) * 0.1) * cos(t / 2.0) * metaBallTransform.z) - metaBallTransform.x - ((1.2 + sin(t / 3.4)) * metaBallTransform.w);
float metaballs = SmoothUnion1(ball2, SmoothUnion1(ball0, ball1, metaBallTransform.y), metaBallTransform.y);
result = metaballs;";
        
        public override string SdfMethodName => "GetMetaBallsSdf";
        
        public override ISDFEntity.SDFUniformField[] SdfUniformFields =>
            new ISDFEntity.SDFUniformField[]
            {
                new ISDFEntity.SDFUniformField(METALBALL_DATA0, ISDFEntity.SDFUniformType.Float4),
                 new ISDFEntity.SDFUniformField(METALBALL_DATA1, ISDFEntity.SDFUniformType.Float2)
            };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            raymarcherSceneMaterial.SetVector(METALBALL_DATA0 + iterationIndex, new Vector4(Mathf.Abs(metaballRadius), Mathf.Abs(metaballSmoothness), metaballSpacing, Mathf.Abs(metaballSizeVariation)));
            raymarcherSceneMaterial.SetVector(METALBALL_DATA1 + iterationIndex, new Vector2(metaballFloatingSpeed, metaballMaxHeight));
        }
    }
}