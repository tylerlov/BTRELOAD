using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TND.FSR;

public class SteamDeckOptimizer : MonoBehaviour
{
    [SerializeField] [Range(0.5f, 1f)] private float steamDeckRenderScale = 0.8f;
    [SerializeField] private Vector2 indicatorPosition = new Vector2(10, 10);
    [SerializeField] private KeyCode toggleKey = KeyCode.F8;
    [SerializeField] private TMP_FontAsset fontAsset;

    private TextMeshProUGUI indicatorText;
    private bool optimizationsEnabled = true;
    private bool isOnSteamDeck = false;
    private FSR3_BASE fsrComponent;
    private float previousMinScale;
    private float previousMaxScale;

    private void Awake()
    {
        #if UNITY_STANDALONE_LINUX
        isOnSteamDeck = SystemInfo.graphicsDeviceName.Contains("Steam Deck");
        #endif

        Debug.Log($"[SteamDeckOptimizer] Starting up. Is Steam Deck: {isOnSteamDeck}");

        fsrComponent = FindObjectOfType<FSR3_BASE>();
        Debug.Log($"[SteamDeckOptimizer] FSR Component found: {fsrComponent != null}");
        if (fsrComponent != null)
        {
            Debug.Log($"[SteamDeckOptimizer] FSR Quality: {fsrComponent.FSRQuality}");
        }

        // Store the original dynamic resolution settings
        previousMinScale = ScalableBufferManager.widthScaleFactor;
        previousMaxScale = ScalableBufferManager.heightScaleFactor;

        // Always create the indicator in builds, but only enable optimizations on Steam Deck
        CreateIndicator();
        if (isOnSteamDeck)
        {
            ApplySteamDeckOptimizations();
        }
    }

    private void OnDestroy()
    {
        // Restore original dynamic resolution settings
        if (DynamicResolutionHandler.instance != null)
        {
            ScalableBufferManager.ResizeBuffers(previousMinScale, previousMaxScale);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleOptimizations();
        }
    }

    private void ApplySteamDeckOptimizations()
    {
        bool shouldApplyScale = fsrComponent == null || 
                              fsrComponent.FSRQuality == FSR3_Quality.UltraQuality ||
                              fsrComponent.FSRQuality == FSR3_Quality.NativeAA;

        float targetScale = optimizationsEnabled ? steamDeckRenderScale : 1f;

        Debug.Log($"[SteamDeckOptimizer] Applying optimizations: Enabled={optimizationsEnabled}, " +
                 $"Should apply scale={shouldApplyScale}, " +
                 $"Target scale={targetScale}, " +
                 $"FSR Quality={fsrComponent?.FSRQuality}");

        if (shouldApplyScale && DynamicResolutionHandler.instance != null)
        {
            ScalableBufferManager.ResizeBuffers(targetScale, targetScale);
            Debug.Log($"[SteamDeckOptimizer] Set dynamic resolution scale to: {targetScale}");
        }
        else
        {
            Debug.Log("[SteamDeckOptimizer] Skipping scale change due to FSR settings or missing DynamicResolutionHandler");
        }

        if (indicatorText != null)
        {
            indicatorText.color = optimizationsEnabled ? 
                new Color(1f, 1f, 1f, 1f) : 
                new Color(0.7f, 0.7f, 0.7f, 0.4f);
        }
    }

    private void CreateIndicator()
    {
        Debug.Log("[SteamDeckOptimizer] Starting indicator creation...");
        
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("[SteamDeckOptimizer] No canvas found, creating new one");
            GameObject canvasObj = new GameObject("SteamDeckCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balance between width and height scaling
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject indicatorObj = new GameObject("SteamDeckIndicator");
        indicatorObj.transform.SetParent(canvas.transform, false);
        
        Button button = indicatorObj.AddComponent<Button>();
        button.onClick.AddListener(ToggleOptimizations);
        
        Image buttonImage = indicatorObj.AddComponent<Image>();
        buttonImage.color = new Color(0f, 0f, 0f, 0.2f); // Slightly visible background
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(indicatorObj.transform, false);
        indicatorText = textObj.AddComponent<TextMeshProUGUI>();
        
        // More robust font asset loading
        if (fontAsset == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null && fonts.Length > 0)
            {
                fontAsset = fonts[0];
                Debug.Log($"[SteamDeckOptimizer] Found font asset: {fontAsset.name}");
            }
            else
            {
                Debug.LogError("[SteamDeckOptimizer] No font assets found in Resources!");
                return;
            }
        }
        
        indicatorText.font = fontAsset;
        indicatorText.text = $"SD ({toggleKey})";
        indicatorText.fontSize = 20; // Slightly smaller for better scaling
        indicatorText.color = new Color(1f, 1f, 1f, 1f);
        indicatorText.alignment = TextAlignmentOptions.Center;
        indicatorText.enableWordWrapping = false;
        indicatorText.raycastTarget = false;
        
        // Adjust positioning to be relative to screen size
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1, 0);
        buttonRect.pivot = new Vector2(1, 0);
        
        // Calculate position based on screen resolution
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float scaleFactor = Mathf.Min(screenWidth / 1920f, screenHeight / 1080f);
        float adjustedX = Mathf.Min(indicatorPosition.x * scaleFactor, screenWidth * 0.1f);
        float adjustedY = Mathf.Min(indicatorPosition.y * scaleFactor, screenHeight * 0.1f);
        
        buttonRect.anchoredPosition = new Vector2(-adjustedX - 40, adjustedY + 20);
        buttonRect.sizeDelta = new Vector2(80, 30);
        
        RectTransform textRect = indicatorText.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = buttonRect.sizeDelta;

        Debug.Log($"[SteamDeckOptimizer] Created UI indicator at position {buttonRect.anchoredPosition} with scale factor {scaleFactor}");
    }

    private void ToggleOptimizations()
    {
        optimizationsEnabled = !optimizationsEnabled;
        Debug.Log($"[SteamDeckOptimizer] Toggled optimizations: {optimizationsEnabled}");
        ApplySteamDeckOptimizations();
    }

    private void OnValidate()
    {
        if (fontAsset == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null && fonts.Length > 0)
            {
                fontAsset = fonts[0];
            }
        }
    }
}
