using System.Linq;

using UnityEngine;

using Raymarcher.Toolkit;
using Raymarcher.Attributes;

namespace Raymarcher.Objects.Volumes
{
    using static RMAttributes;

    public sealed class RMSdf_PDVolumeBox : RMSdf_VolumeBoxBase
    {
        [Space]
        public RMVolumeRendererPD volumeRenderer;
        [Space]
        [ShowIf("volumeRenderer", 1, false, true)] public Texture volumeTextureTop;
        [ShowIf("volumeRenderer", 1, false, true)] public Texture volumeTextureForward;
        [ShowIf("volumeRenderer", 1, false, true)] public Texture volumeTextureRight;

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_VOLUMES_PATH + "Perspective Driven (PD) Volume Box")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_PDVolumeBox>();
#endif

        public override string SdfMethodBody =>
            $"half2 volCoords = ({VARCONST_POSITION}.xz / {nameof(volumeSize)}.xz + 1.0) * 0.5;\n" +
            $"float volTexT = RM_SAMPLE_TEXTURE2D({nameof(volumeTextureTop)}, volCoords).r;\n" +
            $"volCoords = ({VARCONST_POSITION}.xy / {nameof(volumeSize)}.xy + 1.0) * 0.5;\n" +
            $"float volTexF = RM_SAMPLE_TEXTURE2D({nameof(volumeTextureForward)}, volCoords).r;\n" +
            $"volCoords = ({VARCONST_POSITION}.zy / {nameof(volumeSize)}.zy + 1.0) * 0.5;\n" +
            $"float volTexR = RM_SAMPLE_TEXTURE2D({nameof(volumeTextureRight)}, volCoords).r;\n\n" +

            $"float3 d = abs({VARCONST_POSITION}) - {nameof(volumeSize)};\n" +
            $"float bsdf = min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));\n\n" +

            $"float t = 1 - (saturate(volTexT * volTexF * volTexR) * {nameof(volumeAmplifier)});\n" +
            $"{VARCONST_RESULT} = max(bsdf, t) / max(EPSILONZEROFIVE, {nameof(volumePrecision)} / {nameof(volumeSize)}.x);";

        public override string SdfMethodName => "GetPerspectiveDrivenVolumeSdf";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields
        {
            get
            {
                ISDFEntity.SDFUniformField[] additionalFields = new ISDFEntity.SDFUniformField[3]
                {
                    new ISDFEntity.SDFUniformField(nameof(volumeTextureTop), ISDFEntity.SDFUniformType.Sampler2D),
                    new ISDFEntity.SDFUniformField(nameof(volumeTextureForward), ISDFEntity.SDFUniformType.Sampler2D),
                    new ISDFEntity.SDFUniformField(nameof(volumeTextureRight), ISDFEntity.SDFUniformType.Sampler2D),
                };
                return base.SdfUniformFields.Concat(additionalFields).ToArray();
            }
        }

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            base.PushSdfEntityToShader(raymarcherSceneMaterial, iterationIndex);

            if(volumeRenderer)
            {
                raymarcherSceneMaterial.SetTexture(nameof(volumeTextureTop) + iterationIndex, volumeRenderer.RTTop);
                raymarcherSceneMaterial.SetTexture(nameof(volumeTextureRight) + iterationIndex, volumeRenderer.RTRight);
                raymarcherSceneMaterial.SetTexture(nameof(volumeTextureForward) + iterationIndex, volumeRenderer.RTFront);
                return;
            }
            raymarcherSceneMaterial.SetTexture(nameof(volumeTextureTop) + iterationIndex, volumeTextureTop);
            raymarcherSceneMaterial.SetTexture(nameof(volumeTextureForward) + iterationIndex, volumeTextureForward);
            raymarcherSceneMaterial.SetTexture(nameof(volumeTextureRight) + iterationIndex, volumeTextureRight);
        }
    }
}