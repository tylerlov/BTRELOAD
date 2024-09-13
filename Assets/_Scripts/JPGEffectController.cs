using System.Collections;
using JPG.Universal;
using UnityEngine;
using UnityEngine.Rendering;

public class JPGEffectController : MonoBehaviour
{
    public static JPGEffectController Instance { get; private set; }

    [SerializeField]
    private Volume globalVolume;
    private JPG.Universal.JPG jpgEffect;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEffect();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeEffect()
    {
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
        }

        if (globalVolume != null && globalVolume.profile.TryGet(out JPG.Universal.JPG effect))
        {
            jpgEffect = effect;
        }
        else
        {
            Debug.LogError("JPG effect not found in the global volume profile.");
        }
    }

    public void SetJPGIntensity(float targetIntensity, float duration)
    {
        if (jpgEffect != null)
        {
            StartCoroutine(LerpJPGIntensity(targetIntensity, duration));
        }
    }

    private IEnumerator LerpJPGIntensity(float targetIntensity, float duration)
    {
        float startIntensity = jpgEffect.EffectIntensity.value;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float newIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            jpgEffect.EffectIntensity.Override(newIntensity);
            yield return null;
        }

        jpgEffect.EffectIntensity.Override(targetIntensity);
    }
}
