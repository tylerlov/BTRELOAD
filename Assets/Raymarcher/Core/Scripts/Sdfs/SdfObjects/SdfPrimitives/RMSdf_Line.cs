using UnityEngine;

namespace Raymarcher.Objects.Primitives
{
    public sealed class RMSdf_Line : RMSdfObjectBase
    {
        public float thicknessA = 1;
        public float thicknessB = 1;
        public Vector3 pointA = Vector3.zero;
        public Vector3 pointB = Vector3.up * 2;
        public Transform pointATransform;
        public Transform pointBTransform;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_PRIMITIVES_PATH + "SDF Line")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_Line>();
#endif
        private const string THICKNESS = "thickness";

        public override string SdfMethodBody =>
            $"float3 ab = {nameof(pointB)} - {nameof(pointA)};\n"+
            $"float h = min(1., max(0., dot(p - {nameof(pointA)}, ab) / dot(ab, ab)));\n" +
            $"{VARCONST_RESULT} = length(p - {nameof(pointA)} - (ab * h)) - lerp({THICKNESS}.x, {THICKNESS}.y, h);";

        public override string SdfMethodName => "GetLineSdf";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields =>
            new ISDFEntity.SDFUniformField[3]
            {
                new ISDFEntity.SDFUniformField(nameof(pointA), ISDFEntity.SDFUniformType.Float3),
                new ISDFEntity.SDFUniformField(nameof(pointB), ISDFEntity.SDFUniformType.Float3),
                new ISDFEntity.SDFUniformField(THICKNESS, ISDFEntity.SDFUniformType.Float2)
            };

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            if (pointATransform)
                pointA = pointATransform.position;
            if (pointBTransform)
                pointB = pointBTransform.position;
            if (pointATransform && pointBTransform)
                transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            raymarcherSceneMaterial.SetVector(nameof(pointA) + iterationIndex, pointA);
            raymarcherSceneMaterial.SetVector(nameof(pointB) + iterationIndex, pointB);
            raymarcherSceneMaterial.SetVector(THICKNESS + iterationIndex, new Vector2(Mathf.Abs(thicknessA), Mathf.Abs(thicknessB)));
        }
    }
}