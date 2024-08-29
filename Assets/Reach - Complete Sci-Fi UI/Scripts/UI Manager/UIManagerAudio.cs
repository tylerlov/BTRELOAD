using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

namespace Michsky.UI.Reach
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class UIManagerAudio : MonoBehaviour
    {
        public static UIManagerAudio instance;

        public UIManager UIManagerAsset;
        [SerializeField] private StudioEventEmitter fmodEventEmitter;
        public Slider masterSlider;

        public StudioEventEmitter FmodEventEmitter
        {
            get { return fmodEventEmitter; }
        }

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            InitVolume();
        }

public void SetMasterVolume(Slider slider)
{
    if (AudioCore.instance != null && AudioCore.instance.IsMasterVCAValid())
    {
        AudioCore.instance.SetMasterVolume(slider.value);
        
        if (GlobalPreferences.instance != null)
        {
            GlobalPreferences.instance.SavePreferences();
        }
    }
    else
    {
        Debug.LogWarning("AudioCore is not ready or Master VCA is not valid.");
    }
}

        public void InitVolume()
        {
            if (masterSlider != null)
            {
                float volume = DEFAULT_VOLUME;
                
                if (GlobalPreferences.instance != null && GlobalPreferences.instance.preferencesData != null)
                {
                    if (GlobalPreferences.instance.preferencesData.hasCustomMasterVolume)
                    {
                        volume = GlobalPreferences.instance.preferencesData.masterVolume;
                    }
                }
                
                masterSlider.value = volume;
                SetMasterVolume(masterSlider);
            }
        }

        public const float DEFAULT_VOLUME = 0.8f;
    }
}

