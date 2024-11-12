using UnityEditor;
using UnityEngine;

namespace Mirage.Impostors.Elements
{
    public class MirageUnlitShaderGUI : ShaderGUI
    {
        private GUIStyle centeredStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        private GUIStyle miniWrappedLabel = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            normal = new GUIStyleState
            {
                textColor = Color.gray
            },
            wordWrap = true
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material targetMaterial = materialEditor.target as Material;

            GUILayout.Label(Resources.Load<Texture>("MirageLogo"), centeredStyle, GUILayout.Height(96f), GUILayout.ExpandWidth(true));

            if (MirageEditorUtilities.BeginSection("Billboarding", true, () => {
                GUILayout.Label("Ajust the billboard effect parameters", miniWrappedLabel, GUILayout.Height(28));
            }, true))
            {
                DisplayProperty(materialEditor, properties, "_BillboardingEnabled");
                MaterialProperty property = FindProperty("_BillboardingEnabled", properties);
                if (property.floatValue > 0.5)
                {
                    DisplayProperty(materialEditor, properties, "_ClampLatitude");
                    DisplayProperty(materialEditor, properties, "_ZOffset");
                }
            }
            MirageEditorUtilities.EndSection();

            if (MirageEditorUtilities.BeginSection("Surface", true, () => {
                GUILayout.Label("Ajust the surface parameters to match the source object", miniWrappedLabel, GUILayout.Height(28));
            }, true))
            {
                DisplayProperty(materialEditor, properties, "_Brightness");
                DisplayProperty(materialEditor, properties, "_Saturation");
            }
            MirageEditorUtilities.EndSection();

            if (MirageEditorUtilities.BeginSection("Contours", true, () => {
                GUILayout.Label("Ajust the alpha clipping parameters", miniWrappedLabel, GUILayout.Height(28));
            }, true))
            {
                DisplayProperty(materialEditor, properties, "_Cutout");
                DisplayProperty(materialEditor, properties, "_Smooth");
                DisplayProperty(materialEditor, properties, "_DitheringFade");
            }
            MirageEditorUtilities.EndSection();

            if (MirageEditorUtilities.BeginSection("Geometry", true, () => {
                GUILayout.Label("Apply geometry offsets", miniWrappedLabel, GUILayout.Height(28));
            }, true))
            {
                DisplayProperty(materialEditor, properties, "_YawOffset");
                DisplayProperty(materialEditor, properties, "_ElevationOffset");
            }
            MirageEditorUtilities.EndSection();
            if (MirageEditorUtilities.BeginSection("Advanced options", true, () =>
            {
                GUILayout.Label("Manage the render queue, GPU Instancing and double sided GI", miniWrappedLabel, GUILayout.Height(28));
            }, true))
            {
                materialEditor.RenderQueueField();
                materialEditor.DoubleSidedGIField();
                materialEditor.EnableInstancingField();
            }
            MirageEditorUtilities.EndSection();
        }

        private void DisplayProperty(MaterialEditor materialEditor, MaterialProperty[] properties, string propertyName)
        {
            MaterialProperty property = FindProperty(propertyName, properties);
            materialEditor.ShaderProperty(property, property.displayName);
        }
    }
}

