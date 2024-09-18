using PrimeTween;
using UnityEngine;

public class DestroyEffect : MonoBehaviour
{
    [SerializeField]
    private float startTemp = 7000f;

    [SerializeField]
    private float endTemp = 1500f;

    [SerializeField]
    private float duration = 0.3f;

    [SerializeField]
    private float localLightDelay = 0.5f;

    [SerializeField]
    private float localLightFadeDuration = 0.3f;

    private Light _mainLight;
    private Light _localLight;
    private Tween _mainLightTween;
    private Tween _localLightTween;

    private void Awake()
    {
        _localLight = GetComponent<Light>();
        _mainLight = GameObject.FindGameObjectWithTag("Main Light")?.GetComponent<Light>();
    }

    private void Start()
    {
        if (_mainLight != null) AnimateMainLight();
        if (_localLight != null) AnimateLocalLight();
    }

    private void AnimateMainLight()
    {
        _mainLightTween = Tween
            .Custom(
                _mainLight,
                _mainLight.colorTemperature,
                endTemp,
                duration,
                (light, value) =>
                {
                    if (light != null && light)
                        light.colorTemperature = value;
                    else
                        _mainLightTween.Stop();
                },
                Ease.InOutQuad
            )
            .OnComplete(() =>
            {
                if (_mainLight != null && _mainLight)
                {
                    _mainLightTween = Tween.Custom(
                        _mainLight,
                        endTemp,
                        startTemp,
                        duration,
                        (light, value) =>
                        {
                            if (light != null && light)
                                light.colorTemperature = value;
                            else
                                _mainLightTween.Stop();
                        },
                        Ease.InOutQuad
                    );
                }
            });
    }

    private void AnimateLocalLight()
    {
        _localLightTween = Tween
            .Delay(localLightDelay)
            .OnComplete(() =>
            {
                if (_localLight != null && _localLight)
                {
                    _localLightTween = Tween.Custom(
                        _localLight,
                        1f,
                        0f,
                        localLightFadeDuration,
                        (light, value) =>
                        {
                            if (light != null && light)
                                light.intensity = value;
                            else
                                _localLightTween.Stop();
                        }
                    );
                }
            });
    }

    private void OnDestroy()
    {
        StopAllTweens();
    }

    public void CleanupEffects()
    {
        StopAllTweens();
        DisableParticleSystems();
        DisableLights();
        DisableRenderers();
        this.enabled = false;
    }

    private void StopAllTweens()
    {
        PrimeTween.Tween.StopAll(this);
    }

    private void DisableParticleSystems()
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }
    }

    private void DisableLights()
    {
        foreach (var light in GetComponentsInChildren<Light>(true))
        {
            if (light != null) light.enabled = false;
        }
    }

    private void DisableRenderers()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null) renderer.enabled = false;
        }
    }
}
