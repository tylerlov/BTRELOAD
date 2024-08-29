using UnityEngine;

namespace Michsky.UI.Reach
{
    public class GlobalPreferences : MonoBehaviour
    {
        public static GlobalPreferences instance;

        public GlobalPreferencesData preferencesData;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            LoadPreferences();
        }

        public void SavePreferences()
        {
            if (GraphicsCore.instance != null)
            {
                preferencesData.defaultResolutionIndex = GraphicsCore.instance.CurrentResolutionIndex;
                preferencesData.textureQuality = GraphicsCore.instance.CurrentTextureQuality;
                preferencesData.anisotropicFiltering = GraphicsCore.instance.CurrentAnisotropicFiltering;
                preferencesData.vSyncEnabled = GraphicsCore.instance.IsVSyncEnabled(); // Add this line
            }

            if (UIManagerAudio.instance != null && UIManagerAudio.instance.masterSlider != null)
            {
                preferencesData.masterVolume = UIManagerAudio.instance.masterSlider.value;
                preferencesData.hasCustomMasterVolume = true;
            }

            // Save the ScriptableObject
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(preferencesData);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public void LoadPreferences()
        {
            if (GraphicsCore.instance != null)
            {
                if (preferencesData.useAutoCalibrationResolution)
                {
                    GraphicsCore.instance.SetAutoCalibrationResolution();
                }
                else if (preferencesData.defaultResolutionIndex >= 0)
                {
                    GraphicsCore.instance.SetResolution(preferencesData.defaultResolutionIndex);
                }
                else
                {
                    GraphicsCore.instance.SetAutoCalibrationResolution();
                }

                GraphicsCore.instance.SetTextureQuality(preferencesData.textureQuality);
                GraphicsCore.instance.SetAnisotropicFiltering(preferencesData.anisotropicFiltering);
                GraphicsCore.instance.SetVSync(preferencesData.vSyncEnabled);
                GraphicsCore.instance.SetFrameRate(preferencesData.targetFrameRate);
                GraphicsCore.instance.SetFullScreenMode(preferencesData.fullScreenMode);
            }

            if (AudioCore.instance != null)
            {
                float volumeToSet = preferencesData.hasCustomMasterVolume ? preferencesData.masterVolume : AudioCore.DEFAULT_VOLUME;
                AudioCore.instance.SetMasterVolume(volumeToSet);
            }
        }
    }
}
