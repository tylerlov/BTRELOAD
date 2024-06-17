using UnityEngine;

using System.Linq;

namespace Raymarcher.Objects.Volumes
{
    public sealed class RMSdf_Tex3DVolumeBox : RMSdf_VolumeBoxBase
    {
        [Space]
        [SerializeField] private Texture volumeTexture;

        public Texture VolumeTexture
        {
            set
            {
                if (volumeTexture != null && volumeTexture is RenderTexture rt)
                    rt.Release();
                volumeTexture = value;
            }
            get => volumeTexture;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem(Constants.RMConstants.RM_EDITOR_OBJECT_VOLUMES_PATH + "Texture 3D Volume Box")]
        private static void CreateSdfObjectInEditor() => CreateSDFObject<RMSdf_Tex3DVolumeBox>();
#endif

        public override string SdfMethodBody =>
            $"half3 volCoords = ({VARCONST_POSITION}.xyz / {nameof(volumeSize)}.xyz + 1.0) * 0.5;\n" +
            $"float volTex = RM_SAMPLE_TEXTURE3D({nameof(volumeTexture)}, volCoords).r;\n" +

            $"float3 d = abs({VARCONST_POSITION}) - {nameof(volumeSize)};\n" +
            "float bsdf = min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));\n\n" +

            $"{VARCONST_RESULT} = max(bsdf, 1 - volTex * {nameof(volumeAmplifier)}) / max(EPSILONZEROFIVE, {nameof(volumePrecision)} / {nameof(volumeSize)}.x);";

        public override string SdfMethodName => "GetTexture3DVolumeSdf";
        
        public override ISDFEntity.SDFUniformField[] SdfUniformFields
        {
            get
            {
                ISDFEntity.SDFUniformField[] additionalFields = new ISDFEntity.SDFUniformField[1]
                {
                   new ISDFEntity.SDFUniformField(nameof(volumeTexture), ISDFEntity.SDFUniformType.Sampler3D),
                };
                return base.SdfUniformFields.Concat(additionalFields).ToArray();
            }
        }

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            base.PushSdfEntityToShader(raymarcherSceneMaterial, iterationIndex);
            raymarcherSceneMaterial.SetTexture(nameof(volumeTexture) + iterationIndex, volumeTexture);
        }
    }
}