using UnityEngine;

using Raymarcher.Constants;
using Raymarcher.Objects.Volumes;
using Raymarcher.Materials;
using Raymarcher.Attributes;

namespace Raymarcher.Objects.Modifiers
{
    using static RMAttributes;

    [System.Serializable]
    [AddComponentMenu(RMConstants.RM_EDITOR_OBJECT_MODIFIERS_PATH + "RM Volume Material Compositor")]
    public sealed class RMModifier_VolumeMaterialCompositor : RMObjectModifierBase, ISDFModifierMaterialHandler
    {
        [Space]
        [SerializeField] private RMSdf_Tex3DVolumeBox targetVolumeBoxTex3D;
        [SerializeField] private bool roundDownIndex = false;
        [Header("Common Material Family")]
        [SerializeField] private RMMaterialBase material1;
        [SerializeField] private RMMaterialBase material2;
        [SerializeField] private RMMaterialBase material3;
        [SerializeField] private RMMaterialBase material4;

        [Space]
        [SerializeField, ReadOnly] private int materialFamilyTypeIndex = -1;
        [SerializeField, ReadOnly] private int materialFamilyTotalCount = -1;
#if UNITY_EDITOR
        [SerializeField, Button("Refresh Material Identity", "RefreshMaterialIndexAndType")] private int TEMP_BUTTON0;
#endif

        [SerializeField, HideInInspector] private RMMaterialBase material1Cache;
        [SerializeField, HideInInspector] private RMMaterialBase material2Cache;
        [SerializeField, HideInInspector] private RMMaterialBase material3Cache;
        [SerializeField, HideInInspector] private RMMaterialBase material4Cache;

        public RMMaterialBase Material1 { get => material1; set => material1 = value; }
        public RMMaterialBase Material2 { get => material2; set => material2 = value; }
        public RMMaterialBase Material3 { get => material3; set => material3 = value; }
        public RMMaterialBase Material4 { get => material4; set => material4 = value; }

        public int MaterialFamilyTypeIndex => materialFamilyTypeIndex;
        public int MaterialFamilyTotalCount => materialFamilyTotalCount;

        private const string PARAM_VOLSIZE = "volumeSizeCompositor";
        private const string PARAM_VOL3DTEX = "volume3DTextureCompositor";
        private const string PARAM_MATTYPE = "materialTypeIndexCompositor";
        private const string PARAM_MATCOUNT = "materialCountInUseCompositor";
        private const string PARAM_VOLPOS = "VolumeMatCompositor_SdfWPosition";
        private const string PARAM_VOLROT = "VolumeMatCompositor_SdfWRotation";

#if UNITY_EDITOR
        private void Reset()
        {
            targetVolumeBoxTex3D = GetComponent<RMSdf_Tex3DVolumeBox>();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (targetVolumeBoxTex3D == null)
                return;

            if (material1Cache != material1)
                targetVolumeBoxTex3D.AlterMaterialInstance(material1, ref material1Cache);
            if (material2Cache != material2)
                targetVolumeBoxTex3D.AlterMaterialInstance(material2, ref material2Cache);
            if (material3Cache != material3)
                targetVolumeBoxTex3D.AlterMaterialInstance(material3, ref material3Cache);
            if (material4Cache != material4)
                targetVolumeBoxTex3D.AlterMaterialInstance(material4, ref material4Cache);
        }
#endif

        public override string SdfMethodBody =>
            "#ifndef RAYMARCHER_TYPE_PERFORMANT\n" +
            $"  {VARCONST_POSITION} = mul(float4({VARCONST_POSITION} - {PARAM_VOLPOS}, 0), {PARAM_VOLROT}).xyz;\n" +
            $"  half3 volCoords = ({VARCONST_POSITION} / {PARAM_VOLSIZE} + 1.0) * 0.5;\n" +
            $"  materialType = {PARAM_MATTYPE};\n" +
            $"  float channel = RM_SAMPLE_TEXTURE3D({PARAM_VOL3DTEX}, volCoords).g * {PARAM_MATCOUNT};\n" +
            $"  materialInstance = clamp(lerp(round(channel), floor(channel), {nameof(roundDownIndex)}), 0, {PARAM_MATCOUNT} - 1);\n" +
            "#endif";

        public override string SdfMethodName => "VolumeMaterialCompositorModifier";

        public override ISDFEntity.SDFUniformField[] SdfUniformFields => new ISDFEntity.SDFUniformField[]
        {
            new ISDFEntity.SDFUniformField(PARAM_VOLSIZE, ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(PARAM_VOL3DTEX, ISDFEntity.SDFUniformType.Sampler3D),
            new ISDFEntity.SDFUniformField(PARAM_MATTYPE, ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(PARAM_MATCOUNT, ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(nameof(roundDownIndex), ISDFEntity.SDFUniformType.Float),
            new ISDFEntity.SDFUniformField(PARAM_VOLPOS, ISDFEntity.SDFUniformType.Float3),
            new ISDFEntity.SDFUniformField(PARAM_VOLROT, ISDFEntity.SDFUniformType.Float4x4)
        };

        public override InlineMode ModifierInlineMode() => InlineMode.PostSdfInstance;

        public override void PushSdfEntityToShader(in Material raymarcherSceneMaterial, in string iterationIndex)
        {
            if (targetVolumeBoxTex3D == null)
                return;
            if (targetVolumeBoxTex3D.VolumeTexture == null)
                return;
            if (materialFamilyTypeIndex <= -1)
                return;
            if (materialFamilyTotalCount <= 0)
                return;
            raymarcherSceneMaterial.SetFloat(PARAM_MATCOUNT + iterationIndex, materialFamilyTotalCount);
            raymarcherSceneMaterial.SetFloat(PARAM_MATTYPE + iterationIndex, materialFamilyTypeIndex);
            raymarcherSceneMaterial.SetFloat(PARAM_VOLSIZE + iterationIndex, targetVolumeBoxTex3D.volumeSize);
            raymarcherSceneMaterial.SetVector(PARAM_VOLPOS + iterationIndex, transform.position);
            raymarcherSceneMaterial.SetMatrix(PARAM_VOLROT + iterationIndex, Matrix4x4.Rotate(transform.rotation));
            raymarcherSceneMaterial.SetTexture(PARAM_VOL3DTEX + iterationIndex, targetVolumeBoxTex3D.VolumeTexture);
            raymarcherSceneMaterial.SetFloat(nameof(roundDownIndex) + iterationIndex, roundDownIndex ? 1 : 0);
        }

        public override void SdfBufferRecompiled()
        {
            base.SdfBufferRecompiled();

            if (targetVolumeBoxTex3D == null)
                return;
            RefreshMaterialIndexAndType();
        }

        public void RefreshMaterialIndexAndType()
        {
            RMMaterialBase mbase = material1;
            if (mbase == null)
                mbase = material2;
            if (mbase == null)
                mbase = material3;
            if (mbase == null)
                mbase = material4;
            if (mbase != null)
            {
                materialFamilyTypeIndex = targetVolumeBoxTex3D.RenderMaster.MasterMaterials.GetMaterialTypeAndInstanceIndex(mbase).typeIndex;
                materialFamilyTotalCount = targetVolumeBoxTex3D.RenderMaster.MasterMaterials.GetCountOfMaterialsInBuffer(mbase);
            }
            else
            {
                materialFamilyTypeIndex = -1;
                materialFamilyTotalCount = 0;
            }
        }
    }
}