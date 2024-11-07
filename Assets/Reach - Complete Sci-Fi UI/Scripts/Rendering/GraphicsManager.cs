using System.Reflection;
using UnityEngine;
using System.Linq;

namespace Michsky.UI.Reach
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class GraphicsManager : MonoBehaviour
    {
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private HorizontalSelector windowModeSelector;
        [SerializeField] private SwitchManager vSyncSwitch;
        [SerializeField] private HorizontalSelector textureQualitySelector;
        [SerializeField] private HorizontalSelector anisotropicFilteringSelector;

        private void OnEnable()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (GraphicsCore.instance == null)
            {
                Debug.LogWarning("GraphicsCore instance is not available. Ensure it's properly initialized.");
                return;
            }

            InitializeResolutionDropdown();
            InitializeWindowModeSelector();
            InitializeVSyncSwitch(); // Add this line
            InitializeTextureQualitySelector();
            InitializeAnisotropicFilteringSelector();
        }

        private void InitializeResolutionDropdown()
        {
            if (resolutionDropdown == null)
            {
                Debug.LogWarning("Resolution Dropdown is not assigned");
                return;
            }

            Resolution[] resolutions = GraphicsCore.instance.GetResolutions();
            int currentIndex = GraphicsCore.instance.CurrentResolutionIndex;

            resolutionDropdown.items.Clear();
            foreach (var resolution in resolutions)
            {
                resolutionDropdown.CreateNewItem($"{resolution.width}x{resolution.height}", false);
            }

            resolutionDropdown.Initialize();

            if (currentIndex >= 0 && currentIndex < resolutionDropdown.items.Count)
            {
                resolutionDropdown.SetDropdownIndex(currentIndex);
            }
            else
            {
                Debug.LogWarning("Invalid CurrentResolutionIndex");
            }
        }

        private void InitializeWindowModeSelector()
        {
            if (windowModeSelector == null)
            {
                Debug.LogWarning("Window Mode Selector is not assigned");
                return;
            }

            FullScreenMode[] windowModes = GraphicsCore.instance.GetWindowModes();
            int currentIndex = GraphicsCore.instance.CurrentWindowModeIndex;

            windowModeSelector.items.Clear();
            foreach (var mode in windowModes)
            {
                windowModeSelector.CreateNewItem(mode.ToString());
            }

            windowModeSelector.defaultIndex = currentIndex;
            windowModeSelector.InitializeSelector();
            windowModeSelector.onValueChanged.AddListener(SetWindowMode);
        }

        private void InitializeVSyncSwitch()
        {
            if (vSyncSwitch == null)
            {
                Debug.LogWarning("VSync Switch is not assigned");
                return;
            }

            // Set VSync to off by default and update the UI switch
            GraphicsCore.instance.SetVSync(false);
            vSyncSwitch.isOn = false;
            vSyncSwitch.onValueChanged.AddListener(SetVSync);
        }

        private void InitializeTextureQualitySelector()
        {
            if (textureQualitySelector == null)
            {
                Debug.LogWarning("Texture Quality Selector is not assigned");
                return;
            }

            textureQualitySelector.items.Clear();
            foreach (GraphicsCore.TextureOption option in System.Enum.GetValues(typeof(GraphicsCore.TextureOption)))
            {
                textureQualitySelector.CreateNewItem(option.ToString());
            }

            textureQualitySelector.defaultIndex = (int)GraphicsCore.instance.CurrentTextureQuality;
            textureQualitySelector.InitializeSelector();
            textureQualitySelector.onValueChanged.AddListener(SetTextureQuality);
        }

        private void InitializeAnisotropicFilteringSelector()
        {
            if (anisotropicFilteringSelector == null)
            {
                Debug.LogWarning("Anisotropic Filtering Selector is not assigned");
                return;
            }

            anisotropicFilteringSelector.items.Clear();
            foreach (GraphicsCore.AnisotropicOption option in System.Enum.GetValues(typeof(GraphicsCore.AnisotropicOption)))
            {
                anisotropicFilteringSelector.CreateNewItem(option.ToString());
            }

            anisotropicFilteringSelector.defaultIndex = (int)GraphicsCore.instance.CurrentAnisotropicFiltering;
            anisotropicFilteringSelector.InitializeSelector();
            anisotropicFilteringSelector.onValueChanged.AddListener(SetAnisotropicFiltering);
        }

        public void SetResolution(int resolutionIndex)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetResolution(resolutionIndex);
            }
        }

        public void SetWindowMode(int windowModeIndex)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetWindowMode(windowModeIndex);
            }
        }

        public void SetVSync(bool value)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetVSync(value);
            }
        }

        public void SetFrameRate(int value)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetFrameRate(value);
            }
        }

        public void SetWindowFullscreen()
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetWindowFullscreen();
            }
        }

        public void SetWindowBorderless()
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetWindowBorderless();
            }
        }

        public void SetWindowWindowed()
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetWindowWindowed();
            }
        }

        public void SetTextureQuality(GraphicsCore.TextureOption option)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetTextureQuality(option);
            }
        }

        public void SetTextureQuality(int index)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetTextureQuality((GraphicsCore.TextureOption)index);
            }
        }

        public void SetAnisotropicFiltering(GraphicsCore.AnisotropicOption option)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetAnisotropicFiltering(option);
            }
        }

        public void SetAnisotropicFiltering(int index)
        {
            if (GraphicsCore.instance != null)
            {
                GraphicsCore.instance.SetAnisotropicFiltering((GraphicsCore.AnisotropicOption)index);
            }
        }
    }
}