using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    private Image fadePanel;
    private float fadeTime = 1f;
    private bool initialized = false;

    void Awake()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("LoadingCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Ensure it's on top
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.transform.SetParent(transform);

        // Create panel
        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform);
        fadePanel = panelObj.AddComponent<Image>();
        
        // Add gradient
        Texture2D gradientTexture = CreateGradientTexture();
        Sprite gradientSprite = Sprite.Create(gradientTexture, new Rect(0, 0, gradientTexture.width, gradientTexture.height), new Vector2(0.5f, 0.5f));
        fadePanel.sprite = gradientSprite;
        fadePanel.color = Color.white; // The color will come from the gradient texture
        
        // Set panel to cover entire screen
        RectTransform rt = fadePanel.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        initialized = true;
    }

    private Texture2D CreateGradientTexture()
    {
        int width = 256;
        int height = 256;
        Texture2D texture = new Texture2D(width, height);

        // Dark red colors for gradient
        Color darkRed = new Color(0.4f, 0, 0, 1); // Darker red
        Color darkerRed = new Color(0.2f, 0, 0, 1); // Even darker red

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedY = (float)y / height;
                // Create a radial gradient effect
                float distanceFromCenter = Vector2.Distance(
                    new Vector2(x, y), 
                    new Vector2(width * 0.5f, height * 0.5f)
                ) / (width * 0.7f);
                distanceFromCenter = Mathf.Clamp01(distanceFromCenter);
                
                Color pixelColor = Color.Lerp(darkRed, darkerRed, distanceFromCenter);
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        return texture;
    }

    public void StartFadeOut()
    {
        if (!initialized) return;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0;
        Color startColor = fadePanel.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeTime;
            fadePanel.color = Color.Lerp(startColor, targetColor, normalizedTime);
            yield return null;
        }

        // Clean up after fade
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Clean up the generated texture when the component is destroyed
        if (fadePanel != null && fadePanel.sprite != null)
        {
            Destroy(fadePanel.sprite.texture);
            Destroy(fadePanel.sprite);
        }
    }
}
