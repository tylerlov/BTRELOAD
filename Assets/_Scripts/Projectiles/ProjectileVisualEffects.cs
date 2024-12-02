using PrimeTween;
using UnityEngine;

public class ProjectileVisualEffects
{
    private ProjectileStateBased projectile;
    private Material material;
    private Color originalColor;
    private float originalIllumination;
    private Tween colorTween;
    private Tween illuminationTween;

    public ProjectileVisualEffects(ProjectileStateBased projectile)
    {
        this.projectile = projectile;
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            material = renderer.material;
            if (material != null)
            {
                originalColor = material.color;
                originalIllumination = material.GetFloat("_SelfIllumination");
            }
        }
    }

    public void PlayDeathEffect(bool hitSomething = false)
    {
        if (material != null)
        {
            illuminationTween.Stop();
            illuminationTween = Tween.Custom(
                material.GetFloat("_SelfIllumination"),
                originalIllumination * 2f,
                0.1f,
                (value) => material.SetFloat("_SelfIllumination", value),
                Ease.OutQuad
            ).OnComplete(() =>
            {
                illuminationTween = Tween.Custom(
                    material.GetFloat("_SelfIllumination"),
                    0f,
                    0.2f,
                    (value) => material.SetFloat("_SelfIllumination", value),
                    Ease.InQuad
                );
            });
        }
    }

    public void PlayImpactEffect(Collision collision)
    {
        if (material != null)
        {
            illuminationTween.Stop();
            illuminationTween = Tween.Custom(
                material.GetFloat("_SelfIllumination"),
                originalIllumination * 1.5f,
                0.2f,
                (value) => material.SetFloat("_SelfIllumination", value),
                Ease.OutQuad
            ).OnComplete(() =>
            {
                illuminationTween = Tween.Custom(
                    material.GetFloat("_SelfIllumination"),
                    originalIllumination,
                    0.1f,
                    (value) => material.SetFloat("_SelfIllumination", value),
                    Ease.InQuad
                );
            });
        }
    }

    public void Cleanup()
    {
        if (material != null)
        {
            colorTween.Stop();
            illuminationTween.Stop();
            material.color = originalColor;
            material.SetFloat("_SelfIllumination", originalIllumination);
        }
    }

    private void OnDestroy()
    {
        colorTween.Stop();
        illuminationTween.Stop();
    }
}
