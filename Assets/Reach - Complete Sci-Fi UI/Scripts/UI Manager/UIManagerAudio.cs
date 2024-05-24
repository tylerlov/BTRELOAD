using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity; // Add FMODUnity namespace
using FMOD.Studio;
using UnityEngine.UI; // Required for UI elements like Slider

namespace Michsky.UI.Reach
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class UIManagerAudio : MonoBehaviour
    {
        // Static Instance
        public static UIManagerAudio instance;

        // Resources
        public UIManager UIManagerAsset;
        [SerializeField] private StudioEventEmitter fmodEventEmitter; // Changed from AudioSource to FMOD's StudioEventEmitter
        [SerializeField] private Slider masterSlider; // Assuming a slider for master volume is available
        [SerializeField] private Slider playerSlider;
        [SerializeField] private Slider uiSlider; // Renamed from menusSlider to uiSlider
        [SerializeField] private Slider enemiesSlider;
        private VCA masterVCA, playerVCA, uiVCA, enemiesVCA; // Changed from Bus to VCA

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
            StartCoroutine(InitializeAudio());
        }

        IEnumerator InitializeAudio()
        {
            yield return new WaitForSeconds(0.1f); // Delay to ensure all systems are ready

            masterVCA = RuntimeManager.GetVCA("vca:/Master VCA");
            playerVCA = RuntimeManager.GetVCA("vca:/Player VCA");
            uiVCA = RuntimeManager.GetVCA("vca:/UI VCA");
            enemiesVCA = RuntimeManager.GetVCA("vca:/Enemies VCA");

            if (!masterVCA.isValid() || !playerVCA.isValid() || !uiVCA.isValid() || !enemiesVCA.isValid()) {
                Debug.LogError("One or more VCAs could not be loaded or are invalid.");
            }

            InitVolume();
        }

        public void SetMasterVolume(Slider slider)
        {
            if (masterVCA.isValid())
            {
                SetVCAVolume(masterVCA, slider.value);
            }
            else
            {
                Debug.LogWarning("Master VCA is not ready.");
            }
        }

        public void SetPlayerVolume(Slider slider)
        {
            if (playerVCA.isValid())
            {
                SetVCAVolume(playerVCA, slider.value);
            }
            else
            {
                Debug.LogWarning("Player VCA is not ready.");
            }
        }

        public void SetUIVolume(Slider slider) // Renamed from SetMenusVolume
        {
            if (uiVCA.isValid())
            {
                SetVCAVolume(uiVCA, slider.value);
            }
            else
            {
                Debug.LogWarning("UI VCA is not ready.");
            }
        }

        public void SetEnemiesVolume(Slider slider)
        {
            if (enemiesVCA.isValid())
            {
                SetVCAVolume(enemiesVCA, slider.value);
            }
            else
            {
                Debug.LogWarning("Enemies VCA is not ready.");
            }
        }

        private void SetVCAVolume(VCA vca, float volume)
        {
            if (vca.isValid())
            {
                float dB = Mathf.Log10(volume) * 20;
                vca.setVolume(Mathf.Pow(10.0f, dB / 20.0f));
            }
            else
            {
                Debug.LogError("VCA is not valid. Cannot set volume.");
            }
        }

        public void InitVolume()
        {
            if (masterSlider != null) SetMasterVolume(masterSlider);
            if (playerSlider != null) SetPlayerVolume(playerSlider);
            if (uiSlider != null) SetUIVolume(uiSlider);
            if (enemiesSlider != null) SetEnemiesVolume(enemiesSlider);
        }
    }
}

