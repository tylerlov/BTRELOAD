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

    private Light _mainLight;
    private Light _localLight;
    private Tween _mainLightTween;
    private Tween _localLightTween;

    private void Awake()
    {
        _localLight = GetComponent<Light>();
    }

    void Start()
    {
        GameObject mainLightObj = GameObject.FindGameObjectWithTag("Main Light");
        if (mainLightObj != null)
        {
            _mainLight = mainLightObj.GetComponent<Light>();
        }

        if (_mainLight != null)
        {
            AnimateMainLight();
        }

        if (_localLight != null)
        {
            AnimateLocalLight();
        }
    }

    void AnimateMainLight()
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

    void AnimateLocalLight()
    {
        _localLightTween = Tween
            .Delay(0.5f)
            .OnComplete(() =>
            {
                if (_localLight != null && _localLight)
                {
                    _localLightTween = Tween.Custom(
                        _localLight,
                        1f,
                        0f,
                        0.3f,
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

    void OnDestroy()
    {
        // Stop any ongoing tweens to prevent null reference errors
        if (_mainLightTween.isAlive)
            _mainLightTween.Stop();
        if (_localLightTween.isAlive)
            _localLightTween.Stop();
    }

    public void CleanupEffects()
    {
        // Stop all tweens associated with this object
        PrimeTween.Tween.StopAll(this);

        // Stop and disable all particle systems
        var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        // Disable all lights
        var lights = GetComponentsInChildren<Light>(true);
        foreach (var light in lights)
        {
            if (light != null)
            {
                light.enabled = false;
            }
        }

        // Disable all renderers
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        // Disable this component
        this.enabled = false;
    }
}
