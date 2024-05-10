using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using DG.Tweening;

public class DestroyEffect : MonoBehaviour
{
    private ParticleSystem particleSystem;
    [SerializeField] private Light mainLight;
    [SerializeField] private float startTemp = 7000f;
    [SerializeField] private float endTemp = 1500f;
    [SerializeField] private float duration = 0.3f;

    void Start()
    {
        mainLight = GameObject.FindGameObjectWithTag("Main Light").GetComponent<Light>();
        // Animate the color temperature to the target value
        DOTween.To(() => mainLight.colorTemperature, x => mainLight.colorTemperature = x, endTemp, duration)
            .SetEase(Ease.InOutQuad);
        // After the first animation is complete, animate the color temperature back to the original value
        DOTween.To(() => mainLight.colorTemperature, x => mainLight.colorTemperature = x, startTemp, duration)
            .SetEase(Ease.InOutQuad)
            .SetDelay(duration);
    }

    void Update()
    {
        if (particleSystem != null && !particleSystem.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}