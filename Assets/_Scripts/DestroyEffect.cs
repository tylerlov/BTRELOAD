using PrimeTween;
using UnityEngine;

public class DestroyEffect : MonoBehaviour
{
    private new ParticleSystem particleSystem;

    [SerializeField]
    private Light mainLight;

    [SerializeField]
    private float startTemp = 7000f;

    [SerializeField]
    private float endTemp = 1500f;

    [SerializeField]
    private float duration = 0.3f;

    void Start()
    {
        mainLight = GameObject.FindGameObjectWithTag("Main Light").GetComponent<Light>();

        // Animate the color temperature to the target value
        Tween.Custom(
            mainLight,
            mainLight.colorTemperature,
            endTemp,
            duration,
            (light, value) => light.colorTemperature = value,
            Ease.InOutQuad
        );

        // After the first animation is complete, animate the color temperature back to the original value
        Tween.Delay(
            duration,
            () =>
                Tween.Custom(
                    mainLight,
                    endTemp,
                    startTemp,
                    duration,
                    (light, value) => light.colorTemperature = value,
                    Ease.InOutQuad
                )
        );
    }

    void Update()
    {
        if (particleSystem != null && !particleSystem.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
