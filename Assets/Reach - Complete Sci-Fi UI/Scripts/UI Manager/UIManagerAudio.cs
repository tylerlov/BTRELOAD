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
        private VCA masterVCA; // Only Master VCA

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

            InitializeVCAs();

            if (!masterVCA.isValid())
            {
                Debug.LogError("Master VCA could not be loaded or is invalid.");
            }

            InitVolume();
        }

        private void InitializeVCAs()
        {
            masterVCA = GetVCA("vca:/Master VCA");
        }

        private VCA GetVCA(string path)
        {
            VCA vca = RuntimeManager.GetVCA(path);
            if (!vca.isValid())
            {
                Debug.LogError($"VCA not found or invalid: {path}");
            }
            return vca;
        }

        public void SetMasterVolume(Slider slider)
        {
            if (!masterVCA.isValid())
            {
                InitializeVCAs();
            }

            if (masterVCA.isValid())
            {
                SetVCAVolume(masterVCA, slider.value);
            }
            else
            {
                Debug.LogWarning("Master VCA is not ready.");
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
        }
    }
}
