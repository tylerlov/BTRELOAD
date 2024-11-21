using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LoadingScreen : MonoBehaviour
{
    private RawImage transitionScreen;
    private float transitionTime = 0.25f;
    private bool initialized = false;
    private Canvas canvas;
    private Material transitionMaterial;
    
    private static readonly int ShaderProgressParam = Shader.PropertyToID("_T");
    private static readonly int ColorParam = Shader.PropertyToID("_Color");
    private static readonly int DistortionParam = Shader.PropertyToID("_Distortion");
    private static readonly int SpreadParam = Shader.PropertyToID("_Spread");
    private static readonly int SplitsParam = Shader.PropertyToID("_Splits");
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializeLoadingScreen();
    }

    private void InitializeLoadingScreen()
    {
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.transform.SetParent(transform);

        transitionScreen = new GameObject("TransitionScreen").AddComponent<RawImage>();
        transitionScreen.transform.SetParent(canvasObj.transform);
        
        if (TryCreateTransitionMaterial())
        {
            ConfigureTransitionScreen();
            initialized = true;
            DisableScreen();
        }
    }

    private bool TryCreateTransitionMaterial()
    {
        Shader shader = Shader.Find("TransitionsPlus/CrossWipe");
        if (shader == null) return false;
        
        transitionMaterial = new Material(shader);
        transitionMaterial.SetFloat(DistortionParam, 0.5f);
        transitionMaterial.SetFloat(SpreadParam, 32f);
        transitionMaterial.SetFloat(SplitsParam, 16f);
        transitionMaterial.SetColor(ColorParam, new Color(0.15f, 0f, 0f, 0.98f));
        
        transitionScreen.material = transitionMaterial;
        return true;
    }

    private void ConfigureTransitionScreen()
    {
        RectTransform rt = transitionScreen.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private void DisableScreen()
    {
        transitionScreen.enabled = false;
        canvas.enabled = false;
    }

    public async Task StartFadeIn()
    {
        if (!initialized) return;
        transitionScreen.enabled = true;
        canvas.enabled = true;
        await TransitionEffect(0, 1);
    }

    public async Task StartFadeOut()
    {
        if (!initialized) return;
        await TransitionEffect(1, 0);
        DisableScreen();
    }

    private async Task TransitionEffect(float startValue, float endValue)
    {
        float elapsedTime = 0;
        transitionMaterial.SetFloat(ShaderProgressParam, startValue);
        
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            transitionMaterial.SetFloat(ShaderProgressParam, Mathf.Lerp(startValue, endValue, elapsedTime / transitionTime));
            await Task.Yield();
        }
        
        transitionMaterial.SetFloat(ShaderProgressParam, endValue);
    }

    private void OnDestroy()
    {
        if (transitionMaterial != null) Destroy(transitionMaterial);
    }
}
