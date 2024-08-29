using UnityEngine;
using FMODUnity;
using FMOD.Studio;

namespace Michsky.UI.Reach
{
    public class AudioCore : MonoBehaviour
    {
        public static AudioCore instance;

        private VCA masterVCA;

        public const float DEFAULT_VOLUME = 0.8f; // 80% of max volume

        void Awake()
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

            InitializeVCAs();
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

        public void SetMasterVolume(float volume)
        {
            if (masterVCA.isValid())
            {
                // Ensure the volume is between 0 and 1
                volume = Mathf.Clamp01(volume);
                masterVCA.setVolume(volume);
            }
            else
            {
                Debug.LogError("Master VCA is not valid. Cannot set volume.");
            }
        }

        public bool IsMasterVCAValid()
        {
            return masterVCA.isValid();
        }
    }
}
