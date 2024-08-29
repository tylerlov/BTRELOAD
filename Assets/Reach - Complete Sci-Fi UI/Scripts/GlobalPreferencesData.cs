using UnityEngine;

namespace Michsky.UI.Reach
{
    [CreateAssetMenu(fileName = "GlobalPreferencesData", menuName = "Reach UI/Global Preferences Data")]
    public class GlobalPreferencesData : ScriptableObject
    {
        [Header("Graphics Preferences")]
        public bool useAutoCalibrationResolution = true;
        public int defaultResolutionIndex = -1; // -1 will indicate auto-calibration
        public bool vSyncEnabled = false; // Set this to false by default
        public int targetFrameRate = -1; // -1 represents unlimited frame rate
        public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
        public GraphicsCore.TextureOption textureQuality = GraphicsCore.TextureOption.FullRes;
        public GraphicsCore.AnisotropicOption anisotropicFiltering = GraphicsCore.AnisotropicOption.ForceEnable;
        public bool taaEnabled = true;

        [Header("Audio Preferences")]
        [Range(0f, 1f)]
        public float masterVolume = 0.8f;
        public bool hasCustomMasterVolume = false;
    }
}
