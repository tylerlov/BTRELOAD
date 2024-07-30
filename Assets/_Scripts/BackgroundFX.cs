using UnityEngine;
using SonicBloom.Koreo;
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
            Debug.LogError("No skybox material set in RenderSettings!");
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
    }

    void UpdateSkyboxColor(Color color)
    {
        if (skyboxMaterial == null)
        {
            Debug.LogError("Skybox material is null! Reinitializing...");
            InitializeSkyboxMaterial();
            if (skyboxMaterial == null) return;
        }

        string propertyName = GetSkyboxColorProperty();
        if (string.IsNullOrEmpty(propertyName))
        {
            Debug.LogError("No valid color property found on skybox material!");
            return;
        }

        skyboxMaterial.SetColor(propertyName, color);

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