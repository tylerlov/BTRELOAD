using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks;
using System.Linq;
using JPG.Universal;
using PrimeTween;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }
    
    private const float TRANSITION_TIME = 1.2f;
    private const float MAX_INTENSITY = 1f;
    private const float MIN_INTENSITY = 0f;
    
    private JPG.Universal.JPG jpgEffect;
    private Volume volume;
    private bool initialized;
    private float currentIntensity = MAX_INTENSITY;
    private Tween currentTween;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeEffect();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeEffect()
    {
        ConditionalDebug.Log("Initializing effect...");
        volume = GetComponentInParent<Volume>();
        
        if (volume != null && volume.profile.TryGet(out JPG.Universal.JPG effect))
        {
            jpgEffect = effect;
            initialized = true;
            ConditionalDebug.Log("Successfully initialized JPG effect.");
        }
        else
        {
            Debug.LogError("[LoadingScreen] Failed to get JPG effect from Volume profile!");
        }
    }

    public void InitializeForFreshStart()
    {
        Debug.Log("[LoadingScreen] Initializing for fresh start (max intensity)");
        SetInitialState(MAX_INTENSITY);
    }

    public void InitializeForExistingScenes()
    {
        ConditionalDebug.Log("[LoadingScreen] Initializing for existing scenes (min intensity)");
        SetInitialState(MIN_INTENSITY);
    }

    private void SetInitialState(float intensity)
    {
        if (!initialized || jpgEffect == null) return;
        
        currentIntensity = intensity;
        jpgEffect.EffectIntensity.Override(currentIntensity);
        volume.enabled = currentIntensity > MIN_INTENSITY;
        jpgEffect.active = currentIntensity > MIN_INTENSITY;
    }

    public async Task StartFadeOut()
    {
        if (!initialized || jpgEffect == null)
        {
            Debug.LogError($"[LoadingScreen] Cannot FadeOut - Initialized: {initialized}, Effect null: {jpgEffect == null}");
            return;
        }
        
        Debug.Log($"[LoadingScreen] StartFadeOut called. Current: {currentIntensity}, Target: {MIN_INTENSITY}");
        volume.enabled = true;
        jpgEffect.active = true;
        
        currentTween.Stop();
        
        var tcs = new TaskCompletionSource<bool>();
        
        currentTween = Tween.Custom(
            startValue: currentIntensity,
            endValue: MIN_INTENSITY,
            duration: TRANSITION_TIME,
            onValueChange: intensity => {
                jpgEffect.EffectIntensity.Override(intensity);
                currentIntensity = intensity;
            })
            .OnComplete(() => {
                currentIntensity = MIN_INTENSITY;
                volume.enabled = false;
                jpgEffect.active = false;
                Debug.Log($"[LoadingScreen] FadeOut complete. Volume enabled: {volume.enabled}, Intensity: {currentIntensity}");
                tcs.SetResult(true);
            });
        
        await tcs.Task;
    }

    public async Task StartFadeIn()
    {
        if (!initialized || jpgEffect == null)
        {
            Debug.LogError($"[LoadingScreen] Cannot FadeIn - Initialized: {initialized}, Effect null: {jpgEffect == null}");
            return;
        }
        
        Debug.Log($"[LoadingScreen] StartFadeIn called. Current: {currentIntensity}, Target: {MAX_INTENSITY}");
        volume.enabled = true;
        jpgEffect.active = true;
        
        currentTween.Stop();
        
        var tcs = new TaskCompletionSource<bool>();
        
        currentTween = Tween.Custom(
            startValue: currentIntensity,
            endValue: MAX_INTENSITY,
            duration: TRANSITION_TIME,
            onValueChange: intensity => {
                jpgEffect.EffectIntensity.Override(intensity);
                currentIntensity = intensity;
            })
            .OnComplete(() => {
                currentIntensity = MAX_INTENSITY;
                Debug.Log($"[LoadingScreen] FadeIn complete. Volume enabled: {volume.enabled}, Intensity: {currentIntensity}");
                tcs.SetResult(true);
            });
        
        await tcs.Task;
    }

    private void OnDestroy()
    {
        if (volume != null)
        {
            volume.enabled = false;
        }
        currentTween.Stop();
    }
}
