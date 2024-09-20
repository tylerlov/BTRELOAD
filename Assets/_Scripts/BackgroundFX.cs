using SonicBloom.Koreo;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BackgroundFX : MonoBehaviour
{
    [EventID]
    public string eventID;

    [ColorUsage(true, true)]
    public Color colorOne = Color.white;

    [ColorUsage(true, true)]
    public Color colorTwo = Color.black;

    [SerializeField, InspectorReadOnly]
    private Color currentSkyboxColor;

    private bool colorChangeSwitch = false;
    private bool lockingFx = true;

    private Material skyboxMaterial;

    void Awake()
    {
        InitializeSkyboxMaterial();
        Koreographer.Instance.RegisterForEvents(eventID, OnMusicalSkybox);
        UpdateCurrentSkyboxColor();
        ConditionalDebug.Log("BackgroundFX initialized");
    }

    void InitializeSkyboxMaterial()
    {
        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = new Material(RenderSettings.skybox);
            RenderSettings.skybox = skyboxMaterial;
        }
        else
        {
            ConditionalDebug.LogError("No skybox material set in RenderSettings!");
        }
    }

    void OnDestroy()
    {
        Koreographer.Instance.UnregisterForEvents(eventID, OnMusicalSkybox);
    }

    void OnMusicalSkybox(KoreographyEvent evt)
    {
        if (Time.timeScale == 0 || !lockingFx)
        {
            return;
        }

        colorChangeSwitch = !colorChangeSwitch;
        Color targetColor = colorChangeSwitch ? colorOne : colorTwo;
        UpdateSkyboxColor(targetColor);
        ConditionalDebug.Log($"Skybox color changed to {(colorChangeSwitch ? "colorOne" : "colorTwo")}");
    }

    void UpdateSkyboxColor(Color color)
    {
        if (skyboxMaterial == null)
        {
            ConditionalDebug.LogError("Skybox material is null! Reinitializing...");
            InitializeSkyboxMaterial();
            if (skyboxMaterial == null)
                return;
        }

        string propertyName = GetSkyboxColorProperty();
        if (string.IsNullOrEmpty(propertyName))
        {
            ConditionalDebug.LogError("No valid color property found on skybox material!");
            return;
        }

        skyboxMaterial.SetColor(propertyName, color);

        // Removed this log as it's not very informative
        // ConditionalDebug.Log($"Skybox color updated to {color}");
        UpdateCurrentSkyboxColor();
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }

    string GetSkyboxColorProperty()
    {
        string[] possibleProperties = { "_SkyTint", "_Tint", "_Color" };
        foreach (var prop in possibleProperties)
        {
            if (skyboxMaterial.HasProperty(prop))
            {
                return prop;
            }
        }
        return string.Empty;
    }

    void UpdateCurrentSkyboxColor()
    {
        if (skyboxMaterial != null)
        {
            string propertyName = GetSkyboxColorProperty();
            if (!string.IsNullOrEmpty(propertyName))
            {
                currentSkyboxColor = skyboxMaterial.GetColor(propertyName);
            }
        }
    }

    void OnValidate()
    {
        if (skyboxMaterial == null && RenderSettings.skybox != null)
        {
            InitializeSkyboxMaterial();
        }
        UpdateCurrentSkyboxColor();
    }

    public void TestColorChange()
    {
        OnMusicalSkybox(null);
    }

    public void ForceColorChange(bool useColorOne)
    {
        Color targetColor = useColorOne ? colorOne : colorTwo;
        UpdateSkyboxColor(targetColor);
    }
}

// Renamed custom attribute to avoid conflicts
public class InspectorReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
// Updated custom property drawer for the renamed attribute
[CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
public class InspectorReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif
