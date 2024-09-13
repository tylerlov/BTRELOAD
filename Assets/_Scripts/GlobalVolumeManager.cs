using JPG;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlobalVolumeManager : MonoBehaviour
{
    public static GlobalVolumeManager Instance { get; private set; }

    private Volume globalVolume;
    private JPG.Universal.JPG jpgEffect; // Updated class name

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        globalVolume = GetComponent<Volume>();
        if (globalVolume.profile.TryGet<JPG.Universal.JPG>(out var effect)) // Updated class name
        {
            jpgEffect = effect;
        }
        else
        {
            Debug.LogError("JPG effect not found in the Volume profile.");
        }
    }

    public void TransitionEffectIn(float duration)
    {
        if (jpgEffect != null)
        {
            Tween.Custom(
                0f,
                1f,
                duration,
                onValueChange: v => jpgEffect.EffectIntensity.Override(v)
            ); // Updated property name
        }
    }

    public void TransitionEffectOut(float duration)
    {
        if (jpgEffect != null)
        {
            Tween.Custom(
                1f,
                0f,
                duration,
                onValueChange: v => jpgEffect.EffectIntensity.Override(v)
            ); // Updated property name
        }
    }
}
