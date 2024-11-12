using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class TyStylizedLitGUI : ShaderGUI
{
    // Material properties
    private MaterialProperty surfaceTypeProp;
    private MaterialProperty blendModeProp;
    private MaterialProperty cullModeProp;
    private MaterialProperty baseMapProp;
    private MaterialProperty baseColorProp;
    private MaterialProperty alphaCutoffProp;
    
    // Lighting properties
    private MaterialProperty selfShadingSizeProp;
    private MaterialProperty lightContributionProp;
    private MaterialProperty shadowColorProp;
    private MaterialProperty lightAttenuationProp;
    
    // Specular properties
    private MaterialProperty specularEnabledProp;
    private MaterialProperty specularColorProp;
    private MaterialProperty specularSizeProp;
    private MaterialProperty specularSmoothnessProp;
    
    // Rim properties
    private MaterialProperty rimEnabledProp;
    private MaterialProperty rimColorProp;
    private MaterialProperty rimSizeProp;
    private MaterialProperty rimSmoothnessProp;
    
    // Gradient properties
    private MaterialProperty gradientEnabledProp;
    private MaterialProperty gradientColorProp;
    private MaterialProperty gradientSizeProp;
    private MaterialProperty gradientAngleProp;
    private MaterialProperty gradientCenterXProp;
    private MaterialProperty gradientCenterYProp;

    // Detail properties
    private MaterialProperty detailMapProp;
    private MaterialProperty detailMapColorProp;
    private MaterialProperty detailMapImpactProp;
    private MaterialProperty detailMapBlendingModeProp;

    // Outline properties
    private MaterialProperty outlineEnabledProp;
    private MaterialProperty outlineColorProp;
    private MaterialProperty outlineWidthProp;
    private MaterialProperty outlineScaleProp;
    private MaterialProperty outlineDepthOffsetProp;
    private MaterialProperty cameraDistanceImpactProp;

    // Emission properties
    private MaterialProperty emissionColorProp;
    private MaterialProperty emissionPowerProp;

    private bool showMainSettings = true;
    private bool showOutlineSettings = true;
    private bool showSpecularSettings = true;
    private bool showRimSettings = true;
    private bool showGradientSettings = true;
    private bool showDetailSettings = true;
    private bool showLightingSettings = true;
    private bool showAdvancedSettings = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;
        FindProperties(properties);

        EditorGUILayout.Space();
        
        // 1. Main Settings (Most important, always needed)
        showMainSettings = EditorGUILayout.Foldout(showMainSettings, "Main Settings", true);
        if (showMainSettings)
        {
            EditorGUI.indentLevel++;
            DrawMainSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        // 2. Outline Settings (Most visible effect)
        showOutlineSettings = EditorGUILayout.Foldout(showOutlineSettings, "Outline", true);
        if (showOutlineSettings)
        {
            EditorGUI.indentLevel++;
            DrawOutlineSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        // 3. Visual Effects
        showSpecularSettings = EditorGUILayout.Foldout(showSpecularSettings, "Specular Highlights", true);
        if (showSpecularSettings)
        {
            EditorGUI.indentLevel++;
            DrawSpecularSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        showRimSettings = EditorGUILayout.Foldout(showRimSettings, "Rim Lighting", true);
        if (showRimSettings)
        {
            EditorGUI.indentLevel++;
            DrawRimSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        // 4. Gradient and Detail Settings
        showGradientSettings = EditorGUILayout.Foldout(showGradientSettings, "Height Gradient", true);
        if (showGradientSettings)
        {
            EditorGUI.indentLevel++;
            DrawGradientSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        showDetailSettings = EditorGUILayout.Foldout(showDetailSettings, "Detail Maps", true);
        if (showDetailSettings)
        {
            EditorGUI.indentLevel++;
            DrawDetailSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        // 5. Lighting Settings
        showLightingSettings = EditorGUILayout.Foldout(showLightingSettings, "Lighting", true);
        if (showLightingSettings)
        {
            EditorGUI.indentLevel++;
            DrawLightingSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        // 6. Advanced Settings
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;
            DrawAdvancedSettings(materialEditor, material);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        DrawPresetButtons(material);
    }

    private void FindProperties(MaterialProperty[] properties)
    {
        surfaceTypeProp = FindProperty("_Surface", properties, false);
        blendModeProp = FindProperty("_Blend", properties, false);
        cullModeProp = FindProperty("_Cull", properties, false);
        baseMapProp = FindProperty("_BaseMap", properties, false);
        baseColorProp = FindProperty("_BaseColor", properties, false);
        alphaCutoffProp = FindProperty("_AlphaClip", properties, false);
        
        selfShadingSizeProp = FindProperty("_SelfShadingSize", properties, false);
        lightContributionProp = FindProperty("_LightContribution", properties, false);
        shadowColorProp = FindProperty("_ShadowColor", properties, false);
        lightAttenuationProp = FindProperty("_LightAttenuation", properties, false);
        
        specularEnabledProp = FindProperty("_SpecularEnabled", properties, false);
        specularColorProp = FindProperty("_SpecularColor", properties, false);
        specularSizeProp = FindProperty("_SpecularSize", properties, false);
        specularSmoothnessProp = FindProperty("_SpecularSmoothness", properties, false);
        
        rimEnabledProp = FindProperty("_RimEnabled", properties, false);
        rimColorProp = FindProperty("_RimColor", properties, false);
        rimSizeProp = FindProperty("_RimSize", properties, false);
        rimSmoothnessProp = FindProperty("_RimSmoothness", properties, false);
        
        gradientEnabledProp = FindProperty("_GradientEnabled", properties, false);
        gradientColorProp = FindProperty("_ColorGradient", properties, false);
        gradientSizeProp = FindProperty("_GradientSize", properties, false);
        gradientAngleProp = FindProperty("_GradientAngle", properties, false);
        gradientCenterXProp = FindProperty("_GradientCenterX", properties, false);
        gradientCenterYProp = FindProperty("_GradientCenterY", properties, false);

        detailMapProp = FindProperty("_DetailMap", properties, false);
        detailMapColorProp = FindProperty("_DetailMapColor", properties, false);
        detailMapImpactProp = FindProperty("_DetailMapImpact", properties, false);
        detailMapBlendingModeProp = FindProperty("_DetailMapBlendingMode", properties, false);

        outlineEnabledProp = FindProperty("_OutlineEnabled", properties, false);
        outlineColorProp = FindProperty("_OutlineColor", properties, false);
        outlineWidthProp = FindProperty("_OutlineWidth", properties, false);
        outlineScaleProp = FindProperty("_OutlineScale", properties, false);
        outlineDepthOffsetProp = FindProperty("_OutlineDepthOffset", properties, false);
        cameraDistanceImpactProp = FindProperty("_CameraDistanceImpact", properties, false);

        emissionColorProp = FindProperty("_EmissionColor", properties, false);
        emissionPowerProp = FindProperty("_EmissionPower", properties, false);
    }

    private void DrawMainSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        
        // Surface Options
        EditorGUILayout.LabelField("Surface Options", EditorStyles.boldLabel);
        
        if (baseMapProp != null && baseColorProp != null)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Base Map"), baseMapProp, baseColorProp);
            materialEditor.TextureScaleOffsetProperty(baseMapProp);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Emission", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(emissionColorProp, "Emission Color");
        materialEditor.ShaderProperty(emissionPowerProp, "Emission Power");

        if (cullModeProp != null)
        {
            materialEditor.ShaderProperty(cullModeProp, "Cull Mode");
        }
    }

    private void DrawLightingSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        materialEditor.ShaderProperty(selfShadingSizeProp, "Self Shading Size");
        materialEditor.ShaderProperty(lightContributionProp, "Light Contribution");
        materialEditor.ShaderProperty(shadowColorProp, "Shadow Color");
        
        if (lightAttenuationProp != null)
        {
            EditorGUI.BeginChangeCheck();
            MinMaxSlider(materialEditor, lightAttenuationProp, "Light Attenuation", 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                material.SetVector("_LightAttenuation", lightAttenuationProp.vectorValue);
            }
        }
    }

    private void DrawSpecularSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        materialEditor.ShaderProperty(specularEnabledProp, "Enable Specular");
        
        if (specularEnabledProp.floatValue == 1)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(specularColorProp, "Specular Color");
            materialEditor.ShaderProperty(specularSizeProp, "Specular Size");
            materialEditor.ShaderProperty(specularSmoothnessProp, "Specular Smoothness");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawRimSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        materialEditor.ShaderProperty(rimEnabledProp, "Enable Rim");
        
        if (rimEnabledProp.floatValue == 1)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(rimColorProp, "Rim Color");
            materialEditor.ShaderProperty(rimSizeProp, "Rim Size");
            materialEditor.ShaderProperty(rimSmoothnessProp, "Rim Smoothness");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawGradientSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        materialEditor.ShaderProperty(gradientEnabledProp, "Enable Gradient");
        
        if (gradientEnabledProp.floatValue == 1)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(gradientColorProp, "Gradient Color");
            materialEditor.ShaderProperty(gradientSizeProp, "Gradient Size");
            materialEditor.ShaderProperty(gradientAngleProp, "Gradient Angle");
            materialEditor.ShaderProperty(gradientCenterXProp, "Center X");
            materialEditor.ShaderProperty(gradientCenterYProp, "Center Y");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawDetailSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        materialEditor.TexturePropertySingleLine(new GUIContent("Detail Map"), detailMapProp, detailMapColorProp);
        if (detailMapProp.textureValue != null)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(detailMapBlendingModeProp, "Blending Mode");
            materialEditor.ShaderProperty(detailMapImpactProp, "Impact");
            materialEditor.TextureScaleOffsetProperty(detailMapProp);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawAdvancedSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
    }

    private void DrawOutlineSettings(MaterialEditor materialEditor, Material material)
    {
        EditorGUILayout.Space();
        
        EditorGUI.BeginChangeCheck();
        materialEditor.ShaderProperty(outlineEnabledProp, new GUIContent("Enable Outline"));
        bool outlineEnabled = outlineEnabledProp.floatValue == 1;
        
        if (outlineEnabled)
        {
            EditorGUI.indentLevel++;
            
            // Use ColorProperty instead of ShaderProperty for better color picking
            outlineColorProp.colorValue = EditorGUILayout.ColorField(new GUIContent("Outline Color"), outlineColorProp.colorValue, true, true, true);
            
            materialEditor.ShaderProperty(outlineWidthProp, "Width");
            materialEditor.ShaderProperty(outlineScaleProp, "Scale");
            materialEditor.ShaderProperty(outlineDepthOffsetProp, "Depth Offset");
            materialEditor.ShaderProperty(cameraDistanceImpactProp, "Camera Distance Impact");
            EditorGUI.indentLevel--;
        }

        if (EditorGUI.EndChangeCheck())
        {
            if (outlineEnabled)
            {
                material.EnableKeyword("DR_OUTLINE_ON");
                // Ensure the outline color is properly set
                material.SetColor("_OutlineColor", outlineColorProp.colorValue);
            }
            else
            {
                material.DisableKeyword("DR_OUTLINE_ON");
            }
        }
    }

    private void SetupMaterialBlendMode(Material material)
    {
        bool isTransparent = (int)surfaceTypeProp.floatValue == 1;
        
        if (isTransparent)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetFloat("_Surface", 1);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        }
        else
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.SetFloat("_Surface", 0);
            material.renderQueue = (int)RenderQueue.Geometry;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SURFACE_TYPE_OPAQUE");
        }
    }

    private void DrawPresetButtons(Material material)
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Default Opaque"))
        {
            SetDefaultOpaquePreset(material);
        }
        
        if (GUILayout.Button("Default Transparent"))
        {
            SetDefaultTransparentPreset(material);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void SetDefaultOpaquePreset(Material material)
    {
        // Set surface type first
        material.SetFloat("_Surface", 0);
        
        // Then set other properties
        material.SetFloat("_Blend", 0);
        material.SetFloat("_Cull", 2);
        material.SetFloat("_ZWrite", 1);
        material.SetFloat("_AlphaClip", 0);
        
        // Set blend modes
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        
        // Set render queue
        material.renderQueue = (int)RenderQueue.Geometry;
        material.SetOverrideTag("RenderType", "Opaque");
        
        // Set keywords
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_SURFACE_TYPE_OPAQUE");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        
        // Set other properties
        material.SetFloat("_SpecularEnabled", 0);
        material.SetFloat("_RimEnabled", 0);
        material.SetFloat("_GradientEnabled", 0);
        material.SetFloat("_SelfShadingSize", 0.5f);
        material.SetFloat("_LightContribution", 1);
        
        // Set outline defaults
        material.SetFloat("_OutlineEnabled", 0);
        material.DisableKeyword("DR_OUTLINE_ON");
    }

    private void SetDefaultTransparentPreset(Material material)
    {
        // Set surface type first
        material.SetFloat("_Surface", 1);
        
        // Then set other properties
        material.SetFloat("_Blend", 0);
        material.SetFloat("_Cull", 2);
        material.SetFloat("_ZWrite", 0);
        material.SetFloat("_AlphaClip", 0);
        
        // Set blend modes
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        
        // Set render queue
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetOverrideTag("RenderType", "Transparent");
        
        // Set keywords
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        
        // Set other properties
        material.SetFloat("_SpecularEnabled", 0);
        material.SetFloat("_RimEnabled", 0);
        material.SetFloat("_GradientEnabled", 0);
        material.SetFloat("_SelfShadingSize", 0.5f);
        material.SetFloat("_LightContribution", 1);
        
        // Set outline defaults
        material.SetFloat("_OutlineEnabled", 0);
        material.DisableKeyword("DR_OUTLINE_ON");
    }

    private void MinMaxSlider(MaterialEditor materialEditor, MaterialProperty property, string label, float minLimit = 0f, float maxLimit = 1f)
    {
        Vector4 vector = property.vectorValue;
        float minValue = vector.x;
        float maxValue = vector.y;
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 28;
        
        minValue = EditorGUILayout.FloatField("Min", minValue, GUILayout.Width(50));
        EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit);
        maxValue = EditorGUILayout.FloatField("Max", maxValue, GUILayout.Width(50));
        
        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUILayout.EndHorizontal();

        if (vector.x != minValue || vector.y != maxValue)
        {
            vector.x = minValue;
            vector.y = maxValue;
            property.vectorValue = vector;
        }
    }
} 