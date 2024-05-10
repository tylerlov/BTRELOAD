using UnityEngine;
using FMODUnity; // Add FMODUnity namespace

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
        [SerializeField] private SliderManager masterSlider;
        [SerializeField] private SliderManager musicSlider;
        [SerializeField] private SliderManager SFXSlider;
        [SerializeField] private SliderManager UISlider;

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            if (fmodEventEmitter == null) { fmodEventEmitter = gameObject.GetComponent<StudioEventEmitter>(); }
            InitVolume();
        }

         public StudioEventEmitter FmodEventEmitter
    {
        get { return fmodEventEmitter; }
    }

        public void InitVolume()
        {
            // FMOD handles volume differently. You might need to use FMOD Studio's exposed parameters.
            // This is a placeholder for the actual implementation.
        }

        // Volume setting functions will need to interact with FMOD parameters.
        // The following methods are placeholders and should be adapted to your FMOD event parameters.
        public void SetMasterVolume(float volume) { /* Set FMOD master volume parameter */ }
        public void SetMusicVolume(float volume) { /* Set FMOD music volume parameter */ }
        public void SetSFXVolume(float volume) { /* Set FMOD SFX volume parameter */ }
        public void SetUIVolume(float volume) { /* Set FMOD UI volume parameter */ }
    }
}
