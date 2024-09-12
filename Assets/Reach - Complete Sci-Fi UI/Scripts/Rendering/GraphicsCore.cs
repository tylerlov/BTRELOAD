using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Michsky.UI.Reach
{
    public class GraphicsCore : MonoBehaviour
    {
        public static GraphicsCore instance;

        private Resolution[] resolutions;
        public Resolution CurrentResolution { get; private set; }
        public int CurrentResolutionIndex { get; private set; }
        public TextureOption CurrentTextureQuality { get; private set; }
        public AnisotropicOption CurrentAnisotropicFiltering { get; private set; }
        private bool taaEnabled = true;
        private bool vSyncEnabled = false;

        // Window mode properties
        private List<FullScreenMode> availableWindowModes;
        public FullScreenMode CurrentWindowMode { get; private set; }
        public int CurrentWindowModeIndex { get; private set; }

        public enum TextureOption { FullRes, HalfRes, QuarterRes, EighthRes }
        public enum AnisotropicOption { Disable, Enable, ForceEnable }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeResolutions();
                InitializeWindowModes();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeResolutions()
        {
            resolutions = Screen.resolutions
                .Where(r => r.refreshRate == Screen.currentResolution.refreshRate)
                .OrderByDescending(r => r.width * r.height)
                .ToArray();

            if (resolutions.Length > 0)
            {
                CurrentResolution = resolutions[0];
                CurrentResolutionIndex = 0;
            }
            else
            {
                // If no resolutions are available, use the current screen resolution
                CurrentResolution = Screen.currentResolution;
                CurrentResolutionIndex = -1;
                Debug.LogWarning("No compatible resolutions found. Using current screen resolution.");
            }

            if (CurrentResolutionIndex == -1)
            {
                CurrentResolution = resolutions.OrderBy(r => Mathf.Abs((r.width * r.height) - (CurrentResolution.width * CurrentResolution.height))).FirstOrDefault();
                CurrentResolutionIndex = System.Array.IndexOf(resolutions, CurrentResolution);
            }

            Debug.Log($"Initialized {resolutions.Length} resolutions. Current index: {CurrentResolutionIndex}");
        }

        private void InitializeWindowModes()
        {
            availableWindowModes = new List<FullScreenMode>
            {
                FullScreenMode.ExclusiveFullScreen,
                FullScreenMode.FullScreenWindow,
                FullScreenMode.MaximizedWindow,
                FullScreenMode.Windowed
            };

            CurrentWindowMode = Screen.fullScreenMode;
            CurrentWindowModeIndex = availableWindowModes.IndexOf(CurrentWindowMode);

            if (CurrentWindowModeIndex == -1)
            {
                CurrentWindowMode = FullScreenMode.Windowed;
                CurrentWindowModeIndex = availableWindowModes.IndexOf(CurrentWindowMode);
            }

            Debug.Log($"Initialized {availableWindowModes.Count} window modes. Current index: {CurrentWindowModeIndex}");
        }

        public void SetResolution(int resolutionIndex, bool applyImmediately = true)
        {
            if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
            {
                CurrentResolutionIndex = resolutionIndex;
                CurrentResolution = resolutions[resolutionIndex];
                if (applyImmediately)
                {
                    ApplyResolution();
                }
            }
            else
            {
                Debug.LogWarning($"Invalid resolution index: {resolutionIndex}");
            }
        }

        public void SetAutoCalibrationResolution()
        {
            Resolution optimalResolution = Screen.currentResolution;
            int optimalIndex = System.Array.IndexOf(resolutions, optimalResolution);

            if (optimalIndex != -1)
            {
                CurrentResolutionIndex = optimalIndex;
                CurrentResolution = optimalResolution;
                ApplyResolution();
            }
            else
            {
                Debug.LogWarning("Couldn't find optimal resolution in available resolutions.");
            }
        }

        private void ApplyResolution()
        {
            Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, Screen.fullScreenMode);
            Debug.Log($"Applied resolution: {CurrentResolution.width}x{CurrentResolution.height} @ {CurrentResolution.refreshRate}Hz");
        }

        public Resolution[] GetResolutions()
        {
            return resolutions;
        }

        public void SetTextureQuality(TextureOption option)
        {
            CurrentTextureQuality = option;
            QualitySettings.globalTextureMipmapLimit = (int)option;
            Debug.Log($"Set texture quality to: {option}");
        }

        public void SetAnisotropicFiltering(AnisotropicOption option)
        {
            CurrentAnisotropicFiltering = option;
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering)option;
            Debug.Log($"Set anisotropic filtering to: {option}");
        }

        public void SetVSync(bool enabled)
        {
            vSyncEnabled = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            Debug.Log($"VSync set to: {enabled}");
        }

        public void SetFrameRate(int targetFrameRate)
        {
            if (targetFrameRate <= 0)
            {
                Application.targetFrameRate = -1; // Set to unlimited
                Debug.Log("Target frame rate set to unlimited");
            }
            else
            {
                Application.targetFrameRate = targetFrameRate;
                Debug.Log($"Target frame rate set to: {targetFrameRate}");
            }
        }

        public FullScreenMode[] GetWindowModes()
        {
            return availableWindowModes.ToArray();
        }

        public void SetWindowMode(int windowModeIndex)
        {
            if (windowModeIndex >= 0 && windowModeIndex < availableWindowModes.Count)
            {
                CurrentWindowModeIndex = windowModeIndex;
                CurrentWindowMode = availableWindowModes[windowModeIndex];
                ApplyWindowMode();
            }
            else
            {
                Debug.LogWarning($"Invalid window mode index: {windowModeIndex}");
            }
        }

        private void ApplyWindowMode()
        {
            Screen.fullScreenMode = CurrentWindowMode;
            Debug.Log($"Applied window mode: {CurrentWindowMode}");
        }

        public void SetWindowFullscreen()
        {
            SetWindowMode(availableWindowModes.IndexOf(FullScreenMode.FullScreenWindow));
        }

        public void SetWindowBorderless()
        {
            SetWindowMode(availableWindowModes.IndexOf(FullScreenMode.MaximizedWindow));
        }

        public void SetWindowWindowed()
        {
            SetWindowMode(availableWindowModes.IndexOf(FullScreenMode.Windowed));
        }

        public void SetFullScreenMode(FullScreenMode mode)
        {
            Screen.fullScreenMode = mode;
            Debug.Log($"Set full screen mode to: {mode}");
        }

        public void SetTAA(bool enabled)
        {
            taaEnabled = enabled;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
#if UNITY_FSR_AVAILABLE
                var fsr = mainCamera.GetComponent("TND.FSR.FSR3_URP") as MonoBehaviour;
                if (fsr != null)
                {
                    var method = fsr.GetType().GetMethod("OnSetQuality");
                    if (method != null)
                    {
                        var qualityType = fsr.GetType().Assembly.GetType("TND.FSR.FSR_Quality");
                        if (qualityType != null)
                        {
                            var taaValue = System.Enum.Parse(qualityType, enabled ? "TAA" : "None");
                            method.Invoke(fsr, new object[] { taaValue });
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("FSR3_URP component not found on main camera");
                }
#else
                Debug.LogWarning("FSR is not available in this build");
#endif
            }
            else
            {
                Debug.LogWarning("Main camera not found");
            }
            Debug.Log($"TAA set to: {enabled}");
        }

        public bool IsTAAEnabled()
        {
            return taaEnabled;
        }

        public bool IsVSyncEnabled()
        {
            return vSyncEnabled;
        }
    }
}
