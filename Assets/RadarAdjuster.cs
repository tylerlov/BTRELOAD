using UnityEngine;
using SickscoreGames.HUDNavigationSystem;

public class RadarAdjuster : MonoBehaviour
{
    [Header("Radar Adjustments")]
    public float radarZoom = 1f;
    public float radarRadius = 50f;
    public float radarMaxRadius = 75f;
    public float radarScaleDistance = 10f;
    public float radarMinScale = 0.35f;
    public float radarFadeDistance = 5f;

    private void Awake()
    {
        // Find the HUDNavigationSystem in the scene
        HUDNavigationSystem hudNavSystem = FindObjectOfType<HUDNavigationSystem>();

        if (hudNavSystem != null)
        {
            // Adjust the radar properties
            hudNavSystem.radarZoom = radarZoom;
            hudNavSystem.radarRadius = radarRadius;
            hudNavSystem.radarMaxRadius = radarMaxRadius;
            hudNavSystem.radarScaleDistance = radarScaleDistance;
            hudNavSystem.radarMinScale = radarMinScale;
            hudNavSystem.radarFadeDistance = radarFadeDistance;

            Debug.Log("Radar properties adjusted successfully.");
        }
        else
        {
            Debug.LogError("HUDNavigationSystem not found in the scene!");
        }
    }
}