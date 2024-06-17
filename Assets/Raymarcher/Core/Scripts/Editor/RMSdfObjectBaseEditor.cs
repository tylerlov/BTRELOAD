using UnityEngine;
using UnityEditor;

using Raymarcher.Objects;
using Raymarcher.RendererData;
using Raymarcher.Objects.Modifiers;

namespace Raymarcher.UEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RMSdfObjectBase), true)]
    public sealed class RMSdfObjectBaseEditor : RMEditorUtilities
    {
        private RMSdfObjectBase sdfObjectBase;

        private void OnEnable()
        {
            sdfObjectBase = (RMSdfObjectBase)target;
        }

        public override void OnInspectorGUI()
        {
            if (sdfObjectBase.RenderMaster != null && RMb("Select Render Master"))
                Selection.activeObject = sdfObjectBase.RenderMaster;

            base.OnInspectorGUI();

            if (sdfObjectBase.MappingMaster == null)
            {
                RMhelpbox("Render Master or Mapping Master is null. This SDF object is unused!");
                return;
            }

            RMs();
            if (sdfObjectBase.RenderMaster.MasterMaterials.HasRegisteredGlobalMaterialInstances)
            {
                RMl("Active Global Materials:");
                RMbv(false);
                foreach (var mat in sdfObjectBase.RenderMaster.MasterMaterials.RegisteredGlobalMaterialInstances)
                {
                    if (mat && RMb(mat.name, new RectOffset(50,250,0,0)))
                    {
                        Selection.activeObject = mat;
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }
                }
                RMbve();
            }

            var renderType = sdfObjectBase.RenderMaster.RenderingData.CompiledRenderType;
            if (renderType == RMCoreRenderMasterRenderingData.RenderTypeOptions.Quality)
            {
                SerializedProperty prop = serializedObject.FindProperty("qualityRenderData");
                RMbh(false);
                RMproperty("objectMaterial");
                if (RMb("Empty", 60))
                    sdfObjectBase.SetObjectMaterialNullEditor();
                RMbhe();
                RMproperty(prop.FindPropertyRelative(nameof(RMSdfObjectBase.QualityRenderData.objectColor)));
                RMproperty(prop.FindPropertyRelative(nameof(RMSdfObjectBase.QualityRenderData.objectTexture)));
                RMproperty(prop.FindPropertyRelative(nameof(RMSdfObjectBase.QualityRenderData.textureTiling)));
                RMproperty(prop.FindPropertyRelative(nameof(RMSdfObjectBase.QualityRenderData.textureScaleX)));
                RMproperty(prop.FindPropertyRelative(nameof(RMSdfObjectBase.QualityRenderData.textureScaleY)));
                RMproperty(prop.FindPropertyRelative(nameof(RMSdfObjectBase.QualityRenderData.textureScaleZ)));
                RMproperty(prop.FindPropertyRelative(nameof(RMSdfObjectBase.QualityRenderData.textureOpacity)));
            }
            else if (renderType != RMCoreRenderMasterRenderingData.RenderTypeOptions.Performant)
            {
                RMbh(false);
                RMproperty("objectMaterial");
                if (RMb("Empty", 60))
                    sdfObjectBase.SetObjectMaterialNullEditor();
                RMbhe();
                RMproperty("hueShift");
            }
            else
            {
                RMproperty("hueShift");
            }

            RMRenderMasterEditorHelper.GenerateRecompilationLayout(sdfObjectBase.RenderMaster);
            GenerateModifiers();

            serializedObject.ApplyModifiedProperties();
        }


        private bool addModifier;
        private void GenerateModifiers()
        {
            RMs(5);
            RMbv();

            Color white = Color.white;
            white *= 0.75f;
            white.a = 1;
            GUI.color = white;
            if (RMb("Refresh Modifiers", 200))
                sdfObjectBase.RefreshModifiers();
            GUI.color = Color.white;

            if (RMb("Add Modifier", 100))
                addModifier = !addModifier;

            if (addModifier)
            {
                RMs();
                RMl("Target Operations", true);
                GUI.backgroundColor = Color.magenta / 3f;

                RMbh();
                if(RMb("Blend", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Blend>();
                if (RMb("Intersect", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Intersect>();
                if (RMb("Subtract", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Subtract>();
                if (RMb("Morph", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Morph>();
                RMbhe();

                RMl("Deformations", true);
                GUI.backgroundColor = Color.cyan / 3f;

                RMbh();
                if (RMb("Fragment", 72))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Fragment>();
                if (RMb("Mirror", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Mirror>();
                if (RMb("Twist", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Twist>();
                if (RMb("Deform", 72))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_LinearDeform>();
                if (RMb("Noiser", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_Noiser>();
                if (RMb("Active State", 98))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_ActiveState>();
                RMbhe();

                RMl("Displacement", true);
                GUI.backgroundColor = Color.yellow / 3f;

                RMbh();
                if (RMb("Radial", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_DisplacementRadial>();
                if (RMb("XY", 48))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_DisplacementXY>();
                if (RMb("XZ", 48))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_DisplacementXZ>();
                if (RMb("YZ", 48))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_DisplacementYZ>();
                RMbhe();

                RMl("Repeat", true);
                GUI.backgroundColor = Color.green / 3f;

                RMbh();
                if (RMb("X", 32))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RepeatX>();
                if (RMb("Y", 32))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RepeatY>();
                if (RMb("Z", 32))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RepeatZ>();
                if (RMb("X Count", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RepeatCountX>();
                if (RMb("Y Count", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RepeatCountY>();
                if (RMb("Z Count", 64))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RepeatCountZ>();
                RMbhe();

                RMl("Material", true);
                GUI.backgroundColor = Color.blue / 3f;

                RMbh();
                if (RMb("Volume Material Compositor", 220))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_VolumeMaterialCompositor>();
                RMbhe();

                RMl("Rotations", true);
                GUI.backgroundColor = Color.red / 3f;

                RMbh();
                if (RMb("Rotate Quaternion", 120))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RotateQuaternion>();
                if (RMb("Rotate Axis", 80))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RotateAxis>();
                if (RMb("Rotate XY", 72))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RotateXY>();
                if (RMb("Rotate XZ", 72))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RotateXZ>();
                if (RMb("Rotate YZ", 72))
                    sdfObjectBase.gameObject.AddComponent<RMModifier_RotateYZ>();
                RMbhe();
            }

            RMbve();

            GUI.backgroundColor = Color.white;
        }
    }
}